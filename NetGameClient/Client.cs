using System;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Lidgren.Network;
using NetGameShared;

namespace NetGameClient
{
    public class Client : Game
    {
        public static bool WindowActive { get; private set; }
        public static AutoResetEvent arEvent;
        public static SpriteFont font;
        public static World world;

        enum State { Menu, Game };
        GraphicsDeviceManager graphics;
        ResourceManager rm;
        SpriteBatch spriteBatch;
        Network network;
        GameObject cursor;
        UI ui;
        TextBox ipBox, portBox, nicknameBox;
        Button connectButton;
        KeyboardState lastKeyState;
        MouseState lastMouseState;
        Camera camera;
        State state;
        bool online;
        //Thread networkThread;

        public Client()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferWidth = Universal.SMALL_SCREEN_WIDTH;
            graphics.PreferredBackBufferHeight = Universal.SMALL_SCREEN_HEIGHT;
            this.Window.Title = "NetGame Client";
        }

        protected override void Initialize()
        {
            Console.WriteLine("NetGame Client (" + Universal.GAME_VERSION + ")");
            state = State.Menu;
            online = true;
            rm = new ResourceManager();
            rm.LoadResources(this.Content, graphics);
            font = ResourceManager.GetFont("font");
            cursor = new GameObject(ResourceManager.GetSprite("cursor"), new Vector2(0, 0));
            ipBox = new TextBox(Window, new Vector2(20, Universal.SMALL_SCREEN_HEIGHT - 160), 300, font);
            ipBox.SetDefaultText("IP Address (127.0.0.1 if left blank)");
            portBox = new TextBox(Window, new Vector2(20, Universal.SMALL_SCREEN_HEIGHT - 130), 300, font);
            portBox.SetDefaultText(String.Format("Port ({0} if left blank)", Universal.DEFAULT_PORT));
            nicknameBox = new TextBox(Window, new Vector2(20, Universal.SMALL_SCREEN_HEIGHT - 100), 300, font);
            nicknameBox.SetDefaultText("Nickname (Player + Random # if left blank)");
            connectButton = new Button(ResourceManager.GetSprite("button_connect"), new Vector2(20, Universal.SMALL_SCREEN_HEIGHT - 52), true);

            lastKeyState = Keyboard.GetState();
            lastMouseState = Mouse.GetState();
            arEvent = new AutoResetEvent(true);
            camera = new Camera();
            camera.Position = new Vector2(Universal.SMALL_SCREEN_WIDTH / 2, Universal.SMALL_SCREEN_HEIGHT / 2);
            //networkThread = new Thread(new ThreadStart(network.UpdateElapsed));
            //networkThread.Start();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
        }

        protected override void UnloadContent()
        {
            if (online && state == State.Game)
                network.Shutdown();
            this.Content.Unload();
            //networkThread.Abort();
        }

        protected override void Update(GameTime gameTime)
        {
            KeyboardState keyState = Keyboard.GetState();
            MouseState mouseState = Mouse.GetState();
            WindowActive = IsActive;
            cursor.X = mouseState.X;
            cursor.Y = mouseState.Y;

            if (state == State.Game)
            {
                if (online)
                {
                    network.Update();
                    world.Update(gameTime, lastKeyState, lastMouseState, network.Chatting);
                }
                else
                    world.Update(gameTime, lastKeyState, lastMouseState, false);
                ui.Update(lastMouseState, world.DrawBlockOutline);
            }
            else if (state == State.Menu)
            {
                ipBox.Update(true);
                portBox.Update(true);
                nicknameBox.Update(true);
                connectButton.Update(mouseState.X, mouseState.Y);
                if (connectButton.ClickFlag)
                {
                    graphics.PreferredBackBufferWidth = Universal.SCREEN_WIDTH;
                    graphics.PreferredBackBufferHeight = Universal.SCREEN_HEIGHT;
                    graphics.ApplyChanges();
                    world = new World(graphics, camera, online);
                    ui = new UI(world, camera, font);
                    if (online)
                    {
                        network = new Network(this.Window);
                        int port = 0;
                        Int32.TryParse(portBox.Text, out port);
                        network.Start(world, camera, ipBox.Text, port, nicknameBox.Text);
                        if (!network.Connected)
                        {
                            world = null;
                            ui = null;
                            connectButton.ClickFlag = false;
                        }
                    }
                    if (!online || network.Connected)
                    {
                        state = State.Game;
                        connectButton = null;
                        ipBox = null;
                        portBox = null;
                    }
                }
            }

            lastKeyState = keyState;
            lastMouseState = mouseState;

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(new Color(131, 174, 229));

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, camera.get_transformation(GraphicsDevice));

            if (state == State.Game)
            {
                world.Draw(spriteBatch);
                if (online)
                    network.Draw(spriteBatch, gameTime);
                ui.Draw(spriteBatch);
            }
            else if (state == State.Menu)
            {
                spriteBatch.DrawString(font, "NetGame (" + Universal.GAME_VERSION + ")", new Vector2(camera.CX + 20, camera.CY + Universal.SMALL_SCREEN_HEIGHT - 190), Color.White);
                ipBox.Draw(spriteBatch, camera, gameTime);
                portBox.Draw(spriteBatch, camera, gameTime);
                nicknameBox.Draw(spriteBatch, camera, gameTime);
                connectButton.Draw(spriteBatch, camera);
            }
            cursor.Draw(spriteBatch, camera);

            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
