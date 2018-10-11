using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace NetGameShared
{
    public class Sprite
    {
        public Rectangle Bounds { get; set; }
        public Texture2D Texture { get { return texture; } }
        public Vector2 Origin { get { return origin; } }
        public int SourceWidth { get; set; }
        public int SourceHeight { get; set; }
        public int AnimBegin { get; set; }
        public int Frame { get; set; }
        public int MaxFrames { get; set; }

        private Texture2D texture;
        private Vector2 origin;

        public Sprite(Texture2D texture)
        {
            this.texture = texture;
            origin = new Vector2(0, 0);
            Bounds = new Rectangle(0, 0, texture.Width, texture.Height);
            Frame = 0;
            MaxFrames = 1;
            SourceWidth = texture.Width;
            SourceHeight = texture.Height;
            AnimBegin = 0;
        }

        public Sprite(Texture2D texture, int maxFrames, int sourceWidth) : this(texture)
        {
            MaxFrames = maxFrames;
            SourceWidth = sourceWidth;
        }

        public Sprite(Texture2D texture, int maxFrames, int sourceWidth, int animBegin) : this(texture)
        {
            MaxFrames = maxFrames;
            SourceWidth = sourceWidth;
            AnimBegin = animBegin;
        }

        public Sprite(Texture2D texture, Vector2 origin) : this(texture)
        {
            this.origin = origin;
        }

        public Sprite(Texture2D texture, Vector2 origin, int maxFrames, int sourceWidth) : this(texture)
        {
            this.origin = origin;
            MaxFrames = maxFrames;
            SourceWidth = sourceWidth;
        }

        public void Draw(SpriteBatch spriteBatch, Vector2 position)
        {
            spriteBatch.Draw(Texture, position, new Rectangle((Frame + AnimBegin) * SourceWidth, 0, SourceWidth, Texture.Height), Color.White, 0, Origin, new Vector2(1, 1), SpriteEffects.None, 0);
        }

        public void Draw(SpriteBatch spriteBatch, Vector2 position, Color drawColor, float rotation, Vector2 origin, Vector2 scale, SpriteEffects spriteEffects, float layerDepth)
        {
            spriteBatch.Draw(Texture, position, new Rectangle((Frame + AnimBegin) * SourceWidth, 0, SourceWidth, Texture.Height), drawColor, rotation, origin, scale, spriteEffects, layerDepth);
        }

        public void DrawFrame(SpriteBatch spriteBatch, Vector2 position, int frame)
        {
            spriteBatch.Draw(Texture, position, new Rectangle((frame + AnimBegin) * SourceWidth, 0, SourceWidth, Texture.Height), Color.White, 0, Origin, new Vector2(1, 1), SpriteEffects.None, 0);
        }
    }
}
