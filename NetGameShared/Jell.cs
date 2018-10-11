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
    public class Jell : Mob
    {
        private const float MOVE = 0.05f;

        public Jell(bool server) : base(server)
        {
            InitVars();
            //Drops.Add(new ItemDrop(ItemName.Dirt, random.Next(5) + 5, server));
            Drops.Add(new ItemDrop(ItemName.Stone, random.Next(5) + 5, server));
            if (random.Next(10) < 1)
                Drops.Add(new ItemDrop(ItemName.Sword, 1, server));
        }

        private void InitVars()
        {
            if (!server)
                sprite = ResourceManager.GetSprite("mob_jell");
            Name = "Jell";
            MaxHP = 50;
            HP = MaxHP;
            Bounds = new Rectangle(0, 6, 32, 26);
            isGravity = true;
            speed = 2000f;
            maxSpeed = 400f;
            slow = 200f;
            jump = 700f;
            gravity = 2000f;
            maxGravity = 1500.0f;
            DX = 1;
        }

        public override void Update(GameTime gameTime, Map map)
        {
            base.Update(gameTime, map);

            float time = (float)gameTime.ElapsedGameTime.TotalSeconds;
            Move(time);
            CheckCollisions(time, map);
            StayInBounds(map);
            Animate(gameTime);
        }

        public override void Update(float msElapsed, Map map)
        {
            float time = msElapsed / 1000;

            Move(time);
            CheckCollisions(time, map);
            StayInBounds(map);
        }

        private void Move(float time)
        {
            if (DX > 0)
                DX -= slow * time;
            else if (DX < 0)
                DX += slow * time;
            if (Math.Abs(DX) < 10)
                DX = 0;

            if (DX > maxSpeed)
                DX = maxSpeed;
            else if (DX < -maxSpeed)
                DX = -maxSpeed;

            if (!onFloor)
                SetAnimSpeed(0);
            else
                SetAnimSpeed(0.5f);

            if (isGravity)
            {
                DY += gravity * time;
                if (DY > Math.Abs(maxGravity))
                    DY = maxGravity;
            }

            previousX = (int)X;
            previousY = (int)Y;

            if (server)
            {
                if (onFloor && (random.Next() % 300) < 1)
                {
                    DX = 200 - random.Next() % 400;
                    DY = -200 - random.Next() % 400;
                }
            }

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
    }
}
