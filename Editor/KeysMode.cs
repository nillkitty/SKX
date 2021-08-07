using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SKX.Editor
{

    /// <summary>
    /// Edits extended key information
    /// </summary>
    public class KeysMode : EditorMode
    {
        public Point EditSelectedKey;                          // Selected key in keys mode
        public KeysMode(LevelEditor editor) : base(editor) 
        {
            Tidbits.Add(new Tidbit("MOUSE", "LEFT", "SELECT KEY/LINK DOOR"));
            Tidbits.Add(new Tidbit("MOUSE", "RIGHT", "CLEAR LINKS"));

        }

        public override void HUD(SpriteBatch batch)
        {
            var doors = Layout.Keys.Where(x => x.KeyPosition == EditSelectedKey).Count();
            batch.DrawString($"KEY: X {EditSelectedKey.X} Y {EditSelectedKey.Y}", new Point(8 * 18, line_d2), Color.White);
            batch.DrawString($"DOORS: {doors}", new Point(8 * 18, line_1), Color.White);
        }

        public override void LeftClick(bool ctrl)
        {
            EditKeyClick(Editor.MouseCell);
        }

        void EditDoorClick(Point p)
        {

            Cell c = Layout[p];
            if (Layout.IsDoorAtAll(c))
            {

                if (Layout.Keys.Any(x => x.KeyPosition == EditSelectedKey && x.DoorPosition == p))
                {
                    // Delete link
                    Layout.Keys.RemoveAll(x => x.KeyPosition == EditSelectedKey && x.DoorPosition == p);
                    Game.StatusMessage("DELETED LINK");

                }
                else
                {
                    // Create link
                    Layout.Keys.Add(new KeyInfo() { KeyPosition = EditSelectedKey, DoorPosition = p });
                    Game.StatusMessage("CREATED LINK");

                }
            }
            else
            {
                Game.StatusMessage("NOT A DOOR");
            }
        }

        public override void Render(SpriteBatch batch)
        {
            var v = new Vector2(8, 8);
            var off = new Point(4, 4);

            if (EditSelectedKey.X > 0)
            {
                batch.DrawRectangle(new RectangleF(EditSelectedKey.ToWorld(), new Size2(16, 16)), Color.Yellow, 1);
            }


            foreach (var k in Layout.Keys)
            {
                if (k.Valid)
                {
                    var color = (k.KeyPosition == EditSelectedKey) ? Color.Yellow : Color.Blue;
                    batch.DrawLine(k.KeyPosition.ToWorld().ToVector2() + v,
                                   k.DoorPosition.ToWorld().ToVector2() + v, color * 0.66f, 2);

                    if (k.KeyPosition == EditSelectedKey)
                    {
                        batch.DrawRectangle(k.DoorPosition.ToWorld().ToVector2(), new Size2(16, 16),
                            Color.Cyan, 1);
                    }
                }
            }
        }

        public override void RightClick(bool ctrl)
        {
            if (EditKeyClick(Editor.MouseCell))
            {
                Layout.Keys.RemoveAll(x => x.KeyPosition == EditSelectedKey);
                Game.StatusMessage("CLEARED LINKS FOR THIS KEY");
            }
        }

        bool EditKeyClick(Point p)
        {
            Cell c = Layout[p];
            if (Layout.IsKeyAtAll(c))
            {
                EditSelectedKey = Editor.MouseCell;
                return true;
            }
            else if (Layout.IsDoorAtAll(c))
            {
                EditDoorClick(p);
            }
            else
            {
                Game.StatusMessage("NOT A KEY OR DOOR");
            }
            return false;
        }

    }
}
