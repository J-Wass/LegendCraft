//Copyright (C) <2012>  <Jon Baker, Glenn Mariën and Lao Tszy>

//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.

//You should have received a copy of the GNU General Public License
//along with this program.  If not, see <http://www.gnu.org/licenses/>.

//Copyright (C) <2012> Glenn Mariën (http://project-vanilla.com)
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace fCraft
{
    static class PluginManager
    {
        public static List<Plugin> Plugins = new List<Plugin>();

        public static void Initialize() {
            try {
                if (!Directory.Exists("plugins")) {
                    Directory.CreateDirectory("plugins");
                }
            } catch ( Exception ex ) {
                Logger.Log( LogType.Error, "PluginManager.Initialize: " + ex );
                return;
            }

            // Load plugins
            string[] files = Directory.GetFiles( "plugins", "*.dll" );
            if (files.Length == 0) {
                Logger.Log( LogType.ConsoleOutput, "PluginManager: No plugins found" );
                return;
            }
            
            Logger.Log(LogType.ConsoleOutput, "PluginManager: Loading " + files.Length + " plugins");
            foreach (string file in files) {
                try {
                    LoadTypes(file);
                } catch (Exception ex) {
                    Logger.Log(LogType.Error, "PluginManager: Unable to load plugin at location " + file + ": " + ex);
                }
            }
            InitPlugins();
        }

        static void LoadTypes(string file) {
            Assembly lib = Assembly.LoadFrom(Path.GetFullPath(file));
            if (lib == null) return;
            
            foreach (Type t in lib.GetTypes()) {
                if (t.IsAbstract || t.IsInterface || !IsPlugin(t)) continue;
                Plugins.Add((Plugin)Activator.CreateInstance(t));
            }
        }
        
        static bool IsPlugin(Type plugin) {
            Type[] interfaces = plugin.GetInterfaces();
            foreach (Type t in interfaces) {
                if (t == typeof(Plugin)) return true;
            }
            return false;
        }

        static void InitPlugins() {
            if (Plugins.Count == 0) return;
            
            foreach (Plugin plugin in Plugins) {
                try {
                    plugin.Initialize();
                    Logger.Log(LogType.ConsoleOutput, "PluginManager: Loading plugin " + plugin.Name);
                } catch (Exception ex) {
                    Logger.Log(LogType.Error, "PluginManager: Failed loading plugin " + plugin.Name);
                    Logger.Log(LogType.Debug, ex.ToString());
                }
            }
        }
    }
}
