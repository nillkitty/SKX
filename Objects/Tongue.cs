using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace SKX.Objects
{
    /// <summary>
    /// The fiery breath/tongue of a dragon, salamander, etc.
    /// </summary>
    public class Tongue : GameObject
    {

        public GameObject Parent;       // Parent enemy

        public Tongue(GameObject parent, Point offset) : base(parent.Level, ObjType.Effect)
        {
            HurtsPlayer = true;
            HitBox = new Rectangle(1, 1, 14, 15);
            HurtBox = new Rectangle(2, 2, 12, 12);
            Parent = parent;
            Position = Parent.Position + parent.Direction.RotateOffset(new Point(16, -3) + offset);
            FlipX = Parent.FlipX;
            FlipY = Parent.FlipY;
            if (Parent.Direction == Heading.Up || Parent.Direction == Heading.Down)
                Animation = Animation.TongueY;
            else
                Animation = Animation.Tongue;
        }

        public override void Init()
        {
            Sound.Hiss.Play();
            base.Init();
        }

        public override void OnAnimationEnd()
        {
            // Remove the tongue at the end of its animation
            Remove();
            base.OnAnimationEnd();
        }

        public override void Update(GameTime gameTime)
        {
            // Remove the tongue if the enemy goes away
            if (Parent.Type == ObjType.None) Remove();
            base.Update(gameTime);
        }

    }
}
