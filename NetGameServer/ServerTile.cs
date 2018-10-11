using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using NetGameShared;

namespace NetGameServer
{
    class ServerTile : Tile
    {
        Bitmap bmTilesheet;

        public ServerTile(Bitmap bmTilesheet, int x, int y) : base()
        {
            this.bmTilesheet = bmTilesheet;
            X = x * Universal.TILE_SIZE;
            Y = y * Universal.TILE_SIZE;
        }

        public override void SetID(int id)
        {
            if (id >= 0)
            {
                if (ID != id)
                    SetDefaultDurability(id);
                Solid = true;
                int tx = id % 16;
                int ty = id / 16;
                Color[,] colorMap = new Color[Universal.TILE_SIZE, Universal.TILE_SIZE];
                for (int x = tx; x < tx + Universal.TILE_SIZE; x++)
                    for (int y = ty; y < ty + Universal.TILE_SIZE; y++)
                        colorMap[x - tx, y - ty] = bmTilesheet.GetPixel(x, y);
                for (int i = 0; i < Universal.TILE_SIZE; i++)
                {
                    for (int j = 0; j < Universal.TILE_SIZE; j++)
                    {
                        Color color = colorMap[i, j];
                        if (HeightMap[i] == 0 && color.A != 0)
                            HeightMap[i] = j;
                        if (color.A == 0)
                            Complete = false;
                    }
                }
            }
            else
            {
                Solid = false;
                Durability = 0;
            }

            ID = id;
        }
    }
}
