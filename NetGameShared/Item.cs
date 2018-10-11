using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace NetGameShared
{
    public enum ItemType { Block, Consumable, Equipment, None, Weapon };
    public enum ItemName
    {
        None,
        Dirt,
        LeftDirtRamp,
        RightDirtRamp,
        Stone,
        Sword,
        RedFlag,
        BlueFlag
    }

    public class Item : CollideObject
    {
        public int Count { get; set; }
        public int MaxCount { get; set; }
        public ItemType Type { get; set; }
        public ItemName Name { get; set; }
        public bool Selected { get; set; }
        public int Index { get; set; }
        public int BlockID { get; set; }
        protected Sprite itemborder;
        Sprite itemsquare;
        SpriteFont font;
        Keys key;
        string letter;

        public Item(ItemName itemName, int count, int index, Vector2 position) : this(itemName, count, index, false)
        {
            Position = position;
            Index = index;
            if (index == 0)
            {
                letter = "1";
                key = Keys.D1;
                Selected = true;
            }
            else if (index == 1)
            {
                letter = "2";
                key = Keys.D2;
            }
            else
            {
                letter = "E";
                key = Keys.D3;
            }
        }

        public Item(ItemName itemName, int count, int index, bool server)
        {
            Count = count;
            SetAnimSpeed(0);
            Name = itemName;
            SetType();
            if (!server)
                SetSprite();
            else
            {
                Index = index;
                if (index == 0)
                    Selected = true;
            }
        }

        public void RemoveOne()
        {
            if (Count > 0)
                Count--;
            if (Count <= 0)
            {
                Count = 0;
                Name = ItemName.None;
                Type = ItemType.None;
                sprite = null;
            }
        }

        public bool Update(List<Item> items, KeyboardState keyState, KeyboardState lastKeyState)
        {
            if (keyState.IsKeyDown(key) && !lastKeyState.IsKeyDown(key))
            {
                foreach (Item item in items)
                    item.Selected = false;
                Selected = true;
                return true;
            }
            return false;
        }

        public int SetItem(ItemName name, int count, bool server)
        {
            Name = name;
            Count = count;
            SetType();
            if (Count > MaxCount)
                Count = MaxCount;
            if (!server)
                SetSprite();
            return count - Count;
        }

        public static ItemType GetType(ItemName itemName)
        {
            switch (itemName)
            {
                case ItemName.None:
                default:
                    return ItemType.None;
                case ItemName.Dirt:
                case ItemName.Stone:
                    return ItemType.Block;
                case ItemName.Sword:
                    return ItemType.Weapon;
                case ItemName.RedFlag:
                case ItemName.BlueFlag:
                    return ItemType.Equipment;
            }
        }

        public static string GetNameString(ItemName itemName)
        {
            switch (itemName)
            {
                case ItemName.None:
                default:
                    return "";
                case ItemName.Dirt:
                    return "Dirt";
                case ItemName.Stone:
                    return "Stone";
                case ItemName.Sword:
                    return "Sword";
                case ItemName.RedFlag:
                    return "Red Flag";
                case ItemName.BlueFlag:
                    return "Blue Flag";
            }
        }

        protected void SetType()
        {
            Type = GetType(Name);
            switch (Type)
            {
                case ItemType.None:
                default:
                    MaxCount = 0;
                    break;
                case ItemType.Block:
                    MaxCount = 999;
                    break;
                case ItemType.Equipment:
                case ItemType.Weapon:
                    MaxCount = 1;
                    break;
            }
            if (Type == ItemType.None)
                Count = 0;
            if (Type == ItemType.Block)
            {
                switch (Name)
                {
                    case ItemName.Dirt:
                        BlockID = 0;
                        break;
                    case ItemName.Stone:
                        BlockID = 3;
                        break;
                }
            }
        }

        protected void SetSprite()
        {
            if (itemsquare == null)
                itemsquare = ResourceManager.GetSprite("itemsquare");
            if (itemborder == null)
                itemborder = ResourceManager.GetSprite("itemborder");
            font = ResourceManager.GetFont("font");
            switch (Name)
            {
                case ItemName.None:
                default:
                    sprite = null;
                    break;
                case ItemName.Sword:
                    sprite = ResourceManager.GetSprite("item_sword");
                    break;
                case ItemName.RedFlag:
                    sprite = ResourceManager.GetSprite("item_redflag");
                    break;
                case ItemName.BlueFlag:
                    sprite = ResourceManager.GetSprite("item_blueflag");
                    break;
            }
            if (Type == ItemType.Block)
                sprite = ResourceManager.GetTile(BlockID);
        }

        public void Draw(SpriteBatch spriteBatch, Camera camera)
        {
            if (Selected)
                itemsquare.Frame = 1;
            else
                itemsquare.Frame = 0;
            spriteBatch.Draw(itemsquare.Texture, new Vector2(camera.CX + X, camera.CY + Y), new Rectangle((itemsquare.Frame + itemsquare.AnimBegin) * itemsquare.SourceWidth, 0, itemsquare.SourceWidth, itemsquare.Texture.Height), Color.White);
            if (sprite != null)
            {
                if (Type == ItemType.Block)
                    spriteBatch.Draw(sprite.Texture, new Vector2(camera.CX + X + 8, camera.CY + Y + 8), Color.White);
                else
                    spriteBatch.Draw(sprite.Texture, new Vector2(camera.CX + X, camera.CY + Y), Color.White);
                if (Count > 1)
                    Universal.DrawStringMore(spriteBatch, font, Convert.ToString(Count), new Vector2(camera.CX + X + 27, camera.CY + Y + 15), Color.White, Align.Right, false);
            }
            spriteBatch.DrawString(font, letter, new Vector2(camera.CX + X + 29, camera.CY + Y + 27), Color.White);
        }
    }
}
