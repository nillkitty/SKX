using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace SKX
{
    public class Demo : BundleItem
    {
        public override Story Story { get; set; }
        public override int RoomNumber { get; set; }
        public uint Duration { get; set; }
        public List<DemoFrame> Frames { get; set; } = new List<DemoFrame>();

    }

    public struct DemoFrame
    {
        public uint t;
        public BindingState s;
    }



}
