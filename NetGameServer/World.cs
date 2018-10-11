using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using NetGameShared;

namespace NetGameServer
{
    class World
    {
        const int DEFAULT_TIME_REMAINING = 5 * 60;

        public ConcurrentDictionary<int, Player> PlayerIndex { get; private set; }
        public ConcurrentDictionary<int, Mob> MobIndex { get; private set; }
        public ConcurrentDictionary<int, ItemDrop> DropIndex { get; private set; }
        public ConcurrentDictionary<int, FallingBlock> FallingBlockIndex { get; set; }
        public Tent RedTent { get; set; }
        public Tent BlueTent { get; set; }
        public ServerMap WorldMap { get; private set; }
        public Interval TimeRemaining { get; private set; }
        public Interval RestartInterval { get; private set; }
        public GameMode ServerMode { get; private set; }
        Network network;

        public World()
        {
            PlayerIndex = new ConcurrentDictionary<int, Player>();
            MobIndex = new ConcurrentDictionary<int, Mob>();
            DropIndex = new ConcurrentDictionary<int, ItemDrop>();
            FallingBlockIndex = new ConcurrentDictionary<int, FallingBlock>();
            WorldMap = new ServerMap();
            TimeRemaining = new Interval();
            TimeRemaining.Start(DEFAULT_TIME_REMAINING);
            RestartInterval = new Interval();
            ServerMode = GameMode.CaptureTheFlag;
            RedTent = new Tent(true, WorldMap.RedTentPosition, ServerMode == GameMode.CaptureTheFlag, true);
            BlueTent = new Tent(false, WorldMap.BlueTentPosition, ServerMode == GameMode.CaptureTheFlag, true);
        }

        public void RestartGame()
        {
            foreach (KeyValuePair<int, Player> entry in PlayerIndex.ToArray())
                entry.Value.Restart();
            MobIndex.Clear();
            DropIndex.Clear();
            FallingBlockIndex.Clear();
            WorldMap.InitLevelFromRotation();
            TimeRemaining.Start(DEFAULT_TIME_REMAINING);
            RestartInterval.Reset();
            RedTent = new Tent(true, WorldMap.RedTentPosition, ServerMode == GameMode.CaptureTheFlag, true);
            BlueTent = new Tent(false, WorldMap.BlueTentPosition, ServerMode == GameMode.CaptureTheFlag, true);
        }

        public void SetNetwork(Network network)
        {
            this.network = network;
        }

        public bool AddPlayer(int id, Player player)
        {
            return PlayerIndex.TryAdd(id, player);
        }

        public bool AddMob(int id, Mob mob)
        {
            return MobIndex.TryAdd(id, mob);
        }

        public bool AddDrop(int id, ItemDrop drop)
        {
            return DropIndex.TryAdd(id, drop);
        }

        public bool AddFallingBlock(int id, FallingBlock fallingBlock)
        {
            return FallingBlockIndex.TryAdd(id, fallingBlock);
        }

        public void Update(float msElapsed)
        {
            if (RestartInterval.Left > 1 || !RestartInterval.IsRunning)
            {
                foreach (KeyValuePair<int, Player> entry in PlayerIndex.ToArray())
                {
                    Player player = entry.Value;
                    player.Update(msElapsed, WorldMap, RedTent.Position, BlueTent.Position);
                    if (player.Y >= WorldMap.Height * Universal.TILE_SIZE - Universal.TILE_SIZE * 3)
                    {
                        network.SendKilledPlayer(-1, entry.Key, player);
                        player.DeathAction(false);
                        network.SendServerMove(entry.Key, player);
                        player.RespondMove = false;
                        network.CheckWinCondition();
                    }
                }
                foreach (KeyValuePair<int, Mob> entry in MobIndex.ToArray())
                {
                    if (entry.Value.Dead)
                    {
                        foreach (ItemDrop drop in entry.Value.Drops)
                        {
                            int id = 0;
                            foreach (KeyValuePair<int, ItemDrop> dropEntry in DropIndex.ToArray())
                            {
                                if (dropEntry.Key == id)
                                    id++;
                            }
                            DropIndex.TryAdd(id, drop);
                        }
                        network.SendRemoveMob(entry.Key, entry.Value.Drops);
                        Universal.TryDictRemove(MobIndex, entry.Key);
                    }
                    else
                        entry.Value.Update(msElapsed, WorldMap);
                }
                foreach (KeyValuePair<int, ItemDrop> entry in DropIndex.ToArray())
                    entry.Value.Update(msElapsed, WorldMap);
                foreach (KeyValuePair<int, FallingBlock> entry in FallingBlockIndex.ToArray())
                {
                    FallingBlock fallingBlock = entry.Value;
                    fallingBlock.Update(msElapsed, WorldMap);
                    if (Math.Abs(fallingBlock.DY) > 70)
                    {
                        foreach (KeyValuePair<int, Mob> mobEntry in MobIndex.ToArray())
                        {
                            Mob mob = mobEntry.Value;
                            if (fallingBlock.CollideBox.Intersects(mob.CollideBox))
                            {
                                mob.Damage(2);
                                network.SendHitMob(mobEntry.Key, 2);
                            }
                        }
                        foreach (KeyValuePair<int, Player> playerEntry in PlayerIndex.ToArray())
                        {
                            Player targetPlayer = playerEntry.Value;
                            if (fallingBlock.CollideBox.Intersects(targetPlayer.CollideBox))
                            {
                                targetPlayer.Damage(2);
                                network.SendHitPlayer(playerEntry.Key, 2, targetPlayer.DX, targetPlayer.DY);
                                if (targetPlayer.HP <= 0)
                                {
                                    Player killer;
                                    if (fallingBlock.PlayerID != playerEntry.Key &&
                                        PlayerIndex.TryGetValue(fallingBlock.PlayerID, out killer) &&
                                        (targetPlayer.PlayerTeam.Name == "No Team" || killer.PlayerTeam.Name != targetPlayer.PlayerTeam.Name))
                                    {
                                        killer.Kills++;
                                        network.SendKilledPlayer(fallingBlock.PlayerID, playerEntry.Key, targetPlayer);
                                    }
                                    else
                                        network.SendKilledPlayer(-1, playerEntry.Key, targetPlayer);
                                    targetPlayer.DeathAction(false);
                                    network.SendServerMove(playerEntry.Key, targetPlayer);
                                    targetPlayer.RespondMove = false;
                                    network.CheckWinCondition();
                                }
                            }
                        }
                    }
                    if (fallingBlock.CanDestroy)
                    {
                        network.SendRemoveFallingBlock(entry.Key);
                        Universal.TryDictRemove(FallingBlockIndex, entry.Key);
                    }
                }
                if (ServerMode == GameMode.TeamDeathmatch || ServerMode == GameMode.CaptureTheFlag)
                {
                    RedTent.Update(msElapsed, WorldMap);
                    BlueTent.Update(msElapsed, WorldMap);
                }
            }

            TimeRemaining.Update(msElapsed);
            RestartInterval.Update(msElapsed);
        }
    }
}