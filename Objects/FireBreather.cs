using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace SKX.Objects
{
    public class FireBreather : GameObject
    {
        protected Rectangle areaF;
        protected Rectangle areaB;
        protected uint DanaAttackDelay;
        protected Tongue Tongue;
        protected bool MultiAttack = false;
        protected bool Tongued;       // Unlike the dragon, the salamander can only attack
                                      // once until they walk into something or fall down to a different level
        protected Point TongueOffset;

        public FireBreather(Level level, ObjType type) : base(level, type)
        {

        }

        // Spawns a tongue in the right place
        public void Breathe()
        {
            if (Friendly) { return; }

            Tongued = true;
            Tongue = new Tongue(this, TongueOffset);
            Level.AddObject(Tongue);
        }

        protected bool CheckBreatheOnDana(int hrange = 16, int vrange = 24, uint attack_routine = 2)
        {
            // Salamanders only attack once per routine cycle
            if (!MultiAttack && Tongued) return false;
            if (Friendly) return false;

            Point front, back;

            if (Direction == Heading.Left)
            {
                front = Center - new Point(hrange, 0);
                back = Center ;
            }
            else
            {
                front = Center;
                back = Center - new Point(hrange, 0);
            }


            // make a sensor 8 pixels in front of dragon's mouth
            areaF = new Rectangle(front.X, front.Y, 16, vrange);
            areaB = new Rectangle(back.X, back.Y, 17, vrange);

            // is that sensor inside Dana's hit box (not hurt box)
            if (Level.Dana != null)
            {
                if (areaF.Intersects(Level.Dana.ProjectedBox))
                {
                    // Dana is in front
                    Timer = DanaAttackDelay;
                    Routine = attack_routine;
                    return true;
                }
                else if (areaB.Intersects(Level.Dana.ProjectedBox))
                {
                    // Dana is behind
                    TurnAround();

                    Timer = DanaAttackDelay;
                    Routine = attack_routine;
                    return true;
                }
            }
            return false;
        }
    }
}
