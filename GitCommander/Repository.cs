﻿using System.IO;
using System.Text.RegularExpressions;

namespace GitCommander
{
	public enum SignatureLocations
	{
		Any,
		Local,
		Global
	}

    public partial class Repository
    {
		public bool isOpen {get; private set;}
		public bool hasSubmodules {get; private set;}
		public bool areSubmodulesInit {get; private set;}
		public string lastResult {get; private set;}
		public string lastError {get; private set;}

		public string repoURL {get; private set;}
		public string repoPath {get; private set;}
		public LFS lfs {get; private set;}

		public Repository()
		{
			lfs = new LFS(this);
			InitTools();
		}

		public void Close()
		{
			lock (this)
			{
				isOpen = false;
				hasSubmodules = false;
				areSubmodulesInit = false;
				lastResult = null;
				lastError = null;
				repoURL = null;
				repoPath = null;

				lfs.Close();
			}
		}

		public (string output, string errors) GitInvoke(string args, string workingDirectory = null,
			StdInputStreamCallbackMethod stdInputStreamCallback = null, GetStdInputStreamCallbackMethod getStdInputStreamCallback = null,
			GetStdOutputStreamCallbackMethod getStdOutputStreamCallback = null,
			StdCallbackMethod stdCallback = null, StdCallbackMethod stdErrorCallback = null,
			bool stdResultOn = true, bool stdErrorResultOn = true,
			string stdOutToFilePath = null, Stream stdOutToStream = null
		)
		{
			lock (this)
			{
				var result = RunExe("git", args, workingDirectory: workingDirectory, stdInputStreamCallback: stdInputStreamCallback,
					getStdInputStreamCallback: getStdInputStreamCallback, getStdOutputStreamCallback: getStdOutputStreamCallback,
					stdCallback: stdCallback, stdErrorCallback: stdErrorCallback, stdResultOn: stdResultOn, stdErrorResultOn: stdErrorResultOn,
					stdOutToFilePath: stdOutToFilePath, stdOutToStream: stdOutToStream);
				lastResult = result.output;
				lastError = result.errors;
				return result;
			}
		}

		private bool SimpleGitInvoke(string args, StdCallbackMethod stdCallback = null, StdCallbackMethod stdErrorCallback = null)
		{
			lock (this)
			{
				var result = RunExe("git", args, stdCallback:stdCallback, stdErrorCallback:stdErrorCallback);
				lastResult = result.output;
				lastError = result.errors;

				return string.IsNullOrEmpty(lastError);
			}
		}
		
		public bool Clone(string url, string path, out string repoClonedPath, StdInputStreamCallbackMethod writeUsernameCallback, StdInputStreamCallbackMethod writePasswordCallback)
		{
			lock (this)
			{
				StreamWriter stdInWriter = null;
				void getStdInputStreamCallback(StreamWriter writer)
				{
					stdInWriter = writer;
				}
			
				string repoClonedPathTemp = null;
				void stdCallback(string line)
				{
					if (line.StartsWith("Cloning into"))
					{
						var match = Regex.Match(line, @"Cloning into '(.*)'\.\.\.");
						if (match.Success) repoClonedPathTemp = match.Groups[1].Value;
					}
				}

				void stdErrorCallback(string line)
				{
					if (line.StartsWith("Username for"))
					{
						if (writeUsernameCallback == null || !writeUsernameCallback(stdInWriter)) stdInWriter.WriteLine("");
					}
					else if (line.StartsWith("Password for"))
					{
						if (writePasswordCallback == null || !writePasswordCallback(stdInWriter)) stdInWriter.WriteLine("");
					}
				}
			
				var result = RunExe("git", string.Format("clone \"{0}\"", url), workingDirectory:path, getStdInputStreamCallback:getStdInputStreamCallback, stdCallback:stdCallback, stdErrorCallback:stdErrorCallback);
				lastResult = result.output;
				lastError = result.errors;
			
				repoClonedPath = repoClonedPathTemp;
				return string.IsNullOrEmpty(lastError);
			}
		}

		public bool Open(string path)
		{
			lock (this)
			{
				Close();

				void stdCallback(string line)
				{
					repoURL = line;
				}
				
				hasSubmodules = false;
				areSubmodulesInit = false;
				void stdCallback_Submodules(string line)
				{
					if (hasSubmodules) return;

					var match = Regex.Match(line, @"(\s|\-)(\w*).*");
					if (match.Success)
					{
						hasSubmodules = true;
						areSubmodulesInit = match.Groups[1].Value == " ";
					}
				}
			
				var result = RunExe("git", "rev-parse --git-dir", workingDirectory:path);
				lastResult = result.output;
				lastError = result.errors;
				if (!string.IsNullOrEmpty(lastError)) return false;

				// check for submodules
				result = RunExe("git", "submodule status", stdCallback:stdCallback_Submodules, workingDirectory:path);
				lastResult = result.output;
				lastError = result.errors;
			
				// get repo url
				repoURL = "";
				result = RunExe("git", "ls-remote --get-url", stdCallback:stdCallback, workingDirectory:path);
				lastResult = result.output;
				lastError = result.errors;
			
				repoPath = path;
				lfs.Open();
				return isOpen = true;
			}
		}

		public bool Init(string path)
		{
			lock (this)
			{
				var result = RunExe("git", "init", workingDirectory:path);
				lastResult = result.output;
				lastError = result.errors;
				if (!string.IsNullOrEmpty(lastError)) return false;
			
				return true;
			}
		}

		public bool GetSignature(SignatureLocations location, out string name, out string email)
		{
			lock (this)
			{
				name = null;
				email = null;
				string globalValue;
				if (location == SignatureLocations.Global) globalValue = " --global";
				else if (location == SignatureLocations.Local) globalValue = " --local";
				else globalValue = string.Empty;

				bool result = SimpleGitInvoke(string.Format("config{0} user.name", globalValue));
				name = lastResult;
				if (!result) return false;

				result = SimpleGitInvoke(string.Format("config{0} user.email", globalValue));
				email = lastResult;
				return result;
			}
		}

		public bool SetSignature(SignatureLocations location, string name, string email)
		{
			lock (this)
			{
				string globalValue;
				if (location == SignatureLocations.Global) globalValue = " --global";
				else if (location == SignatureLocations.Local) globalValue = " --local";
				else globalValue = string.Empty;

				bool result = SimpleGitInvoke(string.Format("config{1} user.name \"{0}\"", name, globalValue));
				name = lastResult;
				if (!result) return false;

				result = SimpleGitInvoke(string.Format("config{1} user.email \"{0}\"", email, globalValue));
				email = lastResult;
				return result;
			}
		}

		public bool RemoveSettings(SignatureLocations location, string section)
		{
			lock (this)
			{
				string globalValue;
				if (location == SignatureLocations.Global) globalValue = " --global";
				else if (location == SignatureLocations.Local) globalValue = " --local";
				else globalValue = string.Empty;

				return SimpleGitInvoke(string.Format("config{1} --remove-section {0}", section, globalValue));
			}
		}

		public bool UnpackedObjectCount(out int count, out string size)
		{
			lock (this)
			{
				bool result = SimpleGitInvoke("count-objects");
				if (!string.IsNullOrEmpty(lastError) || string.IsNullOrEmpty(lastResult))
				{
					count = -1;
					size = null;
					return false;
				}

				var match = Regex.Match(lastResult, @"(\d*) objects, (\d* kilobytes)");
				if (match.Groups.Count != 3)
				{
					count = -1;
					size = null;
					return false;
				}
			
				count = int.Parse(match.Groups[1].Value);
				size = match.Groups[2].Value;
				return true;
			}
		}

		public bool GarbageCollect()
		{
			StreamWriter stdInWriter = null;
			void getStdInputStreamCallback(StreamWriter writer)
			{
				stdInWriter = writer;
			}

			bool failed = false;
			void stdCallback(string line)
			{
				if (line.EndsWith("Should I try again? (y/n)"))
				{
					failed = true;
					stdInWriter.WriteLine("n");
				}
			}

			lock (this)
			{
				var result = RunExe("git", "gc", stdCallback:stdCallback, getStdInputStreamCallback:getStdInputStreamCallback);
				lastResult = result.output;
				lastError = result.errors;

				return !failed && string.IsNullOrEmpty(lastError);
			}
		}

		public bool GetVersion(out string version)
		{
			lock (this)
			{
				bool result = SimpleGitInvoke("version");
				version = lastResult;
				return result;
			}
		}

		public bool EnsureUnicodeDisabledLocally()
		{
			bool unicodeEntryExists = false;
			void stdCallback(string line)
			{
				if (line != null) unicodeEntryExists = true;
			}

			lock (this)
			{
				if (!SimpleGitInvoke("config --local core.quotepath", stdCallback:stdCallback)) return false;
				if (unicodeEntryExists)
				{
					if (!SimpleGitInvoke("config --local --unset core.quotepath")) return false;
				}

				return true;
			}
		}

		public bool EnsureUnicodeEnabledGlobally()
		{
			bool unicodeEnabled = false;;
			void stdCallback(string line)
			{
				if (line == "off") unicodeEnabled = true;
			}

			lock (this)
			{
				if (!SimpleGitInvoke("config --global core.quotepath", stdCallback:stdCallback)) return false;
				if (!unicodeEnabled)
				{
					return SimpleGitInvoke("config --global core.quotepath off");
				}

				return true;
			}
		}
    }
}
