﻿using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Bricklayer.Core.Client.Interface.Controls;
using Bricklayer.Core.Client.World;
using Bricklayer.Core.Common.Entity;
using Bricklayer.Core.Common.Net.Messages;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoForce.Controls;

namespace Bricklayer.Core.Client.Interface.Screens
{
    internal class GameScreen : Screen
    {
        protected internal override GameState State => GameState.Game;
        internal Level Level => Client.Level;
        private StatusBar sbStats;
        private Label lblStats;
        private ControlList<ChatDataControl> lstChats;
        private ControlList<PlayerListDataControl> lstPlayers;
        private TextBox txtChat;

        /// <summary>
        /// Setup the game UI.
        /// </summary>
        public override void Add(ScreenManager screenManager)
        {
            base.Add(screenManager);

            sbStats = new StatusBar(Manager);
            sbStats.Init();
            sbStats.Bottom = Manager.ScreenHeight;
            sbStats.Left = 0;
            sbStats.Width = Manager.ScreenWidth;

            lblStats = new Label(Manager) { Top = 4, Left = 8, Width = Manager.ScreenWidth - 16 };
            lblStats.Init();

            sbStats.Add(lblStats);
            Window.Add(sbStats);

            //Chat input box
            txtChat = new TextBox(Manager);
            txtChat.Init();
            txtChat.Left = 8;
            txtChat.DrawFormattedText = false;
            txtChat.Bottom = sbStats.Top - 8;
            txtChat.Width = (int)(Manager.TargetWidth * .4f) - 16; //Remove 16 to align due to invisible scrollbar
            txtChat.Visible = false;
            txtChat.Passive = true;
            Window.Add(txtChat);

            lstChats = new ControlList<ChatDataControl>(Manager)
            {
                Left = txtChat.Left,
                Width = txtChat.Width + 16,
                Height = (int)(Manager.TargetHeight * .25f),
            };
            lstChats.Init();
            lstChats.Color = Color.Transparent;
            lstChats.HideSelection = true;
            lstChats.Passive = true;
            lstChats.HideScrollbars = true;
            lstChats.Top = txtChat.Top - lstChats.Height;
            Window.Add(lstChats);

            lstPlayers = new ControlList<PlayerListDataControl>(Manager)
            {
                Width = (int)(Manager.TargetWidth * .4f) - 16,
                Height = (int)(Manager.TargetHeight * .25f),
            };
            lstPlayers.Init();
            lstPlayers.HideSelection = true;
            lstPlayers.Left = Manager.TargetWidth/2 - (lstPlayers.Width / 2);
            lstPlayers.Passive = true;
            lstPlayers.HideScrollbars = true;
            lstPlayers.Visible = false;
            Window.Add(lstPlayers);

            foreach (var player in Level.Players)
                lstPlayers.Items.Add(new PlayerListDataControl(player.Username, Manager, lstPlayers));


            // Listen for later player joins
            Client.Events.Network.Game.PlayerJoinReceived.AddHandler(args =>
            {
                lstPlayers.Items.Add(new PlayerListDataControl(args.Player.Username, Manager, lstPlayers));
            });

            // Listen for ping updates for players
            Client.Events.Network.Game.PingUpdateReceived.AddHandler(args =>
            {
                foreach (var ping in args.Pings)
                {
                    ((PlayerListDataControl)lstPlayers.Items.FirstOrDefault(i => Level.Players.FirstOrDefault(x => x.UUID == ping.Key)?.Username == ((PlayerListDataControl)i).GetUser()))?.ChangePing(ping.Value);
                };
            });


            // Hackish way to get chats to start at the bottom
            for (var i = 0; i < (Manager.TargetHeight * 0.25f) / 18; i++)
            {
                lstChats.Items.Add(new ChatDataControl("", Manager, lstChats, this));
            }

            Client.Events.Network.Game.ChatReceived.AddHandler(args =>
            {
                lstChats.Items.Add(new ChatDataControl(args.Message, Manager, lstChats, this));
                lstChats.ScrollTo(lstChats.Items.Count);
            });
        }

        public override void Update(GameTime gameTime)
        {
            lblStats.Text = "FPS: " + Window.FPS;

            HandleInput();

            base.Update(gameTime);
        }


        private void HandleInput()
        {
            if (Client.Input.IsKeyPressed(Keys.T) && !txtChat.Visible) // Open chat
            {
                txtChat.Visible = true;
                txtChat.Passive = false;
                txtChat.Focused = true;
                lstChats.Items.ForEach(x => ((ChatDataControl)x).Show());
            }
            else if ((Client.Input.IsKeyPressed(Keys.Enter) && txtChat.Visible) || Client.Input.IsKeyPressed(Keys.Escape))
            {
                // If there's characters in chatbox, send chat
                // Cancel out of chat if player clicks escape
                if (!string.IsNullOrWhiteSpace(txtChat.Text) && !Client.Input.IsKeyPressed(Keys.Escape))
                {
                     Client.Network.Send(new ChatMessage(txtChat.Text));
                    txtChat.Text = string.Empty;
                }
                // If nothing is typed and player clicked enter, close out of chat
                txtChat.Visible = false;
                txtChat.Passive = true;
                txtChat.Focused = false;
                lstChats.Items.ForEach(x => ((ChatDataControl)x).Hide());
            }
            else if (Client.Input.IsKeyDown(Keys.Tab) && !lstPlayers.Visible)
            {
                lstPlayers.Visible = true;
            }
            else if (Client.Input.IsKeyUp(Keys.Tab) && lstPlayers.Visible)
            {
                lstPlayers.Visible = false;
            }
        }

        private void AddChat(string text, Manager manager, ControlList<ChatDataControl> controlList)
        {
            var lines = WrapText(text, lstChats.Width - 8, FontSize.Default9).Split('\n');
            foreach (var line in lines)
                lstChats.Items.Add(new ChatDataControl(line, Manager, lstChats, this));
        }

        private string WrapText(string text, float maxLineWidth, FontSize fontsize)
        {
            var spriteFont = Manager.Skin.Fonts[fontsize.ToString()].Resource;
            var words = text.Split(' ');
            var sb = new StringBuilder();
            var lineWidth = 0f;
            var spaceWidth = spriteFont.MeasureString(" ").X;
            foreach (var word in words)
            {
                var size = spriteFont.MeasureRichString(word, Manager);

                if (lineWidth + size.X < maxLineWidth)
                {
                    sb.Append(word + " ");
                    lineWidth += size.X + spaceWidth;
                }
                else
                {
                    sb.Append("\n" + word + " ");
                    lineWidth = size.X + spaceWidth;
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// If chat is open
        /// </summary>
        /// <returns></returns>
        public bool ChatOpen()
        {
            return txtChat.Visible;
        }

        /// <summary>
        /// Remove the game UI.
        /// </summary>
        public override void Remove()
        {
            Manager.Remove(sbStats);
        }
    }
}
