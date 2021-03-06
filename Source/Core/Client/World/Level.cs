﻿using System;
using Bricklayer.Core.Common.Data;
using Bricklayer.Core.Common.Net.Messages;
using Bricklayer.Core.Common.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Bricklayer.Core.Client.World
{
    public class Level : Common.World.Level
    {
        private static readonly float cameraSpeed = .18f;
        private static readonly float cameraParallax = .9f;

        /// <summary>
        /// The main camera to follow the player.
        /// </summary>
        public Camera Camera { get; private set; }

        /// <summary>
        /// The game client.
        /// </summary>
        public Client Client { get; internal set; }

        public Level(PlayerData creator, string name, Guid uuid, string description, int plays, double rating)
            : base(creator, name, uuid, description, plays, rating)
        {
            Tiles.BlockPlaced = BlockPlaced;
        }

        public Level(LevelData level, Client client) : base(level)
        {
            Client = client;
            Tiles.BlockPlaced = BlockPlaced;
        }

        /// <summary>
        /// Converts an instance of a common level to a client level.
        /// </summary>
        public Level(Common.World.Level level, Client client) : base(level)
        {
            Tiles = level.Tiles;
            Players = level.Players;
            Spawn = level.Spawn;
            Client = client;

            Camera =
                new Camera(new Vector2(Client.GraphicsDevice.Viewport.Width, Client.GraphicsDevice.Viewport.Height - 24))
                {
                    MinBounds =
                        new Vector2(-Client.GraphicsDevice.Viewport.Width, -Client.GraphicsDevice.Viewport.Height),
                    MaxBounds =
                        new Vector2((Width*Tile.Width) + Client.GraphicsDevice.Viewport.Width,
                            (Height*Tile.Height) + Client.GraphicsDevice.Viewport.Height)
                };

            Tiles.BlockPlaced = BlockPlaced;
        }

        /// <summary>
        /// Action to be run when a tile is changed.
        /// This is called by the tilemap array indexer.
        /// </summary>
        private void BlockPlaced(int x, int y, int z, Tile newTile, Tile oldTile)
        {
            // Send message to server.
            Client.Network.Send(new BlockPlaceMessage(x, y, z, newTile.Type));

            //Fire event so plugins are aware of the block placement.
            Client.Events.Game.Level.BlockPlaced.Invoke(
                new EventManager.GameEvents.LevelEvents.BlockPlacedEventArgs(this, x, y, z, newTile.Type, oldTile.Type));
        }

        public void Update(GameTime delta)
        {
        }

        public void Draw(SpriteBatch batch, GameTime delta)
        {
            DrawTiles(batch);
        }

        /// <summary>
        /// Draws the foreground and background blocks of a map
        /// </summary>
        private void DrawTiles(SpriteBatch batch)
        {
            // Draw Foreground Blocks
            for (var x = (int) Camera.Left/Tile.Width; x <= (int) Camera.Right/Tile.Width; x++)
            {
                for (var y = ((int) Camera.Bottom/Tile.Height); y >= (int) Camera.Top/Tile.Height; y--)
                {
                    if (!InDrawBounds(x, y)) continue;
                    var tile = Tiles[x, y, 1];
                    if (tile.Type.IsRenderable)
                    {
                        tile.Type.Draw(batch, tile, x, y);
                    }
                }
            }
        }
    }
}