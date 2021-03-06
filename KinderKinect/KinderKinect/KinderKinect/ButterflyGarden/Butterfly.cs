﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KinderKinect.Utils.Gui;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Diagnostics;
using Microsoft.Xna.Framework.Content;
using KinderKinect.Utils;
using SkinnedModel;
using DPSF;

namespace KinderKinect.ButterflyGarden
{
    class Butterfly
    {
        private int padX = 50, padY = 50, selectionMilis = 300;
        private Stopwatch timeSelected;
        public delegate void ButterflySelectedEventHandler(object sender, EventArgs e);
        public event ButterflySelectedEventHandler Selected;
        private bool hidden;
        private AnimationPlayer animationPlayer;


        public int SelectionTime
        {
            get
            {
                return selectionMilis;
            }
            set
            {
                selectionMilis = value;
            }
        }


        private double positionScale;

        private bool _selectable = false;
        public bool Selectable
        {
            get
            {
                return _selectable;
            }
            set
            {
                _selectable = value;
            }
        }

        //AI needs these members
        private Vector2 wanderDirection;
        public Vector2 WanderDirection
        {
            get
            {
                return wanderDirection;
            }
            set
            {
                wanderDirection = value;
            }
        }

        private Vector2 tether;
        public Vector2 TetherPoint
        {
            get
            {
                return tether;
            }
        }


        /// <summary>
        /// I do this to simplify my content loading/unloading job
        /// </summary>
        public static Texture2D[] ButterflyTextures;

        private Matrix[] transforms;
        private Matrix world;
        public Matrix World
        {
            get
            {
                return world;
            }
        }

        private Model myModel;

        private Vector3 position;

        int msOffset;

        private ButterflyColors myColor;
        public ButterflyColors Color
        {
            get
            {
                return myColor;
            }
            set
            {
                myColor = value;
            }
        }

        public enum ButterflyColors
        {
            Red,
            Yellow,
            Blue,
            Green,
            Purple,
            Black,
            White,
            Brown,
            Orange,
            Pink
        }

        private Hitbox hitbox;
        public Hitbox HitBox
        {
            get
            {
                return hitbox;
            }
        }

        private static Random rand = new Random();

        private Vector3 startPosition;

        Viewport viewPort;
        public Viewport ViewPort
        {
            get
            {
                return viewPort;
            }
        }
        
        Matrix projection;
        public Matrix Projection
        {
            get
            {
                return projection;
            }
        }

        Matrix view;
        public Matrix View
        {
            get
            {
                return view;
            }
        }


        public Butterfly(Vector3 Position, float rotation, Viewport ViewPort, Matrix View, Matrix Projection, ButterflyColors color)
        {

            world = Matrix.CreateFromAxisAngle(Vector3.Right, -1 * (float)(Math.PI / 2f)) * Matrix.CreateTranslation(Position);
            Vector3 ScreenProjection = ViewPort.Project(Position, Projection, View, Matrix.CreateScale(0.1f) * Matrix.CreateFromAxisAngle(Vector3.Right, -1 * (float)(Math.PI / 2f)) * Matrix.CreateTranslation(Position));
            hitbox = new Hitbox(new Rectangle((int)(ScreenProjection.X - padX), (int)(ScreenProjection.Y - padY), 2 * padX, 2 * padY));
            viewPort = ViewPort;
            projection = Projection;
            view = View;
            hitbox.Entered += new Hitbox.EnteredEventHandler(hitbox_Entered);
            hitbox.Exited += new Hitbox.ExitedEventHandler(hitbox_Exited);
            timeSelected = new Stopwatch();
            myColor = color;
            hidden = false;
            position = Position;
            startPosition = Position * 20;
            msOffset = rand.Next(0, 500);
            wanderDirection = new Vector2();
            tether = Vector2.Transform(new Vector2(Position.X, Position.Y), Matrix.CreateScale(0.1f));
        }

        public void Hide()
        {
            hidden = true;
        }

        public void Show()
        {
            hidden = false;
        }

        public void setPosition(Vector3 Position)
        {
            position = Position;
            world = Matrix.CreateFromAxisAngle(Vector3.Right, -1 * (float)(Math.PI / 2f)) * Matrix.CreateTranslation(Position);
        }

        public Vector3 getPosition()
        {
            return position;
        }


        public bool GetHidden()
        {
            return hidden;
        }

        public void LoadContent(ContentManager content)
        {
            myModel = content.Load<Model>(@"Models\butterfly");
            SkinningData data = myModel.Tag as SkinningData;
            
            if (data == null)
                throw new InvalidOperationException
                    ("This model does not contain a SkinningData tag.");

            // Create an animation player, and start decoding an animation clip.
            animationPlayer = new AnimationPlayer(data);

            AnimationClip clip = data.AnimationClips["ArmatureAction_001"];

            animationPlayer.StartClip(clip);
            animationPlayer.Update(new TimeSpan(0, 0, 0, msOffset), true, Matrix.Identity);
            
        }

        void hitbox_Exited(object sender, EventArgs e)
        {
            timeSelected.Stop();
            timeSelected.Reset();
        }

        void hitbox_Entered(object sender, EventArgs e)
        {
            timeSelected.Reset();
            timeSelected.Start();
        }

        public void Update(GameTime gameTime)
        {
            Vector3 ScreenProjection = viewPort.Project(position, projection, view, Matrix.CreateScale(0.1f) * Matrix.CreateFromAxisAngle(Vector3.Right, -1 * (float)(Math.PI / 2f)) * Matrix.CreateTranslation(position));
            hitbox.HitArea = new Rectangle((int)(ScreenProjection.X - padX), (int)(ScreenProjection.Y - padY), 2 * padX, 2 * padY);
            if (timeSelected.ElapsedMilliseconds >= selectionMilis)
            {
                Selected(this, new EventArgs());
            }
            animationPlayer.Update(gameTime.ElapsedGameTime, true, Matrix.Identity);
            
        }

        public void Draw(Camera myCam, SpriteBatch sb)
        {
            if (!hidden)
            {
                transforms = animationPlayer.GetSkinTransforms();

                // Draw the model. A model can have multiple meshes, so loop.
                foreach (ModelMesh mesh in myModel.Meshes)
                {
                    // This is where the mesh orientation is set, as well 
                    // as our camera and projection.
                    foreach (SkinnedEffect effect in mesh.Effects)
                    {
                        //effect.GraphicsDevice.SamplerStates[0] = SamplerState.LinearWrap
                        effect.SetBoneTransforms(transforms);
                        effect.EnableDefaultLighting();
                        effect.PreferPerPixelLighting = true;
                        effect.World = Matrix.CreateScale(0.1f) * world;
                        effect.View = myCam.ViewMatrix;
                        effect.Projection = myCam.ProjectionMatrix;
                        effect.DiffuseColor = Microsoft.Xna.Framework.Color.White.ToVector3();
                        effect.Texture = ButterflyTextures[(int)(myColor)];
                    }
                    mesh.Draw();
                }

                //For Debugging
               // sb.Begin();
               // sb.Draw(ButterflyTextures[0], hitbox.HitArea, Microsoft.Xna.Framework.Color.White);
               // sb.End();
                 
            }
        }
    }
}
