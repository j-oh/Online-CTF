using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Lidgren.Network;
using NetGameShared;

namespace NetGameServer
{
    struct Disconnection
    {
        public NetConnection connection;
        public Error error;

        public Disconnection(NetConnection connection, Error error)
        {
            this.connection = connection;
            this.error = error;
        }
    }

    class Network
    {
        const int MAX_CONNECTIONS = 100;

        NetServer server;
        NetPeerConfiguration config;
        World world;
        ConcurrentDictionary<int, Player> playerIndex;
        ConcurrentDictionary<int, Mob> mobIndex;
        ConcurrentDictionary<int, ItemDrop> dropIndex;
        ConcurrentDictionary<int, FallingBlock> fallingBlockIndex;
        Tent redTent, blueTent;
        List<Disconnection> disconnections;
        Timer updateTimer;
        Random random;
        string serverName;
        bool loginsProcessed, gameEnded;
        int redTickets, blueTickets;

        public Network()
        {
            random = new Random();
            disconnections = new List<Disconnection>();
            loginsProcessed = true;
            serverName = "My Server";
        }

        public void Start(World world, string serverName)
        {
            this.world = world;
            this.serverName = serverName;
            config = new NetPeerConfiguration(Universal.ID);
            config.Port = Universal.DEFAULT_PORT;
            config.MaximumConnections = MAX_CONNECTIONS;
            config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
            config.EnableMessageType(NetIncomingMessageType.ConnectionLatencyUpdated);
            server = new NetServer(config);
            server.Start();
            Console.WriteLine("Starting server");
            playerIndex = world.PlayerIndex;
            mobIndex = world.MobIndex;
            dropIndex = world.DropIndex;
            fallingBlockIndex = world.FallingBlockIndex;
            redTent = world.RedTent;
            blueTent = world.BlueTent;
            updateTimer = new Timer(100);
            updateTimer.Elapsed += new ElapsedEventHandler(UpdateElapsed);
            updateTimer.Start();
            redTickets = 40;
            blueTickets = 40;
            Console.WriteLine("Waiting for connections...");
        }

        private void UpdateElapsed(object sender, ElapsedEventArgs e)
        {
            if (!world.RestartInterval.IsRunning || world.RestartInterval.Left > 2)
            {
                SendWorldState(null);
                if (!loginsProcessed)
                {
                    loginsProcessed = true;
                    foreach (KeyValuePair<int, Player> entry in playerIndex.ToArray())
                    {
                        Player player = entry.Value;
                        if (!player.Connected)
                        {
                            SendWorldState(player.Connection);
                            loginsProcessed = false;
                        }
                    }
                }
                foreach (Disconnection dc in disconnections)
                {
                    switch (dc.error)
                    {
                        case Error.VersionMismatch:
                            SendVersionMismatch(dc.connection);
                            break;
                    }
                }
                /*if (random.Next() % 300 <= 1)
                {
                    int id = 0;
                    foreach (KeyValuePair<int, Mob> entry in mobIndex.ToArray())
                    {
                        if (entry.Key == id)
                            id++;
                    }
                    Jell jell = new Jell(true);
                    jell.X = world.WorldMap.Width * Universal.TILE_SIZE / 4 + random.Next() % (world.WorldMap.Width * Universal.TILE_SIZE / 2);
                    world.AddMob(id, jell);
                    SendNewMob(jell, id);
                    Console.WriteLine("created new mob");
                }*/
            }
        }

        public void Update()
        {
            ReadMessages();
            if (!world.TimeRemaining.IsRunning && !gameEnded)
            {
                if (redTickets > blueTickets)
                    SendGameEnded("Red Team");
                else if (blueTickets > redTickets)
                    SendGameEnded("Blue Team");
                else
                    SendGameEnded("No Team");
                gameEnded = true;
            }
            if (gameEnded && world.RestartInterval.Left < 1)
                RestartGame();
        }

        private void ReadMessages()
        {
            NetIncomingMessage inmsg;

            if ((inmsg = server.ReadMessage()) != null)
            {
                switch (inmsg.MessageType)
                {
                    case NetIncomingMessageType.ConnectionApproval:
                        if (inmsg.ReadByte() == (byte)Packets.Connect)
                        {
                            Console.WriteLine("Receieved a connection.");
                            string version = inmsg.ReadString();
                            if (version == Universal.GAME_VERSION)
                            {
                                Console.WriteLine("Version matches.");
                                inmsg.SenderConnection.Approve();
                                Random r = Server.random;
                                int id = 0, redCount = 0, blueCount = 0;
                                foreach (KeyValuePair<int, Player> entry in playerIndex.ToArray())
                                {
                                    if (entry.Key == id)
                                        id++;
                                    if (entry.Value.PlayerTeam.Name == "Red Team")
                                        redCount++;
                                    else if (entry.Value.PlayerTeam.Name == "Blue Team")
                                        blueCount++;
                                }
                                Color color = new Color(r.Next(256), r.Next(256), r.Next(256));
                                String nickname = inmsg.ReadString();
                                Player player = new Player(id, nickname, 0, 0, color, inmsg.SenderConnection);
                                if (world.ServerMode == GameMode.TeamDeathmatch || world.ServerMode == GameMode.CaptureTheFlag)
                                {
                                    if (redCount <= blueCount)
                                        player.PlayerTeam = new Team("Red Team", Color.Maroon);
                                    else
                                        player.PlayerTeam = new Team("Blue Team", Color.Blue);
                                }
                                player.SetNetwork(this);
                                player.DeathAction(true);
                                player.RespondMove = false;
                                world.AddPlayer(id, player);
                                SendWorldState(player.Connection);
                                SendNewPlayer(player, id);
                                loginsProcessed = false;
                            }
                            else
                            {
                                Console.WriteLine("Version mismatch! Server version: " + Universal.GAME_VERSION + "  Client version: " + version);
                                inmsg.SenderConnection.Approve();
                                SendVersionMismatch(inmsg.SenderConnection);
                                disconnections.Add(new Disconnection(inmsg.SenderConnection, Error.VersionMismatch));
                            }
                        }
                        break;

                    case NetIncomingMessageType.Data:
                        byte data = inmsg.ReadByte();
                        switch(data)
                        { 
                            case (byte)Packets.ConnectVerified:
                                Player verifiedPlayer;
                                int id = inmsg.ReadInt32();
                                if (playerIndex.TryGetValue(id, out verifiedPlayer))
                                {
                                    verifiedPlayer.Connected = true;
                                    Console.WriteLine("Connection approved! " + verifiedPlayer.Nickname + " of ID " + id + " joined the game.");
                                    SendChat(verifiedPlayer.Nickname + " (ID " + id + ") joined the game", "[Server]");
                                }
                                break;
                            case (byte)Packets.ColorChange:
                                int changerID = inmsg.ReadInt32();
                                SendRandomColor(changerID);
                                break;
                            case (byte)Packets.Chat:
                                Player sentPlayer;
                                if (playerIndex.TryGetValue(inmsg.ReadInt32(), out sentPlayer))
                                    SendChat(inmsg.ReadString(), sentPlayer);
                                break;
                            case (byte)Packets.Move:
                                Player player;
                                if (playerIndex.TryGetValue(inmsg.ReadInt32(), out player) && player.RespondMove)
                                {
                                    player.SetPreviousPosition();
                                    player.X = inmsg.ReadFloat();
                                    player.Y = inmsg.ReadFloat();
                                    player.DX = inmsg.ReadFloat();
                                    player.DY = inmsg.ReadFloat();
                                    player.State = (Player.PlayerState)inmsg.ReadInt32();
                                    player.SetFlipHorizontally(inmsg.ReadBoolean());
                                }
                                SendWorldState(null);
                                break;
                            case (byte)Packets.ServerMove:
                                Player movedPlayer;
                                if (playerIndex.TryGetValue(inmsg.ReadInt32(), out movedPlayer))
                                    movedPlayer.RespondMove = true;
                                break;
                            case (byte)Packets.Attack:
                                Player attacker;
                                int attackerID = inmsg.ReadInt32();
                                if (playerIndex.TryGetValue(attackerID, out attacker))
                                {
                                    int offset = -16;
                                    if (attacker.FlippedHorizontally())
                                        offset = -32;
                                    Rectangle hitbox = new Rectangle((int)attacker.X + offset, (int)attacker.Y - 8, 64, 64);
                                    foreach (KeyValuePair<int, Mob> entry in mobIndex.ToArray())
                                    {
                                        Mob mobEntry = entry.Value;
                                        if (hitbox.Intersects(mobEntry.CollideBox))
                                        {
                                            if (offset == -16)
                                                mobEntry.DX += 300;
                                            else
                                                mobEntry.DX -= 300;
                                            mobEntry.DY -= 400;
                                            int damage = 8 + random.Next() % 4;
                                            mobEntry.Damage(damage);
                                            SendHitMob(entry.Key, damage);
                                        }
                                    }
                                    foreach (KeyValuePair<int, Player> entry in playerIndex.ToArray())
                                    {
                                        int attackedID = entry.Key;
                                        Player playerEntry = entry.Value;
                                        if (attacker != playerEntry && hitbox.Intersects(playerEntry.CollideBox) && 
                                            (playerEntry.PlayerTeam.Name == "No Team" || attacker.PlayerTeam.Name != playerEntry.PlayerTeam.Name))
                                        {
                                            if (offset == -16)
                                                playerEntry.DX += 300;
                                            else
                                                playerEntry.DX -= 300;
                                            playerEntry.DY -= 400;
                                            int damage = 8 + random.Next() % 4;
                                            playerEntry.Damage(damage);
                                            SendHitPlayer(attackedID, damage, playerEntry.DX, playerEntry.DY);
                                            if (playerEntry.HP <= 0)
                                            {
                                                SendKilledPlayer(attackerID, attackedID, playerEntry);
                                                playerEntry.DeathAction(false);
                                                attacker.Kills++;
                                                SendServerMove(attackedID, playerEntry);
                                                playerEntry.RespondMove = false;
                                                CheckWinCondition();
                                            }
                                        }
                                    }
                                }
                                SendWorldState(null);
                                break;
                            case (byte)Packets.AddBlock:
                                Player addedPlayer;
                                int addedID = inmsg.ReadInt32();
                                if (playerIndex.TryGetValue(addedID, out addedPlayer))
                                {
                                    int index = inmsg.ReadInt32();
                                    ItemName itemName = (ItemName)inmsg.ReadInt32();
                                    Item selectedItem = addedPlayer.Items[index];
                                    if (selectedItem.Name == itemName && selectedItem.Count > 0 && Item.GetType(itemName) == ItemType.Block)
                                    {
                                        int blockX = inmsg.ReadInt32() / Universal.TILE_SIZE;
                                        int blockY = inmsg.ReadInt32() / Universal.TILE_SIZE;
                                        if (Vector2.Distance(new Vector2(addedPlayer.X, addedPlayer.Y), 
                                            new Vector2(blockX * Universal.TILE_SIZE, blockY * Universal.TILE_SIZE)) <= Universal.TILE_SIZE * Universal.PLACE_DISTANCE * 1.5f &&
                                            world.WorldMap.ChangeTile(0, blockX, blockY, selectedItem.BlockID, true))
                                        {
                                            selectedItem.RemoveOne();
                                            SendChangeBlock(0, blockX, blockY, selectedItem.BlockID, true);
                                            SendRemoveOneItem(addedPlayer.Connection, index);
                                        }
                                    }
                                }
                                break;
                            case (byte)Packets.HitBlock:
                                Player hitPlayer;
                                int hitID = inmsg.ReadInt32();
                                if (playerIndex.TryGetValue(hitID, out hitPlayer))
                                {
                                    bool hitting = inmsg.ReadBoolean();
                                    int blockX = inmsg.ReadInt32() / Universal.TILE_SIZE;
                                    int blockY = inmsg.ReadInt32() / Universal.TILE_SIZE;
                                    if (hitting)
                                    {
                                        if (Vector2.Distance(new Vector2(hitPlayer.X, hitPlayer.Y),
                                            new Vector2(blockX * Universal.TILE_SIZE, blockY * Universal.TILE_SIZE)) <= Universal.TILE_SIZE * Universal.PLACE_DISTANCE * 1.5f &&
                                            world.WorldMap.GetTile(0, blockX, blockY) != null)
                                        {
                                            hitPlayer.HittingBlock = true;
                                            hitPlayer.HitBlock = world.WorldMap.GetTile(0, blockX, blockY);
                                        }
                                        else
                                            hitPlayer.HittingBlock = false;
                                    }
                                    else
                                        hitPlayer.HittingBlock = false;
                                }
                                break;
                            case (byte)Packets.PickUpItem:
                                Player pickedPlayer;
                                int pickedID = inmsg.ReadInt32();
                                int pickedIndex = inmsg.ReadInt32();
                                bool redTentSelected = inmsg.ReadBoolean();
                                bool blueTentSelected = inmsg.ReadBoolean();
                                if (playerIndex.TryGetValue(pickedID, out pickedPlayer))
                                {
                                    Vector2 checkPosition = new Vector2(pickedPlayer.X + pickedPlayer.BoundWidth / 2, pickedPlayer.Y + pickedPlayer.BoundHeight);
                                    ItemName itemName = (ItemName)inmsg.ReadInt32();
                                    Item selectedItem = pickedPlayer.Items[pickedIndex];
                                    if ((selectedItem.Name == itemName && selectedItem.Count > 0) || selectedItem.Name == ItemName.None)
                                    {
                                        int count = inmsg.ReadInt32();
                                        for (int i = 0; i < count; i++)
                                        {
                                            ItemDrop drop;
                                            int dropID = inmsg.ReadInt32();
                                            if (dropIndex.TryGetValue(dropID, out drop) && Vector2.Distance(checkPosition, drop.Position) <= Universal.TILE_SIZE * 2)
                                            {
                                                if (drop.Name == ItemName.RedFlag && pickedPlayer.PlayerTeam.Name == "Red Team")
                                                {
                                                    redTent.HasFlag = true;
                                                    SendTentState(true);
                                                    Universal.TryDictRemove(dropIndex, dropID);
                                                    SendRemoveDrop(dropID);
                                                }
                                                else if (drop.Name == ItemName.BlueFlag && pickedPlayer.PlayerTeam.Name == "Blue Team")
                                                {
                                                    blueTent.HasFlag = true;
                                                    SendTentState(false);
                                                    Universal.TryDictRemove(dropIndex, dropID);
                                                    SendRemoveDrop(dropID);
                                                }
                                                else if (selectedItem.Name == drop.Name || selectedItem.Name == ItemName.None)
                                                {
                                                    if (selectedItem.Name == drop.Name)
                                                        selectedItem.Count += drop.Count;
                                                    else
                                                        selectedItem.SetItem(drop.Name, drop.Count, true);
                                                    if (selectedItem.Count > selectedItem.MaxCount)
                                                    {
                                                        drop.Count = selectedItem.Count - selectedItem.MaxCount;
                                                        selectedItem.Count = selectedItem.MaxCount;
                                                        SendModifyDrop(dropID, drop.Count);
                                                    }
                                                    else
                                                    {
                                                        if (selectedItem.Name == ItemName.RedFlag)
                                                            SendPlayerFlag(pickedID, 1);
                                                        else if (selectedItem.Name == ItemName.BlueFlag)
                                                            SendPlayerFlag(pickedID, 2);
                                                        Universal.TryDictRemove(dropIndex, dropID);
                                                        SendRemoveDrop(dropID);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    if (redTentSelected)
                                    {
                                        if (redTent.HasFlag && Vector2.Distance(checkPosition, redTent.Position) <= Universal.TILE_SIZE * 4)
                                        {
                                            if (selectedItem.Name == ItemName.None && pickedPlayer.PlayerTeam.Name == "Blue Team")
                                            {
                                                SendPlayerFlag(pickedID, 1);
                                                selectedItem.SetItem(ItemName.RedFlag, 1, true);
                                                redTent.HasFlag = false;
                                                SendTentState(true);
                                            }
                                            else if (selectedItem.Name == ItemName.BlueFlag)
                                            {
                                                SendPlayerFlag(pickedID, 0);
                                                selectedItem.SetItem(ItemName.None, 0, true);
                                                SendGameEnded("Red Team");
                                                gameEnded = true;
                                            }
                                        }
                                    }
                                    if (blueTentSelected)
                                    {
                                        if (blueTent.HasFlag && Vector2.Distance(checkPosition, blueTent.Position) <= Universal.TILE_SIZE * 4)
                                        {
                                            if (selectedItem.Name == ItemName.None && pickedPlayer.PlayerTeam.Name == "Red Team")
                                            {
                                                SendPlayerFlag(pickedID, 2);
                                                selectedItem.SetItem(ItemName.BlueFlag, 1, true);
                                                blueTent.HasFlag = false;
                                                SendTentState(false);
                                            }
                                            else if (selectedItem.Name == ItemName.RedFlag)
                                            {
                                                SendPlayerFlag(pickedID, 0);
                                                selectedItem.SetItem(ItemName.None, 0, true);
                                                SendGameEnded("Blue Team");
                                                gameEnded = true;
                                            }
                                        }
                                    }
                                    SendItemChange(pickedPlayer.Connection, pickedPlayer);
                                }
                                break;
                            case (byte)Packets.DropItem:
                                Player droppedPlayer;
                                int droppedID = inmsg.ReadInt32();
                                int droppedIndex = inmsg.ReadInt32();
                                if (playerIndex.TryGetValue(droppedID, out droppedPlayer))
                                {
                                    Item selectedItem = droppedPlayer.Items[droppedIndex];
                                    ItemDrop drop = new ItemDrop(selectedItem.Name, selectedItem.Count, true);
                                    drop.Position = new Vector2(droppedPlayer.X + droppedPlayer.BoundWidth / 2, droppedPlayer.Y);
                                    drop.DX = droppedPlayer.DX * 2;
                                    drop.DY = droppedPlayer.DY * 1.5f - 300;
                                    int droppedEntryID = 0;
                                    foreach (KeyValuePair<int, ItemDrop> dropEntry in dropIndex.ToArray())
                                    {
                                        if (dropEntry.Key == droppedEntryID)
                                            droppedEntryID++;
                                    }
                                    if (world.AddDrop(droppedEntryID, drop))
                                        SendNewDrop(droppedEntryID, drop);
                                    if (selectedItem.Name == ItemName.RedFlag || selectedItem.Name == ItemName.BlueFlag)
                                        SendPlayerFlag(droppedID, 0);
                                    selectedItem.SetItem(ItemName.None, 0, true);
                                    SendItemChange(droppedPlayer.Connection, droppedPlayer);
                                }
                                break;
                        }
                        break;
                    case NetIncomingMessageType.StatusChanged:
                        Console.WriteLine(inmsg.SenderConnection.ToString() + " status changed. " + (NetConnectionStatus)inmsg.SenderConnection.Status);
                        if (inmsg.SenderConnection.Status == NetConnectionStatus.Disconnected || inmsg.SenderConnection.Status == NetConnectionStatus.Disconnecting)
                        {
                            foreach (KeyValuePair<int, Player> entry in playerIndex.ToArray())
                            {
                                Player player = entry.Value;
                                if (player.Connection == inmsg.SenderConnection)
                                {
                                    for (int i = 0; i < player.Items.Length; i++)
                                    {
                                        if (player.Items[i].Name == ItemName.RedFlag)
                                        {
                                            redTent.HasFlag = true;
                                            SendTentState(true);
                                        }
                                        else if (player.Items[i].Name == ItemName.BlueFlag)
                                        {
                                            blueTent.HasFlag = true;
                                            SendTentState(false);
                                        }
                                    }
                                    SendRemovePlayer(entry.Key);
                                    SendChat(player.Nickname + " (ID " + entry.Key + ") left the game", "[Server]");
                                    Universal.TryDictRemove(playerIndex, entry.Key);
                                    break;
                                }
                            }
                        }
                        break;
                    case NetIncomingMessageType.ConnectionLatencyUpdated:
                        int ping = (int)(inmsg.ReadFloat() * 1000);
                        foreach (KeyValuePair<int, Player> entry in playerIndex.ToArray())
                        {
                            Player player = entry.Value;
                            if (player.Connection == inmsg.SenderConnection)
                                player.Ping = ping;
                        }
                        break;
                    default:
                        break;
                }
                server.Recycle(inmsg);
            }
        }

        public void CheckWinCondition()
        {
            if (!gameEnded)
            {
                bool allRedsDead = true, allBluesDead = true;
                foreach (KeyValuePair<int, Player> entry in playerIndex)
                {
                    Player player = entry.Value;
                    if (player.Respawned)
                    {
                        if (player.PlayerTeam.Name == "Red Team")
                            allRedsDead = false;
                        else if (player.PlayerTeam.Name == "Blue Team")
                            allBluesDead = false;
                    }
                }
                if (allRedsDead && redTickets <= 0)
                {
                    SendGameEnded("Blue Team");
                    gameEnded = true;
                }
                else if (allBluesDead && blueTickets <= 0)
                {
                    SendGameEnded("Red Team");
                    gameEnded = true;
                }
            }
        }

        public bool CheckTeamTickets(string teamName)
        {
            if (teamName == "Red Team" && redTickets > 0)
            {
                redTickets--;
                return true;
            }
            else if (teamName == "Blue Team" && blueTickets > 0)
            {
                blueTickets--;
                return true;
            }
            return false;
        }

        private void RestartGame()
        {
            gameEnded = false;
            loginsProcessed = false;
            redTickets = 40;
            blueTickets = 40;
            world.RestartGame();
            redTent = world.RedTent;
            blueTent = world.BlueTent;
            SendChat("New game starting!", "[Server]");
            if (server.ConnectionsCount > 0)
            {
                NetOutgoingMessage outmsg = server.CreateMessage();
                outmsg.Write((byte)Packets.RestartGame);
                server.SendMessage(outmsg, server.Connections, NetDeliveryMethod.ReliableUnordered, 31);
            }
            foreach (KeyValuePair<int, Player> entry in playerIndex.ToArray())
                SendWorldState(entry.Value.Connection);
        }

        private void SendVersionMismatch(NetConnection connection)
        {
            NetOutgoingMessage outmsg = server.CreateMessage();
            outmsg.Write((byte)Packets.VersionMismatch);
            outmsg.Write(Universal.GAME_VERSION);
            server.SendMessage(outmsg, connection, NetDeliveryMethod.UnreliableSequenced, 31);
        }

        private void SendRemoveOneItem(NetConnection connection, int index)
        {
            NetOutgoingMessage outmsg = server.CreateMessage();
            outmsg.Write((byte)Packets.RemoveOneItem);
            outmsg.Write(index);
            server.SendMessage(outmsg, connection, NetDeliveryMethod.ReliableOrdered, 1);
        }

        private void SendGameEnded(string teamName)
        {
            if (server.ConnectionsCount > 0)
            {
                NetOutgoingMessage outmsg = server.CreateMessage();
                outmsg.Write((byte)Packets.GameEnded);
                switch (teamName)
                {
                    case "Red Team":
                        outmsg.Write("Red Team wins!");
                        break;
                    case "Blue Team":
                        outmsg.Write("Blue Team wins!");
                        break;
                    case "No Team":
                    default:
                        outmsg.Write("It's a draw!");
                        break;
                }
                server.SendMessage(outmsg, server.Connections, NetDeliveryMethod.ReliableUnordered, 0);
            }
            world.RestartInterval.Start(10, true);
        }

        private void SendPlayerFlag(int id, int flagIndex)
        {
            if (server.ConnectionsCount > 0)
            {
                NetOutgoingMessage outmsg = server.CreateMessage();
                outmsg.Write((byte)Packets.PlayerFlag);
                outmsg.Write(id);
                outmsg.Write(flagIndex);
                server.SendMessage(outmsg, server.Connections, NetDeliveryMethod.ReliableOrdered, 3);
            }
        }

        // fix in v4

        private void SendPlayerFlags(NetConnection connection)
        {
            if (connection != null)
            {
                foreach (KeyValuePair<int, Player> entry in playerIndex.ToArray())
                {
                    Player player = entry.Value;
                    NetOutgoingMessage outmsg = server.CreateMessage();
                    outmsg.Write((byte)Packets.PlayerFlag);
                    outmsg.Write(entry.Key);
                    int flagIndex = 0;
                    foreach (Item item in player.Items)
                    {
                        if (item.Name == ItemName.RedFlag)
                        {
                            flagIndex = 1;
                            break;
                        }
                        else if (item.Name == ItemName.BlueFlag)
                        {
                            flagIndex = 2;
                            break;
                        }
                    }
                    outmsg.Write(flagIndex);
                    server.SendMessage(outmsg, connection, NetDeliveryMethod.ReliableOrdered, 3);
                }
            }
        }

        private void SendRandomColor(int id)
        {
            if (server.ConnectionsCount > 0)
            {
                Random r = Server.random;
                NetOutgoingMessage outmsg = server.CreateMessage();
                outmsg.Write((byte)Packets.ColorChange);
                outmsg.Write(id);
                Color color = new Color(r.Next(256), r.Next(256), r.Next(256));
                Player player;
                if (playerIndex.TryGetValue(id, out player))
                    player.DrawColor = color;
                outmsg.Write(color.R);
                outmsg.Write(color.G);
                outmsg.Write(color.B);
                server.SendMessage(outmsg, server.Connections, NetDeliveryMethod.ReliableSequenced, 0);
            }
        }

        public void SendKilledPlayer(int killerId, int killedId, Player killedPlayer)
        {
            for (int i = 0; i < killedPlayer.Items.Length; i++)
            {
                Item item = killedPlayer.Items[i];
                ItemDrop drop = new ItemDrop(item.Name, item.Count, true);
                drop.Position = new Vector2(killedPlayer.X + killedPlayer.BoundWidth / 2, killedPlayer.Y);
                drop.DX = killedPlayer.DX / 2;
                drop.DY = killedPlayer.DY - 300;
                int id = 0;
                foreach (KeyValuePair<int, ItemDrop> dropEntry in dropIndex.ToArray())
                {
                    if (dropEntry.Key == id)
                        id++;
                }
                if (world.AddDrop(id, drop))
                    SendNewDrop(id, drop);
            }
            if (server.ConnectionsCount > 0)
            {
                NetOutgoingMessage outmsg = server.CreateMessage();
                outmsg.Write((byte)Packets.KilledPlayer);
                outmsg.Write(killerId);
                outmsg.Write(killedId);
                server.SendMessage(outmsg, server.Connections, NetDeliveryMethod.ReliableUnordered, 0);
                // fix in v4
                SendPlayerFlag(killedId, 0);
            }
        }

        public void SendServerMove(int id, Player movedPlayer)
        {
            if (server.ConnectionsCount > 0)
            {
                NetOutgoingMessage outmsg = server.CreateMessage();
                outmsg.Write((byte)Packets.ServerMove);
                outmsg.Write(id);
                outmsg.Write(movedPlayer.X);
                outmsg.Write(movedPlayer.Y);
                outmsg.Write(movedPlayer.DX);
                outmsg.Write(movedPlayer.DY);
                server.SendMessage(outmsg, server.Connections, NetDeliveryMethod.ReliableUnordered, 0);
            }
        }

        public void SendChangeBlock(int layer, int blockX, int blockY, int blockID, bool add)
        {
            if (server.ConnectionsCount > 0)
            {
                NetOutgoingMessage outmsg = server.CreateMessage();
                outmsg.Write((byte)Packets.AddBlock);
                outmsg.Write(layer);
                outmsg.Write(blockX);
                outmsg.Write(blockY);
                outmsg.Write(blockID);
                outmsg.Write(add);
                server.SendMessage(outmsg, server.Connections, NetDeliveryMethod.ReliableUnordered, 0);
            }
        }

        public void SendChangeBlockDurability(int layer, int blockX, int blockY, float blockDurability)
        {
            if (server.ConnectionsCount > 0)
            {
                NetOutgoingMessage outmsg = server.CreateMessage();
                outmsg.Write((byte)Packets.ChangeBlockDurability);
                outmsg.Write(layer);
                outmsg.Write(blockX);
                outmsg.Write(blockY);
                outmsg.Write(blockDurability);
                server.SendMessage(outmsg, server.Connections, NetDeliveryMethod.ReliableSequenced, 0);
            }
        }

        private void SendNewPlayer(Player player, int id)
        {
            if (server.ConnectionsCount > 0)
            {
                NetOutgoingMessage outmsg = server.CreateMessage();
                outmsg.Write((byte)Packets.NewPlayer);
                outmsg.Write(id);
                outmsg.Write(player.DrawColor.R);
                outmsg.Write(player.DrawColor.G);
                outmsg.Write(player.DrawColor.B);
                //outmsg.Write(player.HP);
                //outmsg.Write(player.MaxHP);
                outmsg.WriteAllProperties(player);
                outmsg.Write(player.X);
                outmsg.Write(player.Y);
                outmsg.Write(player.DX);
                outmsg.Write(player.DY);
                outmsg.Write(player.PlayerTeam.Name);
                outmsg.Write(player.PlayerTeam.TeamColor.R);
                outmsg.Write(player.PlayerTeam.TeamColor.G);
                outmsg.Write(player.PlayerTeam.TeamColor.B);
                server.SendMessage(outmsg, server.Connections, NetDeliveryMethod.ReliableUnordered, 0);
            }
        }

        private void SendRemovePlayer(int id)
        {
            if (server.ConnectionsCount > 0)
            {
                NetOutgoingMessage outmsg = server.CreateMessage();
                outmsg.Write((byte)Packets.RemovePlayer);
                outmsg.Write(id);
                server.SendMessage(outmsg, server.Connections, NetDeliveryMethod.ReliableUnordered, 0);
            }
        }

        private void SendNewMob(Mob mob, int id)
        {
            if (server.ConnectionsCount > 0)
            {
                NetOutgoingMessage outmsg = server.CreateMessage();
                outmsg.Write((byte)Packets.NewMob);
                outmsg.Write(mob.MobId);
                outmsg.Write(id);
                outmsg.WriteAllProperties(mob);
                outmsg.Write(mob.X);
                outmsg.Write(mob.Y);
                outmsg.Write(mob.DX);
                outmsg.Write(mob.DY);
                server.SendMessage(outmsg, server.Connections, NetDeliveryMethod.ReliableUnordered, 0);
            }
        }

        public void SendRemoveMob(int id, List<ItemDrop> drops)
        {
            foreach (ItemDrop drop in drops)
            {
                foreach (KeyValuePair<int, ItemDrop> entry in dropIndex.ToArray())
                {
                    if (drop == entry.Value)
                        SendNewDrop(entry.Key, drop);
                }
            }
            if (server.ConnectionsCount > 0)
            {
                NetOutgoingMessage outmsg = server.CreateMessage();
                outmsg.Write((byte)Packets.RemoveMob);
                outmsg.Write(id);
                server.SendMessage(outmsg, server.Connections, NetDeliveryMethod.ReliableUnordered, 0);
            }
        }

        public void SendNewDrop(int id, ItemDrop drop)
        {
            if (server.ConnectionsCount > 0)
            {
                NetOutgoingMessage outmsg = server.CreateMessage();
                outmsg.Write((byte)Packets.NewItemDrop);
                outmsg.Write(id);
                outmsg.Write((int)drop.Name);
                outmsg.Write(drop.Count);
                outmsg.Write(drop.X);
                outmsg.Write(drop.Y);
                outmsg.Write(drop.DX);
                outmsg.Write(drop.DY);
                server.SendMessage(outmsg, server.Connections, NetDeliveryMethod.ReliableUnordered, 0);
            }
        }

        public void SendModifyDrop(int id, int count)
        {
            if (server.ConnectionsCount > 0)
            {
                NetOutgoingMessage outmsg = server.CreateMessage();
                outmsg.Write((byte)Packets.ModifyItemDrop);
                outmsg.Write(id);
                outmsg.Write(count);
                server.SendMessage(outmsg, server.Connections, NetDeliveryMethod.ReliableUnordered, 0);
            }
        }

        public void SendRemoveDrop(int id)
        {
            if (server.ConnectionsCount > 0)
            {
                NetOutgoingMessage outmsg = server.CreateMessage();
                outmsg.Write((byte)Packets.RemoveItemDrop);
                outmsg.Write(id);
                server.SendMessage(outmsg, server.Connections, NetDeliveryMethod.ReliableUnordered, 0);
            }
        }

        public void SendRemoveFallingBlock(int id)
        {
            if (server.ConnectionsCount > 0)
            {
                NetOutgoingMessage outmsg = server.CreateMessage();
                outmsg.Write((byte)Packets.RemoveFallingBlock);
                outmsg.Write(id);
                server.SendMessage(outmsg, server.Connections, NetDeliveryMethod.ReliableUnordered, 0);
            }
        }

        public void SendItemChange(NetConnection connection, Player player)
        {
            NetOutgoingMessage outmsg = server.CreateMessage();
            outmsg.Write((byte)Packets.ItemChange);
            outmsg.Write(player.Items.Length);
            for (int i = 0; i < player.Items.Length; i++)
            {
                outmsg.Write((int)player.Items[i].Name);
                outmsg.Write(player.Items[i].Count);
            }
            server.SendMessage(outmsg, connection, NetDeliveryMethod.ReliableUnordered, 0);
        }

        public void SendHitPlayer(int id, int damage, float playerDX, float playerDY)
        {
            if (server.ConnectionsCount > 0)
            {
                NetOutgoingMessage outmsg = server.CreateMessage();
                outmsg.Write((byte)Packets.HitPlayer);
                outmsg.Write(id);
                outmsg.Write(damage);
                outmsg.Write(playerDX);
                outmsg.Write(playerDY);
                server.SendMessage(outmsg, server.Connections, NetDeliveryMethod.ReliableUnordered, 0);
            }
        }

        public void SendHitMob(int id, int damage)
        {
            if (server.ConnectionsCount > 0)
            {
                NetOutgoingMessage outmsg = server.CreateMessage();
                outmsg.Write((byte)Packets.HitMob);
                outmsg.Write(id);
                outmsg.Write(damage);
                server.SendMessage(outmsg, server.Connections, NetDeliveryMethod.ReliableUnordered, 0);
            }
        }

        private void SendChat(string message, string nickname)
        {
            if (server.ConnectionsCount > 0)
            {
                NetOutgoingMessage outmsg = server.CreateMessage();
                outmsg.Write((byte)Packets.Chat);
                outmsg.Write(message);
                outmsg.Write(nickname);
                outmsg.Write(-1);
                server.SendMessage(outmsg, server.Connections, NetDeliveryMethod.ReliableOrdered, 0);
            }
        }

        private void SendChat(string message, Player player)
        {
            if (server.ConnectionsCount > 0)
            {
                NetOutgoingMessage outmsg = server.CreateMessage();
                outmsg.Write((byte)Packets.Chat);
                outmsg.Write(message);
                outmsg.Write(player.Nickname);
                outmsg.Write(player.ID);
                server.SendMessage(outmsg, server.Connections, NetDeliveryMethod.ReliableOrdered, 0);
            }
        }

        public void SendCreateFallingBlocks(List<FallingBlock> fallingBlocks)
        {
            if (server.ConnectionsCount > 0 && fallingBlocks != null && fallingBlocks.Count > 0)
            {
                NetOutgoingMessage outmsg = server.CreateMessage();
                outmsg.Write((byte)Packets.CreateFallingBlocks);
                outmsg.Write(fallingBlocks.Count);
                foreach (FallingBlock fallingBlock in fallingBlocks)
                {
                    int id = 0;
                    foreach (KeyValuePair<int, FallingBlock> entry in fallingBlockIndex.ToArray())
                    {
                        if (entry.Key == id)
                            id++;
                    }
                    world.AddFallingBlock(id, fallingBlock);
                    outmsg.Write(id);
                    outmsg.Write((int)fallingBlock.X);
                    outmsg.Write((int)fallingBlock.Y);
                    outmsg.Write(fallingBlock.DX);
                    outmsg.Write(fallingBlock.Rotation);
                    outmsg.Write(fallingBlock.BounceAway);
                }
                server.SendMessage(outmsg, server.Connections, NetDeliveryMethod.ReliableUnordered, 0);
            }
        }

        private void SendTentState(bool redTeam)
        {
            if (server.ConnectionsCount > 0)
            {
                NetOutgoingMessage outmsg = server.CreateMessage();
                outmsg.Write((byte)Packets.TentState);
                outmsg.Write(redTeam);
                if (redTeam)
                    outmsg.Write(redTent.HasFlag);
                else
                    outmsg.Write(blueTent.HasFlag);
                server.SendMessage(outmsg, server.Connections, NetDeliveryMethod.ReliableOrdered, 2);
            }
        }

        private void SendWorldState(NetConnection connection)
        {
            if (server.ConnectionsCount > 0 || connection != null)
            {
                NetOutgoingMessage outmsg = server.CreateMessage();
                int id = 0;
                if (connection != null)
                {
                    outmsg.Write((byte)Packets.ServerInfo);
                    outmsg.Write(serverName);
                    outmsg.Write((int)world.ServerMode);
                    if (world.ServerMode == GameMode.TeamDeathmatch || world.ServerMode == GameMode.CaptureTheFlag)
                    {
                        outmsg.Write(redTickets);
                        outmsg.Write(blueTickets);
                    }
                    outmsg.Write(world.WorldMap.Name);
                    outmsg.Write(world.WorldMap.Width);
                    outmsg.Write(world.WorldMap.Height);
                    for (int layer = 0; layer < 2; layer++)
                        for (int x = 0; x < world.WorldMap.Width; x++)
                            for (int y = 0; y < world.WorldMap.Height; y++)
                            {
                                outmsg.Write(world.WorldMap.Tiles[layer, x, y].ID);
                                outmsg.Write(world.WorldMap.Tiles[layer, x, y].Durability);
                            }
                }
                else
                    outmsg.Write((byte)Packets.UpdateWorld);
                outmsg.Write((int)world.TimeRemaining.Left);
                outmsg.Write(playerIndex.Count);
                foreach (KeyValuePair<int, Player> entry in playerIndex.ToArray())
                {
                    outmsg.Write(entry.Key);
                    Player player = entry.Value;
                    if (connection != null)
                    {
                        outmsg.Write(player.DrawColor.R);
                        outmsg.Write(player.DrawColor.G);
                        outmsg.Write(player.DrawColor.B);
                        outmsg.Write(player.HP);
                        outmsg.Write(player.MaxHP);
                        outmsg.Write(player.PlayerTeam.Name);
                        outmsg.Write(player.PlayerTeam.TeamColor.R);
                        outmsg.Write(player.PlayerTeam.TeamColor.G);
                        outmsg.Write(player.PlayerTeam.TeamColor.B);
                        for (int i = 0; i < player.Items.Length; i++)
                        {
                            outmsg.Write((int)player.Items[i].Name);
                            outmsg.Write(player.Items[i].Count);
                        }
                        outmsg.WriteAllProperties(player);
                        if (connection == player.Connection)
                            id = entry.Key;
                    }
                    outmsg.Write(player.X);
                    outmsg.Write(player.Y);
                    outmsg.Write(player.DX);
                    outmsg.Write(player.DY);
                    outmsg.Write((int)player.State);
                    outmsg.Write(player.FlippedHorizontally());
                    outmsg.Write(player.Ping);
                }
                outmsg.Write(mobIndex.Count);
                foreach (KeyValuePair<int, Mob> entry in mobIndex.ToArray())
                {
                    outmsg.Write(entry.Key);
                    Mob mob = entry.Value;
                    if (connection != null)
                    {
                        outmsg.Write(mob.MobId);
                        outmsg.Write(mob.HP);
                    }
                    outmsg.Write(mob.X);
                    outmsg.Write(mob.Y);
                    outmsg.Write(mob.DX);
                    outmsg.Write(mob.DY);
                }
                outmsg.Write(dropIndex.Count);
                foreach (KeyValuePair<int, ItemDrop> entry in dropIndex.ToArray())
                {
                    outmsg.Write(entry.Key);
                    ItemDrop drop = entry.Value;
                    if (connection != null)
                    {
                        outmsg.Write((int)drop.Name);
                        outmsg.Write(drop.Count);
                    }
                    outmsg.Write(drop.X);
                    outmsg.Write(drop.Y);
                    outmsg.Write(drop.DX);
                    outmsg.Write(drop.DY);
                }
                outmsg.Write(world.FallingBlockIndex.Count);
                foreach (KeyValuePair<int, FallingBlock> entry in fallingBlockIndex.ToArray())
                {
                    outmsg.Write(entry.Key);
                    FallingBlock fallingBlock = entry.Value;
                    if (connection != null)
                    {
                        outmsg.Write(fallingBlock.ID);
                        outmsg.Write(fallingBlock.Rotation);
                        outmsg.Write(fallingBlock.BounceAway);
                    }
                    outmsg.Write(fallingBlock.X);
                    outmsg.Write(fallingBlock.Y);
                    outmsg.Write(fallingBlock.DX);
                    outmsg.Write(fallingBlock.DY);
                }
                if (world.ServerMode == GameMode.TeamDeathmatch || world.ServerMode == GameMode.CaptureTheFlag)
                {
                    outmsg.Write(true);
                    if (connection != null)
                    {
                        outmsg.Write(redTent.HasFlag);
                        outmsg.Write(blueTent.HasFlag);
                    }
                    outmsg.Write((int)redTent.X);
                    outmsg.Write((int)redTent.Y);
                    outmsg.Write((int)blueTent.X);
                    outmsg.Write((int)blueTent.Y);
                }
                else
                    outmsg.Write(false);
                if (connection != null)
                {
                    outmsg.Write(id);
                    server.SendMessage(outmsg, connection, NetDeliveryMethod.ReliableSequenced, 1);
                    // fix in v4
                    SendPlayerFlags(connection);
                }
                else if (server.ConnectionsCount > 0)
                    server.SendMessage(outmsg, server.Connections, NetDeliveryMethod.UnreliableSequenced, 0);
            }
        }

        public void Shutdown()
        {
            server.Shutdown("Server shut down.");
        }
    }
}
