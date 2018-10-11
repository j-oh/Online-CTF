using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace NetGameShared
{
    class Quadtree
    {
        const int MAX_OBJECTS = 10;
        const int MAX_LEVEL = 5;

        List<GameObject> objects;
        Quadtree[] nodes;
        Rectangle bounds;
        int level;

        public Quadtree(int level, Rectangle bounds) : this(level)
        {
            this.bounds = bounds;
        }

        public Quadtree(int level)
        {
            this.level = level;
            bounds = new Rectangle(0, 0, Universal.SCREEN_WIDTH, Universal.SCREEN_HEIGHT);
            objects = new List<GameObject>();
            nodes = new Quadtree[4];
        }

        public void Clear()
        {
            objects.Clear();

            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i] != null)
                {
                    nodes[i].Clear();
                    nodes[i] = null;
                }
            }
        }

        private void Split()
        {
            int halfWidth = bounds.Width / 2;
            int halfHeight = bounds.Height / 2;

            nodes[0] = new Quadtree(level + 1, new Rectangle(bounds.X + halfWidth, bounds.Y, halfWidth, halfHeight));
            nodes[1] = new Quadtree(level + 1, new Rectangle(bounds.X, bounds.Y, halfWidth, halfHeight));
            nodes[2] = new Quadtree(level + 1, new Rectangle(bounds.X, bounds.Y + halfHeight, halfWidth, halfHeight));
            nodes[3] = new Quadtree(level + 1, new Rectangle(bounds.X + halfWidth, bounds.Y + halfHeight, halfWidth, halfHeight));
        }

        private int GetIndex(Rectangle collideBox)
        {
            int index = 0;
            Vector2 collisionPoint = new Vector2(collideBox.X + collideBox.Width / 2, collideBox.Y + collideBox.Height / 2);
            Vector2 midPoint = new Vector2(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);

            if (bounds.Contains(collisionPoint))
            {
                if (collisionPoint.X < midPoint.X)
                {
                    if (collisionPoint.Y < midPoint.Y)
                        index = 1;
                    else
                        index = 2;
                }
                else
                {
                    if (collisionPoint.Y >= midPoint.Y)
                        index = 3;
                }
            }

            return index;
        }

        public void Insert(GameObject gameObject)
        {
            if (nodes[0] != null)
            {
                int index = GetIndex(gameObject.CollideBox);
                nodes[index].Insert(gameObject);
            }
            else
            {
                objects.Add(gameObject);
                if (objects.Count > MAX_OBJECTS && level < MAX_LEVEL)
                {
                    Split();
                    for (int i = 0; i < objects.Count; i++)
                    {
                        GameObject moveObject = objects[i];
                        int index = GetIndex(moveObject.CollideBox);
                        nodes[index].Insert(moveObject);
                        objects.Remove(moveObject);
                        i--;
                    }
                }
            }
        }
    }
}
