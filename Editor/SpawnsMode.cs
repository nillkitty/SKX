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
    /// Edits spawn points
    /// </summary>
    public class SpawnsMode : EditorMode
    {
        public Spawn SelectedSpawn;                        // Selected spawn point
        public int SelectedSpawnSlot;                       // Selected spawn type index
        public SpawnsMode(LevelEditor editor) : base(editor) 
        { 
            SelectedSpawn = null; 
            SelectedSpawnSlot = 0;

            // Controls
            Commands.Add(new Command("SPAWNS MODE", nextSlot, "NEXT SPAWN SLOT", Keys.OemCloseBrackets, false, false));
            Commands.Add(new Command("SPAWNS MODE", prevSlot, "PREV SPAWN SLOT", Keys.OemOpenBrackets, false, false));
            Commands.Add(new Command("SPAWNS MODE", AddSpawnSlot, "ADD SPAWN SLOT", Keys.OemPlus, false, false));
            Commands.Add(new Command("SPAWNS MODE", DelSpawnSlot, "DEL SPAWN SLOT", Keys.OemMinus, false, false));
            Commands.Add(new Command("SPAWNS MODE", incTTL, "SPAWN TTL +", Keys.T, false, false));
            Commands.Add(new Command("SPAWNS MODE", decTTL, "SPAWN TTL -", Keys.T, true, false));
            Commands.Add(new Command("SPAWNS MODE", incMax, "MAX INSTANCES +", Keys.M, false, false));
            Commands.Add(new Command("SPAWNS MODE", decMax, "MAX INSTANCES -", Keys.M, true, false));
            Commands.Add(new Command("SPAWNS MODE", changeRate, "SET DROPLET RATE", Keys.R, false, false));
            Commands.Add(new Command("SPAWNS MODE", changeChance, "SET DROPLET CHANCE", Keys.C, true, false));
            Commands.Add(new Command("SPAWNS MODE", changeType, "SET DROPLET TYPE", Keys.Y, false, false));

            Commands.Add(new Command("SPAWNS MODE - QUICK TIMINGS", fill0, "TIMING: ALL OFF", Keys.D0, false, false));
            Commands.Add(new Command("SPAWNS MODE - QUICK TIMINGS", fill1, "TIMING: EVERY 1", Keys.D1, false, false));
            Commands.Add(new Command("SPAWNS MODE - QUICK TIMINGS", fill2, "TIMING: EVERY 2", Keys.D2, false, false));
            Commands.Add(new Command("SPAWNS MODE - QUICK TIMINGS", fill4, "TIMING: EVERY 3", Keys.D3, false, false));
            Commands.Add(new Command("SPAWNS MODE - QUICK TIMINGS", fill3, "TIMING: EVERY 4", Keys.D4, false, false));
            Commands.Add(new Command("SPAWNS MODE - QUICK TIMINGS", fill5, "TIMING: 1+EVERY 4", Keys.D5, false, false));
            Commands.Add(new Command("SPAWNS MODE - QUICK TIMINGS", fill6, "TIMING: EVERY 8+1", Keys.D6, false, false));
            Commands.Add(new Command("SPAWNS MODE - QUICK TIMINGS", fill7, "TIMING: EVERY 8+2", Keys.D7, false, false));
            Commands.Add(new Command("SPAWNS MODE - QUICK TIMINGS", fill8, "TIMING: EVERY 8", Keys.D8, false, false));
            Tidbits.Add(new Tidbit("MOUSE", "LEFT", "SELECT SPAWN POINT"));
            Tidbits.Add(new Tidbit("MOUSE", "LEFT-DRAG", "MOVE SPAWN"));
            Tidbits.Add(new Tidbit("MOUSE", "RIGHT", "CREATE/DELETE SPAWN PT"));

            // Object manipulation controls
            AddObjectCommands();

            // Control handlers
            void incTTL() => ChangeTTL(1);
            void decTTL() => ChangeTTL(-1);
            void incMax() => ChangeMaxInstances(1);
            void decMax() => ChangeMaxInstances(-1);
            void prevSlot() => ChangeSpawnSlot(-1);
            void nextSlot() => ChangeSpawnSlot(1);

            void fill0() => SpawnPhaseFill(0x00000000);
            void fill1() => SpawnPhaseFill(0xFFFFFFFF);
            void fill2() => SpawnPhaseFill(0x88888888);
            void fill3() => SpawnPhaseFill(0xAAAAAAAA);
            void fill4() => SpawnPhaseFill(0x92492492);
            void fill5() => SpawnPhaseFill(0x44444444);
            void fill6() => SpawnPhaseFill(0x40404040);
            void fill7() => SpawnPhaseFill(0x20202020);
            void fill8() => SpawnPhaseFill(0x80808080);

        }

        void changeType()
        {
            if (SelectedSpawn is null)
            {
                Game.StatusMessage($"NO SPAWN SELECTED.");
                return;
            }
            SelectedSpawn.DropletType++;
            if (SelectedSpawn.DropletType > DropletType.LastEditorType)
            {
                SelectedSpawn.DropletType = default;
            }
        }

        void changeRate()
        {
            if (SelectedSpawn is null)
            {
                Game.StatusMessage($"NO SPAWN SELECTED.");
                return;
            }

            Game.InputPromptNumber("ENTER DROP RATE IN TICKS:", ChangeDropRate,
                SelectedSpawn.DropletRate.ToString());
        }

        void ChangeDropRate(int i)
        {
            if (SelectedSpawn is null)
            {
                Game.StatusMessage($"NO SPAWN SELECTED.");
                return;
            }

            SelectedSpawn.DropletRate = i;
            if (SelectedSpawn.DropletRate < 0) SelectedSpawn.DropletRate = 0;
            Game.StatusMessage($"DROPLET RATE: EVERY {SelectedSpawn.DropletRate} TICKS");
        }

        void changeChance()
        {
            if (SelectedSpawn is null)
            {
                Game.StatusMessage($"NO SPAWN SELECTED.");
                return;
            }

            Game.InputPromptNumber("ENTER DROPLET CHANCE PERCENT:", ChangeSpread, "100");
        }

        void ChangeSpread(int i)
        {
            if (SelectedSpawn is null)
            {
                Game.StatusMessage($"NO SPAWN SELECTED.");
                return;
            }
            SelectedSpawn.DropletChance = i;
            Game.StatusMessage($"DROPLET CHANCE SET: {i}");
        }

        public override void LeftClick(bool ctrl)
        {
            SelectedSpawn = Layout.Spawns.FirstOrDefault(op => op.Position == Editor.MouseCell);
            if (SelectedSpawn != null)
            {
                Editor.Dragging = true;
            }
        }

        public override void MouseDrag()
        {
            if (SelectedSpawn != null)
            {
                Editor.ObjectsChanged = true;
                SelectedSpawn.Position = Editor.MouseCell;
            }
        }

        public override bool HUDClick(bool ctrl)
        {

            if (SelectedSpawn == null) return false;

            // Determine if the user is clicking on the grid of red/green dots
            if (Control.MousePos.X < 128) return false;
            if (Control.MousePos.Y < 8) return false;
            if (Control.MousePos.Y > 16) return false;

            // Which one?
            int x, y;
            x = (Control.MousePos.X - 128) / 4;
            y = (Control.MousePos.Y < 12) ? 0 : 1;

            // Toggle spawn phase bit
            SpawnPhaseClick(y, x);

            return true;

        }

        void SpawnPhaseClick(int phase, int tick)
        {
            // Get the bitmask
            uint value = phase == 0 ? SelectedSpawn.Phase0 : SelectedSpawn.Phase1;
            // Toggle the one bit
            value ^= (0x80000000 >> tick);
            // Write it back
            if (phase == 0)
                SelectedSpawn.Phase0 = value;
            else
                SelectedSpawn.Phase1 = value;

        }

        public override void HUD(SpriteBatch batch)
        {
            if (SelectedSpawn != null)
            {
                // Draw spawn point info and phase controls
                batch.DrawString($"X{SelectedSpawn.X} Y{SelectedSpawn.Y} TTL:{SelectedSpawn.TTL}", new Point(8, line_d2), Color.White);
                DrawPhases(SelectedSpawn.Phase0, 128, 8);
                DrawPhases(SelectedSpawn.Phase1, 128, 12);

                // Draw droplet info
                Tile t = Objects.Droplet.GetDropTile(SelectedSpawn.DropletType);
                batch.DrawString($"[s={(int)t}] x{SelectedSpawn.DropletRate} {SelectedSpawn.DropletChance}", new Point(8 * 14, line_d1), Color.White);
                
                // Draw selected spawn slot item type info (if one is selected) 
                var item = GetObjDef();
                if (item != null)
                {
                    batch.DrawString($"DIR: {item.Direction.ToString().ToUpper()[0]}", new Point(8 * 18, line_1), Color.White);
                    batch.DrawString($"SPD: {item.GetSpeed()}", new Point(8 * 25, line_1), Color.White);
                    batch.DrawString($"FLAGS: {item.GetFlags()}", new Point(8 * 18, line_2), Color.White);
                    if (item is SpawnItem sl)
                        batch.DrawString($"MAX: {sl.MaxInstances}", new Point(8 * 25, 0), Color.White);
                }

                // Draw spawn slot list
                int x = 8;
                int n = 0;
                foreach (var i in SelectedSpawn.SpawnItems)
                {
                    var r = LevelEditor.GetObjTile(i);
                    Level.RenderTileWorld(batch, x, line_1, r.Tile, Color.White, r.Effect);

                    if (n == SelectedSpawnSlot)
                    {
                        batch.DrawRectangle(new RectangleF(x, line_1, 16, 16), Color.Yellow, 1);
                    }

                    n++;
                    x += 16;
                }

            }
            else
            {
                batch.DrawString("NO SPAWN SELECTED", new Point(24, 12), Color.White);
            }

            // Subroutine for drawing a 32-bit unsigned int as 32 red or green dots
            void DrawPhases(uint value, int x, int y)
            {
                for (int i = 0; i < 32; i++)
                {
                    bool set = (value & (0x80000000 >> i)) > 0;
                    batch.DrawRectangle(new RectangleF(x, y, 2, 2), set ? Color.Lime : Color.Red, 1);
                    x += 4;
                }
            }
        }

        public override void Render(SpriteBatch batch)
        {
            // Outline spawn points
            foreach (var s in Layout.Spawns)
            {
                batch.DrawRectangle(new RectangleF(new Point(s.X, s.Y).ToWorld().ToVector2(),
                                                   new Size2(16, 16)),
                                                   s == SelectedSpawn ? Color.Yellow : Color.White, 2);
            }
        }

        public override void ScrollUp()
        {
            // Cycle object type
            var item = GetObjDef();
            if (item != null)
            {
                item.Type++;
                if (item.Type > ObjType.LastEditType) item.Type = ObjType.LastEditType;
                FixFlags(item);

                Game.StatusMessage($"SELECTED OBJ: {(int)item.Type:X2}");
            }
        }

        public override void ScrollDown()
        {
            // Cycle object type
            var item = GetObjDef();
            if (item != null)
            {
                item.Type--;
                if (item.Type < 0) item.Type = 0;
                FixFlags(item);

                Game.StatusMessage($"SELECTED OBJ: {(int)item.Type:X2}");
            }
        }

        public override void RightClick(bool ctrl)
        {
            // Create or delete
            var sp = Layout.Spawns.FirstOrDefault(o => o.Position == Editor.MouseCell);
            if (sp != null)
            {
                Layout.Spawns.Remove(sp);
                Editor.ObjectsChanged = true;
                Game.StatusMessage("SPAWN DELETED");
            }
            else
            {
                sp = new Spawn();
                if (SelectedSpawn != null)
                {
                    sp.TTL = SelectedSpawn.TTL;
                    sp.Phase0 = SelectedSpawn.Phase0;
                    sp.Phase1 = SelectedSpawn.Phase1;
                    sp.DropletChance = SelectedSpawn.DropletChance;
                    sp.DropletRate = SelectedSpawn.DropletRate;
                    sp.DropletType = SelectedSpawn.DropletType;
                }
                else
                {
                    sp.TTL = 12;
                }
                sp.Position = Editor.MouseCell;
                Layout.Spawns.Add(sp);
                SelectedSpawn = sp;
                Editor.ObjectsChanged = true;
                Game.StatusMessage("SPAWN CREATED");
            }
        }


        void ChangeTTL(int offset)
        {
            if (SelectedSpawn == null) return;
            SelectedSpawn.TTL += offset;

            if (SelectedSpawn.TTL < 0) SelectedSpawn.TTL = 0;
            if (SelectedSpawn.TTL > 0xFF) SelectedSpawn.TTL = 0xFF;

        }

        void ChangeMaxInstances(int offset)
        {
            if (SelectedSpawn == null) return;
            var slot = SelectedSpawn.SpawnItems[SelectedSpawnSlot];
            if (slot == null) return;

            slot.MaxInstances += offset;

            if (slot.MaxInstances < 0) slot.MaxInstances = 0;
            if (slot.MaxInstances > 0xFF) slot.MaxInstances = 0xFF;

        }

        void SpawnPhaseFill(uint mask)
        {
            if (SelectedSpawn == null) return;

            SelectedSpawn.Phase0 = mask;
            SelectedSpawn.Phase1 = mask;
        }

        void ChangeSpawnSlot(int off)
        {
            if (SelectedSpawn == null)
            {
                Game.StatusMessage("NO SPAWN POINT SELECTED");
                return;
            }
            SelectedSpawnSlot += off;
            if (SelectedSpawnSlot < 0) SelectedSpawnSlot = 0;
            if (SelectedSpawnSlot > SelectedSpawn.SpawnItems.Count() - 1)
            {
                SelectedSpawnSlot = SelectedSpawn.SpawnItems.Count() - 1;
            }
        }

        void DelSpawnSlot()
        {
            // Make sure a spawn slot is selected
            if (SelectedSpawn == null)
            {
                Game.StatusMessage("NO SPAWN POINT SELECTED");
                return;
            }
            // Make sure there are spawn slots
            if (SelectedSpawn.SpawnItems.Count() == 0)
            {
                Game.StatusMessage("NO SPAWN SLOTS TO DELETE");
                return;
            }

            // Remove the selected slot
            if (SelectedSpawnSlot > -1) SelectedSpawn.SpawnItems.RemoveAt(SelectedSpawnSlot);

            // If the slot is no longer valid,  select the last slot
            if (SelectedSpawnSlot > SelectedSpawn.SpawnItems.Count - 1)
            {
                SelectedSpawnSlot = SelectedSpawn.SpawnItems.Count - 1;
            }

            Game.StatusMessage("SPAWN SLOT DELETED");
        }

        void AddSpawnSlot()
        {
            // Make sure a spawn slot is selected
            if (SelectedSpawn == null)
            {
                Game.StatusMessage("NO SPAWN SELECTED");
                return;
            }

            // Get current item (if any) and select defaults for the new slot
            var item = GetObjDef();
            var dir = item?.Direction ?? Heading.Right;
            var type = item?.Type ?? ObjType.Goblin;
            var flags = item?.Flags ?? 0;

            // Add a slot
            SelectedSpawn.SpawnItems.Add(new SpawnItem()
            { Type = type, Direction = dir, Flags = flags });

            // Select the slot
            SelectedSpawnSlot = SelectedSpawn.SpawnItems.Count() - 1;
            Game.StatusMessage("SPAWN SLOT ADDED");
        }


        public override void MiddleClick(bool ctrl)
        {
            var i = GetObjDef();
            ChangeDirection(i);
            FixFlags(i);
        }
    }
}
