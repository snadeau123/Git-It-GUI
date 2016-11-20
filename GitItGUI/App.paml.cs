﻿using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Diagnostics;
using Avalonia.Logging.Serilog;
using Avalonia.Themes.Default;
using Avalonia.Markup.Xaml;
using Serilog;

namespace GitItGUI
{
	class App : Application
	{

		public override void Initialize()
		{
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
			
			AvaloniaXamlLoader.Load(this);
			base.Initialize();
		}

		private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			var ex = e.ExceptionObject as Exception;
			string msg = "Unknown";
			if (ex != null) msg = ex.Message;
			MessageBox.Show("Critical Error: " + msg);
		}

		static void Main(string[] args)
		{
			InitializeLogging();
			AppBuilder.Configure<App>()
				.UseWin32()
				.UseDirect2D1()
				.Start<MainWindow>();
		}

		public static void AttachDevTools(Window window)
		{
			#if DEBUG
            DevTools.Attach(window);
			#endif
		}

		private static void InitializeLogging()
		{
			#if DEBUG
            SerilogLogger.Initialize(new LoggerConfiguration()
                .MinimumLevel.Warning()
                .WriteTo.Trace(outputTemplate: "{Area}: {Message}")
                .CreateLogger());
			#endif
		}
	}
}