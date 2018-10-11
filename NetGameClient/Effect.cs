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
    class Effect : GameObject
    {
        public Effect(Sprite sprite, Vector2 position, float seconds) : base(sprite, position)
        {
            Dead = false;
            SetAnimSpeed(seconds);
        }

        public override void Update(GameTime gameTime)
        {
            Animate(gameTime);
        }

        public void SetFlip(bool flip)
        {
            if (flip)
                spriteEffects = SpriteEffects.FlipHorizontally;
            else
                spriteEffects = SpriteEffects.None;
        }

        protected override void AnimationEnd()
        {
            Dead = true;
        }
    }
}
