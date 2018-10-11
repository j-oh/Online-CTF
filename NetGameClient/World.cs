using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using NetGameShared;

namespace NetGameClient
{
    public class World
    {
        public ConcurrentDictionary<int, Player> PlayerIndex { get; private set; }
        public ConcurrentDictionary<int, Mob> MobIndex { get; private set; }
        public ConcurrentDictionary<int, ItemDrop> DropIndex { get; private set; }
        public ConcurrentDictionary<int, FallingBlock> FallingBlockIndex { get; set; }
        public Player MainPlayer { get; set; }
        public Tent RedTent { get; set; }
        public Tent BlueTent { get; set; }
        public Map WorldMap { get; private set; }
        public GameMode ServerMode { get; set; }
        public string ServerName { get; set; }
        public string MapName { get { return WorldMap.Name; } }
        public bool DrawBlockOutline { get; private set; }
        public int TeamCount { get; set; }
        List<GameObject> objectList;
        Network network;
        Camera camera;
        Sprite sky;
        int lastBlockX, lastBlockY;
        bool online;

        public World(GraphicsDeviceManager graphics, Camera camera, bool online)
        {
            PlayerIndex = new ConcurrentDictionary<int, Player>();
            MobIndex = new ConcurrentDictionary<int, Mob>();
            DropIndex = new ConcurrentDictionary<int, ItemDrop>();
            FallingBlockIndex = new ConcurrentDictionary<int, FallingBlock>();
            WorldMap = new Map(graphics);
            RedTent = new Tent(true, Vector2.Zero, false, false);
            BlueTent = new Tent(false, Vector2.Zero, false, false);
            sky = ResourceManager.GetSprite("sky");
            objectList = new List<GameObject>();
            lastBlockX = 0;
            lastBlockY = 0;
            DrawBlockOutline = false;
            this.camera = camera;
            this.online = online;
        }

        public void RestartGame()
        {
            PlayerIndex.Clear();
            MobIndex.Clear();
            DropIndex.Clear();
            FallingBlockIndex.Clear();
            RedTent = new Tent(true, Vector2.Zero, false, false);
            BlueTent = new Tent(false, Vector2.Zero, false, false);
        }

        public void Update(GameTime gameTime, KeyboardState lastKeyState, MouseState lastMouseState, bool chatting)
        {
            MouseState mouseState = Mouse.GetState();
            bool playerTeam = MainPlayer.PlayerTeam.Name == "Red Team";

            if (online)
            {
                foreach (KeyValuePair<int, Player> entry in PlayerIndex.ToArray())
                {
                    if (!chatting)
                        entry.Value.Update(gameTime, lastKeyState, lastMouseState, DrawBlockOutline, entry.Value == MainPlayer);
                    else
                        entry.Value.Update(gameTime, lastKeyState, lastMouseState, DrawBlockOutline, false);
                }
            }
            foreach (KeyValuePair<int, Mob> entry in MobIndex.ToArray())
                entry.Value.Update(gameTime, WorldMap);
            foreach (KeyValuePair<int, ItemDrop> entry in DropIndex.ToArray())
                entry.Value.Update(gameTime, WorldMap, MainPlayer.Items, new Vector2(MainPlayer.X + MainPlayer.BoundWidth, MainPlayer.Y + MainPlayer.BoundHeight), playerTeam);
            foreach (KeyValuePair<int, FallingBlock> entry in FallingBlockIndex.ToArray())
            {
                FallingBlock fallingBlock = entry.Value;
                fallingBlock.Update(gameTime, WorldMap);
                if (fallingBlock.CanDestroy)
                    Universal.TryDictRemove(FallingBlockIndex, entry.Key);
            }
            if (ServerMode == GameMode.TeamDeathmatch || ServerMode == GameMode.CaptureTheFlag)
            {
                RedTent.Update(gameTime, WorldMap, MainPlayer.Items, MainPlayer.Position, playerTeam);
                BlueTent.Update(gameTime, WorldMap, MainPlayer.Items, MainPlayer.Position, playerTeam);
            }
            foreach (GameObject gameObject in objectList.ToArray())
            {
                gameObject.Update(gameTime);
                if (gameObject.Dead)
                    objectList.Remove(gameObject);
            }

            int blockX = (camera.CX + mouseState.X) / 16;
            int blockY = (camera.CY + mouseState.Y) / 16;
            Item selectedItem = MainPlayer.SelectedItem;

            if (MainPlayer.Motion != Vector2.Zero || 
                mouseState.LeftButton != lastMouseState.LeftButton || 
                mouseState.RightButton != lastMouseState.RightButton || blockX != lastBlockX || blockY != lastBlockY)
            {
                DrawBlockOutline = false;
                if (blockX >= 0 && blockX < WorldMap.Width &&
                    blockY >= 0 && blockY < WorldMap.Height &&
                    Vector2.Distance(new Vector2(MainPlayer.X + MainPlayer.BoundWidth, MainPlayer.Y + MainPlayer.BoundHeight / 2), new Vector2(camera.CX + mouseState.X, camera.CY + mouseState.Y)) <= Universal.TILE_SIZE * Universal.PLACE_DISTANCE)
                {
                    if (mouseState.RightButton == ButtonState.Pressed && WorldMap.Tiles[0, blockX, blockY].ID >= 0)
                        DrawBlockOutline = true;
                    else if (selectedItem.Type == ItemType.Block && mouseState.RightButton == ButtonState.Released && WorldMap.Tiles[0, blockX, blockY].ID == -1)
                    {
                        if (blockX >= 0 && blockX < WorldMap.Width)
                        {
                            if (blockY - 1 >= 0 && WorldMap.Tiles[0, blockX, blockY - 1].ID >= 0)
                                DrawBlockOutline = true;
                            if (blockY + 1 < WorldMap.Height && WorldMap.Tiles[0, blockX, blockY + 1].ID >= 0)
                                DrawBlockOutline = true;
                        }
                        if (blockY >= 0 && blockY < WorldMap.Height)
                        {
                            if (blockX - 1 >= 0 && WorldMap.Tiles[0, blockX - 1, blockY].ID >= 0)
                                DrawBlockOutline = true;
                            if (blockX + 1 < WorldMap.Width && WorldMap.Tiles[0, blockX + 1, blockY].ID >= 0)
                                DrawBlockOutline = true;
                        }
                        if (blockX == 0 || blockX == WorldMap.Width - 1 ||
                            blockY == 0 || blockY == WorldMap.Height - 1)
                            DrawBlockOutline = true;
                    }
                }
            }

            if (network != null && 
                (mouseState.RightButton != lastMouseState.RightButton || blockX != lastBlockX || blockY != lastBlockY))
                network.SendHitBlock(DrawBlockOutline, mouseState.X, mouseState.Y);

            lastBlockX = blockX;
            lastBlockY = blockY;

            if (!MainPlayer.RespawnInterval.IsRunning)
                camera.Position = MainPlayer.Position;
            if (camera.X < Universal.SCREEN_WIDTH / 2)
                camera.X = Universal.SCREEN_WIDTH / 2;
            else if (camera.X > WorldMap.Width * Universal.TILE_SIZE - Universal.SCREEN_WIDTH / 2)
                camera.X = WorldMap.Width * Universal.TILE_SIZE - Universal.SCREEN_WIDTH / 2;
            if (camera.Y < Universal.SCREEN_HEIGHT / 2)
                camera.Y = Universal.SCREEN_HEIGHT / 2;
            else if (camera.Y > WorldMap.Height * Universal.TILE_SIZE - Universal.SCREEN_HEIGHT / 2)
                camera.Y = WorldMap.Height * Universal.TILE_SIZE - Universal.SCREEN_HEIGHT / 2;

            WorldMap.CheckRender();
        }

        public void SetNetwork(Network network)
        {
            this.network = network;
        }

        public void AddEffect(Sprite sprite, Vector2 position, float seconds)
        {
            Effect effect = new Effect(sprite, position, seconds);
            objectList.Add(effect);
        }

        public void AddEffect(Sprite sprite, Vector2 position, float seconds, bool flip)
        {
            Effect effect = new Effect(sprite, position, seconds);
            effect.SetFlip(flip);
            objectList.Add(effect);
        }

        public void AddDamageText(Vector2 position, int damage)
        {
            DamageText dtext = new DamageText(position, damage);
            objectList.Add(dtext);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (network.Connected)
            {
                spriteBatch.Draw(sky.Texture, new Rectangle(0, 0, WorldMap.Width * Universal.TILE_SIZE, sky.SourceHeight * 2), Color.White);
                WorldMap.Draw(spriteBatch);
                if (!online)
                {
                    MainPlayer.Draw(spriteBatch);
                    foreach (GameObject gameObject in objectList)
                        gameObject.Draw(spriteBatch);
                }
            }
        }

        public void DrawObjects(SpriteBatch spriteBatch)
        {
            foreach (KeyValuePair<int, FallingBlock> entry in FallingBlockIndex.ToArray())
                entry.Value.Draw(spriteBatch);
            foreach (GameObject gameObject in objectList)
                gameObject.Draw(spriteBatch);
        }
    }
}
