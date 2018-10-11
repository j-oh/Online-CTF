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
    class UI
    {
        GameObject bar;
        List<Button> buttonList;
        Button inventoryButton, statsButton, socialButton, button1, button2, button3, optionsButton;
        World world;
        Camera camera;
        KeyboardState keyState;
        MouseState mouseState;
        SpriteFont font;
        Texture2D pixel, blockoutline;
        GameMode serverMode;
        int infoHeight;
        bool barActivated, drawBlockOutline;
        string modeName, rules, controls;

        public UI(World world, Camera camera, SpriteFont font)
        {
            bar = new GameObject(ResourceManager.GetSprite("menu_bar"), new Vector2(384,432));
            //button3 = new Button(ResourceManager.GetSprite("button_options"), new Vector2(388, 444));
            inventoryButton = new Button(ResourceManager.GetSprite("button_options"), new Vector2(400, 444));
            statsButton = new Button(ResourceManager.GetSprite("button_options"), new Vector2(440, 444));
            socialButton = new Button(ResourceManager.GetSprite("button_options"), new Vector2(480, 444));
            button1 = new Button(ResourceManager.GetSprite("button_options"), new Vector2(520, 444));
            button2 = new Button(ResourceManager.GetSprite("button_options"), new Vector2(560, 444));
            optionsButton = new Button(ResourceManager.GetSprite("button_options"), new Vector2(600, 444));
            buttonList = new List<Button>();
            buttonList.Add(inventoryButton);
            buttonList.Add(statsButton);
            buttonList.Add(socialButton);
            buttonList.Add(button1);
            buttonList.Add(button2);
            //buttonList.Add(button3);
            buttonList.Add(optionsButton);
            barActivated = false;
            drawBlockOutline = false;
            this.world = world;
            this.camera = camera;
            this.font = font;
            pixel = ResourceManager.GetSprite("pixel").Texture;
            blockoutline = ResourceManager.GetSprite("blockoutline").Texture;
            infoHeight = 16;
            keyState = Keyboard.GetState();
            mouseState = Mouse.GetState();
            serverMode = world.ServerMode;
            modeName = "Free For All";
            rules = "";
            controls = "A and D - Move Left and Right\nW - Jump\nLeft Mouse - Use Item\nRight Mouse - Destroy Blocks\n1 and 2 - Item Slots 1 and 2\n3 - Equipment Slot\nQ - Drop Selected Item\nEnter - Chat";
        }

        public void Update(MouseState lastMouseState, bool drawBlockOutline)
        {
            keyState = Keyboard.GetState();
            mouseState = Mouse.GetState();
            this.drawBlockOutline = drawBlockOutline;

            barActivated = (mouseState.X > 384 && mouseState.Y > 432);
            if (barActivated)
            {
                if (bar.Y > 432)
                    bar.Y += (432 - bar.Y) / 4;
            }
            else
            {
                if (bar.Y < 464)
                    bar.Y += (464 - bar.Y) / 4;
            }
            foreach (Button button in buttonList)
            {
                button.Y = bar.Y + 12;
                button.Update(mouseState.X, mouseState.Y);
            }

            UpdateInfo();
        }

        public void UpdateInfo()
        {
            if (serverMode != world.ServerMode)
            {
                switch (world.ServerMode)
                {
                    case GameMode.FreeForAll:
                    default:
                        modeName = "Free For All";
                        rules = "Everyone is your enemy! Try to rack up\nthe most kills, but watch your back.";
                        break;
                    case GameMode.TeamDeathmatch:
                        modeName = "Team Deathmatch";
                        rules = "Work together to take down the enemy team!\nYour team's tickets go down when a team\nmember dies, until one team has no tickets left.";
                        break;
                    case GameMode.CaptureTheFlag:
                        modeName = "Capture The Flag";
                        rules = "Take the enemy flag from their base while\nprotecting your own! Carry the flag back to\nyour base to secure victory. Don't let your\ntickets run out either!\n\nNote: Your own flag must be at home to cap.";
                        break;
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            int blockX = (camera.CX + mouseState.X) / 16;
            int blockY = (camera.CY + mouseState.Y) / 16;
            Item selectedItem = world.MainPlayer.SelectedItem;

            if (drawBlockOutline)
            {
                spriteBatch.Draw(blockoutline, new Vector2(blockX * 16 - 2, blockY * 16 - 2), new Color(Color.White, 0.15f));
                if (selectedItem.Sprite != null && mouseState.RightButton == ButtonState.Released)
                    spriteBatch.Draw(selectedItem.Sprite.Texture, new Vector2(blockX * 16, blockY * 16), new Color(Color.White, 0.5f));
            }
            else if (selectedItem.Sprite != null)
                spriteBatch.Draw(selectedItem.Sprite.Texture, new Vector2(camera.CX + mouseState.X - selectedItem.Sprite.SourceWidth / 2, camera.CY + mouseState.Y - selectedItem.Sprite.SourceHeight / 2), new Color(Color.White, 0.5f));

            foreach (Item item in world.MainPlayer.Items)
                item.Draw(spriteBatch, camera);

            if (world.MainPlayer.RespawnInterval.IsRunning)
            {
                int respawnTime = (int)world.MainPlayer.RespawnInterval.Left;
                spriteBatch.Draw(pixel, new Rectangle(camera.CX, camera.IntY - 4, Universal.SCREEN_WIDTH, 24), new Color(Color.Black, 128));
                Universal.DrawStringMore(spriteBatch, font, String.Format("Respawning in {0}...", respawnTime), camera.Position, Color.White, Align.Center, true);
            }

            //bar.Draw(spriteBatch, camera);
            //foreach (Button button in buttonList)
            //button.Draw(spriteBatch, camera);
            Universal.DrawStringMore(spriteBatch, font, Universal.GAME_VERSION, new Vector2(camera.CX + 10, camera.CY + 8), Color.White, Align.Left, true);
            if (keyState.IsKeyDown(Keys.Tab))
            {
                int playerCount = world.PlayerIndex.Count;
                int teamCount = world.TeamCount;
                infoHeight = playerCount * 16 + teamCount * 16 + 16;

                spriteBatch.Draw(pixel, new Rectangle(camera.IntX - 280, camera.IntY - infoHeight / 2, 560, 16), new Color(Color.Black, 64));
                Universal.DrawStringMore(spriteBatch, font, "Players Online: " + playerCount, new Vector2(camera.IntX - 276, camera.IntY - infoHeight / 2), Color.White, Align.Left, true);
                Universal.DrawStringMore(spriteBatch, font, world.ServerName, new Vector2(camera.IntX, camera.IntY - infoHeight / 2), Color.White, Align.Center, true);
                Universal.DrawStringMore(spriteBatch, font, modeName, new Vector2(camera.IntX + 276, camera.IntY - infoHeight / 2), Color.White, Align.Right, true);

                int entryCount = 0, lastTeamY = camera.IntY - infoHeight / 2 + 16;
                bool setTeam = false;
                List<Player> teamList = new List<Player>();
                Team currentTeam = new Team("No Team", Color.Black);
                foreach(KeyValuePair<int, Player> entry in world.PlayerIndex.OrderBy(o => o.Value.PlayerTeam.Name).ToList())
                {
                    Player player = entry.Value;
                    if (!setTeam)
                    {
                        currentTeam = player.PlayerTeam;
                        teamList.Add(player);
                        setTeam = true;
                    }
                    else if (currentTeam.Name != player.PlayerTeam.Name)
                    {
                        DrawTeamInfo(spriteBatch, currentTeam, teamList, ref entryCount, ref lastTeamY);
                        teamList.Add(player);
                        currentTeam = player.PlayerTeam;
                    }
                    else
                        teamList.Add(player);
                }
                if (teamList.Count > 0)
                {
                    DrawTeamInfo(spriteBatch, currentTeam, teamList, ref entryCount, ref lastTeamY);
                }

                Universal.DrawRectangleOutline(spriteBatch, new Rectangle(camera.IntX - 280, camera.IntY - infoHeight / 2, 560, infoHeight), Color.Black);

                spriteBatch.Draw(pixel, new Rectangle(camera.CX + 20, camera.IntY - 150, 320, 300), new Color(Color.Black, 200));
                Universal.DrawStringMore(spriteBatch, font, "Rules: " + modeName, new Vector2(camera.CX + 30, camera.IntY - 140), Color.White, Align.Left, true);
                Universal.DrawStringMore(spriteBatch, font, rules, new Vector2(camera.CX + 40, camera.IntY - 120), Color.White, Align.Left, true);
                Universal.DrawStringMore(spriteBatch, font, "Controls", new Vector2(camera.CX + 30, camera.IntY), Color.White, Align.Left, true);
                Universal.DrawStringMore(spriteBatch, font, controls, new Vector2(camera.CX + 40, camera.IntY + 20), Color.White, Align.Left, true);
            }
        }

        private void DrawTeamInfo(SpriteBatch spriteBatch, Team currentTeam, List<Player> teamList, ref int entryCount, ref int lastTeamY)
        {
            spriteBatch.Draw(pixel, new Rectangle(camera.IntX - 280, lastTeamY, 560, (teamList.Count + 1) * 16), new Color(currentTeam.TeamColor, 64));
            spriteBatch.Draw(pixel, new Rectangle(camera.IntX - 280, lastTeamY, 560, 1), Color.Black);
            spriteBatch.Draw(pixel, new Rectangle(camera.IntX - 105, lastTeamY, 1, (teamList.Count + 1) * 16), Color.Black);
            Universal.DrawStringMore(spriteBatch, font, "Kills", new Vector2(camera.IntX - 70, lastTeamY), Color.White, Align.Center, true);
            spriteBatch.Draw(pixel, new Rectangle(camera.IntX - 35, lastTeamY, 1, (teamList.Count + 1) * 16), Color.Black);
            Universal.DrawStringMore(spriteBatch, font, "Assists", new Vector2(camera.IntX, lastTeamY), Color.White, Align.Center, true);
            spriteBatch.Draw(pixel, new Rectangle(camera.IntX + 35, lastTeamY, 1, (teamList.Count + 1) * 16), Color.Black);
            Universal.DrawStringMore(spriteBatch, font, "Deaths", new Vector2(camera.IntX + 70, lastTeamY), Color.White, Align.Center, true);
            spriteBatch.Draw(pixel, new Rectangle(camera.IntX + 105, lastTeamY, 1, (teamList.Count + 1) * 16), Color.Black);
            spriteBatch.Draw(pixel, new Rectangle(camera.IntX + 240, lastTeamY, 1, (teamList.Count + 1) * 16), Color.Black);
            Universal.DrawStringMore(spriteBatch, font, "Ping", new Vector2(camera.IntX + 276, lastTeamY), Color.White, Align.Right, true);
            spriteBatch.Draw(pixel, new Rectangle(camera.IntX - 280, lastTeamY + 16, 560, 1), Color.Black);
            Universal.DrawStringMore(spriteBatch, font, currentTeam.Name, new Vector2(camera.IntX - 276, lastTeamY), Color.White, Align.Left, true);

            entryCount++;
            foreach (Player teamPlayer in teamList)
            {
                DrawPlayerInfo(spriteBatch, teamPlayer, entryCount);
                entryCount++;
            }
            lastTeamY += entryCount * 16;
            teamList.Clear();
        }

        private void DrawPlayerInfo(SpriteBatch spriteBatch, Player player, int entryCount)
        {
            Universal.DrawStringMore(spriteBatch, font, player.Nickname, new Vector2(camera.IntX - 276, camera.IntY + 16 - infoHeight / 2 + entryCount * 16), Color.White, Align.Left, true);
            Universal.DrawStringMore(spriteBatch, font, Convert.ToString(player.Kills), new Vector2(camera.IntX - 70, camera.IntY + 16 - infoHeight / 2 + entryCount * 16), Color.White, Align.Center, true);
            Universal.DrawStringMore(spriteBatch, font, Convert.ToString(player.Assists), new Vector2(camera.IntX, camera.IntY + 16 - infoHeight / 2 + entryCount * 16), Color.White, Align.Center, true);
            Universal.DrawStringMore(spriteBatch, font, Convert.ToString(player.Deaths), new Vector2(camera.IntX + 70, camera.IntY + 16 - infoHeight / 2 + entryCount * 16), Color.White, Align.Center, true);
            Universal.DrawStringMore(spriteBatch, font, Convert.ToString(player.Ping), new Vector2(camera.IntX + 276, camera.IntY + 16 - infoHeight / 2 + entryCount * 16), Color.White, Align.Right, true);
        }
    }
}
