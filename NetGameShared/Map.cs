using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace NetGameShared
{
    public class Map
    {
        public const int CHUNK_FACTOR = 10;
        protected const int DEFAULT_MAP_WIDTH = Universal.SCREEN_WIDTH / Universal.TILE_SIZE;
        protected const int DEFAULT_MAP_HEIGHT = Universal.SCREEN_HEIGHT / Universal.TILE_SIZE;
        protected const string DEFAULT_MAP_NAME = "My Map";

        public int Width { get; protected set; }
        public int Height { get; protected set; }
        public string Name { get; set; }
        public Tile[,,] Tiles { get; protected set; }
        public List<Tile>[,] SolidChunks { get; protected set; }
        protected GraphicsDeviceManager graphics;
        protected Sprite tilesheet, blockcrack;
        protected Random random;
        RenderTarget2D mapRender;
        SpriteBatch mapBatch;
        GraphicsDevice graphicsDevice;
        bool needRender;

        public Map()
        { }

        public Map(GraphicsDeviceManager graphics, string mapName)
        {
            InitVars(graphics);
            LoadLevel(mapName);
            RenderMap();
        }

        public Map(GraphicsDeviceManager graphics, int width, int height)
        {
            InitVars(graphics);
            InitEmptyLevel(width, height);
            RenderMap();
        }

        public Map(GraphicsDeviceManager graphics)
        {
            InitVars(graphics);
            InitEmptyLevel();
            RenderMap();
        }

        private void InitVars(GraphicsDeviceManager graphics)
        {
            this.graphics = graphics;
            Name = DEFAULT_MAP_NAME;
            tilesheet = ResourceManager.GetSprite("tilesheet");
            blockcrack = ResourceManager.GetSprite("blockcrack");
            random = new Random();
            graphicsDevice = graphics.GraphicsDevice;
            needRender = true;
            mapBatch = new SpriteBatch(graphicsDevice);
            ResetVars(DEFAULT_MAP_WIDTH, DEFAULT_MAP_HEIGHT);
        }

        private void ResetVars()
        {
            ResetVars(DEFAULT_MAP_WIDTH, DEFAULT_MAP_HEIGHT);
        }

        private void ResetVars(int width, int height)
        {
            Width = width;
            Height = height;
            Tiles = new Tile[2, Width, Height];
            SolidChunks = new List<Tile>[Width / CHUNK_FACTOR, Height / CHUNK_FACTOR];
            mapRender = new RenderTarget2D(
                graphicsDevice,
                Width * Universal.TILE_SIZE,
                Height * Universal.TILE_SIZE,
                false,
                graphicsDevice.PresentationParameters.BackBufferFormat,
                DepthFormat.Depth24);
        }

        public void InitEmptyLevel(int width, int height)
        {
            ResetVars(width, height);

            for (int i = 0; i < Width / CHUNK_FACTOR; i++)
                for (int j = 0; j < Height / CHUNK_FACTOR; j++)
                    SolidChunks[i, j] = new List<Tile>();
            for (int layer = 0; layer < 2; layer++)
                for (int x = 0; x < Width; x++)
                    for (int y = 0; y < Height; y++)
                        Tiles[layer, x, y] = new Tile(graphics, tilesheet, new Vector2(x, y));
        }

        public void ResetChecks()
        {
            for (int layer = 0; layer < 2; layer++)
                for (int x = 0; x < Width; x++)
                    for (int y = 0; y < Height; y++)
                        Tiles[layer, x, y].Checked = false;
        }

        public bool CheckTileGrounded(int layer, int x, int y)
        {
            if (layer >= 0 && layer <= 2 && x >= 0 && x <= Width - 1 && y >= 0 && y <= Height - 1)
            {
                Tile tile = Tiles[layer, x, y];
                if (tile.Checked || tile.ID == -1)
                    return false;
                tile.Checked = true;
                if (x == 0 || x == Width - 1 || y == 0 || y == Height - 1)
                    return true;
                else
                    return CheckTileGrounded(layer, x - 1, y) || CheckTileGrounded(layer, x + 1, y) ||
                            CheckTileGrounded(layer, x, y - 1) || CheckTileGrounded(layer, x, y + 1);
            }
            return false;
        }

        public List<FallingBlock> FallAllConnected(List<FallingBlock> fallingBlocks, int playerID, int layer, int x, int y)
        {
            if (fallingBlocks == null)
                fallingBlocks = new List<FallingBlock>();
            if (layer >= 0 && layer <= 2 && x >= 0 && x <= Width - 1 && y >= 0 && y <= Height - 1)
            {
                Tile tile = Tiles[layer, x, y];
                if (tile.Checked || tile.ID == -1)
                    return fallingBlocks;
                else
                {
                    tile.Checked = true;
                    FallingBlock fallingBlock = new FallingBlock(tile.ID, playerID, new Vector2(tile.X + Universal.TILE_SIZE / 2, tile.Y + Universal.TILE_SIZE / 2), random);
                    fallingBlocks.Add(fallingBlock);
                    ChangeTile(layer, x, y, -1, false);
                }
                fallingBlocks = FallAllConnected(fallingBlocks, playerID, layer, x - 1, y);
                fallingBlocks = FallAllConnected(fallingBlocks, playerID, layer, x + 1, y);
                fallingBlocks = FallAllConnected(fallingBlocks, playerID, layer, x, y - 1);
                fallingBlocks = FallAllConnected(fallingBlocks, playerID, layer, x, y + 1);
                return fallingBlocks;
            }
            needRender = true;
            return fallingBlocks;
        }

        public virtual bool ChangeTile(int layer, int x, int y, int id, bool add)
        {
            if (layer >= 0 && layer <= 2 && x >= 0 && x <= Width - 1 && y >= 0 && y <= Height - 1)
            {
                Tile oldTile = Tiles[layer, x, y];
                for (int i = 0; i < Width / CHUNK_FACTOR; i++)
                    for (int j = 0; j < Height / CHUNK_FACTOR; j++)
                        if (SolidChunks[i, j].Contains(oldTile))
                            SolidChunks[i, j].Remove(oldTile);
                if (add)
                {
                    Tiles[layer, x, y] = new Tile(graphics, tilesheet, new Vector2(x, y), id);
                    Tile newTile = Tiles[layer, x, y];
                    if (layer == 0 && id >= 0)
                        AddSolidTile(layer, x, y);
                    needRender = true;
                    return (oldTile.ID != newTile.ID);
                }
                else
                {
                    oldTile.SetID(-1);
                    needRender = true;
                    return true;
                }
            }
            return false;
        }

        public virtual void ChangeTileDurability(int layer, int x, int y, float durability)
        {
            if (layer >= 0 && layer <= 2 && x >= 0 && x <= Width - 1 && y >= 0 && y <= Height - 1)
            {
                Tiles[layer, x, y].Durability = durability;
                needRender = true;
            }
        }

        public Tile GetTile(int layer, int x, int y)
        {
            if (layer >= 0 && layer <= 2 && x >= 0 && x <= Width - 1 && y >= 0 && y <= Height - 1)
                return Tiles[layer, x, y];
            else
                return null;
        }

        public void AddSolids()
        {
            SolidChunks = new List<Tile>[Width / CHUNK_FACTOR, Height / CHUNK_FACTOR];
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                {
                    int chunkX = x / CHUNK_FACTOR;
                    int chunkY = y / CHUNK_FACTOR;
                    if (chunkX >= SolidChunks.GetLength(0))
                        chunkX--;
                    if (chunkY >= SolidChunks.GetLength(1))
                        chunkY--;
                    if (SolidChunks[chunkX, chunkY] == null)
                        SolidChunks[chunkX, chunkY] = new List<Tile>();
                    AddSolidTile(0, x, y);
                }
        }

        protected void AddSolidTile(int layer, int x, int y)
        {
            if (layer == 0 && Tiles[layer, x, y].Solid)
            {
                int chunkX = x / CHUNK_FACTOR;
                int chunkY = y / CHUNK_FACTOR;
                if (chunkX >= SolidChunks.GetLength(0))
                    chunkX--;
                if (chunkY >= SolidChunks.GetLength(1))
                    chunkY--;
                SolidChunks[chunkX, chunkY].Add(Tiles[layer, x, y]);
                if (chunkX - 1 >= 0)
                    SolidChunks[chunkX - 1, chunkY].Add(Tiles[layer, x, y]);
                if (chunkX + 1 < Width / CHUNK_FACTOR)
                    SolidChunks[chunkX + 1, chunkY].Add(Tiles[layer, x, y]);
                if (chunkY - 1 >= 0)
                    SolidChunks[chunkX, chunkY - 1].Add(Tiles[layer, x, y]);
                if (chunkY + 1 < Height / CHUNK_FACTOR)
                    SolidChunks[chunkX, chunkY + 1].Add(Tiles[layer, x, y]);
            }
        }

        protected virtual void InitEmptyLevel()
        {
            ResetVars();

            for (int i = 0; i < Width / CHUNK_FACTOR; i++)
                for (int j = 0; j < Height / CHUNK_FACTOR; j++)
                    SolidChunks[i, j] = new List<Tile>();
            for (int layer = 0; layer < 2; layer++)
                for (int x = 0; x < Width; x++)
                    for (int y = 0; y < Height; y++)
                        Tiles[layer, x, y] = new Tile(graphics, tilesheet, new Vector2(x, y));
        }

        public virtual void InitBasicLevel()
        {
            ResetVars();

            for (int i = 0; i < Width / CHUNK_FACTOR; i++)
                for (int j = 0; j < Height / CHUNK_FACTOR; j++)
                    SolidChunks[i, j] = new List<Tile>();
            for (int layer = 0; layer < 2; layer++)
                for (int x = 0; x < Width; x++)
                    for (int y = 0; y < Height; y++)
                    {
                        Tiles[layer, x, y] = new Tile(graphics, tilesheet, new Vector2(x, y));
                        Tile tile = Tiles[layer, x, y];
                        if (layer == 0 && y > 24)
                            tile.SetID(0);
                        AddSolidTile(layer, x, y);
                    }
        }

        public void LoadLevel(string mapName)
        {
            try
            {
                using (FileStream fileStream = new FileStream(String.Format("Content/Maps/{0}.map", mapName), FileMode.OpenOrCreate))
                {
                    fileStream.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public void CheckRender()
        {
            if (needRender)
            {
                RenderMap();
                needRender = false;
            }
        }

        private void RenderMap()
        {
            graphicsDevice.SetRenderTarget(mapRender);
            graphicsDevice.DepthStencilState = new DepthStencilState() { DepthBufferEnable = true };
            graphicsDevice.Clear(Color.Transparent);

            mapBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, null);

            for (int layer = 0; layer < 2; layer++)
            {
                for (int x = 0; x < Width; x++)
                {
                    for (int y = 0; y < Height; y++)
                    {
                        Tile tile = Tiles[layer, x, y];
                        tile.Draw(mapBatch);
                        //mapBatch.Draw(tilesheet.Texture, tile.Position, new Rectangle(tile.ID % 16, tile.ID / 16, 16, 16), Color.White);
                        if (tile.ID >= 0 && tile.MaxDurability > 0)
                        {
                            int frame = 4 - (int)((tile.Durability / tile.MaxDurability) * 5);
                            blockcrack.DrawFrame(mapBatch, tile.Position, frame);
                        }
                    }
                }
            }

            mapBatch.End();

            graphicsDevice.SetRenderTarget(null);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            /*DrawLayer(spriteBatch, 0);
            DrawLayer(spriteBatch, 1);*/
            spriteBatch.Draw(mapRender, new Vector2(0, 0), Color.White);
        }

        private void DrawLayer(SpriteBatch spriteBatch, int layer)
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    Tile tile = Tiles[layer, x, y];
                    //tile.Draw(spriteBatch);
                    //spriteBatch.Draw(tilesheet.Texture, tile.Position, new Rectangle(tile.ID % 16, tile.ID / 16, 16, 16), Color.White);
                    if (tile.ID >= 0 && tile.MaxDurability > 0)
                    {
                        int frame = 4 - (int)((tile.Durability / tile.MaxDurability) * 5);
                        blockcrack.DrawFrame(spriteBatch, tile.Position, frame);
                        //blockcrack.Draw(spriteBatch, tile.Position);
                    }
                }
            }
        }
    }
}
