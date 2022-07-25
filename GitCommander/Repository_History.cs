using System.Collections.Generic;

namespace GitCommander
{
	public class HistoryState
	{
		public string name {get; internal set;}
		public string url {get; internal set;}

		public HistoryState() {}
		public HistoryState(string name)
		{
			this.name = name;
		}

		public override string ToString()
		{
			return name;
		}
	}

	public partial class Repository
	{
		public bool GetRecentHistory(out string history)
		{
			lock (this)
			{
				int length = 40;
				var result = RunExe("git", string.Format("log --pretty=oneline --abbrev-commit --graph --decorate -n {0}", length));
				lastResult = result.output;
				lastError = result.errors;

				if (!string.IsNullOrEmpty(lastError) || string.IsNullOrEmpty(lastResult))
				{
					history = null;
					return false;
				}

				history = lastResult;
				return true;
			}
		}

	}
}
