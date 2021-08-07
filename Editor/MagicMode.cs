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
    /// Edits Spell objects
    /// </summary>
    public class MagicMode : EditorMode
    {
        public Spell SelectedSpell;

        public MagicMode(LevelEditor editor) : base(editor) 
        {

            // Controls
            Commands.Add(new Command("MAGIC MODE", incItemRoom, "REQ'D ITEM ROOM +", Keys.Q, false, false));
            Commands.Add(new Command("MAGIC MODE", decItemRoom, "REQ'D ITEM ROOM -", Keys.Q, true, false));
            Commands.Add(new Command("MAGIC MODE", incSpellRoom, "REQ'D SPELL ROOM +", Keys.E, false, false));
            Commands.Add(new Command("MAGIC MODE", decSpellRoom, "REQ'D SPELL ROOM +", Keys.E, true, false));
            Commands.Add(new Command("MAGIC MODE", incSpellId, "REQ'D SPELL", Keys.R, false, false));
            Commands.Add(new Command("MAGIC MODE", EditRequireItemCell, "SET REQ'D ITEM CELL", Keys.W, true, false));

            // These are handled in Controls()
            Commands.Add(new Command("MAGIC MODE", null, "TOGGLE ANIMATE FLAG", Keys.A, false, false));
            Commands.Add(new Command("MAGIC MODE", null, "DEL LAST RND LIST ITEM", Keys.OemMinus, false, false));
            Commands.Add(new Command("MAGIC MODE", null, "REQ'D COUNT +", Keys.Z, false, false));
            Commands.Add(new Command("MAGIC MODE", null, "REQ'D COUNT -", Keys.Z, true, false));

            Tidbits.Add(new Tidbit("MOUSE", "LEFT", "SELECT SPELL"));
            Tidbits.Add(new Tidbit("MOUSE", "C+LEFT", "SET TARGET CELL POS"));
            Tidbits.Add(new Tidbit("MOUSE", "LEFT-DRAG", "MOVE SPELL"));
            Tidbits.Add(new Tidbit("MOUSE", "RIGHT", "CREATE/DELETE SPELL"));
            Tidbits.Add(new Tidbit("MOUSE", "C+RIGHT", "SET SECONDARY DATA"));
            Tidbits.Add(new Tidbit("MOUSE", "MIDDLE", "TOGGLE SPELL TYPE"));

            // Control handlers
            void incItemRoom() => EditRequireItemRoom(1);
            void decItemRoom() => EditRequireItemRoom(-1);
            void incSpellRoom() => EditRequireSpellRoom(1);
            void decSpellRoom() => EditRequireSpellRoom(-1);
            void incSpellId()
            {
                Game.InputPromptNumber("ENTER SPELL ID", i =>
                {
                    if (SelectedSpell is null)
                    {
                        Game.StatusMessage("NO SPELL SELECTED");
                        return;
                    }

                    SelectedSpell.RequiredSpell = new SpellExecuted() { SpellID = i };
                });
            }
        }

        public override void LeftClick(bool ctrl)
        {

            if (!ctrl)
            {
                // Select spell based on mouse click
                var sp = Layout.Spells.FirstOrDefault(s => s.Position == Editor.MouseCell);
                if (sp != null)
                {
                    SelectedSpell = sp;
                    Editor.Dragging = true;
                }
                return;
            }

            // Ctrl+Click -- set spell target cell if applicable

            Editor.Dragging = false;
            if (SelectedSpell is null) return;

            switch (SelectedSpell.Type)
            {
                case SpellType.RandomCell:
                case SpellType.ExitLevel:
                case SpellType.ChangeCell:
                case SpellType.ChangeStartPos:
                    SelectedSpell.Target = Editor.MouseCell;
                    Game.StatusMessage($"TARGET SET TO X{Editor.MouseCell.X} Y{Editor.MouseCell.Y}");
                    break;
                case SpellType.SpawnObject:                    
                    var sp = Layout.Spawns.FirstOrDefault(x => x.Position == Editor.MouseCell);
                    if (sp is null)
                    {
                        Game.StatusMessage($"NOT A SPAWN POINT");
                        return;
                    }
                    Game.StatusMessage($"TARGET SET TO X{Editor.MouseCell.X} Y{Editor.MouseCell.Y}");
                    SelectedSpell.Target = Editor.MouseCell;
                    break;
                
            }

        }

        public override void RightClick(bool ctrl)
        {
            if (ctrl)
            {
                // Ctrl+click -- Set spell secondary data, if applicable
                switch (SelectedSpell.Type)
                {
                    case SpellType.ChangeCell:
                        // Pick up type
                        SelectedSpell.CellType = Layout[Editor.MouseCell];
                        break;
                    case SpellType.RandomCell:
                        var i = Layout[Editor.MouseCell].GetContents();
                        Layout.RandomList.Add(i);
                        Game.StatusMessage($"{Layout.RandomList.Count} ITEMS IN RANDOM LIST");
                        break;
                    case SpellType.ExitLevel:
                        break;
                }
                return;
            }

            // Deletes or creates
            var sp = Layout.Spells.FirstOrDefault(s => s.Position == Editor.MouseCell);
            if (sp != null)
            {
                Layout.Spells.Remove(sp);
                SelectedSpell = Layout.Spells.FirstOrDefault();
                Game.StatusMessage($"SPELL {sp.ID} DELETED");
            }
            else
            {
                SelectedSpell = new Spell();
                SelectedSpell.Position = Editor.MouseCell;
                Layout.Spells.Add(SelectedSpell);
                Game.StatusMessage($"SPELL {SelectedSpell.ID} CREATED");
            }
        }

        public override void MiddleClick(bool ctrl)
        {
            // Cycle spell trigger types
            if (SelectedSpell is null) return;
            SelectedSpell.Trigger++;
            if (SelectedSpell.Trigger > SpellTrigger.ExitLevel)
            {
                SelectedSpell.Trigger = 0;
            }
            Game.StatusMessage($"TRIGGER TYPE: {SelectedSpell.Trigger.ToString().ToUpper()}");

        }

        public override void ScrollDown()
        {
            if (SelectedSpell is null) return;
            SelectedSpell.Type--;
            if (SelectedSpell.Type < 0)
            {
                SelectedSpell.Type = 0;
            }
            Game.StatusMessage($"TYPE: {SelectedSpell.Type.ToString().ToUpper()}");
        }

        public override void ScrollUp()
        {
            if (SelectedSpell is null) return;
            SelectedSpell.Type++;
            if (SelectedSpell.Type > SpellType.LastType)
            {
                SelectedSpell.Type = SpellType.LastType;
            }
            Game.StatusMessage($"TYPE: {SelectedSpell.Type.ToString().ToUpper()}");
        }

        public override void Controls(bool ctrl, bool shift)
        {
            base.Controls(ctrl, shift);
            if (SelectedSpell is null) return;


            // A - toggle animate flag for Change Cell type
            if (Control.KeyPressed(Keys.A) && !shift && SelectedSpell.Type == SpellType.ChangeCell)
            {
                // Toggle animate flag
                SelectedSpell.Param = (SelectedSpell.Param > 0) ? 0 : 1;
                Game.StatusMessage($"ANIMATION: " + (SelectedSpell.Param == 0 ? "OFF" : "ON"));
                return;
            }

            // - key -- Delete item from random list -- for random cell type
            if (Control.KeyPressed(Keys.OemMinus) && !shift && SelectedSpell.Type == SpellType.RandomCell)
            {
                if (Layout.RandomList.Count == 0)
                {
                    Game.StatusMessage($"RANDOM LIST IS EMPTY");
                    return;
                }
                Layout.RandomList.RemoveAt(Layout.RandomList.Count - 1);
                Game.StatusMessage($"DELETED. ITEM COUNT {Layout.RandomList.Count}");
            }

            // Z - Increase require count
            if (Control.KeyPressed(Keys.Z) && !shift && SelectedSpell != null)
            {
                SelectedSpell.RequireCount++;
                Game.StatusMessage($"REQUIRE COUNT: {SelectedSpell.RequireCount}");
            }

            // Shift+Z - Decrease require count
            if (Control.KeyPressed(Keys.Z) && shift && SelectedSpell != null)
            {
                SelectedSpell.RequireCount--;
                Game.StatusMessage($"REQUIRE COUNT: {SelectedSpell.RequireCount}");
            }


        }

        void EditRequireItemRoom(int off)
        {
            if (SelectedSpell is null) return;
            var r = SelectedSpell.RequiredItem ?? new InventoryItem();
            r.FromRoom += off;
            if (r.FromRoom < 0) r.FromRoom = 0;
            SelectedSpell.RequiredItem = r;
        }

        // Open cell picker
        void EditRequireItemCell()
        {
            Editor.CellSelectMode.Title = "SELECT REQUIRED ITEM";
            Editor.CellSelectMode.OnSelectedCellRead = GetRequiredCell;
            Editor.CellSelectMode.OnCellSelected = UpdateRequiredCell;
            Editor.ChangeMode(EditMode.TileSelect);
        }

        // Return from cell picker
        void UpdateRequiredCell(Cell c)
        {
            Editor.ChangeMode(EditMode.Magic);  // Go back to magic mode
            if (SelectedSpell is null) return;
            var r = SelectedSpell.RequiredItem ?? new InventoryItem();
            r.Type = c;
            SelectedSpell.RequiredItem = r;
        }

        // Used by cell picker
        Cell GetRequiredCell()
        {
            if (SelectedSpell is null) return Cell.Empty;
            var r = SelectedSpell.RequiredItem ?? new InventoryItem();
            return r.Type;
        }

        void EditRequireSpellRoom(int off)
        {
            if (SelectedSpell is null) return;
            var r = SelectedSpell.RequiredSpell ?? new SpellExecuted();
            r.FromRoom += off;
            if (r.FromRoom < 0) r.FromRoom = 0;
            SelectedSpell.RequiredSpell = r;
        }

        void EditRequireSpellID(int off)
        {
            if (SelectedSpell is null) return;
            var r = SelectedSpell.RequiredSpell ?? new SpellExecuted();
            r.SpellID += off;
            if (r.SpellID < 0) r.SpellID = 0;
            SelectedSpell.RequiredSpell = r;
        }


        public override void Render(SpriteBatch batch)
        {
            foreach(var s in Layout.Spells)
            {
                var color = (s == SelectedSpell) ? Color.Yellow : Color.White;
                batch.DrawRectangle(new RectangleF(s.Position.ToWorld(), new Point(16, 16)), color, 1);
            }
        }


        public override void MouseDrag()
        {
            if (SelectedSpell != null)
            {
                SelectedSpell.Position = Editor.MouseCell;
            }
        }

        public override void HUD(SpriteBatch batch)
        {
            base.HUD(batch);
            if (SelectedSpell is null)
                batch.DrawString($"NO SPELL SELECTED", new Point(8, 8), Color.White);
            else
            {
                batch.DrawString($"SPELL ID:{SelectedSpell.ID} TRIG:{SelectedSpell.Trigger.ToString().ToUpper()}", new Point(8, 8), Color.White);

                var prereq = "";
                if (SelectedSpell.RequireCount > 0) prereq += $"x{SelectedSpell.RequireCount} ";
                if (SelectedSpell.RequiredItem.HasValue)
                    prereq += $" RM {SelectedSpell.RequiredItem.Value.FromRoom:X}-{SelectedSpell.RequiredItem.Value.Type.ToString().ToUpper()}";
                if (SelectedSpell.RequiredSpell.HasValue)
                    prereq += $" SPELL {SelectedSpell.RequiredSpell.Value.SpellID} RM{SelectedSpell.RequiredSpell.Value.FromRoom:X}";
                prereq = prereq.Trim();
                if (!string.IsNullOrEmpty(prereq))
                    batch.DrawString($"REQ {prereq.Trim()}", new Point(8, 16), Color.White);

                batch.DrawString($"{SelectedSpell}", new Point(8, 24), Color.White);
            }
        }
    }
}
