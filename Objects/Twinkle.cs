using System;
using System.Collections.Generic;
using System.Text;

namespace SKX.Objects
{

    /// <summary>
    /// Plays a short animation for effect and then usually removes itself.
    /// </summary>
    public class Twinkle : GameObject
    {
        public Action<Twinkle> OnFinished { get; set; }   // Invoked when animation finishes
        public bool AnimateForever { get; set; }          // Used in the animation test only

        public Twinkle(World world, bool blocks_magic = true) : base(world, ObjType.Effect)
        {
            BlocksMagic = blocks_magic;
        }

        public override void OnAnimationEnd()
        {
            if (AnimateForever)
            {
                AnimationCounter = 0;
                Animate = true;
                return;
            }
            OnFinished?.Invoke(this);
            Remove();
        }

    }

}
