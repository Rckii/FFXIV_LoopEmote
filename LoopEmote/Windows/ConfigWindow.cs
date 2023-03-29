using Dalamud.Interface.Windowing;
using ImGuiNET;
using System;
using System.Numerics;

namespace LoopEmote.Windows;

public class ConfigWindow : Window, IDisposable
{
	private Configuration Configuration;
	private Plugin Plugin;

	public ConfigWindow(Plugin plugin) : base(
	    "Loop Emote Configuration",
	    ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.AlwaysAutoResize)
	{
		this.Size = new Vector2(150, 75);
		this.SizeCondition = ImGuiCond.FirstUseEver;

		this.Configuration = plugin.Configuration;
		Plugin = plugin;
	}

	public void Dispose() { }

	public override void Draw()
	{
		ImGui.SetNextItemWidth(110.0f);

		var interval = Configuration.LoopIntervalInMS;

		if (ImGui.InputInt("Default Loop Interval In MS", ref interval, 500, 1000))
		{
			Configuration.LoopIntervalInMS = Math.Max(interval, Plugin.MIN_INTERVAL);
			Configuration.Save();
		}

		var doPrint = Configuration.PrintInChat;

		if(ImGui.Checkbox("Print In Chat", ref doPrint))
		{
			Configuration.PrintInChat = doPrint;
			Configuration.Save();

			Plugin.PrintInChat((doPrint ? "Enabled chat printing like this" : "Disabled chat printing like this"), true);
		}
	}
}
