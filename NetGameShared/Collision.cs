using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace NetGameShared
{
    static class Collision
    {
        public static bool AABB(GameObject object1, GameObject object2)
        {
            return object1.CollideBox.Intersects(object2.CollideBox);
        }

        public static bool AABBTiles(GameObject testObject, List<Tile> tiles)
        {
            foreach (Tile tile in tiles)
            {
                if (AABB(testObject, tile))
                    return true;
            }
            return false;
        }

        public static bool CollideAtPosition(Vector2 position, List<Tile> solids)
        {
            foreach (Tile tile in solids)
            {
                if (position.X >= tile.CollideX && position.X <= tile.CollideX + tile.CollideBox.Width)      
                {
                    if ((tile.Complete && position.Y >= tile.CollideY && position.Y <= tile.CollideY + tile.CollideBox.Height) ||
                        (!tile.Complete && position.Y >= tile.CollideY + tile.HeightMap[(int)position.X % Universal.TILE_SIZE] && position.Y <= tile.CollideY + tile.CollideBox.Height))
                    return true;
                }
            }
            return false;
        }
    }
}
