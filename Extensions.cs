using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using MonoGame.Extended;
using Microsoft.Xna.Framework.Input;
using System.Linq;
using System.Reflection;
using System.IO;

namespace SKX
{
    public static class Extensions
    {

        private static StringBuilder sb = new StringBuilder(200);

        /// <summary>
        /// Converts a grid cell coordinate into a world coordinate
        /// </summary>
        public static Point ToWorld(this Point p)
        {
            return new Point(p.X * Game.NativeTileSize.X, p.Y * Game.NativeTileSize.Y);
        }

        /// <summary>
        /// Removes any characters from the string that we can't display
        /// in our text character set
        /// </summary>
        public static string SafeString(this string x) => Assets.SafeString(x);

        /// <summary>
        /// Converts a world coordinate into a vector in the range of 0f-1.0f
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static Vector2 ToWorldLerp(this Point x)
        {
            return new Vector2((float)x.X / (float)Game.World.WorldWidth, (float)x.Y / (float)Game.World.WorldHeight);
        }

        /// <summary>
        /// Gets the X component of a unit vector (1 right, -1 left)
        /// </summary>
        public static double XUnit(this Heading direction) => direction switch
        {
            Heading.Left => -1,
            Heading.Right => 1,
            _ => 0
        };

        /// <summary>
        /// Gets the Y component of a unit vector (1 down, -1 up)
        /// </summary>
        public static double YUnit(this Heading direction) => direction switch
        {
            Heading.Down => 1,
            Heading.Up => -1,
            _ => 0
        };

        /// <summary>
        /// Converts a hex string to an integer
        /// </summary>
        /// <returns></returns>
        public static int ToIntFromHex(this string x, int @default = -1)
        {
            try
            {
                return int.Parse(x, System.Globalization.NumberStyles.HexNumber);
            } catch { return @default; }
        }

        /// <summary>
        /// Converts a world coordinate into a grid cell coordinate
        /// </summary>
        public static Point ToCell(this Point p)
        {
            return new Point(p.X / Game.NativeTileSize.X, p.Y / Game.NativeTileSize.Y);
        }

        /// <summary>
        /// Converts a rectangle in cell space to world space
        /// </summary>
        public static Rectangle ToWorldRect(this Point p, int w, int h)
        {
            return new Rectangle(p.X * Game.NativeTileSize.X, p.Y * Game.NativeTileSize.Y,
                w * Game.NativeTileSize.X, h * Game.NativeTileSize.Y);
        }

        /// <summary>
        /// Converts a rectangle in cell space to world space
        /// </summary>
        public static Rectangle RectToWorldRect(this Point p, Point size)
        {
            return new Rectangle(p.X * Game.NativeTileSize.X, p.Y * Game.NativeTileSize.Y,
                size.X * Game.NativeTileSize.X, size.Y * Game.NativeTileSize.Y);
        }

        /// <summary>
        /// Gets the distance between two points
        /// </summary>
        public static double DistanceTo(this Point a, Point b)
        {
            return Math.Sqrt((Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2)));
        }

        /// <summary>
        /// Converts a rectangle in cell space to world space
        /// </summary>
        public static Rectangle ToWorld(this Rectangle rect)
        {
            return new Rectangle(rect.X * Game.NativeTileSize.X,
                rect.Y * Game.NativeTileSize.Y, rect.Width * Game.NativeTileSize.X, rect.Height * Game.NativeTileSize.Y);
        }

        /// <summary>
        /// Returns the opposite direction
        /// </summary>
        public static Heading Opposite(this Heading h)
        {
            return h switch
            {
                Heading.Right => Heading.Left,
                Heading.Left => Heading.Right,
                Heading.Up => Heading.Down,
                Heading.Down => Heading.Up,
                _ => Heading.None,
            };
        }

        public static Cell ToCracked(this Cell c)
        {
            if (c == Cell.Dirt || c == Cell.Empty || c == Cell.BlockCracked || c == Cell.FakeConcrete) 
                return Cell.BlockCracked;
            return c.GetContents() | Cell.Cracked;
        }

        public static Cell ToCovered(this Cell c)
        {
            if (c == Cell.Empty || c == Cell.Dirt || c == Cell.BlockCracked || c == Cell.FakeConcrete) 
                return Cell.Dirt;
            return c.GetContents() | Cell.Covered;
        }

        public static Cell ToVisible(this Cell c)
        {
            if (c == Cell.Empty || c == Cell.Dirt || c == Cell.BlockCracked || c == Cell.FakeConcrete) 
                return Cell.Empty;
            return c.GetContents();
        }

        public static Cell ToFrozen(this Cell c)
        {
            if (c == Cell.Empty || c == Cell.Dirt || c == Cell.BlockCracked) return Cell.Frozen;
            return c.GetContents() | Cell.Frozen;
        }

        /// <summary>
        /// Gets the innards of a cell
        /// </summary>
        public static Cell GetContents(this Cell c) => (Cell)((int)c & 0xFFF);
        /// <summary>
        /// Gets the outards of a cell
        /// </summary>
        public static Cell GetModifier(this Cell c) => (Cell)((int)c & 0xFFFFF000);

        public static Cell GetEffectiveModifier(this Cell c)
        {
            if (c == Cell.Dirt) return Cell.Covered;
            if (c == Cell.BlockCracked) return Cell.Cracked;
            return (Cell)((int)c & 0xFFFFF000);
        }

        public static Cell SetContents(this Cell c, Cell i)
        {
            var n = c | i;
            if (n == Cell.Covered) return Cell.Dirt;
            if (n == Cell.Cracked) return Cell.Cracked;
            if (n == Cell.Hidden) return Cell.Empty;
            return n;
        }

        /// <summary>
        /// Returns the pretty name for a Keys key.
        /// </summary>
        public static string ToKeyName(this Keys keys, bool shift) 
        {
            switch(keys)
            {
                case Keys.OemTilde: return "`";
                case Keys.OemQuestion: return shift ? "?" : "/";
                case Keys.OemPlus: return shift ? "=" : "+";
                case Keys.OemMinus: return "-";
                case Keys.OemCloseBrackets: return "[";
                case Keys.OemOpenBrackets: return "]";
                case Keys.Back: return "BKSP";
                case Keys.OemPeriod: return ".";
                case Keys.OemComma: return ",";
                case Keys.OemQuotes: return shift ? "\"" : "'";
            }
            var kn = keys.ToString().ToUpper();
            
            // D0 through D9
            if (kn.StartsWith("D") && kn.Length == 2) return kn.Substring(1);
            
            // Everything else
            return kn.Replace("OEM", "").Replace("NUMPAD", "");
        }

        /// <summary>
        /// Gets the number of lines in a string
        /// </summary>
        public static int GetLineCount(this string x)
        {
            return 1 + x.Where(c => c == '\n').Count();
        }


        /// <summary>
        /// Draws text from our custom bitmap font (because XNA's SpriteFont is too fucking complicated
        /// to build from a partial character set that already exists as a bitmap)
        /// 
        /// Special commands:
        ///     \n          Next line
        ///     [c=color]   Change color (e.g. [c=0000ff] is red)
        ///     [t=nn]      Insert 16x16 tile #nn
        ///     [x=nn]      Set X cursor to screen position nn, use +nn or -nn for relative offset
        ///     [y=nn]      Set Y cursor to screen position nn, use +nn or -nn for relative offset
        ///     [b=color]   Blink on (color is blink color)
        ///     [b=0]       Blink off
        ///     [f=color]   Fade (color is fade color)
        ///     [f=0]       Fade off
        /// </summary>
        public static void DrawString(this SpriteBatch batch, string text, Point position, Color color, bool ignoreTags = false)
        {

            if (text is null) return;

            int originalX = position.X;
            int size = 8;
            bool cmdon = false;
            Color oldColor = color;


            bool outlined = false;

            foreach (var c in text)
            {
                // Handle new line
                if (c == '\n')
                {
                    position.Y += 8;
                    position.X = originalX;
                    continue;
                }
                else if (cmdon && c == ']')
                {
                    if (!ignoreTags) command(sb.ToString());
                    sb.Clear();
                    cmdon = false;
                    continue;
                }
                else if (cmdon)
                {
                    if (c == '[')   // Escaped [[
                    {
                        cmdon = false;
                        position.X += size; // hack to make help text line up
                        goto print;
                    }
                    sb.Append(c);
                    continue;
                }
                else if (c == '[')
                {
                    cmdon = true;
                    continue;
                } 

            print:
                var sr = Game.Assets.CharMap[c];
                var dr = new Rectangle(position.X, position.Y, size, size);
                if (outlined)
                {
                    batch.FillRectangle(new RectangleF(position - new Point2(1, 1), new Size2(size, size)), Color.Black);
                }
                batch.Draw(Game.Assets.Text, dr, sr, color);
                position.X += size;
            }

            void command(string cmd)
            {
                if (cmd.Length < 3) return;
                switch(cmd[0])
                {
                    case 'c':
                        setColor(cmd.Substring(2));
                        break;
                    case 's':
                        tile(cmd.Substring(2), true);
                        break;
                    case 't':
                        tile(cmd.Substring(2));
                        break;
                    case 'x':
                        setx(cmd.Substring(2));
                        break;
                    case 'y':
                        sety(cmd.Substring(2));
                        break;
                    case 'b':
                        blink(cmd.Substring(2));
                        break;
                    case 'f':
                        fade(cmd.Substring(2));
                        break;
                    case 'o':
                        outline(cmd.Substring(2));
                        break;
                }
            }

            void blink(string p)
            {
                Color blinkColor = Color.Black;
                if (p == "0")
                {
                    color = oldColor;
                }
                else if (uint.TryParse(p, System.Globalization.NumberStyles.HexNumber, null, out uint packed))
                {
                    blinkColor = new Color(packed);
                    color = ((Game.World.Ticks % 30) > 15) ? oldColor : Color.Black;
                }
                
            }
            void fade(string p)
            {
                Color fadeColor = Color.Black;
                if (p == "0")
                {
                    color = oldColor;
                }
                else if (uint.TryParse(p, System.Globalization.NumberStyles.HexNumber, null, out uint packed))
                {
                    fadeColor = new Color(packed);

                    uint t = Game.Ticks % 120;    // Slice a 120 tick window
                    bool f = t < 60f;                    // 0-59 = Fade in, 60-119 Fade out
                    t %= 60;                            // Slice it down to 60 ticks now
                    float amt = Math.Clamp((float)t / 60f, 0f, 1f);         // And normalize it
                    if (f)
                        color = Color.Lerp(oldColor, fadeColor, amt);
                    else
                        color = Color.Lerp(fadeColor, oldColor, amt);
                }

            }
            void setColor(string c)
            {
                if (c == "0")
                {
                    color = oldColor;
                    return;
                }
                if (uint.TryParse(c, System.Globalization.NumberStyles.HexNumber, null, out uint p)) {
                    color = new Color(p);
                }
            }
            void outline(string x)
            {
                if (x == "0") outlined = false;
                if (x == "1") outlined = true;
            }
            void tile(string t, bool small = false)
            { 
                var i = t.ToInt();
                if (small)
                {
                    Game.World.RenderSmallTileWorld(batch, 0, position.X, position.Y, (Tile)i, color);
                } else
                {
                    Game.World.RenderTileWorld(batch, position.X, position.Y, (Tile)i, color);
                }
            }
            void setx(string p)
            {
                if (p[0]=='+' || p[0] == '-')
                {
                    position.X += p.TrimStart('+').ToInt();
                } else
                {
                    position.X = p.ToInt();
                }
                
            }
            void sety(string p)
            {
                if (p[0] == '+' || p[0] == '-')
                {
                    position.Y += p.TrimStart('+').ToInt();
                }
                else
                {
                    position.Y = p.ToInt();
                }
            }
        }


        /// <summary>
        /// Takes an int (inputVal) in the range of (inputMin .. inputMax) and projects it into
        /// a float in the range of (outputMin .. outputMax).  I guess you would call this scaling?
        /// </summary>
        /// <returns></returns>
        public static float Massage(this int inputVal, int inputMax, int inputMin = 0, float outputMax = 1.0f,
            float outputMin = 0.0f, bool invert = false)
        {
            if (inputVal < inputMin) inputVal = inputMin;
            if (inputVal > inputMax) inputVal = inputMax;
            float relative = (float)(inputVal - inputMin) / (float)(inputMax - inputMin);
            float answer = outputMin + relative * (outputMax - outputMin);
            if (invert) answer = outputMax - answer + outputMin;
            return answer;
        }

        /// <summary>
        /// Same as DrawString but includes a drop shadow
        /// </summary>
        public static void DrawShadowedString(this SpriteBatch batch, string text, Point position, Color color,
                            int shadowWidth = 1)
        {
            DrawString(batch, text, position + new Point(shadowWidth, shadowWidth), Color.Black, true);
            DrawString(batch, text, position, color);
        }

        /// <summary>
        /// Same as DrawString but includes a black background
        /// </summary>
        public static void DrawOutlinedString(this SpriteBatch batch, string text, Point position, Color color,
                    int outlineWidth = 1, Color outlineColor = default)
        {
            if (outlineColor == default) outlineColor = Color.Black;
            var size = new Size2(text.Length * 8 + outlineWidth * 2, 7 + outlineWidth * 2);
            batch.FillRectangle(new RectangleF(position - new Point2(outlineWidth, outlineWidth), size), outlineColor);
            DrawString(batch, text, position, color);
        }



        /// <summary>
        /// Same as DrawString but centers the text in the X axis on the screen
        /// </summary>
        public static void DrawStringCentered(this SpriteBatch batch, string text, int yPos, Color color,
            bool worldToScreen = false, Point offset = default)
        {

            // Calculate width of text
            var w = text.Length * 8;
            // Calculate center of screen
            var sc = Game.NativeWidth / 2;

            if (worldToScreen)
            {
                DrawString(batch, text, new Point(sc - (w / 2) + 8, yPos) + Game.CameraPos + offset, color);
            } 
            else
            {
                DrawString(batch, text, new Point(sc - (w / 2) + 8, yPos), color);
            }


        }

        /// <summary>
        /// Same as DrawStringCentered but with a drop shadow
        /// </summary>
        public static void DrawShadowedStringCentered(this SpriteBatch batch, string text, int yPos, Color color,
            int shadowWidth = 1)
        {

            var w = text.Length * 8;
            var sc = (Game.NativeWidth - 16) / 2;

            DrawShadowedString(batch, text, new Point(sc - (w / 2) + 8, yPos), color, shadowWidth);

        }

        /// <summary>
        /// Same as DrawStringCentered but with a drop shadow
        /// </summary>
        public static void DrawShadowedStringCentered(this SpriteBatch batch, string text, Point point, Color color,
          int shadowWidth = 1)
        {

            var w = text.Length * 8;

            DrawShadowedString(batch, text, new Point(point.X - (w / 2) + 8, point.Y), color, shadowWidth);

        }

        /// <summary>
        /// Same as DrawStringCentered but with a black background
        /// </summary>
        public static void DrawOutlinedStringCentered(this SpriteBatch batch, string text, int yPos, Color color,
         int outlineWidth = 1)
        {

            var w = text.Length * 8;
            var sc = Game.NativeWidth / 2;

            DrawOutlinedString(batch, text, new Point(sc - (w / 2) + 8, yPos), color, outlineWidth);

        }

        /// <summary>
        /// Shrinks a rectangle on all sides by some number of pixels
        /// </summary>
        public static Rectangle Shrink(this Rectangle r, int x, int y)
        {
            return new Rectangle(r.X + x, r.Y + y, r.Width - x - x, r.Height - y - y);
        }

        /// <summary>
        /// Loads a game file (does NOT catch exceptions)
        /// </summary>
        public static T LoadFile<T>(this string filename) where T : class, new()
        {
            var file = filename;
            if (!string.IsNullOrEmpty(Game.AppDirectory))
            {
                file = System.IO.Path.Combine(Game.AppDirectory, file);
            }
            var json = System.IO.File.ReadAllText(file);
            return json.To<T>();
        }

        public static void FillRectangleScreen(this SpriteBatch batch, 
            Point position, Size2 size, Color color, float layerDepth = 0)
        {
            batch.FillRectangle((position + Game.CameraPos - Game.CameraOffset).ToVector2(), size, color, layerDepth);
        }

        public static void DrawRectangleScreen(this SpriteBatch batch,
            Point position, Size2 size, Color color, float layerDepth = 0)
        {
            batch.DrawRectangle((position + Game.CameraPos - Game.CameraOffset).ToVector2(), size, color, layerDepth);
        }

        /// <summary>
        /// Returns a vector in the given direction and magnitude;  e.g. (Left, 2, 2) would return
        /// (-2, 0).
        /// </summary>
        public static Point RotateOffset(this Heading direction, Point mag)
        {
            return RotateOffset(direction, mag.X, mag.Y);
        }

        /// <summary>
        /// Returns a vector in the given direction and magnitude;  e.g. (Left, 2, 2) would return
        /// (-2, 0).
        /// </summary>
        public static Point RotateOffset(this Heading direction, int magX, int magY)
        {
            return direction switch
            {
                Heading.Right => new Point(magX, magY),
                Heading.Left => new Point(-magX, magY),
                Heading.Up => new Point(-magY, -magX),
                Heading.Down => new Point(magY, magX),
                _ => new Point(0,0)
            };
        }

        /// <summary>
        /// Reads raw JSON from a file relative to the game's app directory
        /// </summary>
        public static string ReadJSON(this string filename) 
        {
            var file = filename;
            if (!string.IsNullOrEmpty(Game.AppDirectory))
            {
                file = System.IO.Path.Combine(Game.AppDirectory, file);
            }
            var json = System.IO.File.ReadAllText(file);
            return json;
        }

        /// <summary>
        /// Checks if a file in the user's directory exists
        /// </summary>
      public static bool FileExists(this string filename)
        {
            var file = filename;
            if (!string.IsNullOrEmpty(Game.AppDirectory))
            {
                file = System.IO.Path.Combine(Game.AppDirectory, file);
            }

            return System.IO.File.Exists(file);
        }

        /// <summary>
        /// Renames a file in the work directory
        /// </summary>
        public static void RenameFile(this string filename, string newName)
        {
            var file = filename;
            if (!string.IsNullOrEmpty(Game.AppDirectory))
            {
                file = System.IO.Path.Combine(Game.AppDirectory, file);
                newName = System.IO.Path.Combine(Game.AppDirectory, newName);
            }

            System.IO.File.Move(file, newName);
        }

        public static string GetResource(this string resourceName)
        {

            var assembly = Assembly.GetExecutingAssembly();

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        /// <summary>
        /// Saves a game file (catches exceptions)
        /// </summary>
        public static bool SaveFile(this object o, string filename)
        {
            var file = filename;
            if (!string.IsNullOrEmpty(Game.AppDirectory))
            {
                file = System.IO.Path.Combine(Game.AppDirectory, file);
            }
            if (o is string s)
            {
                System.IO.File.WriteAllText(file, s);
                return true;
            }
            var json = o.ToJSON();
            try
            {
                System.IO.File.WriteAllText(file, json);
                return true;
            } 
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Serializes an object into JSON
        /// </summary>
        public static string ToJSON(this object x)
        {
            var settings = new JsonSerializerSettings();
            settings.MissingMemberHandling = MissingMemberHandling.Ignore;
            settings.NullValueHandling = NullValueHandling.Ignore;
            return JsonConvert.SerializeObject(x, Formatting.Indented, settings);
        }

        /// <summary>
        /// Returns the story ID (char used in file names) for a given Story
        /// </summary>
        public static char ToStoryID(this Story story)
        {
            return story switch
            {
                Story.Test => 't',
                Story.Plus => 'p',
                Story.SKX => 'x',
                _ => 'c'
            };
        }

        /// <summary>
        /// Converts a char (t/p/x/c) to a Story
        /// </summary>
        public static Story ToStory(this char c)
        {
            return c switch
            {
                 'c' => Story.Classic,
                 'p' => Story.Plus,
                _ => Story.Test
            };
        }

        /// <summary>
        /// Deserializes an object from JSON
        /// </summary>
        public static T To<T>(this string json) where T : new()
        {
            var settings = new JsonSerializerSettings();
            settings.MissingMemberHandling = MissingMemberHandling.Ignore;
            settings.NullValueHandling = NullValueHandling.Ignore;
            return JsonConvert.DeserializeObject<T>(json, settings);
        }

        public static int ToInt(this string x, bool from_hex = false)
        {
            if (from_hex)
            {
                if (int.TryParse(x, System.Globalization.NumberStyles.HexNumber, null, out int z)) return z;
                return 0;
            }
            if (int.TryParse(x, out int y)) return y;
            return 0;
        }

    }
}
