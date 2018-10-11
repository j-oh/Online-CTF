using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace NetGameShared
{
    public class FallingBlock : CollideObject
    {
        public bool CanDestroy { get; private set; }
        public float BounceAway { get { return bounceAway; } set { bounceAway = value; } }
        public int ID { get; set; }
        public int PlayerID { get; set; }

        private const float MOVE = 0.05f;

        float gravity;

        public FallingBlock(Sprite sprite, Vector2 position, float dx, float rotation, float bounceAway) : base(sprite, position)
        {
            InitVars();
            DX = dx;
            Rotation = rotation;
            BounceAway = bounceAway;
        }

        public FallingBlock(int id, int playerID, Vector2 position, Random random)
        {
            InitVars();
            DX = random.Next(20) - 10;
            Rotation = random.Next(360);
            bounceAway = random.Next(200) - 100;
            Position = position;
            ID = id;
            PlayerID = playerID;
        }

        private void InitVars()
        {
            Origin = new Vector2(Universal.TILE_SIZE / 2, Universal.TILE_SIZE / 2);
            Bounds = new Rectangle(0, 0, Universal.TILE_SIZE, Universal.TILE_SIZE);
            gravity = 150f;
            bounce = true;
            bounceFactor = 0.15f;
        }

        public void Update(GameTime gameTime, Map map)
        {
            float time = (float)gameTime.ElapsedGameTime.TotalSeconds;
            Move(time);
            CheckCollisions(time, map);
            StayInBounds(map);
        }

        public void Update(float msElapsed, Map map)
        {
            float time = msElapsed / 1000;
            Move(time);
            CheckCollisions(time, map);
            StayInBounds(map);
            if (bounces >= 7 || Y > map.Height * Universal.TILE_SIZE - Universal.TILE_SIZE)
                CanDestroy = true;
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

            Y = (int)Y;

            Rotation += (Math.Abs(DY) + 50) / 1000;
        }
    }
}
