using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using System;
using System.Collections.Generic;
using System.Text;

namespace SKX.Editor
{
    /// <summary>
    /// Edits dynamic camera resize routines
    /// </summary>
    public class CamerasMode : EditorMode
    {
        public int SelectedResize;      // Selected Resize index
        private bool TargetMode;        // True=editing target boundary rect; false=editing trigger rect

        public CamerasMode(LevelEditor editor) : base(editor) 
        {
            // Controls
            Commands.Add(new Command("CAMERAS MODE", NewResize, "NEW RESIZE", Keys.OemPlus, false, false));
            Commands.Add(new Command("CAMERAS MODE", DelResize, "DELETE RESIZE", Keys.OemMinus, false, false));

            Commands.Add(new Command("CAMERAS MODE", ResetDefaultCameraBounds, "RESET CAM BOUNDS", Keys.B, false, false));
            Commands.Add(new Command("CAMERAS MODE", SetDefaultCameraPos, "SET INITIAL CAMERA POS", Keys.P, false, false));
            Commands.Add(new Command("CAMERAS MODE", EditCameraMode, "SWITCH CAMERA MODE", Keys.M, false, false));
            Commands.Add(new Command("CAMERAS MODE", ResetSelBounds, "RESET SEL BOUNDS", Keys.X, false, false));

            Tidbits.Add(new Tidbit("MOUSE", "LEFT", "SET TOP/LEFT"));
            Tidbits.Add(new Tidbit("MOUSE", "RIGHT", "SET BOTTOM/RIGHT"));
            Tidbits.Add(new Tidbit("MOUSE", "MIDDLE", "TOGGLE TRIGGER/TARGET"));
            Tidbits.Add(new Tidbit("MOUSE", "C+MIDDLE", "DUPLICATE"));

        }

        /// <summary>
        /// Gets the currently selected Resize object
        /// </summary>
        public Resize GetResize()
        {
            if (Layout.Resizes.Count == 0) return null;
            if (SelectedResize > Layout.Resizes.Count - 1) return null;
            if (SelectedResize == -1) return null;
            return Layout.Resizes[SelectedResize];

        }

        void ResetSelBounds()
        {
            var x = GetResize();
            if (x != null)
            {
                x.NewBounds.Width = 256;
                x.NewBounds.Height = 240 - 16;
                Game.StatusMessage("SET TARGET TO 17x14");
            }
        }

        public override void LeftClick(bool ctrl)
        {
            var sr = GetResize();
            if (sr is null) return;

            if (TargetMode)
            {
                sr.NewBounds.Location = Editor.MouseWorld;
                //if (sr.NewBounds.Right > Level.WorldWidth) sr.NewBounds.X = Level.WorldWidth - sr.NewBounds.Width;
                //if (sr.NewBounds.Bottom > Level.WorldHeight) sr.NewBounds.Y = Level.WorldHeight - sr.NewBounds.Height;
            }
            else
            {
                sr.Trigger.Location = Editor.MouseWorld;
            }
        }

        public override void RightClick(bool ctrl)
        {
            var sr = GetResize();
            if (sr is null) return;

            if (TargetMode)
            {
                sr.NewBounds.Size = Editor.MouseWorld - sr.NewBounds.Location;
                //if (sr.NewBounds.Size.X < Game.NativeWidth) sr.NewBounds.Size = new Point(Game.NativeWidth, Game.NativeHeight);
                //if (sr.NewBounds.Size.Y < Game.NativeHeight) sr.NewBounds.Size = new Point(Game.NativeWidth, Game.NativeHeight);
            }
            else
            {
                sr.Trigger.Size = Editor.MouseWorld - sr.Trigger.Location;
            }
        }

        public override void MiddleClick(bool ctrl)
        {
            if (ctrl)
            {
                // Ctrl+Middle click to copy 
                var sr = GetResize();
                if (sr is null) return;
                var s = new Resize() { NewBounds = sr.NewBounds, Trigger = sr.Trigger };
                Layout.Resizes.Add(s);
                SelectedResize = Layout.Resizes.Count - 1;
                Game.StatusMessage($"COPIED RESIZE");
                return;
            }

            // Otherwise, toggle target/trigger mode
            TargetMode = !TargetMode;
            Game.StatusMessage($"EDITING: {(TargetMode ? "CAM BOUNDS" : "TRIGGER")}");
        }

        public override void HUD(SpriteBatch batch)
        {
            base.HUD(batch);

            batch.DrawString($"MODE: {Layout.CameraMode.ToString().ToUpper()}", new Point(8, 8), Color.White);

            if (SelectedResize == -1 || Layout.Resizes.Count == 0)
            {
                batch.DrawString($"NO RESIZES", new Point(8, 16), Color.White);
            }
            else
            {
                if (SelectedResize > Layout.Resizes.Count - 1)
                {
                    SelectedResize = 0;
                }
                var sr = Layout.Resizes[SelectedResize];
                batch.DrawString($"{SelectedResize+1}/{Layout.Resizes.Count}", new Point(192, 8), Color.White);
                batch.DrawString($"TRIGGER:  X{sr.Trigger.X} Y{sr.Trigger.Y} R{sr.Trigger.Right} B{sr.Trigger.Bottom}", new Point(8, 16), Color.White);
                batch.DrawString($" BOUNDS:  X{sr.NewBounds.X} Y{sr.NewBounds.Y} R{sr.NewBounds.Right} B{sr.NewBounds.Bottom}", new Point(8, 24), Color.White);
            }

        }

        public override void Render(SpriteBatch batch)
        {
            var sr = GetResize();
            Color col1;
            Color col2;
            foreach(var r in Layout.Resizes)
            {
                if (r == sr)
                {
                    col1 = Color.Lime;
                    col2 = Color.Pink;
                } else
                {
                    col1 = Color.DarkGray;
                    col2 = Color.Black;
                }
                batch.DrawRectangle(r.Trigger.ToRectangleF(), col1, 2);
                batch.DrawRectangle(r.NewBounds.ToRectangleF(), col2, 2);

            }
        }

        public override void ScrollUp()
        {
            // Cycle between resizes
            if (Layout.Resizes.Count == 0) { SelectedResize = -1; return; }
            SelectedResize++;
            if (SelectedResize > Layout.Resizes.Count - 1) SelectedResize = Layout.Resizes.Count;
        }

        public override void ScrollDown()
        {
            // Cycle between resizes
            if (Layout.Resizes.Count == 0) { SelectedResize = -1; return; }
            SelectedResize--;
            if (SelectedResize < 0) SelectedResize = 0;
        }


        public void EditCameraMode()
        {
            Layout.CameraMode++;
            if (Layout.CameraMode > CameraMode.LockedUntilNear) Layout.CameraMode = 0;
            Game.StatusMessage($"CAM MODE: {Layout.CameraMode.ToString().ToUpper()}");
        }



        void NewResize()
        {
            var s = new Resize();
            s.NewBounds = Layout.CameraBounds;
            Layout.Resizes.Add(s);
            SelectedResize = Layout.Resizes.Count - 1;
            Game.StatusMessage("RESIZE CREATED");
        }

        void DelResize()
        {
            if (SelectedResize == -1)
            {
                Game.StatusMessage("NO RESIZE SELECTED");
                return;
            }
            if (SelectedResize > Layout.Resizes.Count - 1)
            {
                Game.StatusMessage("INVALID RESIZE");
                return;
            }
            Layout.Resizes.RemoveAt(SelectedResize);
            SelectedResize = Layout.Resizes.Count - 1;
            Game.StatusMessage("RESIZE DELETED");
        }


        void ResetDefaultCameraBounds()
        {
            Layout.CameraBounds = new Rectangle(8, 16, Level.WorldWidth - 16, Level.WorldHeight);
            Game.StatusMessage($"RESET CAMERA BOUNDS");
        }

        void SetDefaultCameraPos()
        {
            Layout.CameraStart = Game.CameraPos;
            Game.StatusMessage($"SET CAMERA START POS");
        }

    }
}
