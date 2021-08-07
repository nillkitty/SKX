using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace SKX.Objects
{
    public class StarPath : GameObject
    {
        public Point StartPos;
        public Point Target;
        public int State;
        public int Speed;
        public bool Fast;
        public Action OnFinished;

        private int step;

        public StarPath(Level level, Point start, Point end, int speed, bool fast) : base(level, ObjType.Effect)
        {
            StartPos = start;
            Target = end;
            Speed = speed;
            Fast = fast;
            step = 0;

        }

        public override void Init()
        {
            Animation = Animation.SparkleLoop;
            if (Fast) AnimSpeed = 2;
            State = 1;
        }

        public override void Update(GameTime gameTime)
        {

            // scale the step
            float nStep = (float)step / (float)Speed;

            // lerp the position -- x is straightforward
            X = MathHelper.Lerp(StartPos.X, Target.X, nStep);
            // the y component of the path juts up about 16 pixels to make a slight arc
            // which looks nicer when the key and door and right next to each other
            var Ym = StartPos.Y + (Target.Y - StartPos.Y) / 2 - 16;
            var Y1 = MathHelper.Lerp(StartPos.Y, Ym, nStep);
            var Y2 = MathHelper.Lerp(Ym, Target.Y, nStep);
            Y = MathHelper.SmoothStep(Y1, Y2, nStep);

            step++;
            if (step > Speed)
            {
                State = 0;
                OnFinished?.Invoke();
                Remove();
            }

            base.Update(gameTime);
        }

    }
}
