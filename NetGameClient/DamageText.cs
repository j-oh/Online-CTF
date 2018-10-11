using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using NetGameShared;

namespace NetGameClient
{
    class DamageText : GameObject
    {
        private int damage, moved, moveTo;

        public DamageText(Vector2 position, int damage)
        {
            Position = position;
            moved = 0;
            moveTo = (int)Y - 60;
            this.damage = damage;
            Dead = false;
        }

        public override void Update(GameTime gameTime)
        {
            Y += (moveTo - Y) / 16;
            moved++;
            if (moved > 60)
                Dead = true;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            Universal.DrawStringMore(spriteBatch, Client.font, "-" + damage, new Vector2((int)X, (int)Y), Color.White, 0, new Vector2(0, 0), new Vector2(2, 2), SpriteEffects.None, 0, Align.Left, true);
        }
    }
}
