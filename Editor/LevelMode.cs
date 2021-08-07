using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SKX.Editor
{
    /// <summary>
    /// Edits the cell layout and general level properties
    /// </summary>
    public class LayoutMode : EditorMode
    {

        public Cell EditSelectedCell = Cell.Dirt;              // Selected brick
        public Cell[] EditCellValues;                          // Valid Cell values
        public int EditSelectedCellIndex = 2;                  // Selected index into valid cells
        public int EditSelectedModifier = 0;                   // Selected cell modifier

        public override void OnExitMode()
        {
            Sesh.EditorCell = EditSelectedCell;
            base.OnExitMode();
        }

        public LayoutMode(LevelEditor editor) : base(editor)
        {
            EditCellValues = (Cell[])Enum.GetValues(typeof(Cell));
            EditSelectedCell = Sesh.EditorCell;

            // Controls (see the Controls() method for more)
            Commands.Add(new Command("LAYOUT MODE", incX, "X SIZE +", Keys.Z, false, false));
            Commands.Add(new Command("LAYOUT MODE", decX, "X SIZE -", Keys.Z, true, false));
            Commands.Add(new Command("LAYOUT MODE", incY, "Y SIZE +", Keys.X, false, false));
            Commands.Add(new Command("LAYOUT MODE", decY, "Y SIZE -", Keys.X, true, false));
            Commands.Add(new Command("LAYOUT MODE", ClearLevel, "CLEAR LAYOUT", Keys.Back, false, false));
            Commands.Add(new Command("LAYOUT MODE", EditName, "EDIT ROOM NAME", Keys.N, false, false));
            Commands.Add(new Command("LAYOUT MODE", incEffect, "AUDIO EFFECT +", Keys.E, false, false));
            Commands.Add(new Command("LAYOUT MODE", decEffect, "AUDIO EFFECT -", Keys.E, true, false));
            Commands.Add(new Command("LAYOUT MODE", incMusic, "MUSIC +", Keys.M, false, false));
            Commands.Add(new Command("LAYOUT MODE", decMusic, "MUSIC -", Keys.M, true, false));
            Commands.Add(new Command("LAYOUT MODE", togglePlayer, "TOGGLE CHARACTER", Keys.P, false, false));
            Commands.Add(new Command("LAYOUT MODE", EditDefaultLevelBounds, "RESET 17x14 BOUNDS", Keys.R, true, true));

            Commands.Add(new Command("LAYOUT MODE", thankyou1, "EDIT THANK YOU 1", Keys.D8, false, true));
            Commands.Add(new Command("LAYOUT MODE", thankyou2, "EDIT THANK YOU 2", Keys.D9, false, true));
            Commands.Add(new Command("LAYOUT MODE", thankyou3, "EDIT THANK YOU 3", Keys.D0, false, true));
            Commands.Add(new Command("LAYOUT MODE", startlife, "LIFE OVERRIDE", Keys.L, true, true));
            Commands.Add(new Command("ITEM SEARCH", findAll, "FIND ALL W/ITEM", Keys.F, true, true));
            Commands.Add(new Command("ITEM SEARCH", findCell, "FIND NEXT RM W/ITEM", Keys.N, true, true));


            Tidbits.Add(new Tidbit("MOUSE", "LEFT", "PAINT CELL"));
            Tidbits.Add(new Tidbit("MOUSE", "C+LEFT", "PAINT CELL RECT"));
            Tidbits.Add(new Tidbit("MOUSE", "RIGHT", "PICK UP CELL TYPE"));
            Tidbits.Add(new Tidbit("MOUSE", "MIDDLE", "CHANGE MODIFIER"));

            // Control handlers
            void incEffect() { EditAudioEffect(1); }
            void decEffect() { EditAudioEffect(-1); }
            void incMusic() { EditMusic(1); }
            void decMusic() { EditMusic(-1); }
            void incY() { Resize(0, 1); }
            void decY() { Resize(0, -1); }
            void incX() { Resize(1, 0); }
            void decX() { Resize(-1, 0); }
            void togglePlayer()
            {
                Layout.Character++;
                if (Layout.Character > CharacterMode.ForceAdam) Layout.Character = CharacterMode.Any;
                Layout.Level.UpdateCharacter();
                Game.StatusMessage($"CHARACTER MODE: {Layout.Character.ToString().ToUpper()}");
            }
            void startlife()
            {
                Game.InputPromptNumber($"ENTER START LIFE AMT:", i =>
                {
                    if (i < 1)
                    {
                        Layout.StartLife = null;
                        Game.StatusMessage("CLEARED STARTING LIFE OVERRIDE");
                    }
                    else
                    {
                        Layout.StartLife = i;
                        Game.StatusMessage($"LIFE OVERRIDE: {i}");
                    }
                }, (Layout.StartLife ?? 10_000).ToString());
            }

            void thankyou1() => Game.InputPrompt($"THANK YOU LINE 1:", s => { Layout.ThankYouTextA = s == "" ? null : s; }, Layout.ThankYouTextA);
            void thankyou2() => Game.InputPrompt($"THANK YOU LINE 2:", s => { Layout.ThankYouTextB = s == "" ? null : s; }, Layout.ThankYouTextB);
            void thankyou3() => Game.InputPrompt($"THANK YOU LINE 3:", s => { Layout.ThankYouTextC = s == "" ? null : s; }, Layout.ThankYouTextC);
        }

        void findAll()
        {
            var cell = EditSelectedCell;
            StringBuilder o = new StringBuilder();
            var rooms = Game.Assets.Bundle.Layouts.Where(l => l.Story == Sesh.Story)
                                                  .OrderBy(l => l.RoomNumber);

            o.Append($"[c=ff888888]_____________________________[c=ffffffff]\n");
            o.Append($"ROOMS CONTAINING\n");
            o.Append($"[t={(int)Layout.CellToTile(cell)}]   {cell.ToString().ToUpper()}\n");
            o.Append("\n\n");
            o.Append($"[c=ff00ffff]RM  X   Y\n");
            o.Append($"[c=ff888888]_____________________________[c=ffffffff]\n");

            foreach (var l in rooms)
            {
                var r = l.FindCell(cell);
                foreach(var x in r)
                {
                    o.Append($"{l.RoomNumber,-3:X} {x.X,-3} {x.Y,-3}\n");
                }
            }

            Game.SetHelpText(o.ToString());

        }

        // Find the next room that contains the currently selected cell
        void findCell()
        {
            var cell = EditSelectedCell;
            var rooms_after = Game.Assets.Bundle.Layouts.Where(l => l.RoomNumber > Sesh.RoomNumber
                                                           && l.Story == Sesh.Story)
                                                  .OrderBy(l => l.RoomNumber);
            foreach (var l in rooms_after)
            {
                var pos = l.FindCell(cell);
                if (pos.Count > 0)
                {
                    GoToRoom(l.RoomNumber);
                    return;
                }
            }

            // We didn't find it -- lets go loop around
            var rooms_before = Game.Assets.Bundle.Layouts.Where(l => l.RoomNumber <= Sesh.RoomNumber
                                                           && l.Story == Sesh.Story)
                                                  .OrderBy(l => l.RoomNumber);


            foreach (var l in rooms_before)
            {
                var pos = l.FindCell(cell);
                if (pos.Count > 0)
                {
                    if (l.RoomNumber == Sesh.RoomNumber)
                    {
                        Game.StatusMessage($"ONLY THIS RM HAS {cell.ToString().ToUpper()}");
                        return;
                    }

                    GoToRoom(l.RoomNumber);
                    return;
                }
            }

            Game.StatusMessage($"NO ROOMS FOUND CONTAINING {cell.ToString().ToUpper()}");

        }

        void EditDefaultLevelBounds()
        {
            Level.Resize(17, 14);
            Layout.CameraBounds = new Rectangle(8, 8, 256 + 8, 216);
            Game.StatusMessage($"RESET TO 17X14");
        }

        public override void Controls(bool ctrl, bool shift)
        {
            // extra shortcut shift+digit key bindings that aren't commands
            if (ctrl) return;

            int m = 0;
            if (shift) m = 1;

            if (Control.KeyPressed(Keys.D1)) { EditSelectedCell = Cell.Concrete;    EditSelectedModifier = 0; }
            if (Control.KeyPressed(Keys.D2)) { EditSelectedCell = Cell.Dirt;        EditSelectedModifier = 0; }
            if (Control.KeyPressed(Keys.D3)) { EditSelectedCell = Cell.Frozen;      EditSelectedModifier = 0; }
            if (Control.KeyPressed(Keys.D4)) { EditSelectedCell = Cell.Key;         EditSelectedModifier = m; }
            if (Control.KeyPressed(Keys.D5)) { EditSelectedCell = Cell.DoorClosed;  EditSelectedModifier = m; }
            if (Control.KeyPressed(Keys.D6)) { EditSelectedCell = Cell.Ash;         EditSelectedModifier = 0; }
            if (Control.KeyPressed(Keys.D7)) { EditSelectedCell = Cell.Seal;        EditSelectedModifier = m; }
            if (Control.KeyPressed(Keys.D8)) { EditSelectedCell = Cell.Mirror;      EditSelectedModifier = 0; }
            if (Control.KeyPressed(Keys.D9)) { EditSelectedCell = Cell.Bell;        EditSelectedModifier = m; }
            if (Control.KeyPressed(Keys.D0)) { EditSelectedCell = Cell.Empty;       EditSelectedCellIndex = 0; }

        }

        public override void LeftClick(bool ctrl)
        {
            if (ctrl)
            {
                FillChunk(Editor.LastClick, Editor.MouseCell, EditSelectedCell | GetModifier());
            }
            Layout[Editor.MouseCell] = EditSelectedCell | GetModifier();
        }

        public Cell GetModifier()
        {
            return EditSelectedModifier switch
            {
                1 => Cell.Covered,
                2 => Cell.Hidden,
                3 => Cell.Cracked,
                4 => Cell.Frozen,
                _ => Cell.Empty,
            };
        }

        public string GetModifierText()
        {
            return EditSelectedModifier switch
            {
                1 => "INSIDE",
                2 => "HIDDEN",
                3 => "CRACKED",
                4 => "FROZEN",
                _ => "NORMAL",
            };
        }

        void FillChunk(Point p1, Point p2, Cell cell)
        {
            var left = Math.Min(p1.X, p2.X);
            var right = Math.Max(p1.X, p2.X);
            var top = Math.Min(p1.Y, p2.Y);
            var bottom = Math.Max(p1.Y, p2.Y);
            for (int y = top; y <= bottom; y++)
                for (int x = left; x <= right; x++)                
                    Layout[x, y] = cell;
                
        }

        public override void ScrollUp()
        {
            EditSelectedCellIndex -= (Control.ControlKeyDown() ? 8 : 1);
            if (EditSelectedCellIndex < 0) EditSelectedCellIndex = 0;
            if (EditSelectedCellIndex > EditCellValues.Length - 1) EditSelectedCellIndex = EditCellValues.Length - 1;
            EditSelectedCell = EditCellValues[EditSelectedCellIndex];
            if (EditSelectedCell == Cell.Empty || EditSelectedCell == Cell.Concrete || EditSelectedCell == Cell.Dirt)
                EditSelectedModifier = 0;

        }

        public override void ScrollDown()
        {
            EditSelectedCellIndex += (Control.ControlKeyDown() ? 8 : 1);
            if (EditSelectedCellIndex < 0) EditSelectedCellIndex = 0;
            if (EditSelectedCellIndex > EditCellValues.Length - 1) EditSelectedCellIndex = EditCellValues.Length - 1;
            EditSelectedCell = EditCellValues[EditSelectedCellIndex];
            if (EditSelectedCell == Cell.Empty || EditSelectedCell == Cell.Concrete || EditSelectedCell == Cell.Dirt)
                EditSelectedModifier = 0;

        }

        public override void RightClick(bool ctrl)
        {
            // Pick up like an ink dropper tool
            var c = Layout[Editor.MouseCell];
            if (Layout.IsFrozen(c)) EditSelectedModifier = 4;
            else if (Layout.IsCovered(c)) EditSelectedModifier = 1;
            else if (Layout.IsHidden(c)) EditSelectedModifier = 2;
            else if (Layout.IsCracked(c)) EditSelectedModifier = 3;
            else EditSelectedModifier = 0;
            EditSelectedCell = c.GetContents();
            EditSelectedCellIndex = Array.FindIndex(EditCellValues, x => x == EditSelectedCell);
        }

       

        // Edit room name prompt
        public void EditName()
        {
            Game.InputPrompt("ENTER ROOM NAME", (x) => UpdateName(x), Layout.Name);
        }

        // Updates room name (called from InputMenu callback)
        void UpdateName(string x)
        {
            if (string.IsNullOrEmpty(x))
            {
                Layout.Name = null;
                Game.StatusMessage("ROOM NAME CLEARED");
            }
            else
            {
                Layout.Name = x;
                Game.StatusMessage($"NAME CHANGED TO: {x}");
            }
        }


        public override void HUD(SpriteBatch batch)
        {
            Layout.RenderCellWorld(batch, new Point(4, line_d2 + 4), EditSelectedCell | GetModifier());
            batch.DrawString(EditSelectedCell.ToString().ToUpper(), new Point(24, 12), Color.White);
            batch.DrawString(GetModifierText(), new Point(24, 20), Color.White);

            batch.DrawString($"W: {Layout.Width}", new Point(8 * 18, line_d2), Color.White);
            batch.DrawString($"H: {Layout.Height}", new Point(8 * 18, line_1), Color.White);

        }

        void EditAudioEffect(int x)
        {
            Layout.AudioEffect += x;
            if (Layout.AudioEffect < 0) Layout.AudioEffect = 0;
            if (Layout.AudioEffect > AudioEffect.LastEntry) Layout.AudioEffect = 0;
            Game.StatusMessage($"AUDIO EFFECT: {Layout.AudioEffect.ToString().ToUpper()}");
            Sound.StopAll();
            Level.PlayMusic();
        }

        void EditMusic(int x)
        {
            Layout.Music += x;
            if (Layout.Music < 0) Layout.Music = 0;
            if (Layout.Music > 5) Layout.Music = 0;
            Game.StatusMessage($"MUSIC: {Layout.Music}");
            Sound.StopAll();
            Level.PlayMusic();
        }



        void Resize(int xCh, int yCh)
        {
            var newSize = new Point(Level.TileWidth + xCh, Level.TileHeight + yCh);
            if (newSize.X < 17 || newSize.Y < 14)
            {
                Game.StatusMessage("CANNOT DECREASE FURTHER");
            }
            else
            {
                Level.Resize(newSize.X, newSize.Y);
                Game.StatusMessage($"NEW SIZE: X{newSize.X} Y{newSize.Y}");
            }
        }

        public override void MiddleClick(bool ctrl)
        {
            EditSelectedModifier++;
            if (EditSelectedModifier > 4) EditSelectedModifier = 0;
            if (EditSelectedCell == Cell.Empty || EditSelectedCell == Cell.Concrete || EditSelectedCell == Cell.Dirt) EditSelectedModifier = 0;
            Game.StatusMessage($"CELL MODIFIER: {((int)GetModifier()):X2}");
        }


    }

}
