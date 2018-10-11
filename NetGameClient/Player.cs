using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Lidgren.Network;
using NetGameShared;

namespace NetGameClient
{
    public class Player : CollideObject
    {
        private const float MOVE = 0.05f;

        public bool Connected { get; set; }
        public int ID { get; set; }
        public string Nickname { get; set; }
        public int HP { get; set; }
        public int MaxHP { get; set; }
        public int Ping { get; set; }
        public int Kills { get; set; }
        public int Assists { get; set; }
        public int Deaths { get; set; }
        public int FlagIndex { get; set; }
        public bool RespondMove { get; set; }
        public bool HittingBlock { get; set; }
        public bool Respawned { get; set; }
        public Team PlayerTeam { get; set; }
        public Tile HitBlock { get; set; }
        public NetConnection Connection { get; set; }
        public PlayerState State { get; set; }
        public Item[] Items { get; set; }
        public Item SelectedItem { get; set; }
        public Interval RespawnInterval { get; set; }

        public enum PlayerState { idle, moving, jumping, attacking };

        Network network;
        Sprite idleSprite, runSprite, jumpSprite, attackSprite, pixel, slashEffect, redFlag, blueFlag;
        Interval HPBarInterval, pickUpInterval, chatInterval;
        KeyboardState lastKeyState;
        MouseState lastMouseState;
        PlayerState previousState;
        float speed, maxSpeed, slow, jump, gravity, maxGravity;
        int previousX, previousY, sentIdle;
        bool isGravity, createdSlash, godMode;
        string chatMessage;

        public Player(string nickname, int x, int y, Color color, NetConnection connection) : base()
        {
            Nickname = nickname;
            X = x;
            Y = y;
            Connection = connection;
            InitVars();
        }

        public Player() : base()
        {
            Nickname = "asdf";
            InitVars();
        }

        private void InitVars()
        {
            idleSprite = ResourceManager.GetSprite("player");
            sprite = idleSprite;
            runSprite = ResourceManager.GetSprite("player_run");
            jumpSprite = ResourceManager.GetSprite("player_jump");
            attackSprite = ResourceManager.GetSprite("player_attack");
            pixel = ResourceManager.GetSprite("pixel");
            slashEffect = ResourceManager.GetSprite("effect_slash");
            redFlag = ResourceManager.GetSprite("item_redflag");
            blueFlag = ResourceManager.GetSprite("item_blueflag");
            RespawnInterval = new Interval();
            PlayerTeam = new Team("No Team", Color.Black);
            Items = new Item[3];
            for (int i = 0; i < Items.Length; i++)
                Items[i] = new Item(ItemName.None, 0, i, new Vector2(Universal.SCREEN_WIDTH - 138 + i * 45, 16));
            SelectedItem = Items[0];
            HPBarInterval = new Interval();
            pickUpInterval = new Interval();
            chatInterval = new Interval();
            Bounds = new Rectangle(10, 8, 13, 40);
            isGravity = true;
            godMode = false;
            sentIdle = 0;
            speed = 800f;
            maxSpeed = 250f;
            slow = 400f;
            jump = 500f;
            gravity = 1500f;
            maxGravity = 1500.0f;
            SetAnimSpeed(0.25f);
            State = PlayerState.idle;
            previousState = PlayerState.idle;
            Ping = 0;
            Kills = 0;
            Assists = 0;
            Deaths = 0;
            MaxHP = 50;
            HP = MaxHP;
            RespondMove = true;
            chatMessage = "";
        }

        public void Update(GameTime gameTime, KeyboardState lastKeyState, MouseState lastMouseState, bool drawBlockOutline, bool control)
        {
            RespawnInterval.Update(gameTime);
            chatInterval.Update(gameTime);
            if (!RespawnInterval.IsRunning)
            {
                KeyboardState keyState = Keyboard.GetState();
                MouseState mouseState = Mouse.GetState();
                this.lastKeyState = lastKeyState;
                this.lastMouseState = lastMouseState;
                float time = (float)gameTime.ElapsedGameTime.TotalSeconds;

                HPBarInterval.Update(gameTime);
                pickUpInterval.Update(gameTime);
                Move(time, keyState, mouseState, Client.world, drawBlockOutline, control);
                CheckCollisions(time, Client.world.WorldMap);
                StayInBounds(Client.world.WorldMap);
                Animate(gameTime);
            }
        }

        public void SetChatMessage(string chatMessage)
        {
            this.chatMessage = chatMessage;
            chatInterval.Start(5);
        }

        public void ShowHPBar()
        {
            HPBarInterval.Start(3);
        }

        public void SetNetwork(Network network)
        {
            this.network = network;
        }

        protected override void AnimationEnd()
        {
            if (State == PlayerState.attacking)
            {
                createdSlash = false;
                State = PlayerState.idle;
            }
        }

        public void RemoveAllItems()
        {
            for (int i = 0; i < Items.Length; i++)
                Items[i].SetItem(ItemName.None, 0, false);
            FlagIndex = 0;
        }

        private void Move(float time, KeyboardState keyState, MouseState mouseState, World world, bool drawBlockOutline, bool control)
        {
            if (control)
            {
                if (State == PlayerState.attacking)
                    maxSpeed = 100f;
                else
                    maxSpeed = 250f;
                if ((keyState.IsKeyDown(Keys.A) || keyState.IsKeyDown(Keys.D)))
                {
                    if (keyState.IsKeyDown(Keys.A))
                    {
                        DX -= speed * time;
                        spriteEffects = SpriteEffects.FlipHorizontally;
                    }
                    if (keyState.IsKeyDown(Keys.D))
                    {
                        DX += speed * time;
                        spriteEffects = SpriteEffects.None;
                    }
                    if (onFloor || State != PlayerState.jumping)
                        State = PlayerState.moving;
                }
                else
                {
                    if (DX > 0)
                        DX -= slow * time;
                    else if (DX < 0)
                        DX += slow * time;
                    if (Math.Abs(DX) < 10)
                        DX = 0;
                    if (State != PlayerState.attacking && (onFloor || State != PlayerState.jumping))
                        State = PlayerState.idle;
                }

                if (godMode)
                {
                    if ((keyState.IsKeyDown(Keys.W) || keyState.IsKeyDown(Keys.S)) && State != PlayerState.attacking)
                    {
                        if (keyState.IsKeyDown(Keys.W))
                        {
                            DY -= speed * time;
                        }
                        if (keyState.IsKeyDown(Keys.S))
                        {
                            DY += speed * time;
                        }
                        State = PlayerState.moving;
                    }
                    else
                    {
                        if (DY > 0)
                            DY -= slow * time;
                        else if (DY < 0)
                            DY += slow * time;
                        if (Math.Abs(DY) < 10)
                            DY = 0;
                        if (State != PlayerState.attacking)
                            State = PlayerState.idle;
                    }
                }

                if (keyState.IsKeyDown(Keys.W) && (onFloor || Y >= world.WorldMap.Height * Universal.TILE_SIZE - sprite.SourceHeight) && !godMode)
                {
                    DY = -jump;
                    onFloor = false;
                    State = PlayerState.jumping;
                    sprite.Frame = 0;
                }

                foreach (Item item in Items)
                {
                    if (item.Update(Items.ToList(), keyState, lastKeyState))
                        SelectedItem = item;
                }

                if (network != null)
                {
                    if (X != previousX || Y != previousY)
                    {
                        network.SendMove();
                        sentIdle = 0;
                    }
                    else
                    {
                        if (sentIdle < 60)
                        {
                            sentIdle++;
                            network.SendMove();
                        }
                    }

                    if (keyState.IsKeyDown(Keys.Q) && !lastKeyState.IsKeyDown(Keys.Q))
                        network.SendDropItem(SelectedItem.Index);

                    if (!pickUpInterval.IsRunning)
                    {
                        Vector2 checkPosition = new Vector2(X + BoundWidth / 2, Y + BoundHeight);
                        foreach (KeyValuePair<int, ItemDrop> entry in world.DropIndex)
                        {
                            ItemDrop drop = entry.Value;
                            if (drop.Selected)
                                network.SendPickUpItem(drop.SelectedItem.Index, drop.SelectedItem.Name);
                        }
                        if (world.RedTent.Selected || world.RedTent.WinSelected)
                            network.SendPickUpItem(world.RedTent.SelectedItem.Index, world.RedTent.SelectedItem.Name);
                        if (world.BlueTent.Selected || world.BlueTent.WinSelected)
                            network.SendPickUpItem(world.BlueTent.SelectedItem.Index, world.BlueTent.SelectedItem.Name);

                        pickUpInterval.Start(0.166f);
                    }

                    if (Client.WindowActive && SelectedItem.Type == ItemType.Weapon &&
                    mouseState.LeftButton == ButtonState.Pressed && State != PlayerState.attacking)
                        State = PlayerState.attacking;

                    if (drawBlockOutline && SelectedItem.Type == ItemType.Block && mouseState.LeftButton == ButtonState.Pressed && lastMouseState.LeftButton == ButtonState.Released)
                        network.SendAddBlock(SelectedItem.Index, SelectedItem.Name, mouseState.X, mouseState.Y);
                    //if (mouseState.RightButton == ButtonState.Pressed && lastMouseState.RightButton == ButtonState.Released)
                        //network.SendBlockChange(mouseState.X, mouseState.Y, false);
                }
            }

            if (previousState != State)
            {
                sprite.Frame = 0;
                sentIdle = 0;
                if (!control && previousState == PlayerState.attacking)
                    AnimationEnd();
                if (State == PlayerState.attacking)
                {
                    SetSprite(attackSprite);
                    SetAnimSpeed(0.15f);
                    loopAnimation = true;
                }
                else if (State == PlayerState.jumping)
                {
                    SetSprite(jumpSprite);
                    SetAnimSpeed(0f);
                    loopAnimation = false;
                }
                else if (State == PlayerState.moving)
                {
                    SetSprite(runSprite);
                    SetAnimSpeed(0.05f);
                    loopAnimation = true;
                }
                else
                {
                    SetSprite(idleSprite);
                    SetAnimSpeed(0.25f);
                    loopAnimation = true;
                }
                previousState = State;
            }

            if (State == PlayerState.jumping)
            {
                if (DY > 0 && animSpeed == 0)
                    SetAnimSpeed(0.05f);
                else if (DY <= 0 && animSpeed != 0)
                    SetAnimSpeed(0);
            }

            if (State == PlayerState.attacking && sprite.Frame == 2 && !createdSlash)
            {
                int offset = -16;
                if (FlippedHorizontally())
                {
                    world.AddEffect(slashEffect, new Vector2(X - 20, Y), 0.05f, true);
                    offset = -32;
                }
                else
                    world.AddEffect(slashEffect, new Vector2(X + 20, Y), 0.05f);
                Rectangle hitbox = new Rectangle((int)X + offset, (int)Y - 8, 64, 64);
                foreach (KeyValuePair<int, Mob> entry in world.MobIndex.ToList())
                {
                    Mob mobEntry = entry.Value;
                    if (hitbox.Intersects(mobEntry.CollideBox))
                    {
                        if (offset == -16)
                            mobEntry.DX += 300;
                        else
                            mobEntry.DX -= 300;
                        mobEntry.DY -= 400;
                    }
                }
                foreach (KeyValuePair<int, Player> entry in world.PlayerIndex.ToList())
                {
                    Player playerEntry = entry.Value;
                    if (this != playerEntry && hitbox.Intersects(playerEntry.CollideBox) &&
                        (playerEntry.PlayerTeam.Name == "No Team" || PlayerTeam.Name != playerEntry.PlayerTeam.Name))
                    {
                        if (offset == -16)
                            playerEntry.DX += 300;
                        else
                            playerEntry.DX -= 300;
                        playerEntry.DY -= 400;
                    }
                }
                createdSlash = true;
                if (network != null)
                    network.SendAttack();
            }

            if (DX > maxSpeed)
                DX = maxSpeed;
            else if (DX < -maxSpeed)
                DX = -maxSpeed;

            if (isGravity && !godMode)
            {
                DY += gravity * time;
                if (DY > Math.Abs(maxGravity))
                    DY = maxGravity;
            }

            previousX = (int)X;
            previousY = (int)Y;

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

        public void Draw(SpriteBatch spriteBatch, Camera camera, bool isMain)
        {
            base.Draw(spriteBatch);
            Vector2 fontSize = Client.font.MeasureString(Nickname);
            spriteBatch.Draw(pixel.Texture, new Rectangle((int)(X + 13 - fontSize.X / 2), IntY + sprite.Texture.Height + 2, (int)(fontSize.X + 6), (int)(fontSize.Y)), new Rectangle(1, 1, 1, 1), PlayerTeam.TeamColor * 0.5f);
            //Universal.DrawRectangleOutline(spriteBatch, new Rectangle((int)(X + 13 - fontSize.X / 2), IntY + sprite.Texture.Height + 2, (int)(fontSize.X + 6), (int)(fontSize.Y)), PlayerTeam.TeamColor);
            spriteBatch.DrawString(Client.font, Nickname, new Vector2(X + 16 - fontSize.X / 2, Y + sprite.Texture.Height + 2), Color.White);
            if (HPBarInterval.IsRunning)
            {
                spriteBatch.Draw(pixel.Texture, new Rectangle(IntX + 16 - MaxHP / 2, IntY - 2, HP, 4), new Rectangle(1, 1, 1, 1), Color.LimeGreen * 0.75f);
                Universal.DrawRectangleOutline(spriteBatch, new Rectangle(IntX + 16 - MaxHP / 2, IntY - 2, MaxHP, 4), Color.DarkGreen);
            }
            if (FlagIndex == 1)
                spriteBatch.Draw(redFlag.Texture, new Vector2(IntX, IntY - 24), Color.White);
            else if (FlagIndex == 2)
                spriteBatch.Draw(blueFlag.Texture, new Vector2(IntX, IntY - 24), Color.White);
            if (chatInterval.IsRunning)
            {
                fontSize = Client.font.MeasureString(chatMessage);
                spriteBatch.Draw(pixel.Texture, new Rectangle((int)(X + 13 - fontSize.X / 2), IntY - 12, (int)(fontSize.X + 6), (int)(fontSize.Y)), new Rectangle(1, 1, 1, 1), Color.Black * 0.5f);
                spriteBatch.DrawString(Client.font, chatMessage, new Vector2(X + 16 - fontSize.X / 2, Y - 12), Color.White);
            }
        }
    }
}
