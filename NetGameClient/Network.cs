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

namespace NetGameClient
{
    public class Network
    {
        public bool Chatting { get { return chat.Activated; } }
        public bool Connected { get; private set; }

        NetClient client;
        Chat chat;
        ConcurrentDictionary<int, Player> playerIndex;
        ConcurrentDictionary<int, Mob> mobIndex;
        ConcurrentDictionary<int, ItemDrop> dropIndex;
        ConcurrentDictionary<int, FallingBlock> fallingBlockIndex;
        Tent redTent, blueTent;
        Player myPlayer;
        World world;
        Camera camera;
        Timer updateTimer;
        Random random;
        int playerID, timeRemaining, redTickets, blueTickets;
        bool colorChanged, gameEnded;
        string gameEndMessage;

        public Network(GameWindow window)
        {
            chat = new Chat(window);
            random = new Random();
        }

        public void Start(World world, Camera camera, string ip, int port, string nickname)
        {
            Connected = false;
            this.world = world;
            this.camera = camera;
            world.SetNetwork(this);
            if (ip.Length <= 0)
                ip = "127.0.0.1";
            if (port == 0)
                port = Universal.DEFAULT_PORT;
            if (nickname.Length <= 0)
                nickname = "Player" + (random.Next() % 9999);
            NetPeerConfiguration config = new NetPeerConfiguration(Universal.ID);
            client = new NetClient(config);
            NetOutgoingMessage outmsg = client.CreateMessage();
            client.Start();
            outmsg.Write((byte)Packets.Connect);
            outmsg.Write(Universal.GAME_VERSION);
            outmsg.Write(nickname);
            client.Connect(ip, port, outmsg);
            Console.WriteLine("Connecting...");
            Connect();
            if (Connected)
            {
                Console.WriteLine("Connected!");
                updateTimer = new Timer(50);
                updateTimer.Elapsed += new ElapsedEventHandler(UpdateElapsed);
                updateTimer.Start();
            }
            else
                Console.WriteLine("Connect failed.");
        }

        private void UpdateElapsed(object sender, ElapsedEventArgs e)
        {
            CheckServerMessages();
            //Console.WriteLine("Connection status: " + (NetConnectionStatus)client.ServerConnection.Status);
        }

        public void Update()
        {
            string message = chat.Update();
            if (message != null)
            {
                if (message.Length > 0)
                    SendChat(message);
            }
            KeyboardState keyState = Keyboard.GetState();
            if (!chat.Activated && keyState.IsKeyDown(Keys.R) && !colorChanged)
            {
                SendColorChange();
                colorChanged = true;
            }
        }

        private void Connect()
        {
            while (!Connected)
            {
                Error error = (Error)CheckServerMessages();
                if (error >= 0 && error != Error.None)
                    Console.Write("ERROR " + (int)error + ": ");
                switch (error)
                {
                    case Error.VersionMismatch:
                        Console.WriteLine("Version mismatch");
                        break;
                    case Error.None:
                        Connected = true;
                        break;
                }
                if (error >= 0 && error != Error.None)
                    break;
            }

            Client.arEvent.Set();
        }

        private int CheckServerMessages()
        {
            Client.arEvent.WaitOne();

            NetIncomingMessage inmsg;

            while ((inmsg = client.ReadMessage()) != null)
            {
                switch (inmsg.MessageType)
                {
                    case NetIncomingMessageType.Data:
                        byte data = inmsg.ReadByte();
                        if (Connected)
                        {
                            switch (data)
                            { 
                                case (byte)Packets.UpdateWorld:
                                    ProcessWorldState(inmsg);
                                    break;
                                case (byte)Packets.NewPlayer:
                                    Player player = new Player();
                                    int id = inmsg.ReadInt32();
                                    Color color = new Color(inmsg.ReadByte(), inmsg.ReadByte(), inmsg.ReadByte());
                                    inmsg.ReadAllProperties(player);
                                    player.X = inmsg.ReadFloat();
                                    player.Y = inmsg.ReadFloat();
                                    player.DX = inmsg.ReadFloat();
                                    player.DY = inmsg.ReadFloat();
                                    player.DrawColor = color;
                                    player.PlayerTeam = new Team(inmsg.ReadString(), new Color(inmsg.ReadByte(), inmsg.ReadByte(), inmsg.ReadByte()));
                                    if (player.X < -50 && player.Y < -50)
                                        player.RespawnInterval.Start(5, true);
                                    playerIndex.TryAdd(id, player);
                                    CheckTeams();
                                    Console.WriteLine(player.Nickname + " of ID " + id + " joined the game.");
                                    break;
                                case (byte)Packets.RemovePlayer:
                                    int removeID = inmsg.ReadInt32();
                                    foreach (KeyValuePair<int, Player> entry in playerIndex.ToArray())
                                    {
                                        if (entry.Key == removeID)
                                        {
                                            Universal.TryDictRemove(playerIndex, entry.Key);
                                            break;
                                        }
                                    }
                                    CheckTeams();
                                    break;
                                case (byte)Packets.HitPlayer:
                                    int hitPlayerID = inmsg.ReadInt32();
                                    int damage = inmsg.ReadInt32();
                                    foreach (KeyValuePair<int, Player> entry in playerIndex.ToArray())
                                    {
                                        if (entry.Key == hitPlayerID)
                                        {
                                            Player hitPlayer = entry.Value;
                                            hitPlayer.HP -= damage;
                                            hitPlayer.DX = inmsg.ReadFloat();
                                            hitPlayer.DY = inmsg.ReadFloat();
                                            hitPlayer.ShowHPBar();
                                            world.AddEffect(ResourceManager.GetSprite("effect_hit"), entry.Value.Position, 0.05f);
                                            world.AddDamageText(entry.Value.Position, damage);
                                            break;
                                        }
                                    }
                                    break;
                                case (byte)Packets.KilledPlayer:
                                    int killerPlayerID = inmsg.ReadInt32();
                                    int killedPlayerID = inmsg.ReadInt32();
                                    foreach (KeyValuePair<int, Player> entry in playerIndex.ToArray())
                                    {
                                        if (entry.Key == killerPlayerID)
                                            entry.Value.Kills++;
                                        else if (entry.Key == killedPlayerID)
                                        {
                                            entry.Value.Deaths++;
                                            entry.Value.HP = entry.Value.MaxHP;
                                            entry.Value.RespawnInterval.Start(5, true);
                                            if (entry.Value == myPlayer)
                                                myPlayer.RemoveAllItems();
                                        }
                                    }
                                    break;
                                case (byte)Packets.ServerMove:
                                    int movedID = inmsg.ReadInt32();
                                    foreach (KeyValuePair<int, Player> entry in playerIndex.ToArray())
                                    {
                                        if (entry.Key == movedID)
                                        {
                                            Player movedPlayer = entry.Value;
                                            if (movedPlayer.X < -50 && movedPlayer.Y < -50 && movedPlayer.RespawnInterval.IsRunning)
                                            {
                                                if (entry.Value.PlayerTeam.Name == "Red Team" && redTickets > 0)
                                                    redTickets--;
                                                else if (entry.Value.PlayerTeam.Name == "Blue Team" && blueTickets > 0)
                                                    blueTickets--;
                                                movedPlayer.RespawnInterval.Reset();
                                            }
                                            movedPlayer.X = inmsg.ReadFloat();
                                            movedPlayer.Y = inmsg.ReadFloat();
                                            movedPlayer.DX = inmsg.ReadFloat();
                                            movedPlayer.DY = inmsg.ReadFloat();
                                            if (playerID == movedID)
                                                SendServerMove();
                                        }
                                    }
                                    break;
                                case (byte)Packets.NewMob:
                                    Mob mob;
                                    int mobID = inmsg.ReadInt32();
                                    InitMob(out mob, mobID);
                                    int mobIndexID = inmsg.ReadInt32();
                                    inmsg.ReadAllProperties(mob);
                                    mob.X = inmsg.ReadFloat();
                                    mob.Y = inmsg.ReadFloat();
                                    mob.DX = inmsg.ReadFloat();
                                    mob.DY = inmsg.ReadFloat();
                                    Universal.TryDictRemove(mobIndex, mobIndexID);
                                    mobIndex.TryAdd(mobIndexID, mob);
                                    break;
                                case (byte)Packets.RemoveMob:
                                    int removeMobID = inmsg.ReadInt32();
                                    foreach (KeyValuePair<int, Mob> entry in mobIndex.ToArray())
                                    {
                                        if (entry.Key == removeMobID)
                                        {
                                            Universal.TryDictRemove(mobIndex, entry.Key);
                                            break;
                                        }
                                    }
                                    break;
                                case (byte)Packets.HitMob:
                                    int hitMobID = inmsg.ReadInt32();
                                    int mobDamage = inmsg.ReadInt32();
                                    foreach (KeyValuePair<int, Mob> entry in mobIndex.ToArray())
                                    {
                                        if (entry.Key == hitMobID)
                                        {
                                            Mob hitMob = entry.Value;
                                            hitMob.HP -= mobDamage;
                                            hitMob.ShowHPBar();
                                            world.AddEffect(ResourceManager.GetSprite("effect_hit"), hitMob.Position, 0.05f);
                                            world.AddDamageText(hitMob.Position, mobDamage);
                                            break;
                                        }
                                    }
                                    break;
                                case (byte)Packets.AddBlock:
                                    int layer = inmsg.ReadInt32();
                                    int blockX = inmsg.ReadInt32();
                                    int blockY = inmsg.ReadInt32();
                                    int blockID = inmsg.ReadInt32();
                                    bool add = inmsg.ReadBoolean();
                                    world.WorldMap.ChangeTile(layer, blockX, blockY, blockID, add);
                                    break;
                                case (byte)Packets.ChangeBlockDurability:
                                    int changeLayer = inmsg.ReadInt32();
                                    int changeBlockX = inmsg.ReadInt32();
                                    int changeBlockY = inmsg.ReadInt32();
                                    float blockDurability = inmsg.ReadFloat();
                                    world.WorldMap.ChangeTileDurability(changeLayer, changeBlockX, changeBlockY, blockDurability);
                                    break;
                                case (byte)Packets.ColorChange:
                                    Player changedPlayer;
                                    if (playerIndex.TryGetValue(inmsg.ReadInt32(), out changedPlayer))
                                    {
                                        changedPlayer.DrawColor = new Color(inmsg.ReadByte(), inmsg.ReadByte(), inmsg.ReadByte());
                                        if (myPlayer == changedPlayer)
                                            colorChanged = false;
                                    }
                                    break;
                                case (byte)Packets.Chat:
                                    string chatMessage = inmsg.ReadString();
                                    chat.AddMessage(chatMessage, inmsg.ReadString());
                                    Player chatPlayer;
                                    if (playerIndex.TryGetValue(inmsg.ReadInt32(), out chatPlayer))
                                        chatPlayer.SetChatMessage(chatMessage);
                                    break;
                                case (byte)Packets.PlayerFlag:
                                    Player flagPlayer;
                                    if (playerIndex.TryGetValue(inmsg.ReadInt32(), out flagPlayer))
                                        flagPlayer.FlagIndex = inmsg.ReadInt32();
                                    break;
                                case (byte)Packets.RemoveOneItem:
                                    world.MainPlayer.Items[inmsg.ReadInt32()].RemoveOne();
                                    break;
                                case (byte)Packets.NewItemDrop:
                                    int newDropID = inmsg.ReadInt32();
                                    if (!dropIndex.ContainsKey(newDropID))
                                    {
                                        ItemDrop drop = new ItemDrop((ItemName)inmsg.ReadInt32(), inmsg.ReadInt32(), false);
                                        drop.X = inmsg.ReadFloat();
                                        drop.Y = inmsg.ReadFloat();
                                        drop.DX = inmsg.ReadFloat();
                                        drop.DY = inmsg.ReadFloat();
                                        dropIndex.TryAdd(newDropID, drop);
                                    }
                                    break;
                                case (byte)Packets.ModifyItemDrop:
                                    int modifiedDropID = inmsg.ReadInt32();
                                    foreach (KeyValuePair<int, ItemDrop> entry in dropIndex.ToArray())
                                    {
                                        if (entry.Key == modifiedDropID)
                                        {
                                            entry.Value.Count = inmsg.ReadInt32();
                                            break;
                                        }
                                    }
                                    break;
                                case (byte)Packets.RemoveItemDrop:
                                    int removedDropID = inmsg.ReadInt32();
                                    Universal.TryDictRemove(dropIndex, removedDropID);
                                    break;
                                case (byte)Packets.ItemChange:
                                    int itemsLength = inmsg.ReadInt32();
                                    for (int i = 0; i < itemsLength; i++)
                                        myPlayer.Items[i].SetItem((ItemName)inmsg.ReadInt32(), inmsg.ReadInt32(), false);
                                    break;
                                case (byte)Packets.CreateFallingBlocks:
                                    int fallingBlockCount = inmsg.ReadInt32();
                                    for (int i = 0; i < fallingBlockCount; i++)
                                    {
                                        int fallingBlockID = inmsg.ReadInt32();
                                        int fallingBlockX = inmsg.ReadInt32();
                                        int fallingBlockY = inmsg.ReadInt32();
                                        float fallingBlockDX = inmsg.ReadFloat();
                                        float fallingBlockRotation = inmsg.ReadFloat();
                                        float bounceAway = inmsg.ReadFloat();
                                        Tile tile = world.WorldMap.GetTile(0, fallingBlockX / Universal.TILE_SIZE, fallingBlockY / Universal.TILE_SIZE);
                                        if (tile != null)
                                        {
                                            Universal.TryDictRemove(fallingBlockIndex, fallingBlockID);
                                            fallingBlockIndex.TryAdd(fallingBlockID, new FallingBlock(tile.Sprite, new Vector2(fallingBlockX, fallingBlockY), fallingBlockDX, fallingBlockRotation, bounceAway));
                                            world.WorldMap.ChangeTile(0, fallingBlockX / Universal.TILE_SIZE, fallingBlockY / Universal.TILE_SIZE, -1, false);
                                        }
                                    }
                                    break;
                                case (byte)Packets.RemoveFallingBlock:
                                    int removedBlockID = inmsg.ReadInt32();
                                    Universal.TryDictRemove(fallingBlockIndex, removedBlockID);
                                    break;
                                case (byte)Packets.TentState:
                                    bool redTeam = inmsg.ReadBoolean();
                                    bool hasFlag = inmsg.ReadBoolean();
                                    if (redTeam)
                                        redTent.HasFlag = hasFlag;
                                    else
                                        blueTent.HasFlag = hasFlag;
                                    break;
                                case (byte)Packets.RestartGame:
                                    gameEnded = false;
                                    Connected = false;
                                    world.RestartGame();
                                    break;
                                case (byte)Packets.GameEnded:
                                    gameEndMessage = inmsg.ReadString();
                                    gameEnded = true;
                                    break;
                            }
                        }
                        else
                        {
                            switch (data)
                            {
                                case (byte)Packets.ServerInfo:
                                    Console.WriteLine("Building world...");
                                    Connected = false;
                                    playerIndex = world.PlayerIndex;
                                    mobIndex = world.MobIndex;
                                    dropIndex = world.DropIndex;
                                    fallingBlockIndex = world.FallingBlockIndex;
                                    redTent = world.RedTent;
                                    blueTent = world.BlueTent;
                                    world.ServerName = inmsg.ReadString();
                                    world.ServerMode = (GameMode)inmsg.ReadInt32();
                                    if (world.ServerMode == GameMode.TeamDeathmatch || world.ServerMode == GameMode.CaptureTheFlag)
                                    {
                                        redTickets = inmsg.ReadInt32();
                                        blueTickets = inmsg.ReadInt32();
                                    }
                                    world.WorldMap.Name = inmsg.ReadString();
                                    world.WorldMap.InitEmptyLevel(inmsg.ReadInt32(), inmsg.ReadInt32());
                                    for (int layer = 0; layer < 2; layer++)
                                        for (int x = 0; x < world.WorldMap.Width; x++)
                                            for (int y = 0; y < world.WorldMap.Height; y++)
                                            {
                                                world.WorldMap.ChangeTile(layer, x, y, inmsg.ReadInt32(), true);
                                                world.WorldMap.ChangeTileDurability(layer, x, y, inmsg.ReadFloat());
                                            }
                                    ProcessWorldState(inmsg);
                                    Connected = true;
                                    NetOutgoingMessage outmsg = client.CreateMessage();
                                    outmsg.Write((byte)Packets.ConnectVerified);
                                    outmsg.Write(playerID);
                                    client.SendMessage(outmsg, NetDeliveryMethod.ReliableOrdered);
                                    client.Recycle(inmsg);
                                    Client.arEvent.Set();
                                    return (int)Error.None;
                                    break;
                                case (byte)Packets.VersionMismatch:
                                    string version = inmsg.ReadString();
                                    client.Recycle(inmsg);
                                    return (int)Error.VersionMismatch;
                                    break;
                            }
                        }
                        break;
                    default:
                        Console.WriteLine("[" + inmsg.MessageType + "] " + inmsg.ReadString());
                        break;
                }
                client.Recycle(inmsg);
            }

            Client.arEvent.Set();

            return -1;
        }

        private void CheckTeams()
        {
            List<string> teams = new List<string>();
            foreach(KeyValuePair<int, Player> entry in playerIndex)
            {
                Player playerEntry = entry.Value;
                bool exists = false;
                foreach(string team in teams)
                {
                    if (team == playerEntry.PlayerTeam.Name)
                    {
                        exists = true;
                        break;
                    }
                }
                if (!exists)
                    teams.Add(playerEntry.PlayerTeam.Name);
            }
            world.TeamCount = teams.Count;
        }

        private void ProcessWorldState(NetIncomingMessage inmsg)
        {
            timeRemaining = inmsg.ReadInt32();
            int count = 0;
            count = inmsg.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                Player player;
                int id = inmsg.ReadInt32();
                if (!Connected)
                {
                    player = new Player();
                    Color color = new Color(inmsg.ReadByte(), inmsg.ReadByte(), inmsg.ReadByte());
                    player.HP = inmsg.ReadInt32();
                    player.MaxHP = inmsg.ReadInt32();
                    player.PlayerTeam = new Team(inmsg.ReadString(), new Color(inmsg.ReadByte(), inmsg.ReadByte(), inmsg.ReadByte()));
                    for (int j = 0; j < player.Items.Length; j++)
                        player.Items[j].SetItem((ItemName)inmsg.ReadInt32(), inmsg.ReadInt32(), false);
                    inmsg.ReadAllProperties(player);
                    player.X = inmsg.ReadFloat();
                    player.Y = inmsg.ReadFloat();
                    player.DX = inmsg.ReadFloat();
                    player.DY = inmsg.ReadFloat();
                    player.State = (Player.PlayerState)inmsg.ReadInt32();
                    player.SetFlipHorizontally(inmsg.ReadBoolean());
                    player.Ping = inmsg.ReadInt32();
                    player.DrawColor = color;
                    if (player.X < -50 && player.Y < -50)
                        player.RespawnInterval.Start(5, true);
                    playerIndex.TryAdd(id, player);
                    CheckTeams();
                }
                else
                {
                    float playerX = inmsg.ReadFloat();
                    float playerY = inmsg.ReadFloat();
                    float playerDX = inmsg.ReadFloat();
                    float playerDY = inmsg.ReadFloat();
                    int playerState = inmsg.ReadInt32();
                    bool playerFlip = inmsg.ReadBoolean();
                    int playerPing = inmsg.ReadInt32();
                    if (playerIndex.TryGetValue(id, out player))
                    {
                        if (playerID != id)
                        {
                            if (Math.Abs(player.X - playerX) > 4)
                                player.X -= (player.X - playerX) / 32;
                            else
                                player.X = playerX;
                            if (Math.Abs(player.Y - playerY) > 4)
                                player.Y -= (player.Y - playerY) / 32;
                            else
                                player.Y = playerY;
                            player.Motion = new Vector2(playerDX, playerDY);
                            /*if (Math.Abs(player.DX - playerDX) > 100)
                                player.DX -= (player.DX - playerDX) / 4;
                            else if (Math.Abs(player.DX - playerDX) > 20)
                                player.DX -= (player.DX - playerDX) / 16;
                            else
                                player.DX = playerDX;
                            if (Math.Abs(player.DY - playerDY) > 100)
                                player.DY -= (player.DY - playerDY) / 4;
                            else if (Math.Abs(player.DY - playerDY) > 20)
                                player.DY -= (player.DY - playerDY) / 16;
                            else
                                player.DY = playerDY;*/
                            player.State = (Player.PlayerState)playerState;
                            player.SetFlipHorizontally(playerFlip);
                        }
                        else
                        {
                            if (Math.Abs(player.X - playerX) > 64)
                                player.X = playerX;
                            if (Math.Abs(player.Y - playerY) > 256)
                                player.Y = playerY;
                            if (Math.Abs(player.DX - playerDX) > 2000f)
                                player.DX = playerDX;
                            if (Math.Abs(player.DY - playerDY) > 1500f)
                                player.DY = playerDY;
                        }
                        player.Ping = playerPing;
                    }
                }
            }
            count = inmsg.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                Mob mob;
                int id = inmsg.ReadInt32();
                if (!Connected)
                {
                    int mobID = inmsg.ReadInt32();
                    InitMob(out mob, mobID);
                    mob.HP = inmsg.ReadInt32();
                    mobIndex.TryAdd(id, mob);
                    mob.X = inmsg.ReadFloat();
                    mob.Y = inmsg.ReadFloat();
                    mob.DX = inmsg.ReadFloat();
                    mob.DY = inmsg.ReadFloat();
                }
                else
                {
                    float mobX = inmsg.ReadFloat();
                    float mobY = inmsg.ReadFloat();
                    float mobDX = inmsg.ReadFloat();
                    float mobDY = inmsg.ReadFloat();
                    if (mobIndex.TryGetValue(id, out mob))
                    {
                        mob.Motion = new Vector2(mobDX, mobDY);
                        if (Math.Abs(mob.X - mobX) > 4)
                            mob.X -= (mob.X - mobX) / 32;
                        else
                            mob.X = mobX;
                        if (Math.Abs(mob.Y - mobY) > 4)
                            mob.Y -= (mob.Y - mobY) / 32;
                        else
                            mob.Y = mobY;
                    }
                }
            }
            count = inmsg.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                ItemDrop drop;
                int id = inmsg.ReadInt32();
                if (!Connected)
                {
                    drop = new ItemDrop((ItemName)inmsg.ReadInt32(), inmsg.ReadInt32(), new Vector2(inmsg.ReadFloat(), inmsg.ReadFloat()), false);
                    dropIndex.TryAdd(id, drop);
                    drop.DX = inmsg.ReadFloat();
                    drop.DY = inmsg.ReadFloat();
                }
                else
                {
                    float dropX = inmsg.ReadFloat();
                    float dropY = inmsg.ReadFloat();
                    float dropDX = inmsg.ReadFloat();
                    float dropDY = inmsg.ReadFloat();
                    if (dropIndex.TryGetValue(id, out drop))
                    {
                        drop.Motion = new Vector2(dropDX, dropDY);
                        if (Math.Abs(drop.X - dropX) > 1)
                            drop.X -= (drop.X - dropX) / 4;
                        else
                            drop.X = dropX;
                        if (Math.Abs(drop.Y - dropY) > 1)
                            drop.Y -= (drop.Y - dropY) / 4;
                        else
                            drop.Y = dropY;
                    }
                }
            }
            count = inmsg.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                FallingBlock fallingBlock;
                int id = inmsg.ReadInt32();
                if (!Connected)
                {
                    fallingBlock = new FallingBlock(ResourceManager.GetTile(inmsg.ReadInt32()), Vector2.Zero, 0, inmsg.ReadFloat(), inmsg.ReadFloat());
                    fallingBlockIndex.TryAdd(id, fallingBlock);
                    fallingBlock.Position = new Vector2(inmsg.ReadFloat(), inmsg.ReadFloat());
                    fallingBlock.Motion = new Vector2(inmsg.ReadFloat(), inmsg.ReadFloat());
                }
                else if (fallingBlockIndex.TryGetValue(id, out fallingBlock))
                {
                    float fallingBlockX = inmsg.ReadFloat();
                    float fallingBlockY = inmsg.ReadFloat();
                    float fallingBlockDX = inmsg.ReadFloat();
                    float fallingBlockDY = inmsg.ReadFloat();
                    if (fallingBlockIndex.TryGetValue(id, out fallingBlock))
                    {
                        fallingBlock.Motion = new Vector2(fallingBlockDX, fallingBlockDY);
                        if (Math.Abs(fallingBlock.X - fallingBlockX) > 1)
                            fallingBlock.X -= (fallingBlock.X - fallingBlockX) / 4;
                        else
                            fallingBlock.X = fallingBlockX;
                        if (Math.Abs(fallingBlock.Y - fallingBlockY) > 1)
                            fallingBlock.Y -= (fallingBlock.Y - fallingBlockY) / 4;
                        else
                            fallingBlock.Y = fallingBlockY;
                    }
                }
            }
            bool teamInfo = inmsg.ReadBoolean();
            if (teamInfo)
            {
                if (!Connected)
                {
                    redTent.HasFlag = inmsg.ReadBoolean();
                    blueTent.HasFlag = inmsg.ReadBoolean();
                }
                redTent.X = inmsg.ReadInt32();
                redTent.Y = inmsg.ReadInt32();
                blueTent.X = inmsg.ReadInt32();
                blueTent.Y = inmsg.ReadInt32();
            }
            if (!Connected)
            {
                playerID = inmsg.ReadInt32();
                Player player;
                playerIndex.TryGetValue(playerID, out player);
                world.MainPlayer = player;
                world.MainPlayer.SetNetwork(this);
                myPlayer = player;
            }
        }

        private void SendColorChange()
        {
            NetOutgoingMessage outmsg = client.CreateMessage();
            outmsg.Write((byte)Packets.ColorChange);
            outmsg.Write(playerID);
            client.SendMessage(outmsg, NetDeliveryMethod.ReliableOrdered);
        }

        private void SendChat(String message)
        {
            NetOutgoingMessage outmsg = client.CreateMessage();
            outmsg.Write((byte)Packets.Chat);
            outmsg.Write(playerID);
            outmsg.Write(message);
            client.SendMessage(outmsg, NetDeliveryMethod.ReliableOrdered);
        }

        public void SendMove()
        {
            NetOutgoingMessage outmsg = client.CreateMessage();
            outmsg.Write((byte)Packets.Move);
            outmsg.Write(playerID);
            outmsg.Write(myPlayer.X);
            outmsg.Write(myPlayer.Y);
            outmsg.Write(myPlayer.DX);
            outmsg.Write(myPlayer.DY);
            outmsg.Write((int)myPlayer.State);
            outmsg.Write(myPlayer.FlippedHorizontally());
            client.SendMessage(outmsg, NetDeliveryMethod.UnreliableSequenced);
        }

        public void SendAttack()
        {
            NetOutgoingMessage outmsg = client.CreateMessage();
            outmsg.Write((byte)Packets.Attack);
            outmsg.Write(playerID);
            client.SendMessage(outmsg, NetDeliveryMethod.ReliableSequenced);
        }

        public void SendAddBlock(int index, ItemName itemName, int mouseX, int mouseY)
        {
            NetOutgoingMessage outmsg = client.CreateMessage();
            outmsg.Write((byte)Packets.AddBlock);
            outmsg.Write(playerID);
            outmsg.Write(index);
            outmsg.Write((int)itemName);
            outmsg.Write(camera.CX + mouseX);
            outmsg.Write(camera.CY + mouseY);
            client.SendMessage(outmsg, NetDeliveryMethod.ReliableUnordered);
        }

        public void SendHitBlock(bool hitting, int mouseX, int mouseY)
        {
            NetOutgoingMessage outmsg = client.CreateMessage();
            outmsg.Write((byte)Packets.HitBlock);
            outmsg.Write(playerID);
            outmsg.Write(hitting);
            outmsg.Write(camera.CX + mouseX);
            outmsg.Write(camera.CY + mouseY);
            client.SendMessage(outmsg, NetDeliveryMethod.ReliableUnordered);
        }

        public void SendPickUpItem(int index, ItemName itemName)
        {
            NetOutgoingMessage outmsg = client.CreateMessage();
            outmsg.Write((byte)Packets.PickUpItem);
            outmsg.Write(playerID);
            outmsg.Write(index);
            outmsg.Write(redTent.Selected || redTent.WinSelected);
            outmsg.Write(blueTent.Selected || blueTent.WinSelected);
            outmsg.Write((int)itemName);
            List<int> selectedDrops = new List<int>();
            foreach (KeyValuePair<int, ItemDrop> entry in dropIndex.ToArray())
                if (entry.Value.Selected)
                    selectedDrops.Add(entry.Key);
            outmsg.Write(selectedDrops.Count);
            foreach (int dropID in selectedDrops)
                outmsg.Write(dropID);
            client.SendMessage(outmsg, NetDeliveryMethod.UnreliableSequenced);
        }

        public void SendDropItem(int index)
        {
            NetOutgoingMessage outmsg = client.CreateMessage();
            outmsg.Write((byte)Packets.DropItem);
            outmsg.Write(playerID);
            outmsg.Write(index);
            client.SendMessage(outmsg, NetDeliveryMethod.ReliableUnordered);
        }

        private void SendServerMove()
        {
            NetOutgoingMessage outmsg = client.CreateMessage();
            outmsg.Write((byte)Packets.ServerMove);
            outmsg.Write(playerID);
            client.SendMessage(outmsg, NetDeliveryMethod.ReliableSequenced);
        }

        private void InitMob(out Mob mob, int id)
        {
            switch (id)
            {
                case 0:
                default:
                    mob = new Jell(false);
                    break;
            }
            mob.SetSpriteFont(Client.font);
        }

        public void Shutdown()
        {
            client.Shutdown("Client shut down.");
        }

        public void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            Client.arEvent.WaitOne();
            foreach (KeyValuePair<int, Player> entry in playerIndex.ToArray())
                entry.Value.Draw(spriteBatch, camera, entry.Value == world.MainPlayer);
            foreach (KeyValuePair<int, Mob> entry in mobIndex.ToArray())
                entry.Value.Draw(spriteBatch);
            foreach (KeyValuePair<int, ItemDrop> entry in dropIndex.ToArray())
                entry.Value.Draw(spriteBatch, Client.font);
            world.DrawObjects(spriteBatch);
            string timeString = String.Format("{0}:{1}", timeRemaining / 60, (timeRemaining % 60).ToString("00"));
            Universal.DrawStringMore(spriteBatch, Client.font, timeString, new Vector2(camera.CX + Universal.SCREEN_WIDTH / 2, camera.CY + 10), Color.White, 0, Vector2.Zero, new Vector2(2, 2), SpriteEffects.None, 0, Align.Center, true);
            if (world.ServerMode == GameMode.TeamDeathmatch || world.ServerMode == GameMode.CaptureTheFlag)
            {
                redTent.Draw(spriteBatch, Client.font);
                blueTent.Draw(spriteBatch, Client.font);
                Universal.DrawStringMore(spriteBatch, Client.font, Convert.ToString(redTickets), new Vector2(camera.CX + Universal.SCREEN_WIDTH / 2 - 100, camera.CY + 10), Color.Red, 0, Vector2.Zero, new Vector2(2, 2), SpriteEffects.None, 0, Align.Center, true);
                Universal.DrawStringMore(spriteBatch, Client.font, Convert.ToString(blueTickets), new Vector2(camera.CX + Universal.SCREEN_WIDTH / 2 + 100, camera.CY + 10), Color.Blue, 0, Vector2.Zero, new Vector2(2, 2), SpriteEffects.None, 0, Align.Center, true);
            }
            if (gameEnded)
                Universal.DrawStringMore(spriteBatch, Client.font, gameEndMessage, new Vector2(camera.X, camera.Y - 36), Color.White, 0, Vector2.Zero, new Vector2(2, 2), SpriteEffects.None, 0, Align.Center, true);
            chat.Draw(spriteBatch, camera, gameTime);

            Client.arEvent.Set();
        }
    }
}
