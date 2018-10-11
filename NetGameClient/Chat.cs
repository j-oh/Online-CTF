using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using NetGameShared;

namespace NetGameClient
{
    class Chat
    {
        const Keys activateKey = Keys.Enter;
        const Keys deactivateKey = Keys.Escape;
        const Keys sendKey = Keys.Enter;
        const int chatWidth = 350;

        public bool Activated { get; set; }
        Texture2D pixel;
        TextBox chatBox;
        List<String> messages;
        KeyboardState lastKeyState;

        public Chat(GameWindow window)
        {
            pixel = ResourceManager.GetSprite("pixel").Texture;
            messages = new List<String>();
            chatBox = new TextBox(window, new Vector2(40, Universal.SCREEN_HEIGHT - 21), chatWidth - 45, Client.font);
            chatBox.SetDefaultText("Enter message...");
            chatBox.DisplayBox = false;
            lastKeyState = Keyboard.GetState();
        }

        public void AddMessage(string message, string nickname)
        {
            if (nickname == "[Server]")
                messages.Insert(0, nickname + " " + message);
            else
                messages.Insert(0, nickname + ": " + message);
            if (messages.Count > 5)
                messages.RemoveAt(5);
        }

        public string Update()
        {
            KeyboardState keyState = Keyboard.GetState();
            chatBox.Selected = Activated;

            if (Activated)
            {
                chatBox.Update(false);
                if (keyState.IsKeyDown(sendKey) && lastKeyState.IsKeyUp(sendKey))
                {
                    string text = chatBox.Text;
                    chatBox.Clear();
                    Activated = false;
                    lastKeyState = Keyboard.GetState();
                    return text;
                }
                else if (keyState.IsKeyDown(deactivateKey) && lastKeyState.IsKeyUp(deactivateKey))
                    Activated = false;
            }
            else
            {
                if (keyState.IsKeyDown(activateKey) && lastKeyState.IsKeyUp(activateKey))
                    Activated = true;
            }
            lastKeyState = Keyboard.GetState();

            return null;
        }

        public void Draw(SpriteBatch spriteBatch, Camera camera, GameTime gameTime)
        {
            int offset = 0;
            if (Activated)
            {
                offset = 24;
                spriteBatch.Draw(pixel, new Rectangle(camera.CX, camera.CY + Universal.SCREEN_HEIGHT - 10 - offset - 16 * messages.Count, chatWidth, 10 + 16 * messages.Count), new Rectangle(1, 1, 1, 1), Color.Black * 0.5f);
                spriteBatch.Draw(pixel, new Rectangle(camera.CX, camera.CY + Universal.SCREEN_HEIGHT - offset, chatWidth, offset), new Rectangle(1, 1, 1, 1), Color.Black * 0.75f);
                spriteBatch.DrawString(Client.font, "Chat:", new Vector2(camera.CX + 8, camera.CY + Universal.SCREEN_HEIGHT - 19), Color.White);
                chatBox.Draw(spriteBatch, camera, gameTime);
            }
            else
                spriteBatch.Draw(pixel, new Rectangle(camera.CX, camera.CY + Universal.SCREEN_HEIGHT - 10 - 16 * messages.Count, chatWidth, 10 + 16 * messages.Count), new Rectangle(1, 1, 1, 1), Color.Black * 0.5f);
            foreach(String message in messages)
            {
                spriteBatch.DrawString(Client.font, message, new Vector2(camera.CX + 8, camera.CY + Universal.SCREEN_HEIGHT - 20 - offset), Color.White);
                offset += 16;
            }
        }
    }
}
