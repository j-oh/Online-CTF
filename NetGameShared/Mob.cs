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
    public class Mob : CollideObject
    {
        public int MobId { get; }
        public string Name { get; protected set; }
        public int HP { get; set; }
        public int MaxHP { get; protected set; }
        public List<ItemDrop> Drops { get; protected set; }
        protected Sprite pixel;
        protected Interval HPBarInterval;
        protected Random random;
        protected bool isGravity, server;
        protected float speed, maxSpeed, slow, jump, gravity, maxGravity;
        protected int previousX, previousY;

        public Mob(bool server) : base()
        {
            this.server = server;
            if (!server)
                pixel = ResourceManager.GetSprite("pixel");
            MobId = 0;
            HPBarInterval = new Interval();
            Drops = new List<ItemDrop>();
            random = new Random();
        }

        public virtual void Update(GameTime gameTime, Map map)
        {
            HPBarInterval.Update(gameTime);
        }

        public virtual void Update(float msElapsed, Map map)
        {

        }

        public void Damage(int damage)
        {
            HP -= damage;
            if (HP <= 0)
            {
                Dead = true;
                foreach (ItemDrop drop in Drops)
                {
                    drop.Position = Position;
                    drop.DX = DX / 2;
                    drop.DY = DY - 300;
                }
            }
        }

        public void ShowHPBar()
        {
            HPBarInterval.Start(3);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);
            Vector2 fontSize = font.MeasureString(Name);
            spriteBatch.Draw(pixel.Texture, new Rectangle((int)(X + 13 - fontSize.X / 2), IntY + sprite.Texture.Height + 2, (int)(fontSize.X + 6), (int)(fontSize.Y)), new Rectangle(1, 1, 1, 1), Color.Black * 0.5f);
            spriteBatch.DrawString(font, Name, new Vector2(X + 16 - fontSize.X / 2, Y + sprite.Texture.Height + 2), Color.White);
            if (HPBarInterval.IsRunning)
            {
                spriteBatch.Draw(pixel.Texture, new Rectangle(IntX + 16 - MaxHP / 2, IntY - 2, HP, 4), new Rectangle(1, 1, 1, 1), Color.LimeGreen * 0.75f);
                Universal.DrawRectangleOutline(spriteBatch, new Rectangle(IntX + 16 - MaxHP / 2, IntY - 2, MaxHP, 4), Color.DarkGreen);
            }
        }
    }
}
