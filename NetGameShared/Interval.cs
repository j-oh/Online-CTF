using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace NetGameShared
{
    public class Interval
    {
        public bool IsRunning { get { return Left > 0; } }
        public float Left { get; private set; }

        bool manualStop;

        public Interval()
        {
            Left = 0;
        }

        public void Update(GameTime gameTime)
        {
            if (Left > 0)
            {
                Left -= (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (Left <= 0)
                {
                    if (manualStop)
                        Left = 0.01f;
                    else
                        Left = 0;
                }
            }
            else if (manualStop)
                Left = 0.01f;
        }

        public void Update(float msElapsed)
        {
            if (Left > 0)
            {
                Left -= msElapsed / 1000;
                if (Left <= 0)
                {
                    if (manualStop)
                        Left = 0.01f;
                    else
                        Left = 0;
                }
            }
            else if (manualStop)
                Left = 0.01f;
        }

        public void Start(float seconds)
        {
            Left = seconds;
        }

        public void Start(float seconds, bool manualStop)
        {
            Left = seconds;
            this.manualStop = manualStop;
        }

        public void Reset()
        {
            Left = 0;
            manualStop = false;
        }
    }
}
