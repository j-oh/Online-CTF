using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace NetGameShared
{
    public class Tile : GameObject
    {
        public int ID { get; protected set; }
        public int[] HeightMap { get; protected set; }
        public float Durability { get; set; }
        public float MaxDurability { get; protected set; }
        public bool Checked { get; set; }
        public bool Solid { get; set; }
        public bool Complete { get; protected set; }
        GraphicsDeviceManager graphics;
        Sprite tilesheet;

        public Tile()
        {
            InitVars();
        }

        public Tile(GraphicsDeviceManager graphics, Sprite tilesheet, Vector2 position)
        {
            InitVars(graphics, tilesheet, position);
        }

        public Tile(GraphicsDeviceManager graphics, Sprite tilesheet, Vector2 position, int id)
        {
            InitVars(graphics, tilesheet, position);
            SetID(id);
        }

        private void InitVars(GraphicsDeviceManager graphics, Sprite tilesheet, Vector2 position)
        {
            this.graphics = graphics;
            this.tilesheet = tilesheet;
            Position = new Vector2(position.X * Universal.TILE_SIZE, position.Y * Universal.TILE_SIZE);
            InitVars();
        }

        private void InitVars()
        {
            ID = -1;
            if (ID >= 0)
            {
                Solid = true;
                SetDefaultDurability(ID);
            }
            else
                Solid = false;
            BoundWidth = Universal.TILE_SIZE;
            BoundHeight = Universal.TILE_SIZE;
            Complete = true;
            HeightMap = new int[Universal.TILE_SIZE];
        }

        protected void SetDefaultDurability(int id)
        {
            switch (id)
            {
                case 0:
                default:
                    Durability = 10;
                    break;
                case 3:
                    Durability = 50;
                    break;
                case 4:
                    Durability = 1000000;
                    break;
            }
            MaxDurability = Durability;
        }

        public virtual void SetID(int id)
        {
            if (id >= 0)
            {
                sprite = ResourceManager.GetTile(id);

                if (ID != id)
                    SetDefaultDurability(id);

                /*Color[] bits = new Color[sprite.Texture.Width * sprite.Texture.Height];
                sprite.Texture.GetData(bits);

                for (int i = 0; i < sprite.Texture.Width; i++)
                {
                    for (int j = 0; j < sprite.Texture.Height; j++)
                    {
                        Color color = bits[i + j * sprite.Texture.Width];
                        if (HeightMap[i] == 0 && color.A != 0)
                            HeightMap[i] = j;
                        if (color.A == 0)
                            Complete = false;
                    }
                }*/

                Solid = true;
            }
            else
            {
                sprite = null;
                Solid = false;
                Durability = 0;
            }

            ID = id;
        }
    }
}
