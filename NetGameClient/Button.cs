using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using NetGameShared;

namespace NetGameClient
{
    class Button : GameObject
    {
        public enum ButtonType { OpenOptions, None };
        public bool ClickFlag { get; set; }
        ButtonType type;
        MouseState lastMouseState; 
        bool activated, clicked, actOnRelease;

        public Button(Sprite sprite, Vector2 position) : base(sprite, position)
        {
            type = ButtonType.None;
            activated = false;
            clicked = false;
            actOnRelease = false;
            ClickFlag = false;
        }

        public Button(Sprite sprite, Vector2 position, bool actOnRelease) : this(sprite, position)
        {
            this.actOnRelease = actOnRelease;
        }

        public Button(Sprite sprite, Vector2 position, ButtonType type) : base(sprite, position)
        {
            this.type = type;
            activated = false;
            clicked = false;
            actOnRelease = false;
            ClickFlag = false;
        }

        public Button(Sprite sprite, Vector2 position, ButtonType type, bool actOnRelease) : this(sprite, position, type)
        {
            this.actOnRelease = actOnRelease;
        }

        public void Update(int mouseX, int mouseY)
        {
            MouseState mouseState = Mouse.GetState();

            activated = CollideBox.Contains(mouseX, mouseY);
            if (activated)
            {
                if (DrawColor == Color.White)
                    DrawColor = new Color(200, 200, 200);
                if (mouseState.LeftButton == ButtonState.Pressed && lastMouseState.LeftButton == ButtonState.Released)
                {
                    DrawColor = new Color(150, 150, 150);
                    clicked = true;
                    if (!actOnRelease && lastMouseState.LeftButton == ButtonState.Released)
                        PerformAction();
                }
                else if (clicked && mouseState.LeftButton == ButtonState.Released && lastMouseState.LeftButton == ButtonState.Pressed)
                {
                    DrawColor = new Color(200, 200, 200);
                    clicked = false;
                    if (actOnRelease)
                        PerformAction();
                }
            }
            else
            {
                DrawColor = Color.White;
                clicked = false;
            }

            lastMouseState = mouseState;
        }

        private void PerformAction()
        {
            switch (type)
            {
                case ButtonType.None:
                default:
                    break;
            }
            ClickFlag = true;
        }
    }
}
