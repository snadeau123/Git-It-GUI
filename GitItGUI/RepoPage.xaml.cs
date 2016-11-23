﻿using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace GitItGUI
{
	public class RepoPage : UserControl
	{
		public static RepoPage singleton;

		// UI objects
		ListBox remotesListView;
		Button addRemoteButton, removeRemoteButton;
		TextBox sigNameTextBox, sigEmailTextBox, usernameTextBox, passwordTextBox;
		CheckBox gitlfsSupportCheckBox, validateGitignoreCheckbox;

		public RepoPage()
		{
			singleton = this;
			AvaloniaXamlLoader.Load(this);

			// remotes
			remotesListView = this.Find<ListBox>("remotesListView");
			addRemoteButton = this.Find<Button>("addRemoteButton");
			removeRemoteButton = this.Find<Button>("removeRemoteButton");

			// user info
			sigNameTextBox = this.Find<TextBox>("sigNameTextBox");
			sigEmailTextBox = this.Find<TextBox>("sigEmailTextBox");
			usernameTextBox = this.Find<TextBox>("usernameTextBox");
			passwordTextBox = this.Find<TextBox>("passwordTextBox");
			//sigNameTextBox.TextInput += sigNameTextBox_TextInput;
			//sigEmailTextBox.TextInput += sigEmailTextBox_TextInput;
			//usernameTextBox.TextInput += usernameTextBox_TextInput;
			//passwordTextBox.TextInput += passwordTextBox_TextInput;

			// lfs / validations
			gitlfsSupportCheckBox = this.Find<CheckBox>("gitlfsSupportCheckBox");
			validateGitignoreCheckbox = this.Find<CheckBox>("validateGitignoreCheckbox");
			//gitlfsSupportCheckBox.Click += gitlfsSupportCheckBox_Click;
			//validateGitignoreCheckbox.Click += validateGitignoreCheckbox_Click;
		}
	}
}