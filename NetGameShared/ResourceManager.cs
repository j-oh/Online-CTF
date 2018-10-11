using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace NetGameShared
{
    public class ResourceManager
    {
        private static Dictionary<string, Sprite> sprites;
        private static Dictionary<string, SpriteFont> fonts;
        private static Dictionary<int, Sprite> tiles;

        public ResourceManager()
        {
            sprites = new Dictionary<string, Sprite>();
            fonts = new Dictionary<string, SpriteFont>();
            tiles = new Dictionary<int, Sprite>();
        }

        public void LoadResources(ContentManager content, GraphicsDeviceManager graphics)
        {
            sprites.Add("player", new Sprite(content.Load<Texture2D>("Content/Sprites/player"), 4, 32));
            sprites.Add("player_run", new Sprite(content.Load<Texture2D>("Content/Sprites/player_run"), 5, 32));
            sprites.Add("player_jump", new Sprite(content.Load<Texture2D>("Content/Sprites/player_jump"), 5, 32));
            sprites.Add("player_attack", new Sprite(content.Load<Texture2D>("Content/Sprites/player_attack"), 3, 32));
            sprites.Add("effect_slash", new Sprite(content.Load<Texture2D>("Content/Sprites/slash"), 2, 32));
            sprites.Add("effect_hit", new Sprite(content.Load<Texture2D>("Content/Sprites/hit"), 5, 64));
            sprites.Add("itemsquare", new Sprite(content.Load<Texture2D>("Content/Sprites/itemsquare"), 2, 32));
            sprites.Add("itemborder", new Sprite(content.Load<Texture2D>("Content/Sprites/itemborder")));
            sprites.Add("item_sword", new Sprite(content.Load<Texture2D>("Content/Sprites/item_sword")));
            sprites.Add("item_redflag", new Sprite(content.Load<Texture2D>("Content/Sprites/item_redflag")));
            sprites.Add("item_blueflag", new Sprite(content.Load<Texture2D>("Content/Sprites/item_blueflag")));
            sprites.Add("mob_jell", new Sprite(content.Load<Texture2D>("Content/Sprites/jell"), 2, 32));
            sprites.Add("menu_bar", new Sprite(content.Load<Texture2D>("Content/Sprites/menu_bar")));
            sprites.Add("button_connect", new Sprite(content.Load<Texture2D>("Content/Sprites/button_connect")));
            sprites.Add("button_options", new Sprite(content.Load<Texture2D>("Content/Sprites/button_options")));
            sprites.Add("redtent", new Sprite(content.Load<Texture2D>("Content/Sprites/redtent"), 2, 64));
            sprites.Add("bluetent", new Sprite(content.Load<Texture2D>("Content/Sprites/bluetent"), 2, 64));
            sprites.Add("cursor", new Sprite(content.Load<Texture2D>("Content/Sprites/cursor")));
            sprites.Add("pixel", new Sprite(content.Load<Texture2D>("Content/Sprites/pixel")));
            sprites.Add("blockcrack", new Sprite(content.Load<Texture2D>("Content/Sprites/blockcrack"), 5, 16));
            sprites.Add("blockoutline", new Sprite(content.Load<Texture2D>("Content/Sprites/blockoutline")));
            sprites.Add("sky", new Sprite(content.Load<Texture2D>("Content/Sprites/sky")));
            sprites.Add("tilesheet", new Sprite(content.Load<Texture2D>("Content/Maps/tilesheet")));

            fonts.Add("font", content.Load<SpriteFont>("Content/Fonts/font"));

            Sprite tilesheet = GetSprite("tilesheet");
            int tileRows = tilesheet.SourceHeight / Universal.TILE_SIZE;
            int tileColumns = tilesheet.SourceWidth / Universal.TILE_SIZE;
            int ID = 0;
            for (int y = 0; y < tileRows; y++)
            {
                for (int x = 0; x < tileColumns; x++)
                {
                    Texture2D texture = new Texture2D(graphics.GraphicsDevice, Universal.TILE_SIZE, Universal.TILE_SIZE);
                    Color[] data = new Color[Universal.TILE_SIZE * Universal.TILE_SIZE];
                    int tx = ID % tileRows * Universal.TILE_SIZE;
                    int ty = ID / tileColumns * Universal.TILE_SIZE;
                    tilesheet.Texture.GetData(0, new Rectangle(tx, ty, Universal.TILE_SIZE, Universal.TILE_SIZE), data, 0, data.Length);
                    texture.SetData(data);
                    tiles.Add(ID, new Sprite(texture));
                    ID++;
                }
            }
        }

        public static Sprite GetSprite(string name)
        {
            if (sprites.ContainsKey(name))
                return sprites[name];
            else
                return null;
        }

        public static SpriteFont GetFont(string name)
        {
            if (fonts.ContainsKey(name))
                return fonts[name];
            else
                return null;
        }

        public static Sprite GetTile(int ID)
        {
            if (tiles.ContainsKey(ID))
                return tiles[ID];
            else
                return null;
        }
    }
}
