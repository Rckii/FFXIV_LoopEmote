using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace LoopEmote
{
	[Serializable]
	public class Configuration : IPluginConfiguration
	{
		public int Version { get; set; } = 0;

		public int LoopIntervalInMS { get; set; } = 5000;

		public bool PrintInChat { get; set; } = true;

		[NonSerialized]
		private DalamudPluginInterface? PluginInterface;

		public void Initialize(DalamudPluginInterface pluginInterface)
		{
			this.PluginInterface = pluginInterface;
		}

		public void Save()
		{
			this.PluginInterface!.SavePluginConfig(this);
		}
	}
}
