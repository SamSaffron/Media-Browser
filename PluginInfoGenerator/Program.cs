/*
 * This program is used to generate a plugin info xml file 
 *  from a directory containing plugins
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using MediaBrowser.Library.Plugins;
using System.Xml;

namespace PluginInfoGenerator {
    class Program {

        const string PLUGIN_INFO = "plugin_info.xml";

        static void Main(string[] args) {
            if (args.Length != 1 || !Directory.Exists(args[0])) {
                Usage();
                return;
            }
            string dir = args[0];

            XmlTextWriter writer = new XmlTextWriter(PLUGIN_INFO ,Encoding.UTF8);
            writer.Formatting = Formatting.Indented;
            writer.Indentation = 3;
            writer.WriteStartElement("Plugins");

            foreach (var file in Directory.GetFiles(dir)) {
                try {
                    var plugin = Plugin.FromFile(file, false);

                    writer.WriteStartElement("Plugin");
                    writer.WriteElementString("Version", plugin.Version.ToString());
                    writer.WriteElementString("Name", plugin.Name);
                    writer.WriteElementString("Description", plugin.Description);
                    writer.WriteElementString("Filename", Path.GetFileName(file));
                    writer.WriteEndElement();

                } catch (Exception e) {
                    Console.WriteLine("Failed to get infor for {0} : {1}", file, e);
                }
            }

            writer.WriteEndElement();
            writer.Close();

            Console.WriteLine("Wrote data to " + PLUGIN_INFO);

        }

        private static void Usage() {
            Console.WriteLine("This program will generate a plugin info file from a directory containing plugins");
            Console.WriteLine("Usage: PluginInfoGenerator <Path>"); 
        }
    }
}
