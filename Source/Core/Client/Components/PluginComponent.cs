﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Bricklayer.Core.Client.Interface.Windows;
using Bricklayer.Core.Common;
using Bricklayer.Core.Common.Net.Messages;

namespace Bricklayer.Core.Client.Components
{
    /// <summary>
    /// Handles plugin management.
    /// </summary>
    public class PluginComponent : ClientComponent
    {
        /// <summary>
        /// The number of plugins currently loaded.
        /// </summary>
        public int PluginCount => Plugins.Count;

        internal List<ClientPlugin> Plugins { get; private set; }

        public PluginComponent(Client client) : base(client)
        {
            Plugins = new List<ClientPlugin>();
        }

        private string loadingPlugin;

        public override async Task Init()
        {
            if (!Client.IO.Initialized)
                throw new InvalidOperationException("The IO component must be initialized first.");
            // Resolve assembly references for plugins.
            AppDomain.CurrentDomain.AssemblyResolve += 
                (sender, args) =>
                {
                    // If a plugin.dll is trying to load a referenced assembly
                    if (args.RequestingAssembly.FullName.Split(',')[0] != "plugin")
                        return null;
                    var path = Path.Combine(loadingPlugin, args.Name.Split(',')[0] + ".dll");
                    if (File.Exists(path))
                        return Assembly.LoadFile(path);
                    path = Path.Combine(loadingPlugin, args.Name.Split(',')[0] + ".exe"); // Try to load a .exe if .dll doesn't exist
                    if (File.Exists(path))
                        return Assembly.LoadFile(path);
                    return null;
                };

            LoadPlugins();

            Client.Events.Network.Auth.PluginDownloadRequested.AddHandler(args =>
            {
                if (!PluginDownloadWindow.IsDownloading(args.Message.ID))
                {
                    var pluginWindow = new PluginDownloadWindow(Client.UI, Client.Window, args.Message.ModName, args.Message.ID,
                        args.Message.FileName, false);
                    pluginWindow.Init();
                    Client.Window.Add(pluginWindow);
                    pluginWindow.Show();
                }
                // Tell the auth server it got the message
                Client.Network.PingAuthMessage(PingAuthMessage.PingResponse.GotPlugin, args.Message.ID.ToString());
            });

            await base.Init();
        }

        /// <summary>
        /// Loads all plugins that are not already loaded.
        /// </summary>
        internal void LoadPlugins()
        {
            // Get a list of all the .dlls in the directory
            List<PluginData> files = null;
            try
            {
                files = IOHelper.GetPlugins(Client.IO.Directories["Plugins"], Client.IO.SerializationSettings).ToList();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            if (files == null)
                return;
            
            foreach (var file in files.Where(file => !Plugins.Contains(file)))
            {
                // TODO: Use AppDomains for security
                // Load the assembly
                try
                {
                    // Make sure dependencies are met.
                    if (file.Dependencies.Count > 0)
                        foreach (var dep in
                            file.Dependencies.Where(dep => files.All(plugin => plugin.Identifier != dep)))
                            throw new FileNotFoundException($"Dependency \"{dep}\" for plugin \"{file.Name}\" not found.");
                    var asm = IOHelper.LoadPlugin(AppDomain.CurrentDomain, file.Path);
                    loadingPlugin = file.Path;
                    RegisterPlugin(IOHelper.CreatePluginInstance<ClientPlugin>(asm, Client, file));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        private void RegisterPlugin(ClientPlugin plugin)
        {
            LoadIcon(plugin);
            Plugins.Add(plugin);

            // If plugin isn't in the plugins config
            if (!Client.IO.ReadPlugins().ContainsKey(plugin.Name))
            {
                var plugins = Client.IO.ReadPlugins();
                plugins.Add(plugin.Name, true);
                Client.IO.WritePlugins(plugins);
            }
            // Check if plugin is disabled
            if (!Client.IO.ReadPlugins()[plugin.Name]) return;
            // Load plugin content
            Client.Content.LoadTextures(Path.Combine(plugin.Path, Path.Combine("Content", "Textures")), Client);
            // Load plugin
            plugin.Load();
            Console.WriteLine($"Plugin: Loaded {plugin.GetInfoString()}");
        }

        /// <summary>
        /// Load icon for plugin
        /// </summary>
        private void LoadIcon(ClientPlugin plugin)
        {
            var path = Path.Combine(plugin.Path, "Content");

            if (Directory.Exists(path))
            {
                var dir = new DirectoryInfo(path);

                string[] extensions = new[] { ".jpg", ".jpeg", ".png" };

                var icon = dir.GetFiles().FirstOrDefault(f => f.Extension.EqualsAny(extensions) && Path.GetFileNameWithoutExtension(f.Name) == "icon");

                if (icon != null)
                {
                    var texture = Client.TextureLoader.FromFile(icon.FullName);
                    if (texture.Height <= 64 && texture.Width <= 64)
                        plugin.Icon = texture;
                    else
                        Console.WriteLine($"Plugin Icon is bigger than the size limit of 64x64. Not loading icon.");
                }
            }
            else
                Console.WriteLine($"Directory {path} does not exist.");
        }
    }
}