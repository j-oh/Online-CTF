using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Lidgren.Network;
using NetGameShared;

namespace NetGameServer
{
    public class Server
    {
        public static Random random;
        const int timeout = 60;
        World world;
        Network network;
        Timer updateTimer;

        public Server()
        {
            Console.WriteLine("NetGame Server (" + Universal.GAME_VERSION + ")");
            string serverName = "My Server";
            random = new Random();
            world = new World();
            network = new Network();
            network.Start(world, serverName);
            world.SetNetwork(network);
            updateTimer = new Timer(1000 / Universal.FRAME_RATE);
            updateTimer.Elapsed += new ElapsedEventHandler(UpdateElapsed);
            updateTimer.Start();
            while (true)
            {
                network.Update();
            }
        }

        ~Server()
        {
            network.Shutdown();
        }

        private void UpdateElapsed(object sender, ElapsedEventArgs e)
        {
            world.Update(1000f / Universal.FRAME_RATE);
        }
    }
}
