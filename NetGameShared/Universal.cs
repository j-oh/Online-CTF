using System.Collections.Concurrent;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace NetGameShared
{
    public static class Universal
    {
        public const string ID = "NetGame";
        public const string GAME_VERSION = "dev v3.41";
        public const string LAUNCHER_VERSION = "dev v1";
        public const string EXECUTABLE_PATH = "content\\NetGameClient.exe";
        public const int DEFAULT_PORT = 56263;
        public const int SCREEN_WIDTH = 1280;
        public const int SCREEN_HEIGHT = 720;
        public const int SMALL_SCREEN_WIDTH = 340;
        public const int SMALL_SCREEN_HEIGHT = 210;
        public const int TILE_SIZE = 16;
        public const int FRAME_RATE = 60;
        public const int PLACE_DISTANCE = 8;

        public static void DrawStringMore(SpriteBatch spriteBatch, SpriteFont font, string str, Vector2 position, Color color, Align align, bool shadow)
        {
            int drawX = (int)position.X;
            if (align == Align.Center)
                drawX -= (int)font.MeasureString(str).X / 2;
            else if (align == Align.Right)
                drawX -= (int)font.MeasureString(str).X;

            if (shadow)
                spriteBatch.DrawString(font, str, new Vector2(drawX + 1, (int)position.Y + 1), Color.Black);
            spriteBatch.DrawString(font, str, new Vector2(drawX, (int)position.Y), color);
        }

        public static void DrawStringMore(SpriteBatch spriteBatch, SpriteFont font, string str, Vector2 position, Color color, float rotation, Vector2 origin, Vector2 scale, SpriteEffects effects, float layerDepth, Align align, bool shadow)
        {
            int drawX = (int)position.X;
            if (align == Align.Center)
                drawX -= (int)font.MeasureString(str).X / 2 * (int)scale.X;
            else if (align == Align.Right)
                drawX -= (int)font.MeasureString(str).X * (int)scale.X;

            if (shadow)
                spriteBatch.DrawString(font, str, new Vector2(drawX + 1, (int)position.Y + 1), Color.Black, rotation, origin, scale, effects, layerDepth);
            spriteBatch.DrawString(font, str, new Vector2(drawX, (int)position.Y), color, rotation, origin, scale, effects, layerDepth);
        }

        public static void DrawRectangleOutline(SpriteBatch spriteBatch, Rectangle rect, Color color)
        {
            Texture2D pixel = ResourceManager.GetSprite("pixel").Texture;
            spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, 1), color);
            spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, 1, rect.Height), color);
            spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y + rect.Height, rect.Width + 1, 1), color);
            spriteBatch.Draw(pixel, new Rectangle(rect.X + rect.Width, rect.Y, 1, rect.Height + 1), color);
        }

        public static bool TryDictRemove<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, TKey key)
        {
            TValue dummy;
            return dictionary.TryRemove(key, out dummy);
        }

        public static string CombinePath(this string pathBase, string pathExtension)
        {
            string str1 = pathBase;
            string str2 = pathExtension;
            if (!str1.EndsWith("\\"))
                str1 += "\\";
            if (str2.StartsWith("\\"))
                str2.Remove(0, 1);
            return str1 + str2;
        }
    }

    public struct Team
    {
        public string Name;
        public Color TeamColor;

        public Team (string name, Color teamColor)
        {
            Name = name;
            TeamColor = teamColor;
        }
    }

    public enum Packets
    {
        Connect,
        ConnectVerified,
        ServerInfo,
        VersionMismatch,
        NewPlayer,
        RemovePlayer,
        HitPlayer,
        KilledPlayer,
        NewMob,
        RemoveMob,
        HitMob,
        ColorChange,
        Chat,
        PickUpItem,
        DropItem,
        RemoveOneItem,
        NewItemDrop,
        ModifyItemDrop,
        RemoveItemDrop,
        ItemChange,
        Move,
        ServerMove,
        Attack,
        AddBlock,
        HitBlock,
        ChangeBlockDurability,
        CreateFallingBlocks,
        RemoveFallingBlock,
        UpdateWorld,
        TentState,
        PlayerFlag,
        RestartGame,
        GameEnded
    }

    public enum GameMode
    {
        FreeForAll,
        TeamDeathmatch,
        CaptureTheFlag
    }

    public enum Error
    {
        None,
        VersionMismatch
    }

    public enum Align
    {
        Left,
        Center,
        Right
    }
}
