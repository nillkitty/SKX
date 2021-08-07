using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using System;
using System.Collections.Generic;
using System.Text;

namespace SKX.Objects
{
    /// <summary>
    /// Class of object that hugs walls (fireballs, sparklers, etc.)
    /// </summary>
    public abstract class Wallhugger : GameObject
    {

        /* I have no idea why the algorithm that Open Solomon's Key uses is so fucking complicated.
         * Yes this took a few moments to get right but I think this is pretty elegant */

        public double Speed = 1;
        public Animation XAnimation;
        public Animation YAnimation;
        public Heading HugDirection = Heading.None;
        public Point HugSensorTrailing, HugSensorLeading;
        public Point HugCell;
        public bool Hugging = true;
        public uint Stickiness = 8;
        public int SensorDepth = 4;
        private uint ProhibitTimer = 0;
        public double XWalk, YWalk;
        protected ColTypes ColType = ColTypes.Solid;
        protected Func<Cell, bool> ColTest = Layout.IsSolid;

        /// <summary>
        /// Raised when the level cell the object is hugging has changed
        /// </summary>
        /// <param name="cell"></param>
        public virtual void HugBlockUpdate(Point cell) { }

        public Wallhugger(World world, ObjType type) : base(world, type)
        {
        }

        // After a direction change,  update the velocity components and get the 
        // animation correct.
        void updateVelocityAndAnimation()
        {
            switch(Direction)
            {
                case Heading.Down:
                    Vy = Speed; Vx = 0; Animation = YAnimation; FlipX = false; FlipY = true;  break;
                case Heading.Up:
                    Vy = -Speed; Vx = 0; Animation = YAnimation; FlipX = false; FlipY = false;  break;
                case Heading.Left:
                    Vx = -Speed; Vy = 0; Animation = XAnimation; FlipX = true; FlipY = false;  break;
                case Heading.Right:
                    Vx = Speed; Vy = 0; Animation = XAnimation; FlipX = false; FlipY = false;  break;
            }
        }
        
        public override void Update(GameTime gameTime)
        {


            if (Hugging)
            {
                // Update our velocities and animation
                updateVelocityAndAnimation();

                // Check what's right in front of us
                Collision = CollideLevel(Direction, ColType, 2, 2, true);
                if (ColTest(Collision.BlockCell))
                {
                    // We're hitting a wall (or an inside corner)
                    // Push out of any wall overlap we dug ourselves into
                    PushOut(Collision.Overlap);
                    // Remember this wall (in case Dana breaks it while we're on it)
                    HugCell = Collision.Block.Value;
                    HugBlockUpdate(Collision.Block.Value);
                    // And change direction
                    changeDirection(false);
                }

                // Move
                Move();

                // If we were hugging a wall already ...
                if (HugDirection != Heading.None && ProhibitTimer == 0)
                {
                    // ... see if we still are

                    // Update the hug sensors
                    HugSensorTrailing = GetTrailingSensorPoint() + Center;
                    HugSensorLeading = GetLeadingSensorPoint() + Center;

                    // Check collision
                    var col2 = CollideLevelAbsolute(HugSensorTrailing, ColTest);
                    var col3 = CollideLevelAbsolute(HugSensorLeading, ColTest);

                    var ok2 = col2.HasValue && Level.Layout[col2.Value] != Cell.Wick;
                    var ok3 = col3.HasValue && Level.Layout[col3.Value] != Cell.Wick;

                    // Are both sensors floating in mid-air?
                    if (!ok2 & !ok3)
                    {
                        // We just lost the wall ... is it because it broke away, or because we hit an outside corner?
                        if (!ColTest(Level.Layout[HugCell]) || Level.Layout[HugCell] == Cell.Wick)
                        {
                            // The wall was broken (or we slid across a Wick cell) .. go back into free flying
                            HugDirection = Heading.None;
                        }
                        else
                        {
                            // We hit an outside corner, change direction, and ignore the sensors for a few ticks
                            // so we don't keep turning around endlessly if we overshot the corner
                            changeDirection(true);
                            ProhibitTimer = Stickiness;
                        }
                    }
                    else
                    {
                        // Still hugging a wall -- update which wall we're clinging to;  favoring
                        // the one slightly behind us rather than in front
                        if (col2.HasValue)
                        {
                            HugCell = col2.Value;
                            HugBlockUpdate(HugCell);
                        }
                        else if (col3.HasValue)
                        {
                            HugCell = col3.Value;
                            HugBlockUpdate(HugCell);
                        }
                    }

                } else if (ProhibitTimer > 0)
                {
                    // Count down ticks until we're allowed to turn on an outside corner again
                    ProhibitTimer--;
                }

            }

            base.Update(gameTime);
        }

        /// <summary>
        /// Gets the relative position (to our center point) of a sensor that sits slightly backward,
        /// but offset into the wall we're hugging
        /// </summary>
        protected Point GetTrailingSensorPoint()
        {
            var p = SensorDepth;
            var xo = HitBox.Width / 2;
            var yo = HitBox.Height / 2;

            return (Direction, HugDirection)
            switch
            {
                (Heading.Right, Heading.Down) =>        new Point(-xo, yo + p),
                (Heading.Right, Heading.Up) =>          new Point(-xo, -yo - p),
                (Heading.Left, Heading.Down) =>         new Point(xo, yo + p),
                (Heading.Left, Heading.Up) =>           new Point(xo, -yo - p),
                (Heading.Up, Heading.Right) =>          new Point(xo + p, yo),
                (Heading.Up, Heading.Left) =>           new Point(-xo - p, yo),
                (Heading.Down, Heading.Right) =>        new Point(xo + p, -yo),
                (Heading.Down, Heading.Left) =>         new Point(-xo - p, -yo),
                _ =>                                    new Point(0,0),    // Doesn't matter
            };
        }

        /// <summary>
        /// Gets the relative position (to our center point) of a sensor that sits slightly on front of us,
        /// but offset into the wall we're hugging.  This is necessary so that when we make a turn on an
        /// outside corner, this sensor immediately digs into the new wall we're now hugging (just like how
        /// cars/bikes need two axles in order to hug/track the ground over bumps, or tires on both sides to
        /// hug/track the ground around lateral curves)
        /// </summary>
        protected Point GetLeadingSensorPoint()
        {
            var p = SensorDepth;
            var xo = HitBox.Width / 2;
            var yo = HitBox.Height / 2;

            return (Direction, HugDirection)
            switch
            {
                (Heading.Right, Heading.Down) => new Point(xo, yo + p),
                (Heading.Right, Heading.Up) => new Point(xo, -yo - p),
                (Heading.Left, Heading.Down) => new Point(-xo, yo + p),
                (Heading.Left, Heading.Up) => new Point(-xo, -yo - p),
                (Heading.Up, Heading.Right) => new Point(xo + p, -yo),
                (Heading.Up, Heading.Left) => new Point(-xo - p, -yo),
                (Heading.Down, Heading.Right) => new Point(xo + p, yo),
                (Heading.Down, Heading.Left) => new Point(-xo - p, yo),
                _ => new Point(0, 0),    // Doesn't matter 
            };
        }
            

        /// <summary>
        /// Changes directions at a corner -- the direction we turn is different depending on whether
        /// it's an inside corner (or wall we flew at), or an outside corner
        /// </summary>
        void changeDirection(bool outsideCorner)
        {
            // If it's an inside corner, the direction we were heading is now the wall we'll be hugging
            HugDirection = Direction;
            // If it's an outside corner,  then it's the opposite
            if (outsideCorner) HugDirection = HugDirection.Opposite();
            // Rotate one way or the other based on which type of corner it is
            Rotate(outsideCorner);               
        }

        public override void Render(SpriteBatch batch)
        {
            base.Render(batch);

            // Draw debug information
            if (Game.ShowCollision)
            {
                batch.FillRectangle(new Rectangle(HugSensorTrailing, new Point(2, 2)), Color.Magenta);
                batch.FillRectangle(new Rectangle(HugSensorLeading, new Point(2, 2)), Color.Cyan);
                batch.DrawString(Direction.ToString().Substring(0, 1), Center - new Point(4, 4), Color.Black);
                //batch.DrawString(HugDirection.ToString().Substring(0, 1), Center + new Point(4, 4), Color.Gray);
                batch.DrawString(ProhibitTimer.ToString("X"), Center + new Point(4, 4), Color.Gray);
            }
        }
    }
}
