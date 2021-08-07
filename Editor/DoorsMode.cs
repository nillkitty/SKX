using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SKX.Editor
{
    /// <summary>
    /// Edits door exit info.  Without DoorInfo tied to the coordinate of the door,
    /// Dana will land in the next level (numerically),  a hidden room (if he has the
    /// constellation sign), or 6 rooms ahead if he has the golden wing.
    /// </summary>
    public class DoorsMode : EditorMode
    {

        public Point EditSelectedDoor;                         // Selected door in doors mode
        public DoorInfo SelectedDoor;                  // Selected door info
        public DoorsMode(LevelEditor editor) : base(editor) 
        {
            // Controls
            Commands.Add(new Command("DOORS MODE", incX, "DOOR TARGET X +", Keys.X, false, false));
            Commands.Add(new Command("DOORS MODE", decX, "DOOR TARGET X +", Keys.X, true, false));
            Commands.Add(new Command("DOORS MODE", incY, "DOOR TARGET Y +", Keys.Y, false, false));
            Commands.Add(new Command("DOORS MODE", decY, "DOOR TARGET Y +", Keys.Y, true, false));
            Commands.Add(new Command("DOORS MODE", doorNum, "DOOR NUM VISIBILITY", Keys.D, false, false));
            Commands.Add(new Command("DOORS MODE", EditToggleDoorStars, "TOGGLE STARS TYPE", Keys.S, false, false));
            Commands.Add(new Command("DOORS MODE", incWing, "SET WING EXIT", Keys.W, false, false));
            Commands.Add(new Command("DOORS MODE", incNext, "SET NORMAL EXIT", Keys.N, false, false));
            Commands.Add(new Command("DOORS MODE", incSec, "SET SECRET EXIT", Keys.E, false, false));
            Commands.Add(new Command("DOORS MODE", incDoor, "SET DOOR EXIT", Keys.D, true, false));

            Tidbits.Add(new Tidbit("MOUSE", "LEFT", "SELECT DOOR"));
            Tidbits.Add(new Tidbit("MOUSE", "C+LEFT", "DELETE DOOR INFO"));
            Tidbits.Add(new Tidbit("MOUSE", "RIGHT", "SET WARP TARGET"));
            Tidbits.Add(new Tidbit("MOUSE", "MIDDLE", "CREATE/CHANGE TYPE"));

            // Control handlers
            void incWing() { Game.InputPromptHex("GOLD WING ROOM NUMBER:", i => Layout.NextRoomWing = i, Layout.NextRoomWing); }
            void incNext() { Game.InputPromptHex("NORMAL EXIT ROOM NUMBER:", i => Layout.NextRoom = i, Layout.NextRoom); }
            void incSec() { Game.InputPromptHex("SECRET EXIT ROOM NUMBER:", i => Layout.NextRoomSecret = i, Layout.NextRoomSecret); }
            void incDoor() { if (SelectedDoor is null) return;  Game.InputPromptHex("TARGET ROOM NUMBER:", i => SelectedDoor.RoomNumber = i, SelectedDoor.RoomNumber); }

            void incY() { if (SelectedDoor != null) { SelectedDoor.Position.Y++; fixxy(); } }
            void decY() { if (SelectedDoor != null) { SelectedDoor.Position.Y--; fixxy(); } }
            void incX() { if (SelectedDoor != null) { SelectedDoor.Position.X++; fixxy(); } }
            void decX() { if (SelectedDoor != null) { SelectedDoor.Position.X--; fixxy(); } }
            void doorNum() { if (SelectedDoor != null) EditToggleDoorNumber(); }

            void fixxy()
            {
                if (SelectedDoor != null)
                {
                    if (SelectedDoor.Position.X < 0) SelectedDoor.Position.X = 0;
                    if (SelectedDoor.Position.Y > 0) SelectedDoor.Position.Y = 0;
                }
            }
        }

        public override void MiddleClick(bool ctrl)
        {
            EditDoorType();
        }

        public override void LeftClick(bool ctrl)
        {
            if (ctrl)
                EditDoorDelete(Editor.MouseCell);
            else
                DoorClick(Editor.MouseCell);
        }


        void DoorClick(Point p)
        {
            Cell c = Layout[p];
            if (Layout.IsDoorAtAll(c))
            {
                EditSelectedDoor = p;
                SelectedDoor = Layout.Doors.FirstOrDefault(x => x.Position == EditSelectedDoor);
                return;
            }
            else
            {
                Game.StatusMessage("NOT A DOOR");
            }
        }


        public override void ScrollUp()
        {
            if (SelectedDoor != null)
            {
                SelectedDoor.RoomNumber++;
            }
        }

        public override void ScrollDown()
        {
            if (SelectedDoor != null)
            {
                SelectedDoor.RoomNumber--;
                if (SelectedDoor.RoomNumber < 0)
                    SelectedDoor.RoomNumber = 0;
            }
        }

        public override void Render(SpriteBatch batch)
        {
            var v = new Vector2(8, 8);
            var off = new Point(4, 4);

            if (EditSelectedDoor.X > 0)
            {
                batch.DrawRectangle(new RectangleF(EditSelectedDoor.ToWorld(), new Size2(16, 16)), Color.White, 1);
            }

            bool here;
            foreach (var d in Layout.Doors)
            {
                here = false;
                switch (d.Type)
                {
                    case DoorType.Room:
                        batch.DrawOutlinedString(d.RoomNumber.ToString("X2"), d.Position.ToWorld() + off, Color.White, 1);
                        break;
                    case DoorType.Warp:
                        batch.DrawOutlinedString($"W", d.Position.ToWorld() + off, Color.Lime, 1);
                        here = true;
                        break;
                    case DoorType.RoomAndWarp:
                        if (d.RoomNumber == Layout.RoomNumber)
                        {
                            batch.DrawOutlinedString($"W", d.Position.ToWorld() + off, Color.Lime, 1);
                            here = true;
                        }
                        else
                        {
                            batch.DrawOutlinedString(d.RoomNumber.ToString("X2"), d.Position.ToWorld() + off, Color.White, 1);
                            batch.DrawOutlinedString($"{d.Target.X} {d.Target.Y}", d.Position.ToWorld() + off + new Point(0, 8), Color.Lime, 1);
                        }
                        break;
                }
                if (here)
                {
                    if (d.Target.X > 0 && d.Target.Y > 0)
                    {
                        batch.DrawLine(d.Position.ToWorld().ToVector2() + v,
                                       d.Target.ToWorld().ToVector2() + v, Color.Lime * 0.66f, 1);
                        Level.RenderTile(batch, d.Target.X, d.Target.Y, Tile.Sparkle2, Color.White * 0.5f);
                    }
                }


            }
        }

        public override void HUD(SpriteBatch batch)
        {
            if (EditSelectedDoor.X > 0)
            {
                if (SelectedDoor is null)
                {
                    batch.DrawString("TYPE: DEFAULT", new Point(24, 12), Color.White);
                }
                else
                {
                    batch.DrawString("TYPE: " + SelectedDoor.Type.ToString().ToUpper(), new Point(24, 12), Color.White);
                    batch.DrawString(SelectedDoor.Text(), new Point(24, 20), Color.White);
                }
            }
            else
            {
                batch.DrawString("NO DOOR SELECTED", new Point(24, 12), Color.White);
            }

            batch.DrawString($"NEXT RM: {Layout.NextRoom:X}", new Point(8 * 18, line_d2), Color.White);
            batch.DrawString($"SECRET : {Layout.NextRoomSecret:X}", new Point(8 * 18, line_1), Color.Gray);
            batch.DrawString($"WING   : {Layout.NextRoomWing:X}", new Point(8 * 18, line_2), Color.Yellow);
        }

        public override void RightClick(bool ctrl)
        {
            if (SelectedDoor != null)
            {
                SelectedDoor.Target = Editor.MouseCell;
            }
            else
            {
                Game.StatusMessage("SELECTED DOOR HAS NO INFO");
            }
        }

        void EditToggleDoorNumber()
        {
            if (SelectedDoor != null)
            {
                SelectedDoor.ShowRoomNumber = !SelectedDoor.ShowRoomNumber;
                Game.StatusMessage("TOGGLED NUMBER VISIBILITY");
            }
        }

        void EditToggleDoorStars()
        {
            if (SelectedDoor != null)
            {
                SelectedDoor.FastStars = !SelectedDoor.FastStars;
                Game.StatusMessage($"FAST STARS: {(SelectedDoor.FastStars ? "ON" : "OFF")}");
            }
        }



        void EditDoorDelete(Point p)
        {
            var c = Layout[p];
            if (Layout.IsDoorAtAll(c))
            {
                if (Layout.Doors.RemoveAll(x => x.Position == Editor.MouseCell) > 0)
                    Game.StatusMessage("DELETED DOOR INFO");
            }
            else
            {
                Game.StatusMessage("NOT A DOOR");
            }
        }


        void EditDoorType()
        {
            if (EditSelectedDoor.X < 1)
            {
                Game.StatusMessage("NO DOOR SELECTED");
                return;
            }
            if (SelectedDoor is null)
            {
                // If this door doesn't have a DoorInfo, create one
                var d = new DoorInfo();
                d.Position = EditSelectedDoor;
                Layout.Doors.Add(d);
                Game.StatusMessage("CREATED DOOR INFO");
                SelectedDoor = d;
            }
            else
            {
                // Otherwise cycle the door type
                SelectedDoor.Type++;
                if (SelectedDoor.Type > DoorType.RoomAndWarp) SelectedDoor.Type = 0;

                // Update door graphic
                var c = Layout[EditSelectedDoor].GetContents();
                if (SelectedDoor.Type == DoorType.Warp && c == Cell.DoorClosed)
                {
                    Layout[EditSelectedDoor] = Layout.ChangeItem(Layout[EditSelectedDoor], Cell.DoorBlue);
                }
                else if (SelectedDoor.Type == DoorType.Room && c == Cell.DoorBlue)
                {
                    Layout[EditSelectedDoor] = Layout.ChangeItem(Layout[EditSelectedDoor], Cell.DoorClosed);
                }
                Game.StatusMessage("DOOR TYPE CHANGED");
            }
        }

    }
}
