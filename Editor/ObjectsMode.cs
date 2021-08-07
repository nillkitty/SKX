using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SKX.Editor
{
    /// <summary>
    /// Edits initial object placements
    /// </summary>
    public class ObjectsMode : EditorMode
    {
        public ObjectPlacement SelectedOP;                 // Selected object placement


        public ObjectsMode(LevelEditor editor) : base(editor) 
        { 
            SelectedOP = null;
            AddObjectCommands();

            Tidbits.Add(new Tidbit("MOUSE", "LEFT", "SELECT OBJECT"));
            Tidbits.Add(new Tidbit("MOUSE", "C+LEFT", "SET DANA START POS"));
            Tidbits.Add(new Tidbit("MOUSE", "LEFT-DRAG", "MOVE OBJECT"));
            Tidbits.Add(new Tidbit("MOUSE", "RIGHT", "CREATE/DELETE OBJECT"));
            Tidbits.Add(new Tidbit("MOUSE", "C+RIGHT", "TEST ROOM FROM HERE"));
            Tidbits.Add(new Tidbit("MOUSE", "MIDDLE", "CHANGE DIRECTION"));
        }

        public override void OnEnterMode()
        {
            // Clean up the selected OP if it's not valid anymore
            if (!Layout.Objects.Contains(SelectedOP)) SelectedOP = null; 

            base.OnEnterMode();
        }

        public override void LeftClick(bool ctrl)
        {
            // Player placement (ctrl/shift/both)
            bool shift = Control.ShiftKeyDown();
            if (ctrl)
            {
                // Move Dana start pos
                Layout.DanaStart = Editor.MouseCell;
                Editor.ObjectsChanged = true;
                Game.StatusMessage($"DANA START: {Layout.DanaStart.X},{Layout.DanaStart.Y}");
            }
            if (shift)
            {
                // Move Adam start pos
                Layout.AdamStart = Editor.MouseCell;
                Editor.ObjectsChanged = true;
                Game.StatusMessage($"ADAM START: {Layout.AdamStart.X},{Layout.AdamStart.Y}");
            }
            if (ctrl && shift) Game.StatusMessage($"DANA AND ADAM: {Layout.AdamStart.X},{Layout.AdamStart.Y}");
            if (ctrl || shift) return;


            // Regular click
            SelectedOP = Layout.Objects.FirstOrDefault(op => op.Position == Editor.MouseCell);
            if (SelectedOP != null)
            {
                Editor.Dragging = true;
            }
        }

        public override void MiddleClick(bool ctrl)
        {

            bool shift = Control.ShiftKeyDown();
            if (ctrl)
            {
                // Move Dana direction
                Layout.DanaDirection = !Layout.DanaDirection;
                Editor.ObjectsChanged = true;
                Game.StatusMessage("FLIPPED DANA START");
            }
            if (shift)
            {
                // Move Adam direction
                Layout.AdamDirection = !Layout.AdamDirection;
                Editor.ObjectsChanged = true;
                Game.StatusMessage("FLIPPED ADAM START");
            }
            if (ctrl && shift) Game.StatusMessage("FLIPPED DANA AND ADAM STARTS");
            if (ctrl || shift) return;

            // Select OP
            var i = GetObjDef();
            ChangeDirection(i);
            FixFlags(i);
        }

        public override void RightClick(bool ctrl)
        {

            bool shift = Control.ShiftKeyDown();
            if (ctrl)
            {
                // delete Dana
                Layout.DanaStart = default;
                Editor.ObjectsChanged = true;
                Game.StatusMessage("REMOVED DANA START");
            }
            else if (shift)
            {
                // delete Adam
                Layout.AdamStart = default;
                Editor.ObjectsChanged = true;
                Game.StatusMessage("REMOVED ADAM START");
            }

            var op = Layout.Objects.FirstOrDefault(o => o.Position == Editor.MouseCell);
            if (op != null)
            {
                if (op == SelectedOP) SelectedOP = null;
                Layout.Objects.Remove(op);
                Editor.ObjectsChanged = true;
                Game.StatusMessage("OBJECT DELETED");
            }
            else
            {
                op = new ObjectPlacement();
                if (SelectedOP != null)
                {
                    op.Type = SelectedOP.Type;
                    op.Flags = SelectedOP.Flags;
                    op.Direction = SelectedOP.Direction;
                }
                else
                {
                    op.Type = ObjType.Goblin;
                }
                op.Position = Editor.MouseCell;
                Layout.Objects.Add(op);
                SelectedOP = op;
                Editor.ObjectsChanged = true;
                Game.StatusMessage("OBJECT CREATED");
            }
        }

        public override void ScrollDown()
        {
            var item = GetObjDef();
            if (item != null)
            {
                item.Type--;
                if (item.Type < 0) item.Type = 0;
                FixFlags(item);

                Game.StatusMessage($"SELECTED OBJ: {(int)item.Type:X2}");
            }
        }

        public override void ScrollUp()
        {
            var item = GetObjDef();
            if (item != null)
            {
                item.Type++;
                if (item.Type > ObjType.LastEditType) item.Type = ObjType.LastEditType;
                FixFlags(item);

                Game.StatusMessage($"SELECTED OBJ: {(int)item.Type:X2}");
            }
        }

        public override void MouseDrag()
        {
            if (SelectedOP != null)
            {
                Editor.ObjectsChanged = true;
                SelectedOP.Position = Editor.MouseCell;
            }
        }



        public override void HUD(SpriteBatch batch)
        {
            if (SelectedOP != null)
            {
                var or = LevelEditor.GetObjTile(SelectedOP);
                Level.RenderTileWorld(batch, 4, line_d2 + 4, or.Tile, Color.White, or.Effect);
                batch.DrawString(SelectedOP.Type.ToString().ToUpper(), new Point(24, 12), Color.White);

                // Direction, speed, flags
                IObjectDef item = SelectedOP;
                batch.DrawString($"DIR: {item.Direction.ToString().ToUpper()[0]}", new Point(8 * 18, line_1), Color.White);
                batch.DrawString($"SPD: {item.GetSpeed()}", new Point(8 * 25, line_1), Color.White);
                batch.DrawString($"FLAGS: {item.GetFlags()}", new Point(8 * 18, line_2), Color.White);
            }
            else
            {
                batch.DrawString("NO OBJ SELECTED", new Point(24, 12), Color.White);
            }
        }


        
    }
}
