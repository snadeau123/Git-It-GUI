﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace GitItGUI.Core
{
	public enum MergeDiffTools
	{
		None,
		Meld,
		kDiff3,
		P4Merge,
		DiffMerge
	}

	namespace XML
	{
		public class CustomErrorCodes
		{
			[XmlElement("ErrorCode")] public List<string> errorCodes = new List<string>();
		}

		[XmlRoot("AppSettings")]
		public class AppSettings
		{
			[XmlAttribute("WinMaximized")] public bool winMaximized = false;
			[XmlAttribute("WinX")] public int winX = -1;
			[XmlAttribute("WinY")] public int winY = -1;
			[XmlAttribute("WinWidth")] public int winWidth = -1;
			[XmlAttribute("WinHeight")] public int winHeight = -1;
			[XmlElement("ChangesPanelHL")] public double changesPanelHL = -1;
			[XmlElement("ChangesPanelHR")] public double changesPanelHR = -1;
			[XmlElement("ChangesPanelStagingVU")] public double changesPanelStagingVU = -1;
			[XmlElement("ChangesPanelStagingVD")] public double changesPanelStagingVD = -1;
			[XmlElement("ChangesPanelCommitDiffVU")] public double changesPanelCommitDiffVU = -1;
			[XmlElement("ChangesPanelCommitDiffVD")] public double changesPanelCommitDiffVD = -1;
			[XmlElement("MergeDiffTool")] public MergeDiffTools mergeDiffTool = MergeDiffTools.P4Merge;
			[XmlElement("AutoRefreshChanges")] public bool autoRefreshChanges = true;
			[XmlElement("ShowLFSTag")] public bool showLFSTags = false;
			[XmlElement("SimpleMode")] public bool simpleMode = true;
			[XmlElement("CustomErrorCodes")] public CustomErrorCodes customErrorCodes = new CustomErrorCodes();
			[XmlElement("Repository")] public List<string> repositories = new List<string>();
			[XmlElement("AutoPullModules")] public bool autoPullModules = true;
		}
	}
	
	public static class Settings
	{
		public const string appSettingsFolderName = "GitItGUI";
		public const string appSettingsFilename = "GitItGUI_Settings.xml";
		public const string repoSettingsFilename = ".gititgui";
		public const string repoUserSettingsFilename = ".gititgui-user";

		public static T Load<T>(string filename) where T : new()
		{
			if (!File.Exists(filename))
			{
				var settings = new T();
				Save<T>(filename, settings);
				return settings;
			}

			try
			{
				var xml = new XmlSerializer(typeof(T));
				using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.None))
				{
					return (T)xml.Deserialize(stream);
				}
			}
			catch (Exception e)
			{
				DebugLog.LogError("Load Settings Error: " + e.Message);
				return new T();
			}
		}

		public static bool Save<T>(string filename, T settings) where T : new()
		{
			string path = Path.GetDirectoryName(filename);
			if (!Directory.Exists(path)) Directory.CreateDirectory(path);

			try
			{
				var xml = new XmlSerializer(typeof(T));
				using (var stream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None))
				{
					xml.Serialize(stream, settings);
				}
			}
			catch (Exception e)
			{
				DebugLog.LogError("Save Settings Error: " + e.Message);
				return false;
			}

			return true;
		}
	}
}
