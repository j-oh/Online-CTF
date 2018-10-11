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
    public class Camera
    {
        protected float _zoom; // Camera Zoom
        public Matrix _transform; // Matrix Transform
        public Vector2 _pos; // Camera Position
        protected float _rotation; // Camera Rotation

        public Camera()
        {
            _zoom = 1.0f;
            _rotation = 0.0f;
            _pos = Vector2.Zero;
        }

        // Sets and gets zoom
        public float Zoom
        {
            get { return _zoom; }
            set { _zoom = value; if (_zoom < 0.1f) _zoom = 0.1f; } // Negative zoom will flip image
        }

        public float Rotation
        {
            get { return _rotation; }
            set { _rotation = value; }
        }

        // Auxiliary function to move the camera
        public void Move(Vector2 amount)
        {
            _pos += amount;
        }
        // Get set position
        public Vector2 Position
        {
            get { return _pos; }
            set { _pos = value; }
        }

        public float X
        {
            get { return _pos.X; }
            set { _pos = new Vector2(value, _pos.Y); }
        }

        public int IntX
        {
            get { return (int)_pos.X; }
        }

        public int CX
        {
            get { return (int)_pos.X - Universal.SCREEN_WIDTH / 2; }
        }

        public float Y
        {
            get { return _pos.Y; }
            set { _pos = new Vector2(_pos.X, value); }
        }

        public int IntY
        {
            get { return (int)_pos.Y; }
        }

        public int CY
        {
            get { return (int)_pos.Y - Universal.SCREEN_HEIGHT / 2; }
        }

        public Matrix get_transformation(GraphicsDevice graphicsDevice)
        {
            _transform =       // Thanks to o KB o for this solution
              Matrix.CreateTranslation(new Vector3(-_pos.X, -_pos.Y, 0)) *
                                         Matrix.CreateRotationZ(Rotation) *
                                         Matrix.CreateScale(new Vector3(Zoom, Zoom, 1)) *
                                         Matrix.CreateTranslation(new Vector3(Universal.SCREEN_WIDTH * 0.5f, Universal.SCREEN_HEIGHT * 0.5f, 0));
            return _transform;
        }
    }
}
