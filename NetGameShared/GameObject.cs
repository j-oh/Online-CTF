using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace NetGameShared
{
    public class GameObject
    {
        public Sprite Sprite { get { return sprite; } }
        public Rectangle CollideBox { get; set; }
        public Rectangle Bounds { get { return bounds; } set { bounds = value; CollideBox = new Rectangle(bounds.X + (int)X - (int)Origin.X, bounds.Y + (int)Y - (int)Origin.Y, bounds.Width, bounds.Height); } }
        public Vector2 Position { get; set; }
        public Vector2 Motion { get; set; }
        public Vector2 Origin { get; set; }
        public Vector2 Scale { get; set; }
        public Color DrawColor { get; set; }
        public int CollideX { get { return CollideBox.X; } set { CollideBox = new Rectangle(value, CollideY, bounds.Width, bounds.Height); } }
        public int CollideY { get { return CollideBox.Y; } set { CollideBox = new Rectangle(CollideX, value, bounds.Width, bounds.Height); } }
        public int IntX { get { return (int)X; } }
        public int IntY { get { return (int)Y; } }
        public float X { get { return Position.X; } set { Position = new Vector2(value, Position.Y); CollideX = bounds.X + (int)value; } }
        public float Y { get { return Position.Y; } set { Position = new Vector2(Position.X, value); CollideY = bounds.Y + (int)value; } }
        public float DX { get { return Motion.X; } set { Motion = new Vector2(value, Motion.Y); } }
        public float DY { get { return Motion.Y; } set { Motion = new Vector2(Motion.X, value); } }
        public int BoundWidth { get { return bounds.Width; } set { Bounds = new Rectangle(bounds.X, bounds.Y, value, bounds.Height); } }
        public int BoundHeight { get { return bounds.Width; } set { Bounds = new Rectangle(bounds.X, bounds.Y, bounds.Width, value); } }
        public float Rotation { get; set; }
        public float LayerDepth { get; set; }
        public bool Dead { get; protected set; }

        protected Sprite sprite;
        protected SpriteFont font;
        protected Rectangle bounds;
        protected SpriteEffects spriteEffects;
        protected float animSpeed;
        protected bool loopAnimation;
        private Interval animTimer;

        public GameObject()
        {
            Position = new Vector2(0, 0);
            Motion = new Vector2(0, 0);
            Origin = new Vector2(0, 0);
            Scale = new Vector2(1, 1);
            Bounds = new Rectangle(0, 0, 0, 0);
            DrawColor = Color.White;
            Rotation = 0;
            LayerDepth = 0;
            spriteEffects = SpriteEffects.None;
            animSpeed = 0.1f;
            animTimer = new Interval();
            loopAnimation = true;
            Dead = false;
        }

        public GameObject(Vector2 position) : this()
        {
            Position = position;
        }

        public GameObject(Sprite sprite, Vector2 position) : this(position)
        {
            SetSprite(sprite);
            Bounds = sprite.Bounds;
        }

        public void SetFlipHorizontally(bool flipped)
        {
            if (flipped)
                spriteEffects = SpriteEffects.FlipHorizontally;
            else
                spriteEffects = SpriteEffects.None;
        }

        public bool FlippedHorizontally()
        {
            return spriteEffects == SpriteEffects.FlipHorizontally;
        }

        public void SetSprite(Sprite sprite)
        {
            this.sprite = sprite;
        }

        public void SetSpriteResource(string name)
        {
            this.sprite = ResourceManager.GetSprite(name);
        }

        public void SetSpriteFont(SpriteFont font)
        {
            this.font = font;
        }

        protected void Animate(GameTime gameTime)
        {
            animTimer.Update(gameTime);
            if (sprite.MaxFrames > 1 && !animTimer.IsRunning)
            {
                if (animSpeed > 0)
                    sprite.Frame++;
                if (sprite.Frame >= sprite.MaxFrames)
                {
                    if (loopAnimation)
                        sprite.Frame = 0;
                    else
                        sprite.Frame = sprite.MaxFrames - 1;
                    AnimationEnd();
                }
                animTimer.Start(animSpeed);
            }
        }

        protected virtual void AnimationEnd()
        {

        }

        protected void SetAnimSpeed(float seconds)
        {
            animSpeed = seconds;
            animTimer.Start(animSpeed);
        }

        public virtual void Update()
        { }

        public virtual void Update(GameTime gameTime)
        { }

        public virtual void Update(GameTime gameTime, KeyboardState keyState, List<GameObject> gameObjects)
        {
            Position += Motion * (float)gameTime.ElapsedGameTime.TotalSeconds;
            Animate(gameTime);
        }

        public virtual void Draw(SpriteBatch spriteBatch)
        {
            if (sprite != null)
                sprite.Draw(spriteBatch, Position, DrawColor, Rotation, Origin, Scale, spriteEffects, LayerDepth);
        }

        public virtual void Draw(SpriteBatch spriteBatch, Camera camera)
        {
            if (sprite != null)
                sprite.Draw(spriteBatch, new Vector2(camera.CX + X, camera.CY + Y), DrawColor, Rotation, Origin, Scale, spriteEffects, LayerDepth);
        }
    }
}
