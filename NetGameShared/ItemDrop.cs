using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace NetGameShared
{
    public class ItemDrop : Item
    {
        private const float MOVE = 0.05f;

        public Item SelectedItem { get; private set; }

        Sprite itemSprite;
        Interval delayInterval;
        float slow, gravity;
        bool playerTeam;

        public ItemDrop(ItemName itemName, int count, bool server) : base(itemName, count, 0, server)
        {
            itemSprite = Sprite;
            SetSprite(itemborder);
            Bounds = new Rectangle(8, 8, 16, 16);
            delayInterval = new Interval();
            delayInterval.Start(1);

            slow = 200f;
            gravity = 2000f;
        }

        public ItemDrop(ItemName itemName, int count, Vector2 position, bool server) : this(itemName, count, server)
        {
            Position = position;
        }

        public void Update(GameTime gameTime, Map map, Item[] items, Vector2 playerPosition, bool playerTeam)
        {
            float time = (float)gameTime.ElapsedGameTime.TotalSeconds;
            Move(time);
            CheckCollisions(time, map);
            StayInBounds(map);
            delayInterval.Update(gameTime);

            this.playerTeam = playerTeam;
            if (!delayInterval.IsRunning && Vector2.Distance(Position - Origin, playerPosition) <= Universal.TILE_SIZE * 2)
            {
                Selected = false;
                foreach (Item item in items)
                {
                    if ((Name == ItemName.RedFlag && (playerTeam || item.Name == ItemName.None)) ||
                        (Name == ItemName.BlueFlag && (!playerTeam || item.Name == ItemName.None)) ||
                        ((Name == item.Name && item.Count < MaxCount) || item.Name == ItemName.None))
                    {
                        Selected = true;
                        SelectedItem = item;
                        break;
                    }
                }
            }
            else
                Selected = false;
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
            else
                DY = 0;

            if (DX > 0)
                DX -= slow * time;
            else if (DX < 0)
                DX += slow * time;
            if (Math.Abs(DX) < 10)
                DX = 0;
            /*if (DY > 0)
                DY -= slow * time;
            else if (DY < 0)
                DY += slow * time;
            if (Math.Abs(DY) < 10)
                DY = 0;*/

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

            X = (int)X;
            Y = (int)Y;
        }

        public void Draw(SpriteBatch spriteBatch, SpriteFont font)
        {
            if (sprite != null && itemSprite != null)
            {
                spriteBatch.Draw(sprite.Texture, new Vector2(X, Y), Color.White);
                if (Type == ItemType.Block)
                    spriteBatch.Draw(itemSprite.Texture, new Vector2(X + Bounds.X / 2, Y + Bounds.Y / 2), null, Color.White, Rotation, Origin, Scale, spriteEffects, LayerDepth);
                else
                    spriteBatch.Draw(itemSprite.Texture, new Vector2(X - Bounds.X / 2, Y - Bounds.Y / 2), null, Color.White, Rotation, Origin, Scale, spriteEffects, LayerDepth);
                /*if (Selected)
                {
                    string pickupText = "<E> - Pick Up";
                    if ((Name == ItemName.RedFlag && playerTeam) || (Name == ItemName.BlueFlag && !playerTeam))
                        pickupText = "Return to Tent";
                    Universal.DrawStringMore(spriteBatch, font, pickupText, new Vector2(X + Bounds.X / 2 + Bounds.Width / 2, Y + Bounds.Y - 40) - Origin, Color.White, Align.Center, true);
                    if (Count > 1)
                        Universal.DrawStringMore(spriteBatch, font, GetNameString(Name) + String.Format(" ({0})", Count), new Vector2(X + Bounds.X / 2 + Bounds.Width / 2, Y + Bounds.Y - 56) - Origin, Color.White, Align.Center, true);
                    else
                        Universal.DrawStringMore(spriteBatch, font, GetNameString(Name), new Vector2(X + Bounds.X / 2 + Bounds.Width / 2, Y + Bounds.Y - 56) - Origin, Color.White, Align.Center, true);
                }*/
            }
        }
    }
}
