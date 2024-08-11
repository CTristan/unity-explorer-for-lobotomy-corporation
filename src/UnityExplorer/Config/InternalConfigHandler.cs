using System;
using System.IO;
using Tomlet;
using Tomlet.Models;
using UnityExplorerForLobotomyCorporation.UnityExplorer.UI;

namespace UnityExplorerForLobotomyCorporation.UnityExplorer.Config
{
    public class InternalConfigHandler : ConfigHandler
    {
        internal static string CONFIG_PATH;

        public override void Init()
        {
            CONFIG_PATH = Path.Combine(ExplorerCore.ExplorerFolder, "data.cfg");
        }

        public override void LoadConfig()
        {
            if (!TryLoadConfig())
            {
                SaveConfig();
            }
        }

        public override void RegisterConfigElement<T>(ConfigElement<T> element)
        {
            // Not necessary
        }

        public override void SetConfigValue<T>(ConfigElement<T> element,
            T value)
        {
            // Not necessary
        }

        // Not necessary, just return the value.
        public override T GetConfigValue<T>(ConfigElement<T> element)
        {
            return element.Value;
        }

        // Always just auto-save.
        public override void OnAnyConfigChanged()
        {
            SaveConfig();
        }

        public bool TryLoadConfig()
        {
            try
            {
                if (!File.Exists(CONFIG_PATH))
                {
                    return false;
                }

                var document = TomlParser.ParseFile(CONFIG_PATH);
                foreach (var key in document.Keys)
                {
                    if (!Enum.IsDefined(typeof(UIManager.Panels), key))
                    {
                        continue;
                    }

                    var panelKey = (UIManager.Panels)Enum.Parse(typeof(UIManager.Panels), key);
                    ConfigManager.GetPanelSaveData(panelKey).Value = document.GetString(key);
                }

                return true;
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning("Error loading internal data: " + ex);

                return false;
            }
        }

        public override void SaveConfig()
        {
            if (UIManager.Initializing)
            {
                return;
            }

            var tomlDocument = TomlDocument.CreateEmpty();
            foreach (var entry in ConfigManager.InternalConfigs)
            {
                tomlDocument.Put(entry.Key, entry.Value.BoxedValue as string);
            }

            File.WriteAllText(CONFIG_PATH, tomlDocument.SerializedValue);
        }
    }
}
