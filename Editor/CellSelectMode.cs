using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using System;
using System.Collections.Generic;
using System.Text;

namespace SKX.Editor
{
    /// <summary>
    /// Renders a menu from which a level cell can be selected
    /// </summary>
    public class CellSelect : EditorMode
    {

        private CameraMode LastCameraMode;
        private Point LastCameraPos;
        public string Title = "CELL SELECT";
        public bool TileSelect = false;
        public EditMode PreviousMode = EditMode.Layout;
        private Cell HoverCell;
        private Tile HoverTile;
        public int ScrollOffset;

        public Func<Cell> OnSelectedCellRead; // Used to determine the current selected cell
                                                    // otherwise -- uses selected cell for level mode

        public Action<Cell> OnCellSelected;  // Used to return the cell back to the thing
                                             // that invoked us;  otherwise we set the selected cell
                                             // for level mode

        public Func<Tile> OnSelectedTileRead;
        public Action<Tile> OnTileSelected;

        public CellSelect(LevelEditor editor) : base(editor) { }

        public override void LeftClick(bool ctrl)
        {
            if (TileSelect)
            {
                if (OnTileSelected != null)
                {
                    var s = OnTileSelected;
                    Editor.ChangeMode(PreviousMode);
                    s.Invoke(HoverTile);
                    return;
                }
                Editor.SelectedTile = HoverTile;
                Editor.ChangeMode(PreviousMode);
            }
            else
            {
                if (OnCellSelected != null)
                {
                    var s = OnCellSelected;
                    Editor.ChangeMode(EditMode.Layout);
                    s.Invoke(HoverCell);
                    return;
                }
                Editor.LevelMode.EditSelectedCell = HoverCell;
            }
            Editor.ChangeMode(PreviousMode);
        }

        public override bool OnEscPressed()
        {
            Editor.ChangeMode(PreviousMode);
            return true;
        }

        public override void ScrollDown()
        {
            Scroll((Control.LastMouseState.ScrollWheelValue - Control.MouseState.ScrollWheelValue) / 8);
        }

        public override void ScrollUp()
        {
            Scroll((Control.LastMouseState.ScrollWheelValue - Control.MouseState.ScrollWheelValue) / 8);
        }

        public void Scroll(int offset)
        {
            int max = (System.Enum.GetValues(typeof(Tile)).Length / 16 + 1) * 16;
            if (max < 0) max = 0;

            ScrollOffset += offset;
            if (ScrollOffset < 0) ScrollOffset = 0;
            if (ScrollOffset > max) ScrollOffset = max;

        }

        public override void MiddleClick(bool ctrl)
        {
            if(TileSelect)
            {
                // Nothing
            }  
            else
            {
                var l = Editor.LevelMode;

                l.EditSelectedModifier++;
                if (l.EditSelectedModifier > 4) l.EditSelectedModifier = 0;
                if (l.EditSelectedCell == Cell.Empty 
                    || l.EditSelectedCell == Cell.Concrete 
                    || l.EditSelectedCell == Cell.Dirt) l.EditSelectedModifier = 0;
                Game.StatusMessage($"CELL MODIFIER: {((int)l.GetModifier()):X2}");
            }
            
        }

        public override void HUD(SpriteBatch batch)
        {
            if (TileSelect)
            {
                var t = Editor.SelectedTile;
                if (HoverTile != Tile.Empty) t = HoverTile;
                Level.RenderTileWorld(batch, 4, line_d2 + 4, t, Color.White);
                batch.DrawString(Editor.SelectedTile.ToString().ToUpper(), new Point(24, 12), Color.White);
            } else
            {
                var l = Editor.LevelMode;
                var c = l.EditSelectedCell;
                if (HoverCell != Cell.Empty) c = HoverCell;
                Layout.RenderCellWorld(batch, new Point(4, line_d2 + 4), c | l.GetModifier());
                batch.DrawString(c.ToString().ToUpper(), new Point(24, 12), Color.White);
                batch.DrawString(l.GetModifierText(), new Point(24, 20), Color.White);

            }
            batch.DrawString(Title, new Point(6 * 16, 20), Color.White);
            
        }

        public override void Render(SpriteBatch batch)
        {

            // Get the current selected sell
            Cell selected = Editor.LevelMode.EditSelectedCell;
            if (OnSelectedCellRead != null)
            {
                selected = OnSelectedCellRead.Invoke();
            }


            // Draw the dialog window
            batch.FillRectangleScreen(Game.CameraOffset, new Size2(Game.NativeWidth + 16, Game.NativeHeight + 16), Color.Gray); ;
            // Draw the cells/tiles
            int y = 0;
            int x = 0;

            int max = TileSelect ? System.Enum.GetValues(typeof(Tile)).Length
                                 : Editor.LevelMode.EditCellValues.Length;

            for (int i = 0; i < max; i++)
            {
                var world = new Point(x, y).ToWorld();
                world.Y -= ScrollOffset;

                if (TileSelect)
                {
                    Level.RenderTileScreen(batch, world.X, world.Y, (Tile)i, Color.White);
                    if (Editor.MouseScreenCell.X == x && Editor.MouseScreenCell.Y == y) HoverTile = (Tile)i;
                } else
                {
                    var c = Editor.LevelMode.EditCellValues[i];
                    if ((int)c > 0xFFF) break;
                    if (Editor.MouseScreenCell.X == x && Editor.MouseScreenCell.Y == y) HoverCell = c;
                    Level.RenderTileScreen(batch, world.X, world.Y, Layout.CellToTile(c), Color.White);

                    if (c == selected)
                    {
                        batch.DrawRectangleScreen(world + Game.CameraOffset, new Size2(16, 16), Color.Cyan, 1);
                    }
                }

                x++;
                if (x > 15)
                {
                    x = 0;
                    y++;
                }

            }


        }

        public override void OnEnterMode()
        {
            PreviousMode = Editor.GetMode(Editor.LastMode);
            LastCameraMode = Level.CameraMode;
            LastCameraPos = Game.CameraPos;
            Level.ResetCamera();
            Level.CameraMode = CameraMode.Locked;
        }

        public override void OnExitMode()
        {
            Level.CameraMode = LastCameraMode;
            Game.CameraPos = LastCameraPos;
            OnSelectedCellRead = null;
            OnCellSelected = null;
        }
    }
}
