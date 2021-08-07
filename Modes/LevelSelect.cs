using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SKX.Modes
{

    /// <summary>
    /// Level Select Mode
    /// </summary>
    public class LevelSelect : World
    {

        private Story SelectedStory;
        private int SelectedIndex;
        private int SelectedRoom;
        private List<Layout> Rooms;
        private int ScrollOffset;
        private bool Outline;

        public LevelSelect() : base(17, 17)
        {
            BackgroundColor = Color.Blue;
            BackgroundTile = Tile.StuccoBackground;
        }

        public override void Init()
        {
            // Clear previous menu/pause
            Game.Pause = false;
            Game.Menu = null;

            Sound.StopAll();
            Sound.HiddenIntro.Play();

            SelectedStory = Game.Story;
            LoadRooms();

            Game.CameraPos = default;
            Game.UpdateTitle();

            base.Init();

        }

        private void LoadRooms(bool preserve_index = false)
        {
            var bundled = Game.Assets.Bundle.Layouts.Where(r => r.Story == SelectedStory);
            var files = Bundle.GetWorkFiles("room", z => Layout.LoadFile(z, null)).Where(r => r.Story == SelectedStory);

            Rooms = bundled.Union(files).OrderBy(x => x.RoomNumber).ToList();

            Game.Story = SelectedStory;
            if (!preserve_index)
            {
                SelectedIndex = -1;
            } else
            {
                if (SelectedIndex < -1) SelectedIndex = -1;
                if (SelectedIndex > Rooms.Count - 1) SelectedIndex = Rooms.Count - 1;
            }
            UpdateRoom();

        }

        private void UpdateRoom()
        {
            if (SelectedIndex < -1) SelectedIndex = -1;
            if (SelectedIndex >= Rooms.Count) SelectedIndex = Rooms.Count - 1;
            if (SelectedIndex < 0)
            {
                SelectedRoom = -1;
            }
            else
            {
                SelectedRoom = Rooms[SelectedIndex].RoomNumber;
            }
        }

        public override void Update(GameTime gameTime)
        {
            Game.DrawHUD = false;

            Controls();

            ObjectMaintenance();
            UpdateObjects(gameTime);
            base.Update(gameTime);
        }

        private void Controls()
        {

            var ctrl = Control.ControlKeyDown();

            if (Control.Esc.Pressed(false))
            {
                Game.Menu = Game.StartMenu;
            }

            if (Control.Up.Pressed(true) || Control.Jump.Pressed(true) || Control.WheelUp)
            {
                if (ctrl) SelectedIndex = 0; else SelectedIndex--;
                UpdateRoom();
                CheckScroll();
            }
            if (Control.Down.Pressed(true) || Control.Crouch.Pressed(true) || Control.WheelDown)
            {
                if (ctrl) SelectedIndex = Rooms.Count - 1; else SelectedIndex++;
                UpdateRoom();
                CheckScroll();
            }
            if (Control.Right.Pressed(true))
            {
                Outline = !Outline;
            }
            if (Control.Enter.Pressed(true))
            {
                if (SelectedIndex < 0)
                {
                    SelectedStory++;
                    if (SelectedStory > Story.Test) SelectedStory = 0;
                    LoadRooms();
                    Sound.Collect.Play();
                    return;
                }

                var r = GetSelected();
                if (r is null) return;

                Sound.StopAll();
                if (!Control.ShiftKeyDown()) Game.SuppressAutoStart = 60;
                Game.StartNoSave(r);
            }

            if (Game.DebugMode)
            {
                if (Control.KeyPressed(Keys.E) && ctrl) EditRoom();
                if (Control.KeyPressed(Keys.D) && ctrl) DelRoom();
                if (Control.KeyPressed(Keys.B) && ctrl) Build();
                if (Control.KeyPressed(Keys.N) && ctrl) NumberRoom();
                if (Control.KeyPressed(Keys.A) && ctrl) NameRoom();
   
            }

        }

        private void ReloadBundle()
        {
            
            LoadRooms(true);
        }

        private void Build()
        {
            // Build merged cspace
            string json = Editor.EditorMode.Build(true);

            // Move the old files away
            var files = Path.Combine(Game.AppDirectory, "merged");
            if (!Directory.Exists(files))
            {
                try
                {
                    Directory.CreateDirectory(files);
                } catch (Exception ex)
                {
                    Game.LogError($"Failed to create 'files' directory: {ex}");
                    Game.StatusMessage("FAILED TO CREATE DIRECTORY");
                    return;
                }
            }
            var di = new DirectoryInfo(Game.AppDirectory);
            var df = di.GetFiles("room_*.json");
            foreach(var f in df)
            {
                var newname = Path.Combine(files, f.Name);
                try
                {
                    f.MoveTo(newname, true);
                } catch (Exception ex)
                {
                    Game.LogError($"Failed to move '{f.FullName}' to '{newname}': {ex}");
                    Game.StatusMessage("FAILED TO MOVE FILE");
                    return;
                }
            }

            // Reload the bundle
            try
            {
                var bundle = json.To<Bundle>();
                if (bundle.Layouts.Count > 1)
                {
                    Game.Assets.Bundle = bundle;
                    Game.StatusMessage("BUNDLE REPLACED");
                    LoadRooms();
                }
            } catch (Exception ex)
            {
                Game.LogError($"Failed to deserialize new bundle: {ex}");
                Game.StatusMessage("FAILED TO LOAD NEW BUNDLE!");
                return;
            }

        }

        private Layout GetSelected()
        {
            if (SelectedIndex < 0) return null;
            if (SelectedIndex > Rooms.Count - 1) return null;
            return Rooms[SelectedIndex];
        }

        private void EditRoom()
        {
            var r = GetSelected();
            if (r is null) return;

            Game.StartNoSave(r);
            ((Level)Game.World).Edit(false);
        }

        private void DelRoom()
        {
            var r = GetSelected();
            if (r is null) return;

            if (r.OriginalFileName is null)
            {
                Game.InputPrompt($"DELETE RM {r.RoomNumber:X}{r.Story.ToStoryID()} FROM BUNDLE? Y/N", DelRoom);
            }
            else
            {
                Game.InputPrompt($"DELETE FILE {r.OriginalFileName.SafeString()}? Y/N", DelRoom);
            }

        }

        private void DelRoom(string confirm)
        {
            if (confirm != "Y") return;
            var r = GetSelected();
            if (r is null) return;

            if (r.OriginalFileName is null)
            {
                Game.Assets.Bundle.Layouts.Remove(r);
                Game.StatusMessage("DELETED FROM RAM");
                Sound.Collect.Play();
                LoadRooms(true);
            }
            else
            {
                try
                {
                    System.IO.File.Delete(r.OriginalFilePath);
                    Game.StatusMessage("FILE DELETED");
                    Sound.Collect.Play();
                    LoadRooms(true);
                }
                catch
                {
                    Sound.Wince.Play();
                    Game.StatusMessage("ERROR DELETING FILE");
                }
            }

        }

        private void NumberRoom()
        {
            var r = GetSelected();
            if (r is null) return;

            Game.InputPromptHex($"NEW ROOM NUM FOR RM. {SelectedRoom:X}:", NumberRoom);
        }

        private void NumberRoom(int i)
        {
            var r = GetSelected();
            if (r is null) return;

            r.RoomNumber = i;
            r.SaveToFile(SelectedStory.ToStoryID());
            LoadRooms(true);
        }

        private void NameRoom()
        {
            var r = GetSelected();
            if (r is null) return;

            Game.InputPrompt($"NEW NAME FOR RM. {SelectedRoom:X}:", NameRoom);
        }

        private void NameRoom(string name)
        {
            var r = GetSelected();
            if (r is null) return;

            if (string.IsNullOrEmpty(name)) name = null;

            r.Name = name;
            r.SaveToFile(SelectedStory.ToStoryID());
            LoadRooms(true);
            
        }


        private void CheckScroll()
        {
            var y = 16 + (16 * SelectedIndex);
            if (y - ScrollOffset > 200)
            {
                ScrollOffset = y - 16;
            }
            if (y - ScrollOffset < 16)
            {
                ScrollOffset = y - 16;
            }
            if (y < 128)
            {
                ScrollOffset = 0;
            }
        }

        protected override void RenderBackground(SpriteBatch batch)
        {
            RenderTileMultiDest(batch, BackgroundTile, 0, 0, TileWidth * TileHeight);
        }

        protected override void RenderForeground(SpriteBatch batch)
        {
            // Scrolling section
            Point p = new Point(0, 8);
            Layout selected = null;

            // Title
            batch.DrawShadowedStringCentered("LEVEL SELECT", p.Y - ScrollOffset, Color.White);
            p.Y += 16;
            p.X = 8;

            bool onstory = SelectedIndex == -1;
            batch.DrawShadowedString($"{(onstory ? ">" : " ")}STORY: {SelectedStory.ToStoryID()}{SelectedStory.ToString().ToUpper()}",
                  p - new Point(0, ScrollOffset), onstory ? Color.Yellow : Color.White);
            p.Y += 16;

            int i = 0;
            // Rooms
            foreach(var r in Rooms)
            {
                var color = Color.White;
                if (i == SelectedIndex)
                {
                    selected = r;
                    color = Color.Yellow;
                }

                // Shrine
                RenderTileWorld(batch, p.X, p.Y - 4 - ScrollOffset, Layout.CellToTile(r.GetShrine(r.Shrine)), Color.White);
                // Room # badge
                batch.DrawOutlinedString(r.RoomNumber.ToString("X"), new Point(p.X + 8, p.Y - ScrollOffset + 4), Color.White, 1, Color.Black * 0.5f);
                p.X += 24;
                // Room name
                var name = $" {(i == SelectedIndex ? ">" : " ")} {(r.OriginalFileName is null ? "d" : "*")}{(r.Name ?? $"ROOM {r.RoomNumber:X}")}";
                batch.DrawShadowedString(name, p - new Point(0, ScrollOffset), color);

                p.X = 8;
                p.Y += 16;
                i++;
            }

            // Non-scrolling section
     

            if (selected != null)
            {
                var origin = new Point(136, 32);
                MiniLayout(selected, batch, origin, 119, 98); p.Y += 128;

                p.X = 136;
                p.Y = origin.Y + 104;

                batch.DrawShadowedString($"ROOM: {selected.RoomNumber:X}", p, Color.White); p.Y += 8;
                batch.DrawShadowedString($"NAME: " + (selected.Name ?? $"ROOM {selected.RoomNumber:X}"), p, Color.White); p.Y += 8;
                batch.DrawShadowedString($"SIZE: {selected.Width} x {selected.Height}", p, Color.White); p.Y += 8;
                if (selected.OriginalFileName is null)
                {
                    batch.DrawShadowedString($"d EMBEDDED", p, Color.White); p.Y += 8;
                } else
                {
                    batch.DrawShadowedString($"*{selected.OriginalFileName.SafeString()}", p, Color.White); p.Y += 8;
                }

                p.Y += 32;
                batch.DrawShadowedString($"[c=ffff]C+E [c=ffffff]EDIT", p, Color.White); p.Y += 8;
                batch.DrawShadowedString($"[c=ffff]C+N [c=ffffff]CHG NUMBER", p, Color.White); p.Y += 8;
                batch.DrawShadowedString($"[c=ffff]C+A [c=ffffff]CHG NAME", p, Color.White); p.Y += 8;
                batch.DrawShadowedString($"[c=ffff]C+D [c=ffffff]DELETE", p, Color.White); p.Y += 8;
                batch.DrawShadowedString($"[c=ffff]C+B [c=ffffff]BUILD", p, Color.White); p.Y += 8;

            }

            RenderObjects(batch);
        }

        private void MiniLayout(Layout l, SpriteBatch batch, Point p, int w, int h)
        {
            float sx = (float)w / (float)l.Width;
            float sy = (float)h / (float)l.Height;

            if (sx < 1) sx = 1;
            if (sy < 1) sy = 1;

            batch.FillRectangle(new RectangleF(p, new Size2(w, h)), l.BackgroundColor);

            for(int y = 0; y < l.Height; y++)
                for(int x = 0; x < l.Width; x++)
                {
                    Color color = Color.Transparent;
                    var c = l[x, y];
                    var t = Layout.CellToTile(c);

                    if (!Outline)
                    {
                        batch.Draw(Game.Assets.Blocks, new Rectangle(new Point((int)(p.X + sx * x), (int)(p.Y + sy * y)), new Point((int)sx, (int)sy)),
                            Game.Assets.BlockSourceTileRect(t), Color.White);
                    } 
                    else
                    {
                        if (Layout.IsEmpty(c)) continue;
                        if (Layout.IsSolid(c)) color = Color.SandyBrown;
                        if (Layout.IsConcrete(c)) color = Color.White;
                        batch.FillRectangle(new RectangleF(new Point2(p.X + sx * x, p.Y + sy * y), new Size2(sx, sy)), color);
                    }

                }

        }


    }

}
