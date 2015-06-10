﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bricklayer.Core.Server
{
    /// <summary>
    /// Represents a configuration for the server.
    /// </summary>
    public sealed class Config
    {
        /// <summary>
        /// The configuration for the server settings, such as port number and max connections.
        /// </summary>
        public ServerConfig Server;

        //More settings, such as database info will be provided in the future.

        public static Config GenerateDefaultConfig()
        {
            return new Config
            {
                Server = ServerConfig.GenerateDefaultConfig(),
            };
        }
    }

    /// <summary>
    /// Represents the server specific configuration elements
    /// </summary>
    public sealed class ServerConfig
    {
        /// <summary>
        /// The name of the server.
        /// </summary>
        public string Name;

        /// <summary>
        /// The "Message of the day/Description", to be shown in the server list.
        /// </summary>
        public string Decription;

        /// <summary>
        /// The extended MOTD, possibly showing news, stats, etc., displayed in the lobby.
        /// </summary>
        public string Intro;

        /// <summary>
        /// The port the server should run on.
        /// </summary>
        public int Port;

        /// <summary>
        /// The maximum allowed players allowed to connect to the server. (not per room)
        /// </summary>
        public int MaxPlayers;

        /// <summary>
        /// The server address to use for authentication.
        /// </summary>
        public string AuthServerAddress;

        /// <summary>
        /// The server port for authentication.
        /// </summary>
        public int AuthServerPort;

        public static ServerConfig GenerateDefaultConfig()
        {
            return new ServerConfig
            {
                Port = Common.Globals.Values.DefaultServerPort,
                AuthServerAddress = Common.Globals.Values.DefaultAuthAddress,
                AuthServerPort = Common.Globals.Values.DefaultAuthPort,
                MaxPlayers = 8,
                Name = "Bricklayer Server",
                Decription = "A Bricklayer Server running on the default configuration.\nPlease edit your Message Of The Day in the config file!",
                Intro = "Welcome to $Name!\nWe currently have $Online player(s) in $Rooms room(s).\n\nServer News:\n-\n-\n-\n\nServer Rules:\n-\n-\n-\n\n\n\n\n...",
            };
        }
    }
}