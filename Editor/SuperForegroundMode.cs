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
    /// Edits the super foreground layer of the level
    /// </summary>
    public class SuperForegroundMode : EditorMode
    {

        public SuperForegroundMode(LevelEditor editor) : base(editor)
        {
            Commands.Add(new Command("SUPER FG MODE", EditFillBg, "FILL SUPER FG", Keys.F, false, false));
            Commands.Add(new Command("SUPER FG MODE", ToggleSFBorders, "USE SF FOR BORDERS", Keys.B, false, false));


            Tidbits.Add(new Tidbit("MOUSE", "LEFT", "PAINT SFG TILE"));
            Tidbits.Add(new Tidbit("MOUSE", "C-LEFT", "PAINT SFG RECTANGLE"));
            Tidbits.Add(new Tidbit("MOUSE", "RIGHT", "PICK UP CLICKED TILE"));
            Tidbits.Add(new Tidbit("MOUSE", "MW-UP", "PREV TILE"));
            Tidbits.Add(new Tidbit("MOUSE", "MW-DN", "NEXT TILE"));
            Tidbits.Add(new Tidbit("MOUSE", "C+MW-UP", "OPACITY UP"));
            Tidbits.Add(new Tidbit("MOUSE", "C+MW-DN", "OPACITY DOWN"));

        }

        public override void OnExitMode()
        {
            // Check to see if we can remove the super foreground
            if (!Layout.SuperForeground.Any(x => x != Tile.Empty))
            {
                Layout.SuperForeground = null;
            }
        }

        public override void OnEnterMode()
        {
            // Create the SFG for editing purposes whether we use it or not...
            Layout.SuperFGCheck();
        }

        public override void LeftClick(bool ctrl)
        {
            if (ctrl)
            {
                // Fill a rectangle from our last click to the ctrl+click
                EditFillChunkSFG(Editor.LastClick, Editor.MouseCell, Editor.SelectedTile);
                return;
            }

            // Paint a cell
            Layout.SuperForeground[Editor.MouseCell.Y * Layout.Width + Editor.MouseCell.X] = Editor.SelectedTile;
        }


        public override void MiddleClick(bool ctrl)
        {
   
        }

        // Draws a filled rectangle of tiles (ctrl+left click)
        void EditFillChunkSFG(Point p1, Point p2, Tile tile)
        {
            var left = Math.Min(p1.X, p2.X);
            var right = Math.Max(p1.X, p2.X);
            var top = Math.Min(p1.Y, p2.Y);
            var bottom = Math.Max(p1.Y, p2.Y);
            for (int y = top; y <= bottom; y++)
                for (int x = left; x <= right; x++)
                {
                    Layout.SuperForeground[y * Layout.Width + x] = tile;
                }
        }

        void ToggleSFBorders()
        {
            Layout.UseSFForBorders = !Layout.UseSFForBorders;
            Game.StatusMessage($"SF FOR BORDERS: {(Layout.UseSFForBorders ? "ON" : "OFF")}");
        }

        public override void HUD(SpriteBatch batch)
        {
            // Selected tile name and tile icon
            batch.FillRectangle(new Rectangle(4, line_d2 + 4, 16, 16), Level.BackgroundColor);
            Level.RenderTileWorld(batch, 4, line_d2 + 4, Editor.SelectedTile, Color.White);
            batch.DrawString(Editor.SelectedTile.ToString().ToUpper(), new Point(24, 12), Color.White);
            batch.DrawString($"OPACITY: {Layout.SuperForegroundOpacity * 100:0}", new Point(64, 24), Color.White);
        }


        public override void ScrollDown()
        {
            bool ctrl = Control.ControlKeyDown();
            bool shift = Control.ShiftKeyDown();
            bool alt = Control.AltKeyDown();
            Color c = Layout.SuperForegroundColor;

            switch (shift, ctrl, alt)
            {
                case (false, false, false):
                    Editor.SelectedTile -= Control.ControlKeyDown() ? 8 : 1;
                    if (Editor.SelectedTile < 0) Editor.SelectedTile = Tile.Empty;
                    Game.StatusMessage($"SELECTED TILE: {(int)Editor.SelectedTile:X2}");
                    return;
                case (false, true, false):
                    Layout.SuperForegroundOpacity -= 0.01f;
                    if (Layout.SuperForegroundOpacity < 0f) Layout.SuperForegroundOpacity = 0f;
                    Game.StatusMessage($"OPACITY: {Layout.SuperForegroundOpacity * 100:0}");
                    return;
                case (true, false, false):
                    c.R -= 5;
                    break;
                case (true, true, false):
                    c.G -= 5;
                    break;
                case (true, false, true):
                    c.B -= 5;
                    break;
            }
            Layout.SuperForegroundColor = c;
            Game.StatusMessage($"SFG COLOR: {c}");
            
        }

        public override void ScrollUp()
        {
            bool ctrl = Control.ControlKeyDown();
            bool shift = Control.ShiftKeyDown();
            bool alt = Control.AltKeyDown();

            Color c = Layout.SuperForegroundColor;

            switch (shift, ctrl, alt)
            {
                case (false, false, false):
                    Editor.SelectedTile += Control.ControlKeyDown() ? 8 : 1;
                    if (Editor.SelectedTile > Tile.LastEditTile) Editor.SelectedTile = Tile.LastEditTile;
                    Game.StatusMessage($"SELECTED TILE: {(int)Editor.SelectedTile:X2}");
                    return;
                case (false, true, false):
                    Layout.SuperForegroundOpacity += 0.01f;
                    if (Layout.SuperForegroundOpacity > 1f) Layout.SuperForegroundOpacity = 1f;
                    Game.StatusMessage($"OPACITY: {Layout.SuperForegroundOpacity * 100:0}");
                    return;
                case (true, false, false):
                    c.R += 5;
                    break;
                case (true, true, false):
                    c.G += 5;
                    break;
                case (true, false, true):
                    c.B += 5;
                    break;
            }

            Layout.SuperForegroundColor = c;
            Game.StatusMessage($"SFG COLOR: {c}");
            
        }

        public override void RightClick(bool ctrl)
        {
            // Pick up like an ink dropper tool
            Editor.SelectedTile = Layout.SuperForeground[Editor.MouseCell.Y * Layout.Width + Editor.MouseCell.X];
        }

      
        void EditFillBg()
        {
            // Fill the entire sfg array with a single tile
            Layout.FillSuperForeground(Editor.SelectedTile);
        }


    }
}
