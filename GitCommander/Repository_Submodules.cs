using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace GitCommander
{
	public class SubmoduleState
	{
		public string name {get; internal set;}
		public bool initialized {get; internal set;}
		public bool synced {get; internal set;}

		public SubmoduleState() {}
		public SubmoduleState(string name)
		{
			this.name = name;
			this.initialized = false;
			this.synced = false;
		}

		public override string ToString()
		{
			return name;
		}
	}

	public partial class Repository
	{
		public void GetSubmoduleList(out SubmoduleState[] submodules)
		{
			var states = new List<SubmoduleState>();

			void stdCallback_Submodules(string line)
			{
				var match = Regex.Match(line, @"(\s|\-|\+)\w*\s(.*)\s\(.*\)");
				if (match.Success)
				{
					SubmoduleState submodule = new SubmoduleState(match.Groups[2].Value);
					switch (match.Groups[1].Value)
					{
						case " ":
							submodule.synced = true;
							submodule.initialized = true;
							break;

						case "-":
							break;

						case "+":
							submodule.synced = false;
							submodule.initialized = true;
							break;

						default:
							break;
					}
					states.Add(submodule);
				}
			}

			lock (this)
			{
				// check for submodules
				var result = RunExe("git", "submodule status", stdCallback:stdCallback_Submodules);
				lastResult = result.output;
				lastError = result.errors;
				
				submodules = states.ToArray();
			}
		}
	}
}
