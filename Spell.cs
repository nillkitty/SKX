using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace SKX
{

    /// <summary>
    /// Represents a scriptable action implemented to avoid hard coding
    /// story elements into the engine.
    /// </summary>
    public class Spell
    {
        [JsonIgnore]
        private Level Level => Game.World as Level;     // Used to make code easier to read

        public int ID { get; set; }             // Each spell gets a unique ID per room
        public Point Position { get; set; }     // Certain trigger types use the spell's cell position
        public SpellType Type { get; set; }     // What the spell does when it executes
        public Cell CellType { get; set; }      // Parameters used by certain spell types
        public ObjType ObjType { get; set; }    // "
        public int Param { get; set; }          // "
        public int RequireCount { get; set; }   // How many more times the spell must be triggered before
                                                // it executes (or how many of the required item must Dana have)
        public Point Target { get; set; }       // Target position used by many spell types
        public InventoryItem? RequiredItem { get; set; }    // If specified, Dana must have this item in
                                                            // inventory
        public SpellExecuted? RequiredSpell { get; set; }   // If specified, this spell must have previously
                                                            // executed
        public SpellTrigger Trigger { get; set; }       // Trigger type

        /* Internals */
        private bool Enabled;
        private static int nextID = 0;
        internal bool Finished;

        public Spell()
        {
            if (Level != null)
            {
                var max = Level.Layout.Spells.Count == 0 ? 0 : Level.Layout.Spells.Max(x => x.ID);
                if (nextID < max) nextID = max + 1;
            }
            ID = nextID++;
        }

        /// <summary>
        /// Returns a description of the spell action
        /// </summary>
        public override string ToString() => Type switch
        {
            SpellType.None => "NULL SPELL",
            SpellType.ChangeCell => $"CHG {Target.X} {Target.Y} TO {CellType.ToString().ToUpper()} {(Param == 0 ? "" : "ANIM")}",
            SpellType.SpawnObject => $"SPAWN AT {Target.X} {Target.Y}",
            SpellType.DisableScroll => $"DISABLE SCROLL",
            SpellType.EnableScroll => $"ENABLE SCROLL",
            SpellType.SecretExit => $"USE SECRET EXIT",
            SpellType.RandomCell => $"RANDOM CELL",
            SpellType.ExitLevel => $"EXIT LEVEL",
            SpellType.UnhideAll => $"UNHIDE ALL CELLS",
            SpellType.ReverseSparkies => $"REVERSE SPARKIES",
            SpellType.ChangeStartPos => $"CHG START POS TO {Target.X} {Target.Y}",
            _ => "",
        };

        /// <summary>
        /// Called by Dana when he collects a question potion.
        /// </summary>
        public void Potion()
        {
            if (Trigger == SpellTrigger.Potion)
            {
                if (RequireCount > 0)
                {
                    RequireCount--;
                    return;
                }
                Enabled = true;
            }
        }

        /// <summary>
        /// Checks to see if spell pre-requisites have been met
        /// </summary>
        /// <returns></returns>
        public bool MeetsPrereqs()
        {

            // Don't process things unless the game is running
            if (Level.State == LevelState.Edit) return false;

            if (Level.State != LevelState.Running && Trigger != SpellTrigger.PreLoad
                && Trigger != SpellTrigger.ExitLevel) return false;
 
            var req = RequireCount == 0 ? 1 : RequireCount;
            if (RequiredItem.HasValue && (Game.Sesh.InventoryContains(RequiredItem.Value) < req))
            {
                return false;
            }
            if (RequiredSpell.HasValue && !Game.Sesh.HasSpell(RequiredSpell.Value))
            {
                return false;
            }
            switch (Trigger)
            {
                case SpellTrigger.PreLoad:
                case SpellTrigger.Immediate:
                    // Like a Karen, always triggered
                    return true;
                case SpellTrigger.CastsMagic:
                    if (Level.Dana != null)
                    {
                        if (Level.Dana.MagicCell == Position)
                        {
                            // Dana is casting magic in this cell
                            return true;
                        }
                    }
                    break;
                case SpellTrigger.DanaTouches:
                    if (Level.Dana.ProjectedBox.Contains(Position.ToWorld() + new Point(8, 8)))
                    {
                        // Dana is touching this cell
                        return true;
                    }
                    break;
                case SpellTrigger.HeadHit:
                    if (Level.Dana != null)
                    {
                        if (Level.Dana.HeadCell == Position && Level.Dana.HeadTimer == 0)
                        {
                            // Dana is hitting this cell with his head
                            return true;
                        }
                    }
                    break;

                case SpellTrigger.VapourMode:
                    return Level.VapourMode;

                case SpellTrigger.Potion:
                    if (Enabled) return true;
                    break;

                case SpellTrigger.ExitLevel:
                    return Level.State == LevelState.DoorStars;

                case SpellTrigger.NoEnemies:
                    if (Level.EnemyCount > 0 || Level.Ticks < 60)
                        break;  // Don't trigger before enemies have loaded
                    else return true;

            }
            return false;
        }

        /// <summary>
        /// Called on every game tick
        /// </summary>
        public void Update()
        {
            if (Level is null) return;      // Just in case
            if (Finished) return;           // Don't re-run if finished
            if (!MeetsPrereqs()) return;    // Don't run if prereqs not met
            if (RequireCount > 0)           // Check requirement count
            {
                RequireCount--;
                return;
            }


            switch (Type)
            {
                case SpellType.None:
                    Finished = true;
                    break;
                case SpellType.ChangeCell:
                    if (Target.X == default) Target = Position;
                    Level.Layout[Target] = CellType;
                    if (Param > 0) // Animate 
                    {
                        Level.Layout.DrawSparkle(Target, Animation.ShortSparkle, false);
                    }
                    Finished = true;
                    break;
                case SpellType.SpawnObject:
                    var sp = Level.Layout.Spawns.FirstOrDefault(x => x.Position == Target);
                    var si = sp.Dispense();
                    if (si != null)
                    {
                        Level.AddObject(GameObject.Create(si.Type, Level, si.Direction, si.Flags, sp.Position.ToWorld()));
                    }
                    Finished = true;
                    break;
                case SpellType.DisableScroll:
                    Game.Sesh.ScrollDisabled = true;
                    Finished = true;
                    break;
                case SpellType.EnableScroll:
                    Game.Sesh.ScrollDisabled = false;
                    Finished = true;
                    break;
                case SpellType.SecretExit:
                    Game.Sesh.SecretExit = true;
                    Finished = true;
                    break;
                case SpellType.RandomCell:
                    Cell rc = Level.Layout.GetRandomCell();
                    if (Target == default) Target = this.Position;
                    var c = Level.Layout[Target];
                    if (c == Cell.Empty)
                    {
                        // If the block is currently empty then
                        // put the item there as hidden (unless the item
                        // is also empty)
                        c = (rc == Cell.Empty) ? Cell.Empty : (rc | Cell.Hidden);
                    } else
                    {
                        // If the block isn't empty, then stuff the
                        // random item into its block
                        c = c.GetEffectiveModifier().SetContents(rc);
                    }
                    Level.Layout[Target] = c;
                    Finished = true;
                    break;
                case SpellType.ExitLevel:
                    Level.Dana?.EnterDoor(CellType, Target);
                    Finished = true;
                    break;
                case SpellType.UnhideAll:
                    Level.Layout.UnhideAll();
                    Finished = true;
                    break;
                case SpellType.ChangeStartPos:
                    Game.Sesh.WarpTo = Target;
                    break;
                case SpellType.ReverseSparkies:
                    foreach(var o in Level.Objects)
                    {
                        if (o is Objects.Sparky s)
                        {
                            if (s.Flags.HasFlag(ObjFlags.Clockwise))
                                s.Flags &= ~ObjFlags.Clockwise;
                            else
                                s.Flags |= ObjFlags.Clockwise;
                            s.Direction = s.Direction.Opposite();
                            s.HugDirection = s.HugDirection.Opposite();
                        }
                    }
                    Finished = true;
                    break;

            }

            if (Finished)
            {
                Game.LogInfo($"Executed spell {ID} ({Trigger}) {Type}");
                Game.Sesh.SpellsExecuted.Add(new SpellExecuted() { FromRoom = Game.Sesh.RoomNumber, SpellID = ID });
            }
        }

    }


    /// <summary>
    /// Valid triggers for Spells
    /// </summary>
    public enum SpellTrigger
    {
        Immediate,
        CastsMagic,
        DanaTouches,
        Potion,
        HeadHit,
        VapourMode,
        NoEnemies,
        PreLoad,
        ExitLevel
    }

    /// <summary>
    /// Valid actions for Spells
    /// </summary>
    public enum SpellType
    {
        None,
        ChangeCell,
        SpawnObject,
        DisableScroll,
        EnableScroll,
        SecretExit,
        RandomCell,
        ExitLevel,
        UnhideAll,
        ReverseSparkies,
        ChangeStartPos,
        LastType = ChangeStartPos
    }

}
