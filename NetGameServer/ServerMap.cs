using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using NetGameShared;

namespace NetGameServer
{
    class ServerMap : Map
    {
        public Vector2 RedTentPosition { get; private set; }
        public Vector2 BlueTentPosition { get; private set; }

        List<string> mapNames;
        Bitmap bmTilesheet;

        public ServerMap()
        {
            mapNames = new List<string>();
            bmTilesheet = new Bitmap("Content/Maps/tilesheet.png");
            random = new Random();
            mapNames.Add("islands");
            mapNames.Add("bridges");
            mapNames.Add("pillars");
            mapNames.Add("circle");
            mapNames.Add("ufo");
            mapNames.Add("tunnel");
            //mapNames.Add("stresstest");
            //mapNames.Add("");
            //mapNames.Add("test");
            InitLevelFromRotation();
        }

        private void InitMapVars()
        {
            Tiles = new Tile[2, Width, Height];
            SolidChunks = new List<Tile>[Width / CHUNK_FACTOR, Height / CHUNK_FACTOR];
            RedTentPosition = new Vector2(Universal.TILE_SIZE * 6, 0);
            BlueTentPosition = new Vector2(Width * Universal.TILE_SIZE - Universal.TILE_SIZE * 8, 0);
        }

        protected override void InitEmptyLevel()
        {
            Width = DEFAULT_MAP_WIDTH;
            Height = DEFAULT_MAP_HEIGHT;
            InitMapVars();

            for (int i = 0; i < Width / CHUNK_FACTOR; i++)
                for (int j = 0; j < Height / CHUNK_FACTOR; j++)
                    SolidChunks[i, j] = new List<Tile>();
            for (int layer = 0; layer < 2; layer++)
                for (int x = 0; x < Width; x++)
                    for (int y = 0; y < Height; y++)
                        Tiles[layer, x, y] = new ServerTile(bmTilesheet, x, y);
        }

        public override void InitBasicLevel()
        {
            bool valley = random.Next(2) < 1;
            Width = 120 + (int)random.Next(2) * 10;
            Height = 60;
            InitMapVars();

            for (int i = 0; i < Width / CHUNK_FACTOR; i++)
                for (int j = 0; j < Height / CHUNK_FACTOR; j++)
                    SolidChunks[i, j] = new List<Tile>();
            for (int layer = 0; layer < 2; layer++)
            {
                int heightLimit = (int)(Height * 0.6);
                if (!valley)
                    heightLimit = (int)(Height * 0.8) - heightLimit;
                for (int x = 0; x < Width; x++)
                {
                    if (random.Next(3) < 2)
                    {
                        if (x < Width * 0.25 || (x > Width * 0.5 && x < Width * 0.75))
                        {
                            if (valley)
                                heightLimit--;
                            else
                                heightLimit++;
                        }
                        else if ((x > Width * 0.25 && x < Width * 0.5) || x > Width * 0.75)
                        {
                            if (valley)
                                heightLimit++;
                            else
                                heightLimit--;
                        }
                    }
                    for (int y = 0; y < Height; y++)
                    {
                        Tiles[layer, x, y] = new ServerTile(bmTilesheet, x, y);
                        Tile tile = Tiles[layer, x, y];
                        if (layer == 0 && y > (Height - heightLimit))
                            tile.SetID(0);
                        AddSolidTile(layer, x, y);
                    }
                }
            }
        }

        private void InitLevelFromFile(string levelName)
        {
            Bitmap levelFile = new Bitmap("Content/Maps/" + levelName + ".png");
            Width = levelFile.Width;
            Height = levelFile.Height;
            InitMapVars();

            for (int i = 0; i < Width / CHUNK_FACTOR; i++)
                for (int j = 0; j < Height / CHUNK_FACTOR; j++)
                    SolidChunks[i, j] = new List<Tile>();
            for (int layer = 0; layer < 2; layer++)
                for (int x = 0; x < Width; x++)
                    for (int y = 0; y < Height; y++)
                    {
                        System.Drawing.Color pixelColor = levelFile.GetPixel(x, y);
                        Tiles[layer, x, y] = new ServerTile(bmTilesheet, x, y);
                        Tile tile = Tiles[layer, x, y];
                        if (layer == 0)
                        {
                            if (pixelColor == System.Drawing.Color.FromArgb(128, 64, 0))
                                tile.SetID(0);
                            else if (pixelColor == System.Drawing.Color.FromArgb(128, 128, 128))
                                tile.SetID(3);
                            else if (pixelColor == System.Drawing.Color.FromArgb(0, 0, 0))
                                tile.SetID(4);
                            else if (pixelColor == System.Drawing.Color.FromArgb(255, 0, 0))
                                RedTentPosition = new Vector2(x * Universal.TILE_SIZE, y * Universal.TILE_SIZE);
                            else if (pixelColor == System.Drawing.Color.FromArgb(0, 0, 255))
                                BlueTentPosition = new Vector2(x * Universal.TILE_SIZE, y * Universal.TILE_SIZE);
                        }
                        AddSolidTile(layer, x, y);
                    }
        }

        public void InitLevelFromRotation()
        {
            int levelNumber = random.Next(mapNames.Count);
            string levelName = mapNames[levelNumber];
            if (mapNames.Count < 0 || levelName.Length <= 0)
            {
                InitBasicLevel();
                Name = DEFAULT_MAP_NAME;
            }
            else
            {
                InitLevelFromFile(levelName);
                Name = levelName;
            }
        }

        public override bool ChangeTile(int layer, int x, int y, int id, bool add)
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
                    Tile newTile = new ServerTile(bmTilesheet, x, y);
                    newTile.SetID(id);
                    Tiles[layer, x, y] = newTile;
                    AddSolidTile(layer, x, y);
                    return (oldTile.ID != newTile.ID);
                }
                else
                {
                    oldTile.SetID(-1);
                    return true;
                }
            }
            return false;
        }
    }
}
