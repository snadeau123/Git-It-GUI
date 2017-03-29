﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GitCommander
{
	public class BranchInfo
	{
		public string name {get; internal set;}
		public string fullname {get; internal set;}
		public RemoteState remoteState {get; internal set;}
	}

	public class BranchState
	{
		public string name {get; internal set;}
		public string fullname {get; internal set;}
		public RemoteState remoteState {get; internal set;}

		public bool isActive {get; internal set;}
		public bool isRemote {get; internal set;}
		public bool isTracking {get; internal set;}
		public bool isHead {get; internal set;}
		public bool isHeadDetached {get; internal set;}
		public BranchInfo headPtr {get; internal set;}
		public BranchInfo tracking {get; internal set;}

		public override string ToString()
		{
			return fullname;
		}
	}

	public static partial class Repository
	{
		public static bool DeleteBranch(string branch)
		{
			return SimpleGitInvoke("branch -d " + branch);
		}

		public static bool DeleteRemoteBranch(string branch, string remote)
		{
			return SimpleGitInvoke(string.Format("push {1} --delete {0}", branch, remote));
		}

		public static bool RenameActiveBranch(string newBranchName)
		{
			return SimpleGitInvoke("branch -m " + newBranchName);
		}

		public static bool RenameNonActiveBranch(string currentBranchName, string newBranchName)
		{
			return SimpleGitInvoke(string.Format("branch -m {0} {1}", currentBranchName, newBranchName));
		}

		public static bool SetActiveBranchTracking(string fullBranchName)
		{
			return SimpleGitInvoke("branch -u " + fullBranchName);
		}

		public static bool RemoveActiveBranchTracking()
		{
			return SimpleGitInvoke("branch --unset-upstream");
		}

		public static bool PruneRemoteBranches()
		{
			return SimpleGitInvoke("remote prune origin");
		}

		public static bool GetRemotePrunableBrancheNames(out string[] branchName)
		{
			var branchNameList = new List<string>();
			var stdCallback = new StdCallbackMethod(delegate(string line)
			{
				branchNameList.Add(line);
			});
			
			var result = Tools.RunExe("git", "remote prune origin --dry-run", stdCallback:stdCallback);
			lastResult = result.Item1;
			lastError = result.Item2;

			if (!string.IsNullOrEmpty(lastError))
			{
				branchName = null;
				return false;
			}
			
			branchName = branchNameList.ToArray();
			return true;
		}
		
		public static bool GetBrancheStates(out BranchState[] brancheStates)
		{
			var states = new List<BranchState>();
			var stdCallback = new StdCallbackMethod(delegate(string line)
			{
				line = line.TrimStart();

				// check if remote
				bool isActive = false;
				if (line[0] == '*')
				{
					isActive = true;
					line = line.Remove(0, 2);
				}

				// state vars
				string name, fullname, trackingBranchName = null, trackingBranchFullName = null, trackingBranchRemoteName = null;
				string remoteName = null, headPtrName = null, headPtrFullName = null, headPtrRemoteName = null;
				bool isRemote = false, isHead = false, headPtrExists = false, isHeadDetached = false, isTracking = false;

				// get name and tracking info
				var match = Regex.Match(line, @"remotes/(.*)/HEAD\s*->\s*(\S*)");
				if (match.Success)
				{
					isRemote = true;
					isHead = true;
					headPtrExists = true;
					name = "HEAD";
					fullname = "remotes/" + match.Groups[1].Value + '/' + name;
					headPtrFullName = match.Groups[2].Value;

					var values = headPtrFullName.Split('/');
					if (values.Length == 2)
					{
						headPtrRemoteName = values[0];
						headPtrName = values[1];
					}
				}
				else
				{
					match = Regex.Match(line, @"\(HEAD detached at (.*)/(.*)\)");
					if (match.Success)
					{
						isHead = true;
						isHeadDetached = true;
						isRemote = true;
						name = "HEAD";
						remoteName = match.Groups[1].Value;
						fullname = remoteName + '/' + name;

						headPtrRemoteName = remoteName;
						headPtrName = match.Groups[2].Value;
					}
					else
					{
						match = Regex.Match(line, @"(\S*).*\[(.*)\]");
						if (match.Success)
						{
							isTracking = true;
							fullname = match.Groups[1].Value;
							name = fullname;
						
							string trackedBranch = match.Groups[2].Value;
							trackingBranchFullName = trackedBranch.Contains(":") ? trackedBranch.Split(':')[0] : trackedBranch;
							var values = trackingBranchFullName.Split('/');
							if (values.Length == 2)
							{
								trackingBranchRemoteName = values[0];
								trackingBranchName = values[1];
							}
						}
						else
						{
							match = Regex.Match(line, @"(\S*)");
							if (match.Success)
							{
								fullname = match.Groups[1].Value;
								name = fullname;
							}
							else
							{
								return;
							}
						}
					}
				}

				// check if branch is remote
				match = Regex.Match(fullname, @"remotes/(.*)/(.*)");
				if (match.Success)
				{
					isRemote = true;
					remoteName = match.Groups[1].Value;
					name = match.Groups[2].Value;
					fullname = remoteName + '/' + name;
					if (name == "HEAD") isHead = true;
				}

				// create branch object
				var branch = new BranchState()
				{
					name = name,
					fullname = fullname,
					isActive = isActive,
					isRemote = isRemote,
					isHead = isHead,
					isHeadDetached = isHeadDetached,
					isTracking = isTracking,
				};

				if (!string.IsNullOrEmpty(remoteName))
				{
					branch.remoteState = new RemoteState() {name = remoteName};
				}

				// fill head info
				if (headPtrExists)
				{
					branch.headPtr = new BranchInfo()
					{
						name = headPtrName,
						fullname = headPtrFullName
					};

					if (!string.IsNullOrEmpty(headPtrRemoteName))
					{
						branch.headPtr.remoteState = new RemoteState() {name = headPtrRemoteName};
					}
				}

				// fill tracking info
				if (isTracking)
				{
					branch.tracking = new BranchInfo()
					{
						name = trackingBranchName,
						fullname = trackingBranchFullName
					};

					if (!string.IsNullOrEmpty(trackingBranchRemoteName))
					{
						branch.tracking.remoteState = new RemoteState() {name = trackingBranchRemoteName};
					}
				}

				states.Add(branch);
			});
			
			var result = Tools.RunExe("git", "branch -a -vv", stdCallback:stdCallback);
			lastResult = result.Item1;
			lastError = result.Item2;

			if (!string.IsNullOrEmpty(lastError))
			{
				brancheStates = null;
				return false;
			}

			// get remote urls
			foreach (var state in states)
			{
				string url;
				if (state.remoteState != null && GetRemoteURL(state.remoteState.name, out url)) state.remoteState.url = url;
				if (state.headPtr != null && state.headPtr.remoteState != null && GetRemoteURL(state.headPtr.remoteState.name, out url)) state.headPtr.remoteState.url = url;
				if (state.tracking != null && state.tracking.remoteState != null && GetRemoteURL(state.tracking.remoteState.name, out url)) state.tracking.remoteState.url = url;
			}

			brancheStates = states.ToArray();
			return true;
		}

		public static bool CheckoutExistingBranch(string branch)
		{
			var result = Tools.RunExe("git", "checkout " + branch);
			lastResult = result.Item1;
			lastError = result.Item2;
			
			return string.IsNullOrEmpty(lastError);
		}

		public static bool CheckoutNewBranch(string branch)
		{
			return SimpleGitInvoke("checkout -b " + branch);
		}

		public static bool PushLocalBranchToRemote(string branch, string remote)
		{
			return SimpleGitInvoke(string.Format("push -u {1} {0}", branch, remote));
		}

		public static bool MergeBranchIntoActive(string branch)
		{
			return SimpleGitInvoke("merge " + branch);
		}

		public static bool IsUpToDateWithRemote(string remote, string branch, out bool yes)
		{
			// check for changes not in sync
			yes = true;
			bool isUpToDate = true;
			var stdCallback_log = new StdCallbackMethod(delegate(string line)
			{
				var match = Regex.Match(line, @"commit (.*)");
				if (match.Success) isUpToDate = false;
			});
			
			var result = Tools.RunExe("git", string.Format("log {0}/{1}..{1}", remote, branch), stdCallback:stdCallback_log);
			lastResult = result.Item1;
			lastError = result.Item2;
			yes = isUpToDate;
			//if (!string.IsNullOrEmpty(lastError)) return false;
			//else if (!isUpToDate) return true;
			return string.IsNullOrEmpty(lastError);

			// if git log doesn't pick up anything try status
			/*var stdCallback_status = new StdCallbackMethod(delegate(string line)
			{
				var match = Regex.Match(line, @"Your branch is ahead of '(.*)' by (\d*) commit.");
				if (match.Success) isUpToDate = false;

				match = Regex.Match(line, @"Your branch is behind '(.*)' by (\d*) commits");
				if (match.Success) isUpToDate = false;
			});

			result = Tools.RunExe("git", "status", stdCallback:stdCallback_status);
			lastResult = result.Item1;
			lastError = result.Item2;
			yes = isUpToDate;
			return string.IsNullOrEmpty(lastError);*/
		}
	}
}
