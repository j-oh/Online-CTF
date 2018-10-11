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
    public class CollideObject : GameObject
    {
        protected bool onFloor, bounce;
        protected float bounceFactor, bounceAway;
        protected int bounces;

        public CollideObject() : base() { }
        public CollideObject(Sprite sprite, Vector2 position) : base(sprite, position) { }

        protected void CheckCollisions(float time, Map map)
        {
            List<Tile> solids = new List<Tile>();
            onFloor = false;

            CollideBox = new Rectangle(bounds.X + (int)X - (int)Origin.X, bounds.Y + (int)Y - (int)Origin.Y, bounds.Width, bounds.Height);
            int collideX = CollideX;
            int collideY = CollideY;
            if (collideX < 0)
                collideX = 0;
            else if (collideX >= map.Width * Universal.TILE_SIZE)
                collideX = map.Width * Universal.TILE_SIZE - 1;
            if (collideY < 0)
                collideY = 0;
            else if (collideY >= map.Height * Universal.TILE_SIZE)
                collideY = map.Height * Universal.TILE_SIZE - 1;
            int chunkX = collideX / Universal.TILE_SIZE / Map.CHUNK_FACTOR;
            int chunkY = collideY / Universal.TILE_SIZE / Map.CHUNK_FACTOR;
            if (chunkX >= map.SolidChunks.GetLength(0))
                chunkX--;
            if (chunkY >= map.SolidChunks.GetLength(1))
                chunkY--;

            if (map.SolidChunks[chunkX, chunkY] != null)
                solids.AddRange(map.SolidChunks[chunkX, chunkY]);

            for (int i = 0; i < solids.Count; i++)
            {
                Tile tile = solids[i];
                if (tile == null ||
                    tile.CollideX - Universal.TILE_SIZE > CollideX + CollideBox.Width ||
                    tile.CollideX + tile.CollideBox.Width + Universal.TILE_SIZE < CollideX ||
                    tile.CollideY - Universal.TILE_SIZE > CollideY + CollideBox.Height ||
                    tile.CollideY + tile.CollideBox.Height + Universal.TILE_SIZE < CollideY)
                {
                    solids.RemoveAt(i);
                    i--;
                }
            }

            if (DY > 0)
            {
                while (Collision.CollideAtPosition(new Vector2(CollideBox.X + (int)(CollideBox.Width * 0.5), CollideBox.Y + CollideBox.Height), solids))
                {
                    onFloor = true;
                    if (Collision.CollideAtPosition(new Vector2(CollideBox.X + (int)(CollideBox.Width * 0.5), CollideBox.Y + CollideBox.Height - 1), solids))
                        Y--;
                    else
                        break;
                }
                if (onFloor)
                {
                    if (bounce)
                    {
                        DY *= -bounceFactor;
                        DX = bounceAway;
                        bounces++;
                    }
                    else
                        DY = 0;
                }
            }
            while (Collision.CollideAtPosition(new Vector2(CollideBox.X, CollideBox.Y + (int)(CollideBox.Height * 0.6)), solids))
            {
                X++;
                DX = 0;
            }
            while (Collision.CollideAtPosition(new Vector2(CollideBox.X + CollideBox.Width, CollideBox.Y + (int)(CollideBox.Height * 0.6)), solids))
            {
                X--;
                DX = 0;
            }
            if (DY < 0 && !onFloor)
            {
                while (Collision.CollideAtPosition(new Vector2(CollideBox.X, CollideBox.Y), solids) ||
                    Collision.CollideAtPosition(new Vector2(CollideBox.X + CollideBox.Width, CollideBox.Y), solids))
                {
                    Y++;
                    DY = 0;
                }
            }
        }

        protected void StayInBounds(Map map)
        {
            if (X < -Bounds.X + Origin.X)
            {
                X = -Bounds.X + Origin.X;
                DX = 0;
            }
            if (X > map.Width * Universal.TILE_SIZE - (Bounds.X + Bounds.Width) + Origin.X)
            {
                X = map.Width * Universal.TILE_SIZE - (Bounds.X + Bounds.Width) + Origin.X;
                DX = 0;
            }
            if (Y < -Bounds.Y + Origin.Y)
            {
                Y = -Bounds.Y + Origin.Y;
                DY = 0;
            }
            if (Y > map.Height * Universal.TILE_SIZE + Universal.TILE_SIZE * 3 - (Bounds.Y + Bounds.Height) + Origin.Y)
            {
                Y = map.Height * Universal.TILE_SIZE + Universal.TILE_SIZE * 3 - (Bounds.Y + Bounds.Height) + Origin.Y;
                DY = 0;
            }
        }
    }
}
