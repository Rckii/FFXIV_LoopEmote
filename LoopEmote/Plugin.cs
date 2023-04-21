using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Common.Math;
using LoopEmote.Windows;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace LoopEmote
{
	public sealed class Plugin : IDalamudPlugin
	{
		public const int MIN_INTERVAL = 1000;
		public const int MOVEMENT_CHECK_INTERVAL = 250;

		[PluginService] public static Framework Framework { get; private set; } = null!;

		public string Name => "Loop Emote Plugin";
		private const string CommandName = "/lemote";

		private DalamudPluginInterface PluginInterface { get; init; }
		public static CommandManager CommandManager { get; private set; } = null!;
		public Configuration Configuration { get; init; }
		public WindowSystem WindowSystem = new("LoopEmote");
		public static GameGui GameGui { get; private set; } = null!;
		private readonly ClientState m_clientState;
		public static ChatGui Chat { get; private set; } = null!;

		private readonly Stopwatch loopStopwatch = new();
		private readonly Stopwatch movementCheckStopwatch = new();

		public static SigScanner SigScanner { get; private set; } = null!;

		private ConfigWindow ConfigWindow { get; init; }

		int m_intervalInMS = 5000;

		Vector3? m_pos = null;

		string m_command = "/read";

		bool m_allowMovement = false;

		public Plugin(
		    [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
		    [RequiredVersion("1.0")] CommandManager commandManager,
		    ClientState cState,
		    ChatGui chat,
		    SigScanner sigScanner,
		    GameGui gameGui)
		{
			m_clientState = cState;
			Chat = chat;
			SigScanner = sigScanner;
			GameGui = gameGui;
			this.PluginInterface = pluginInterface;
			CommandManager = commandManager;
			this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
			this.Configuration.Initialize(this.PluginInterface);
			ChatHelper.Initialize(); //ye

			ConfigWindow = new ConfigWindow(this);
			WindowSystem.AddWindow(ConfigWindow);

			CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
			{
				ShowInHelp = false,
			});

			this.PluginInterface.UiBuilder.Draw += DrawUI;
			this.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

			Framework.Update += Framework_Update;
		}

		private void OnPlayerMoved(object? sender, EventArgs e)
		{
			TryStopLoop();
		}

		private void Framework_Update(Framework framework)
		{
			if (!loopStopwatch.IsRunning)
				return;

			// Just a quick and hacky movement check
			if (movementCheckStopwatch.ElapsedMilliseconds > MOVEMENT_CHECK_INTERVAL)
			{
				if (!m_allowMovement)
				{
					Vector3? newPos = m_clientState.LocalPlayer?.Position;

					if (m_pos != null && newPos != null && m_pos != newPos)
						TryStopLoop("movement");

					m_pos = newPos;
				}

				movementCheckStopwatch.Restart();
			}

			if (loopStopwatch.ElapsedMilliseconds > m_intervalInMS)
			{
				ChatHelper.SendChatMessage(m_command);
				loopStopwatch.Restart();
			}
		}

		public void Dispose()
		{
			this.WindowSystem.RemoveAllWindows();

			ConfigWindow.Dispose();
			CommandManager.RemoveHandler(CommandName);
			ChatHelper.Instance?.Dispose();
			Framework.Update -= Framework_Update;
		}

		private void OnCommand(string command, string args)
		{
			// if no args were specified, let's print the help text.s
			if (args.IsNullOrEmpty() || args == "help")
			{
				PrintInChat("Usage: /lemote <emote command> [loop interval in ms]", true);
				return;
			}

			if (args == "config")
			{
				DrawConfigUI();
				return;
			}

			if (args == "stop")
			{
				if (loopStopwatch.IsRunning)
				{
					TryStopLoop();
				}

				return;
			}

			m_pos = null;
			loopStopwatch.Reset();
			loopStopwatch.Start();
			movementCheckStopwatch.Reset();
			movementCheckStopwatch.Start();

			m_allowMovement = false;

			string[] aargs = args.Split(' ');

			args = !args.IsNullOrEmpty() ? args : "/read";

			string text = args;

			m_intervalInMS = Math.Max(Configuration.LoopIntervalInMS, Plugin.MIN_INTERVAL);

			if (aargs.Length > 1)
			{
				text = aargs[0];

				if (!text.Contains("motion"))
					text += " motion";

				for (int i = 1; i < aargs.Length; i++)
				{
					if (int.TryParse(aargs[i], out int newValue))
					{
						m_intervalInMS = Math.Max(newValue, Plugin.MIN_INTERVAL);
					}
					else if (aargs[i].Contains("move"))
					{
						m_allowMovement = true;
						text += " movement";
					}
				}
			}

			if (!text.StartsWith('/'))
				text = "/" + text;

			if (!text.Contains("motion"))
				text += " motion";

			PrintInChat("Started looping emote '" + text + "' with interval of '" + m_intervalInMS + " ms'");

			m_command = text;

			ChatHelper.SendChatMessage(m_command);
		}

		void TryStopLoop(string reason = "")
		{
			if (!loopStopwatch.IsRunning)
				return;

			PrintInChat("Stopped looping" + (reason != "" ? (" due to " + reason) : ""));

			loopStopwatch.Stop();
			movementCheckStopwatch.Stop();
		}

		private void DrawUI()
		{
			this.WindowSystem.Draw();
		}

		public void DrawConfigUI()
		{
			ConfigWindow.IsOpen = true;
		}

		public void PrintInChat(string message, bool force = false)
		{
			if (!Configuration.PrintInChat && !force)
				return;

			Chat.Print("[Loop Emote] " + message);
		}
	}
}
