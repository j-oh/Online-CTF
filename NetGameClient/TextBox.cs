using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using NetGameShared;

namespace NetGameClient
{

    public class TextBox : GameObject
    {
        public int Width { get; set; }
        public int Height { get; private set; }
        public bool Hovered { get; set; }
        public bool Selected { get; set; }
        public bool Shadow { get; set; }
        public bool DisplayBox { get; set; }
        public string Text { get; set; }
        public string DefaultText { get; set; }
        const int repeatInterval = 30;
        EventHandler<TextInputEventArgs> onTextEntered;
        Texture2D pixel;
        KeyboardState keyState, lastKeyState;
        MouseState lastMouseState;
        Vector2 textSize;
        string visibleText;
        int holdCount;
        char heldKey;

        public TextBox(GameWindow window, Vector2 position, int width, SpriteFont font)
        {
            window.TextInput += TextEntered;
            onTextEntered += HandleInput;
            Text = "";
            DefaultText = Text;
            visibleText = Text;
            textSize = Vector2.Zero;
            pixel = ResourceManager.GetSprite("pixel").Texture;
            holdCount = 0;
            heldKey = '\b';
            this.font = font;
            Position = position;
            Bounds = new Rectangle(0, 0, width, font.LineSpacing + 2);
            Shadow = false;
            DisplayBox = true;

            keyState = Keyboard.GetState();
            lastKeyState = keyState;
            lastMouseState = Mouse.GetState();
        }

        public TextBox(GameWindow window, Rectangle box, SpriteFont font) : this(window, new Vector2(box.X, box.Y), 0, font)
        {
            Bounds = new Rectangle(0, 0, box.Width, box.Height);
        }

        public void Update(bool independent)
        {
            keyState = Keyboard.GetState();
            MouseState mouseState = Mouse.GetState();

            if (independent)
            {
                Hovered = CollideBox.Contains(new Vector2(mouseState.X, mouseState.Y));
                if (lastMouseState.LeftButton == ButtonState.Released && mouseState.LeftButton == ButtonState.Pressed)
                    Selected = Hovered;
            }

            CheckSpecialChar(Keys.Back, '\b');
            CheckSpecialChar(Keys.Enter, '\r');
            CheckSpecialChar(Keys.Tab, '\t');

            lastKeyState = keyState;
            lastMouseState = mouseState;
        }

        public void Clear()
        {
            Text = "";
            visibleText = Text;
            textSize = Vector2.Zero;
        }

        public void SetDefaultText(string text)
        {
            DefaultText = "";
            for (int i = 0; i < text.Length; i++)
            {
                if (font.MeasureString(DefaultText + text.Substring(i, 1)).X + 4 < Bounds.Width)
                    DefaultText += text.Substring(i, 1);
                else
                    break;
            }
        }

        private void CheckSpecialChar(Keys key, char character)
        {
            if (keyState.IsKeyDown(key))
            {
                if (lastKeyState.IsKeyUp(key))
                {
                    holdCount = 0;
                    heldKey = character;
                }
                if (lastKeyState.IsKeyUp(key) || (heldKey == character && holdCount >= repeatInterval))
                    onTextEntered.Invoke(this, new TextInputEventArgs(character));
                else if (heldKey == character && holdCount < repeatInterval)
                    holdCount++;
            }
        }

        private void TextEntered(object sender, TextInputEventArgs e)
        {
            if (onTextEntered != null)
                onTextEntered.Invoke(sender, e);
        }

        private void HandleInput(object sender, TextInputEventArgs e)
        {
            if (Selected)
            {
                switch (e.Character)
                {
                    case '\b':
                        if (Text.Length > 0)
                            Text = Text.Substring(0, Text.Length - 1);
                        break;
                    case '\r':
                        break;
                    case '\t':
                        Text += "   ";
                        break;
                    default:
                        Text += e.Character;
                        holdCount = 0;
                        break;
                }

                textSize = font.MeasureString(Text);
                if (textSize.X + 4 < Bounds.Width)
                    visibleText = Text;
            }
        }

        public void Draw(SpriteBatch spriteBatch, Camera camera, GameTime gameTime)
        {
            bool caretVisible = true;

            if ((gameTime.TotalGameTime.TotalMilliseconds % 1000) < 500)
                caretVisible = false;
            else
                caretVisible = true;

            if (DisplayBox)
                Universal.DrawRectangleOutline(spriteBatch, new Rectangle(camera.CX + IntX, camera.CY + IntY, Bounds.Width, Bounds.Height), DrawColor);

            if (caretVisible && Selected && visibleText == Text)
                spriteBatch.Draw(pixel, new Rectangle(camera.CX + IntX + (int)textSize.X + 4, camera.CY + IntY + 2, 1, Bounds.Height - 3), DrawColor);

            if (!Selected && Text == "")
                spriteBatch.DrawString(font, DefaultText, new Vector2(camera.CX + X + 4, camera.CY + Y + 2), new Color(Color.Gray, 0.5f));
            else
            {
                if (Shadow)
                    spriteBatch.DrawString(font, visibleText, new Vector2(camera.CX + X + 4, camera.CY + Y + 2) + Vector2.One, Color.Black);
                spriteBatch.DrawString(font, visibleText, new Vector2(camera.CX + X + 4, camera.CY + Y + 2), DrawColor);
            }
        }
    }
}
