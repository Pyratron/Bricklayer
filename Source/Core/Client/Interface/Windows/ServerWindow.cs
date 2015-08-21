﻿using System.Collections.Generic;
using System.Linq;
using Bricklayer.Core.Client.Interface.Controls;
using Bricklayer.Core.Client.Interface.Screens;
using Bricklayer.Core.Common;
using Bricklayer.Core.Common.Data;
using MonoForce.Controls;

namespace Bricklayer.Core.Client.Interface.Windows
{
    /// <summary>
    /// The second window shown in Bricklayer, listing the servers and allowing the user to conenct to them.
    /// </summary>
    public sealed class ServerWindow : Dialog
    {
        public Button BtnAdd { get; }
        public Button BtnEdit { get; }

        /// <summary>
        /// Button to connect to the selected server.
        /// </summary>
        public Button BtnJoin { get; }

        /// <summary>
        /// Button to refresh and ping all servers again.
        /// </summary>
        public Button BtnRefresh { get; }

        public Button BtnRemove { get; }

        /// <summary>
        /// The listbox of server control items
        /// </summary>
        public ControlList<ServerDataControl> LstServers { get; }

        private readonly ServerScreen screen;
        private List<ServerData> servers; // The list of loaded servers

        public ServerWindow(Manager manager, ServerScreen screen) : base(manager)
        {
            this.screen = screen;
            // Setup the window
            CaptionVisible = false;
            TopPanel.Visible = false;
            Movable = false;
            Resizable = false;
            Width = 550;
            Height = 500;
            Shadow = true;
            Center();

            // Create main server list
            LstServers = new ControlList<ServerDataControl>(manager)
            {
                Left = 8,
                Top = 8,
                Width = ClientWidth - 16,
                Height = ClientHeight - BottomPanel.Height - 16
            };
            LstServers.Init();
            Add(LstServers);
            RefreshServerList();
            LstServers.DoubleClick += (sender, args) =>
            {
                if (LstServers.ItemIndex < 0)
                    return;

                var sdc = (ServerDataControl) LstServers.Items[LstServers.ItemIndex];
                // Make sure the user clicks the item and not the empty space in the list
                if (sdc.CheckPositionMouse(((MouseEventArgs) args).Position - LstServers.AbsoluteRect.Location))
                    this.screen.Client.Network.SendSessionRequest(servers[LstServers.ItemIndex].Host,
                        servers[LstServers.ItemIndex].Port);
            };

            // Add controls to the bottom panel. (Add server, edit server, etc.)

            BtnEdit = new Button(manager) {Text = "Edit", Top = 8, Width = 64};
            BtnEdit.Init();
            BtnEdit.Left = (Width/2) - (BtnEdit.Width/2);
            BtnEdit.Click += (sender, args) =>
            {
                if (LstServers.Items.Count <= 0)
                    return;

                var window = new AddServerDialog(Manager, this, LstServers.ItemIndex, true,
                    servers[LstServers.ItemIndex].Name, servers[LstServers.ItemIndex].Host,
                    servers[LstServers.ItemIndex].Port);
                window.Init();
                Manager.Add(window);
                window.ShowModal();
            };
            BottomPanel.Add(BtnEdit);

            BtnRemove = new Button(manager) {Text = "Remove", Left = BtnEdit.Right + 8, Top = 8, Width = 64};
            BtnRemove.Init();
            BtnRemove.Click += (clickSender, clickArgs) =>
            {
                // Show a messagebox that asks for confirmation to delete the selected server.
                if (LstServers.Items.Count > 0)
                {
                    var confirm = new MessageBox(manager, MessageBoxType.YesNo,
                        "Are you sure you would like to remove\nthis server from your server list?",
                        "Confirm Removal");
                    confirm.Init();
                    // When the message box is closed, check if the user clicked yes, if so, removed the server from the list.
                    confirm.Closed += async (closedSender, closedArgs) =>
                    {
                        var dialog = closedSender as Dialog;
                        if (dialog?.ModalResult != ModalResult.Yes)
                            return;
                        // If user clicked Yes
                        servers.RemoveAt(LstServers.ItemIndex);
                        LstServers.Items.RemoveAt(LstServers.ItemIndex);
                        await screen.Client.IO.WriteServers(servers); // Write the new server list to disk.
                    };
                    Manager.Add(confirm);
                    confirm.Show();
                }
            };
            BottomPanel.Add(BtnRemove);

            BtnAdd = new Button(manager) {Text = "Add", Top = 8, Width = 64};
            BtnAdd.Init();
            BtnAdd.Right = BtnEdit.Left - 8;
            BtnAdd.Click += (sender, args) =>
            {
                // Show add server dialog
                var window = new AddServerDialog(Manager, this, LstServers.ItemIndex, false, string.Empty,
                    string.Empty, Globals.Values.DefaultServerPort);
                window.Init();
                Manager.Add(window);
                window.ShowModal();
            };
            BottomPanel.Add(BtnAdd);

            BtnJoin = new Button(manager) {Text = "Connect", Top = 8, Width = 100};
            BtnJoin.Init();
            BtnJoin.Right = BtnAdd.Left - 8;
            BtnJoin.Click += (sender, args) =>
            {
                if (LstServers.ItemIndex < 0)
                    return;

                BtnJoin.Enabled = false;
                BtnJoin.Text = "Connecting...";
                this.screen.Client.Network.SendSessionRequest(servers[LstServers.ItemIndex].Host,
                    servers[LstServers.ItemIndex].Port);
            };
            BottomPanel.Add(BtnJoin);

            BtnRefresh = new Button(manager) {Text = "Refresh", Left = BtnRemove.Right + 8, Top = 8, Width = 64};
            BtnRefresh.Init();
            BtnRefresh.Click += (sender, args) => { RefreshServerList(); };
            BottomPanel.Add(BtnRefresh);

            // Listen for when init message is recieved
            screen.Client.Events.Network.Game.InitReceived.AddHandler(OnInit);

            // If user was disconnected from the server
            screen.Client.Events.Network.Game.Disconnected.AddHandler(OnDisconnect);
        }

        private void OnInit(EventManager.NetEvents.GameServerEvents.InitEventArgs args)
        {
            screen.ScreenManager.SwitchScreen(new LobbyScreen(args.Message.Description, args.Message.ServerName,
                args.Message.Intro, args.Message.Online, args.Message.Levels));
        }

        private void OnDisconnect(EventManager.NetEvents.GameServerEvents.DisconnectEventArgs args)
        {
            BtnJoin.Enabled = true;
            BtnJoin.Text = "Connect";
            var msgBox = new MessageBox(Manager, MessageBoxType.Warning, args.Reason, "Error Connecting to Server");
            msgBox.Init();
            screen.ScreenManager.Manager.Add(msgBox);
            msgBox.ShowModal();
        }

        protected override void Dispose(bool disposing)
        {
            screen.Client.Events.Network.Game.InitReceived.RemoveHandler(OnInit);
            screen.Client.Events.Network.Game.Disconnected.RemoveHandler(OnDisconnect);
            base.Dispose(disposing);
        }

        public async void AddServer(ServerData server)
        {
            servers.Add(server);
            await screen.Client.IO.WriteServers(servers);
            RefreshServerList();
        }

        public async void EditServer(int index, ServerData server)
        {
            servers[index] = server;
            await screen.Client.IO.WriteServers(servers);
            RefreshServerList();
        }

        private async void RefreshServerList()
        {
            LstServers.Items.Clear();
            // Read the servers from config
            servers = await screen.Client.IO.ReadServers();
            // Populate the list 
            foreach (var control in servers.Select(server => new ServerDataControl(screen, Manager, server, LstServers))
                )
            {
                LstServers.Items.Add(control);
                control.Init();
                control.PingServer();
            }

            if (LstServers.Items.Count > 0)
                LstServers.ItemIndex = 0;
        }
    }
}