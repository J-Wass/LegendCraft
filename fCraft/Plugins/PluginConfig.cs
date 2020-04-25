using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace fCraft
{
    public abstract class PluginConfig
    {
        public virtual void Save(Plugin plugin, String file)
        {
			// Append plugin directory
			string realFile = Path.Combine("plugins", plugin.Name, file);
            PluginConfigSerializer.Serialize(realFile, this);
        }

        public virtual PluginConfig Load(Plugin plugin, String file, Type type)
        {
            // Append plugin directory
			string realFile = Path.Combine("plugins", plugin.Name, file);

            if (!Directory.Exists(Path.Combine("plugins", plugin.Name)))
            {
				Directory.CreateDirectory(Path.Combine("plugins", plugin.Name));
            }

            if (!File.Exists(realFile))
            {
                Save(plugin, file);
            }

            return PluginConfigSerializer.Deserialize(realFile, type);
        }
    }
}
