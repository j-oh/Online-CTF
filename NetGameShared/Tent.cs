using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace NetGameShared
{
    public class Tent : CollideObject
    {
        public Item SelectedItem { get; private set; }
        public bool HasFlag { get; set; }
        public bool Selected { get; set; }
        public bool WinSelected { get; set; }

        private const float MOVE = 0.05f;

        float gravity;
        bool redTeam;

        public Tent(bool playerTeam, Vector2 position, bool ctf, bool server)
        {
            if (!server)
            {
                if (playerTeam)
                    sprite = ResourceManager.GetSprite("redtent");
                else
                    sprite = ResourceManager.GetSprite("bluetent");
            }
            Bounds = new Rectangle(16, 0, 32, 64);
            Position = position;
            Origin = new Vector2(32, 64);
            gravity = 500f;
            HasFlag = ctf;
            redTeam = playerTeam;
        }

        public void Update(GameTime gameTime, Map map, Item[] items, Vector2 playerPosition, bool playerTeam)
        {
            float time = (float)gameTime.ElapsedGameTime.TotalSeconds;
            Move(time);
            CheckCollisions(time, map);
            StayInBounds(map);
            Selected = false;
            WinSelected = false;
            foreach (Item item in items)
            {
                if (((item.Name == ItemName.BlueFlag && redTeam) || (item.Name == ItemName.RedFlag && !redTeam))
                    && HasFlag && Vector2.Distance(Position - Origin, playerPosition) <= Universal.TILE_SIZE * 2)
                {
                    WinSelected = true;
                    SelectedItem = item;
                    break;
                }
                else if (HasFlag && item.Name == ItemName.None && playerTeam != redTeam && Vector2.Distance(Position - Origin, playerPosition) <= Universal.TILE_SIZE * 2)
                {
                    Selected = true;
                    SelectedItem = item;
                    break;
                }
            }
        }

        public void Update(float msElapsed, Map map)
        {
            float time = msElapsed / 1000;
            Move(time);
            CheckCollisions(time, map);
            StayInBounds(map);
        }

        private void Move(float time)
        {
            if (!onFloor)
                DY += gravity * time;

            for (float i = 0; i < Math.Abs(DX * time); i += MOVE)
            {
                if (DX > 0)
                    X += MOVE;
                else
                    X -= MOVE;
            }

            for (float i = 0; i < Math.Abs(DY * time); i += MOVE)
            {
                if (DY > 0)
                    Y += MOVE;
                else
                    Y -= MOVE;
            }
        }

        public void Draw(SpriteBatch spriteBatch, SpriteFont font)
        {
            if (HasFlag)
                sprite.Frame = 0;
            else
                sprite.Frame = 1;
            string flagName = "Red Flag";
            if (!redTeam)
                flagName = "Blue Flag";
            /*if (Selected)
            {
                Universal.DrawStringMore(spriteBatch, font, "<E> - Take Flag", new Vector2(X, Y - 80), Color.White, Align.Center, true);
                Universal.DrawStringMore(spriteBatch, font, flagName, new Vector2(X, Y - 96), Color.White, Align.Center, true);
            }
            if (WinSelected)
                Universal.DrawStringMore(spriteBatch, font, "Cap Flag", new Vector2(X, Y - 80), Color.White, Align.Center, true);*/
            base.Draw(spriteBatch);
        }
    }
}
