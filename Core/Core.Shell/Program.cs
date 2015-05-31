﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Core.Common;
using Core.IO;
using Core.IO.Streams;
using Core.IO.Terminal;
using Core.Platform;
using Core.Portable;
using Core.Shell.Common;
using Core.Shell.Platform.Commands;
using Core.Shell.Platform.FileSystems;
using Mono.Options;

namespace Core.Shell
{
	public class MainClass
	{
		public static void Main (string[] args)
		{
			DesktopPlatform.Start ();

			Task.Run (async () => await new MainClass ().Run (args)).Wait ();

			DesktopPlatform.Finish ();
		}

		public async Task Run (string[] args)
		{
			fixFileAssociations ();
			
			DesktopPlatform.LogTargets.StandardOutput = false;

			// option values
			bool help = false;
			Mode mode = Mode.Interactive;
			string commandString = null;
			string scriptFile = null;
			List<string> parameters = new List<string> ();

			// option parser
			OptionSet optionSet = new OptionSet ();
			optionSet.Add ("h|help|?", "Prints out the options.", option => help = (option != null));
			optionSet.Add ("log|debug", "Show log messages.", option => DesktopPlatform.LogTargets.StandardOutput = option != null);
			optionSet.Add ("c=", "Execute a command string.", option => {
				mode = Mode.CommandString;
				commandString = option;
			});
			optionSet.Add ("<>", option => {
				if (mode != Mode.CommandString && scriptFile == null) {
					mode = Mode.ScriptFile;
					scriptFile = option;
					Log.Warning ("Script file: ", option);
				} else {
					parameters.Add (option);
					Log.Warning ("Parameter: ", option);
				}
			});
			try {
				optionSet.Parse (args);
			} catch (OptionException) {
				help = true;
			}

			// need help ?
			if (help) {
				printOptions (optionSet);
				return;
			}

			RegularFileSystems.Register ();
			RegularExecutables.Register ();
			UnixShell shell = new UnixShell ();
			shell.Environment.Output.PipeTo (NonBlockingConsole.Instance);
			shell.Environment.Error.PipeTo (NonBlockingConsole.Instance);

			// run code line
			if (mode == Mode.CommandString) {
				try {
					await shell.RunScriptAsync (code: commandString);
				} catch (Exception ex) {
					Log.Error (ex);
				}
			}
			// run script
			if (mode == Mode.ScriptFile) {
				try {
					await shell.RunScriptAsync (code: File.ReadAllText (scriptFile));
				} catch (Exception ex) {
					Log.Error (ex);
				}
			}
			// run interactively
			if (mode == Mode.Interactive) {
				
				// run test code if there is no interactive console
				if (!PlatformInfo.System.IsInteractive) {
					DesktopPlatform.LogTargets.StandardOutput = true;
					test (shell);
				}

				// run interactively
				else {
					await shell.PrintWelcomeAsync ();

					await Interactive (shell: shell, console: NonBlockingConsole.Instance);
				}
			}
		}

		async Task Interactive (UnixShell shell, ITerminalStream console)
		{
			using (ResponsiveReadLine readLine = new ResponsiveReadLine (console: console)) {
				using (readLine.UseHistory (shell.History)) {
					
					bool done = false;

					shell.Environment.Input.PipeToLimbo ();

					await console.WriteAsync (shell.Prompt ());

					PortableBlockingQueue<ILine> queue = new PortableBlockingQueue<ILine> ();

					readLine.Callback = async line => await queue.EnqueueAsync (item: line);

					while (!done && console.IsOpen) {
						while (queue.Count > 0) {
							ILine line = await queue.DequeueAsync ();

							// handle a special command?
							if (line.SpecialCommand != SpecialCommands.None) {
								// do something
								done |= line.SpecialCommand == SpecialCommands.CloseStream;
							}
							// normal string input
							else {
								// filter empty lines
								if (!string.IsNullOrWhiteSpace (line.Line)) {

									await InteractiveLine (line: line.Line, shell: shell, console: console);
								}
							}
							await console.WriteAsync (shell.Prompt ());
						}
						await Task.Delay (50);
					}
				}
			}
			await console.WriteLineAsync ();
		}

		async Task InteractiveLine (string line, UnixShell shell, ITerminalStream console)
		{
			using (ResponsiveReadLine readLine = new ResponsiveReadLine (console: console)) {
				// use a separate history!
				using (readLine.UseHistory (new InputHistory ())) {

					// cache input
					//shell.Environment.Input.PipeToCache ();
					//var cancelToken = new CancellationTokenSource ();

					// task for redirecting input to command!
					//Task inputCapturing = Task.Run (async () => {
					readLine.Callback = shell.Environment.Input.ToReadLineCallback ();
					//cancelToken.Token.ThrowIfCancellationRequested ();
					//await shell.Environment.Input.Eat (readLine: readLine, cancelToken: cancelToken.Token).ConfigureAwait (false);
					//});

					// task for running the command
					Task commandRunning = Task.Run (async () => {
						await shell.InteractiveAsync (line: line);
						//cancelToken.Cancel ();
						await shell.Environment.Input.TryClose ();
					});

					// wait for both
					await Task.WhenAll (new []{ commandRunning });

					// throw input away, if there was any
					//shell.Environment.Input.PipeToLimbo ();
					//readLine.CancelToken = CancellationToken.None;
				}
			}
		}

		private void printOptions (OptionSet optionSet)
		{
			optionSet.WriteOptionDescriptions (Console.Out);
		}

		public enum Mode
		{
			Interactive,
			CommandString,
			ScriptFile,
		}

		void test (UnixShell shell)
		{
			Task.Run (async () => await shell.InteractiveAsync (@"
				echo test;echo test2
				echo test7 a  ""b c d"" """" ''      'c' abc;
				if true;
				then
					echo test4;
				elif false ; then
					echo test5
				else ;;;
					echo test6; echo test7
				fi
			")).Wait ();
		}

		void fixFileAssociations ()
		{
			FileAssociation.SetAssociation (
				extensions: new[]{ ".sh", ".coresh", ".bash" },
				id: "ShellScript",
				description: "Shell Script",
				exePath: PlatformInfo.System.ApplicationPath,
				iconPath: Path.Combine (Path.GetDirectoryName (PlatformInfo.System.ApplicationPath), "icon.ico")
			);
		}

		/*Task output (string text)
		{
			//Log.Debug ("output: ", text);
			NonBlockingConsole.Write (text);
			return TaskHelper.Completed;
		}*/
	}
}
