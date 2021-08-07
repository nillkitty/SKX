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
    /// Edits the background layer of the level
    /// </summary>
    public class BackgroundMode : EditorMode
    {
        public int EditSelectedColor = 0;                      // Selected BG color

        public BackgroundMode(LevelEditor editor) : base(editor)
        {
            Editor.SelectedTile = Level.BackgroundTile;

            Commands.Add(new Command("OBJECT MODE", EditFillBg, "FILL BACKGROUND", Keys.F, false, false));
            Commands.Add(new Command("OBJECT MODE", EditChangeBgTile, "SELECT COMMON BG TILES", Keys.T, false, false));
            Commands.Add(new Command("OBJECT MODE", CopyColor, "COPY BG COLOR", Keys.C, true, false));
            Commands.Add(new Command("OBJECT MODE", PasteColor, "PASTE BG COLOR", Keys.V, true, false));

            Tidbits.Add(new Tidbit("MOUSE", "LEFT", "PAINT BG TILE"));
            Tidbits.Add(new Tidbit("MOUSE", "C-LEFT", "PAINT BG RECTANGLE"));
            Tidbits.Add(new Tidbit("MOUSE", "MIDDLE", "PAINT SHRINE MOTIF"));
            Tidbits.Add(new Tidbit("MOUSE", "RIGHT", "PICK UP CLICKED TILE"));
            Tidbits.Add(new Tidbit("MOUSE", "MW-UP", "PREV TILE"));
            Tidbits.Add(new Tidbit("MOUSE", "MW-DN", "NEXT TILE"));
            Tidbits.Add(new Tidbit("MOUSE", "C+MW-UP", "BACK 8 TILES"));
            Tidbits.Add(new Tidbit("MOUSE", "C+MW-DN", "FORWARD 8 TILES"));
        }

        void CopyColor() { 
            Editor.Copy(Layout.BackgroundColor, "COPIED BG COLOR"); 
        }

        public override void LeftClick(bool ctrl)
        {
            if (ctrl)
            {
                // Fill a rectangle from our last click to the ctrl+click
                EditFillChunkBG(Editor.LastClick, Editor.MouseCell, Editor.SelectedTile);
                return;
            }

            // Paint a cell
            Layout.Background[Editor.MouseCell.Y * Layout.Width + Editor.MouseCell.X] = Editor.SelectedTile;
        }


        public override void MiddleClick(bool ctrl)
        {
            // Paint shrine motif/art
            var m = LargeObject.Motif(Layout, Editor.MouseCell.X, Editor.MouseCell.Y, Layout.Shrine);
            Layout.DrawBGMotif(m);
        }

        // Draws a filled rectangle of tiles (ctrl+left click)
        void EditFillChunkBG(Point p1, Point p2, Tile tile)
        {
            var left = Math.Min(p1.X, p2.X);
            var right = Math.Max(p1.X, p2.X);
            var top = Math.Min(p1.Y, p2.Y);
            var bottom = Math.Max(p1.Y, p2.Y);
            for (int y = top; y <= bottom; y++)
                for (int x = left; x <= right; x++)
                {
                    Layout.Background[y * Layout.Width + x] = tile;
                }
        }

        public override void HUD(SpriteBatch batch)
        {
            // Selected tile name and tile icon
            batch.FillRectangle(new Rectangle(4, line_d2 + 4, 16, 16), Level.BackgroundColor);
            Level.RenderTileWorld(batch, 4, line_d2 + 4, Editor.SelectedTile, Color.White);
            batch.DrawString(Editor.SelectedTile.ToString().ToUpper(), new Point(24, 12), Color.White);
        }



        void PasteColor()
        {
            var r = Editor.Paste<Color>("PASTED BG COLOR");
            if  (r.ok)
            {
                Layout.BackgroundColor = r.value;
                Level.BackgroundColor = r.value;
            }
        }

        public override void ScrollDown()
        {
            var shift = Control.ShiftKeyDown();
            var ctrl = Control.ControlKeyDown();
            var alt = Control.AltKeyDown();

            if (shift | ctrl | alt)
            {
                switch (shift, ctrl, alt)
                {
                    case (false, true, false):
                        EditBGColor(-1);                // Color preset cycle
                        break;
                    case (true, false, false):
                        Level.BackgroundColor.R += 5;   // Color custom R
                        break;
                    case (true, true, false):
                        Level.BackgroundColor.G += 5;   // Color custom G
                        break;
                    case (true, false, true):
                        Level.BackgroundColor.B += 5;   // Color custom B
                        break;

                }
                Layout.BackgroundColor = Level.BackgroundColor;
                Game.StatusMessage($"BG COLOR: {Level.BackgroundColor.R} {Level.BackgroundColor.G} {Level.BackgroundColor.B}");
                return;
            }

            // Otherwise cycle tiles            
            Editor.SelectedTile -= ctrl ? 8 : 1;
            if (Editor.SelectedTile < 0) Editor.SelectedTile = Tile.Empty;

            Game.StatusMessage($"SELECTED TILE: {(int)Editor.SelectedTile:X2}");
        }

        public override void ScrollUp()
        {
            var shift = Control.ShiftKeyDown();
            var ctrl = Control.ControlKeyDown();
            var alt = Control.AltKeyDown();

            if (shift | ctrl | alt)
            {
                switch (shift, ctrl, alt)
                {
                    case (false, true, false):
                        EditBGColor(1);             
                        break;
                    case (true, false, false):
                        Level.BackgroundColor.R -= 5;
                        break;
                    case (true, true, false):
                        Level.BackgroundColor.G -= 5;
                        break;
                    case (true, false, true):
                        Level.BackgroundColor.B -= 5;
                        break;

                }
                Layout.BackgroundColor = Level.BackgroundColor;
                Game.StatusMessage($"BG COLOR: {Level.BackgroundColor.R} {Level.BackgroundColor.G} {Level.BackgroundColor.B}");
                return;
            }

            Editor.SelectedTile += ctrl ? 8 : 1;
            if (Editor.SelectedTile > Tile.LastEditTile) Editor.SelectedTile = Tile.LastEditTile;

            Game.StatusMessage($"SELECTED TILE: {(int)Editor.SelectedTile:X2}");
        }

        public override void RightClick(bool ctrl)
        {
            // Pick up like an ink dropper tool
            Editor.SelectedTile = Layout.Background[Editor.MouseCell.Y * Layout.Width + Editor.MouseCell.X];
        }

        void EditBGColor(int offset)
        {
            // Cycle through hard-coded stock bg colors
            EditSelectedColor += offset; ;
            if (EditSelectedColor > LevelEditor.BGColors.Length - 1) EditSelectedColor = 0;
            if (EditSelectedColor < 0) EditSelectedColor = 0;
            Level.BackgroundColor = LevelEditor.BGColors[EditSelectedColor];
            Layout.BackgroundColor = Level.BackgroundColor;

        }
        void EditFillBg()
        {
            // Fill the entire bg array with a single tile
            Level.BackgroundTile = Editor.SelectedTile;
            Layout.BackgroundTile = Level.BackgroundTile;
            Layout.FillBackground();
        }

        void EditChangeBgTile()
        {
            // Cycle through hard-coded stock bg tiles
            if (Editor.SelectedTile == Tile.BrickBackground)
                Editor.SelectedTile = Tile.StuccoBackground;
            else if (Editor.SelectedTile == Tile.StuccoBackground)
                Editor.SelectedTile = Tile.BlockBackground;
            else
                Editor.SelectedTile = Tile.BrickBackground;
        }

    }
}
