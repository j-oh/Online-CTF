using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Lidgren.Network;
using NetGameShared;

namespace NetGameServer
{
    class Player : GameObject
    {
        public bool Connected { get; set; }
        public int ID { get; set; }
        public string Nickname { get; set; }
        public int HP { get; set; }
        public int MaxHP { get; set; }
        public int Ping { get; set; }
        public int Kills { get; set; }
        public int Assists { get; set; }
        public int Deaths { get; set; }
        public int FlagIndex { get; set; }
        public bool RespondMove { get; set; }
        public bool HittingBlock { get; set; }
        public bool Respawned { get; set; }
        public Team PlayerTeam { get; set; }
        public Tile HitBlock { get; set; }
        public NetConnection Connection { get; set; }
        public PlayerState State { get; set; }
        public Item[] Items { get; set; }
        public Item SelectedItem { get; set; }
        public Interval RespawnInterval { get; set; }

        public enum PlayerState { idle, moving, jumping, attacking };

        float previousX, previousY;
        float maxSpeed, maxGravity;
        Network network;
        Vector2 motion;

        public Player(int id, string nickname, int x, int y, Color color, NetConnection connection)
        {
            ID = id;
            Nickname = nickname;
            X = x;
            Y = y;
            previousX = X;
            previousY = Y;
            DrawColor = color;
            Connection = connection;
            InitVars();
        }

        public Player()
        {
            Nickname = "";
            X = 0;
            Y = 0;
            previousX = 0;
            previousY = 0;
            DrawColor = Color.White;
            Connection = null;
            Connected = false;
            InitVars();
        }

        private void InitVars()
        {
            RespawnInterval = new Interval();
            PlayerTeam = new Team("No Team", Color.Black);
            Bounds = new Rectangle(10, 8, 13, 40);
            Items = new Item[3];
            GiveDefaultItems();
            MaxHP = 50;
            HP = MaxHP;
            Ping = 0;
            Kills = 0;
            Assists = 0;
            Deaths = 0;
            maxSpeed = 400f;
            maxGravity = 1500.0f;
            RespondMove = true;
            HittingBlock = false;
            Respawned = true;
        }

        public void SetNetwork(Network network)
        {
            this.network = network;
        }

        public void SetPreviousPosition()
        {
            previousX = X;
            previousY = Y;
        }

        public void Damage(int damage)
        {
            HP -= damage;
        }

        public void DeathAction(bool justConnected)
        {
            RemoveAllItems();
            X = -100;
            Y = -100;
            DX = 0;
            DY = 0;
            SetPreviousPosition();
            HP = MaxHP;
            if (!justConnected)
                Deaths++;
            RespawnInterval.Start(5);
            Respawned = false;
        }

        private void GiveDefaultItems()
        {
            for (int i = 0; i < Items.Length; i++)
            {
                if (i == 0)
                    Items[i] = new Item(ItemName.Stone, 300, i, true);
                else if (i == 1)
                    Items[i] = new Item(ItemName.Sword, 1, i, true);
                else
                    Items[i] = new Item(ItemName.None, 0, i, true);
            }
        }

        public void RemoveAllItems()
        {
            for (int i = 0; i < Items.Length; i++)
                Items[i].SetItem(ItemName.None, 0, true);
        }

        public void Update(float msElapsed, ServerMap map, Vector2 redTentPosition, Vector2 blueTentPosition)
        {
            if (Respawned)
            {
                LimitMove(map);
                if (HittingBlock && HitBlock.ID != -1 && HitBlock != null)
                {
                    int blockX = (int)HitBlock.X / Universal.TILE_SIZE;
                    int blockY = (int)HitBlock.Y / Universal.TILE_SIZE;
                    HitBlock.Durability -= 1;
                    network.SendChangeBlockDurability(0, blockX, blockY, HitBlock.Durability);
                    if (network != null && HitBlock.Durability <= 0)
                    {
                        map.ChangeTile(0, blockX, blockY, -1, false);
                        network.SendChangeBlock(0, blockX, blockY, HitBlock.ID, false);
                        HittingBlock = false;
                        CheckFallingBlocks(map, 0, blockX - 1, blockY);
                        CheckFallingBlocks(map, 0, blockX + 1, blockY);
                        CheckFallingBlocks(map, 0, blockX, blockY - 1);
                        CheckFallingBlocks(map, 0, blockX, blockY + 1);
                    }
                }
            }
            else
            {
                RespondMove = false;
                RespawnInterval.Update(msElapsed);
                if (!RespawnInterval.IsRunning && network.CheckTeamTickets(PlayerTeam.Name))
                {
                    SetTeamPosition(redTentPosition, blueTentPosition);
                    GiveDefaultItems();
                    DX = 0;
                    DY = 0;
                    Respawned = true;
                    network.SendServerMove(ID, this);
                    network.SendItemChange(Connection, this);
                }
            }
        }

        public void SetTeamPosition(Vector2 redTentPosition, Vector2 blueTentPosition)
        {
            Vector2 offset = new Vector2(32, 32);
            if (PlayerTeam.Name == "Blue Team")
                Position = blueTentPosition - offset;
            else
                Position = redTentPosition - offset;
            SetPreviousPosition();
        }

        public void Restart()
        {
            X = -100;
            Y = -100;
            Items = new Item[3];
            for (int i = 0; i < Items.Length; i++)
            {
                if (i == 0)
                    Items[i] = new Item(ItemName.Stone, 100, i, true);
                else if (i == 1)
                    Items[i] = new Item(ItemName.Sword, 1, i, true);
                else
                    Items[i] = new Item(ItemName.None, 0, i, true);
            }
            MaxHP = 50;
            HP = MaxHP;
            Ping = 0;
            Kills = 0;
            Assists = 0;
            Deaths = 0;
            maxSpeed = 400f;
            maxGravity = 1500.0f;
            RespondMove = false;
            HittingBlock = false;
            RespawnInterval.Start(5);
            Respawned = false;
            Connected = false;
        }

        private bool CheckFallingBlocks(ServerMap map, int layer, int blockX, int blockY)
        {
            Tile tile = map.GetTile(layer, blockX, blockY);
            map.ResetChecks();
            if (tile != null && tile.ID != -1 && !map.CheckTileGrounded(layer, blockX, blockY))
            {
                map.ResetChecks();
                List<FallingBlock> fallingBlocks = map.FallAllConnected(null, ID, 0, blockX, blockY);
                map.ResetChecks();
                network.SendCreateFallingBlocks(fallingBlocks);
                return true;
            }
            return false;
        }

        private void LimitMove(ServerMap map)
        {
            if (Math.Abs(X - previousX) > Math.Abs(DX) + 1)
                X = previousX;
            else if (Math.Abs(Y - previousY) > Math.Abs(DY) + 1)
                Y = previousY;

            if (X < -Bounds.X)
            {
                X = -Bounds.X;
                DX = 0;
            }
            if (X > map.Width * Universal.TILE_SIZE - (Bounds.X + Bounds.Width))
            {
                X = map.Width * Universal.TILE_SIZE - (Bounds.X + Bounds.Width);
                DX = 0;
            }
            if (Y < -Bounds.Y)
            {
                Y = -Bounds.Y;
                DY = 0;
            }
            if (Y > map.Height * Universal.TILE_SIZE - (Bounds.Y + Bounds.Height))
            {
                Y = map.Height * Universal.TILE_SIZE - (Bounds.Y + Bounds.Height);
                DY = 0;
            }

            if (DX > maxSpeed)
                DX = maxSpeed;
            else if (DX < -maxSpeed)
                DX = -maxSpeed;
            if (DY > Math.Abs(maxGravity))
                DY = maxGravity;
            else if (DY < -maxSpeed)
                DY = -maxSpeed;
        }
    }
}
