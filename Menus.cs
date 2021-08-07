using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SKX
{
    /// <summary>
    /// Represents a menu (static, dynamic, mixed) used in the game
    /// </summary>
    public class Menu
    {
        public List<MenuItem> MenuItems { get; } = new List<MenuItem>();    // Items
        public string Caption { get; set; }             // Menu title
        public int Padding { get; set; } = 8;           // Padding around the sides of the menu
        public int Selection { get; set; } = 0;         // Currently selected index
        public Func<Keys, bool> OnKey { get; set; }     // Key press handler
        public Func<string> Updater { get; set; }       // Dynamic text callback
        public Func<Buttons, bool> OnButton { get; set; }   // Button press handler
        public int? Width { get; set; }                 // Static width override
        public Action OnExit { get; set; }              // Callback when menu unloads (any reason)
        public Action OnBack { get; set; }              // Callback when user goes back

        private Point2 Position;        // Where it is drawn on the screen    
        private Size2 Size;             // How big it was determined to be
        private int HCenter;            // Horizontal center
        private Menu PreviousMenu;      // The previous menu we were in (if any)

        public Menu() { }
        public Menu(string caption, params MenuItem[] items)
        {
            Caption = caption;
            MenuItems.AddRange(items);
            UpdateBounds();
        }

        /// <summary>
        /// Updates the dimensions of the menu based on what items it now contains
        /// </summary>
        public void UpdateBounds()
        {
            var longest = 0;
            if (MenuItems.Count > 0)
            {
                longest = MenuItems.Max(x => Game.StringWidth(x.Text));    // Get the longest text line
            }
            longest = Math.Max(longest, Game.StringWidth(Caption));
            longest += 2;                                       // Add 2 to account for the cursor

            var height = MenuItems.Count() + 2;                 // height is number of items + 2

            int width = Width ?? longest * 8 + Padding * 2;
            Size = new Size2(width, height * 8 + Padding * 2);

            HCenter = Game.NativeWidth / 2;
            Position = new Size2(HCenter - Size.Width / 2,
                                 Game.NativeHeight / 2 - Size.Height / 2);  // Center screen
        }

        /// <summary>
        /// Renders the menu
        /// </summary>
        public void Render(SpriteBatch batch)
        {
            // Menu background
            batch.FillRectangle(new RectangleF(Position, Size), new Color(0, 0, 0, 200));

            // Calculate start of text within menu
            int y = (int)Position.Y + Padding;

            // Update caption 
            if (Updater != null) Caption = Updater.Invoke();

            // Caption
            var offset = Caption.Length * 8 / 2;
            batch.DrawShadowedString(Caption, new Point(HCenter - offset, 
                                                        y), 
                                    Color.White);

            // Skip a line
            y += 16;

            // Set up the left margin
            int x = (int)Position.X + Padding + 16;

            // Menu items
            int index = 0;
            int cursor_y = 0;
            foreach(var m in MenuItems)
            {
                // Update item text
                if (m.Updater != null)
                {
                    m.Text = m.Updater.Invoke();
                }

                if (m.Text != null)
                {
                    // Does the cursor go next to this item?
                    if (index == Selection) cursor_y = y;
                    offset = ((m.Text?.Length ?? 100) + 2) * 8 / 2;
                    // Draw item
                    batch.DrawShadowedString(m.Text, new Point(x, y), index == Selection ? Color.Lime : Color.White);
                }
                y += 8;
                index++;
            }

            // Draw cursor
            batch.DrawShadowedString(">", new Point((int)Position.X + Padding, cursor_y), Color.White);

        }

        /// <summary>
        /// Moves the selection cursor up (-1) or down (1)
        /// </summary>
        public void MoveSelection(int by)
        {
            var old = Selection;

            Selection += by;
            if (Selection < 0) Selection = MenuItems.Count - 1;
            if (Selection > MenuItems.Count - 1) Selection = 0;

            // Skip over separators (blank lines)
            while (string.IsNullOrEmpty(MenuItems[Selection].Text)) Selection += by;
            if (Selection < 0) Selection = MenuItems.Count - 1;
            if (Selection > MenuItems.Count - 1) Selection = 0;


        }

        /// <summary>
        /// Switch to a different menu
        /// </summary>
        public void GoTo(Menu nextMenu)
        {
            if (Game.Menu == this) OnExit?.Invoke();
            Game.Menu = nextMenu;
            if (nextMenu != null) nextMenu.PreviousMenu = this;
        }

        /// <summary>
        /// Go back to the previous menu,  or back to the game if there was no previous menu
        /// </summary>
        public void GoBack()
        {
            OnExit?.Invoke();
            Game.Menu = PreviousMenu;
            OnBack?.Invoke();
        }

        /// <summary>
        /// Menu control handling
        /// </summary>
        public void Update(GameTime time)
        {
            if (MenuItems.Count == 0) return;

            var item = MenuItems[Selection];

            if (OnKey != null)
            {
                // Scan for all keys
                Keys? k = Control.GetAnyKey();
                if (k.HasValue)
                {
                    if (OnKey.Invoke(k.Value)) return;
                }
            }
            if (OnButton != null)
            {
                // Scan for any button
                Buttons? b = Control.GetAnyButton();
                if (b.HasValue)
                {
                    if (OnButton.Invoke(b.Value)) return;
                }
            }
            if (Control.Esc.Pressed(false))
            {
                Sound.Tick.Play();
                GoBack();
                return;
            }
            if (Control.Up.Pressed(false))
            {
                Sound.Tick.Play();
                MoveSelection(-1);
            } else if (Control.Down.Pressed(false))
            {
                Sound.Tick.Play();
                MoveSelection(1);
            } else if (Control.Left.Pressed(false) && item.Left != null) 
            {
                Sound.Tick.Play();
                item.Left.Invoke();
            } else if (Control.Right.Pressed(false) && item.Right != null)
            {
                Sound.Tick.Play();
                item.Right.Invoke();
            }
            else if (Control.Enter.Pressed(false) || Control.Space.Pressed(false))
            {
                // do the action

                if (item.NextMenu != null)
                {
                    Sound.Tick.Play();
                    OnExit?.Invoke();
                    GoTo(item.NextMenu);
                }
                else if (item.PrevMenu)
                {
                    Sound.Tick.Play();
                    if (PreviousMenu != null)
                    {
                        OnExit?.Invoke();
                        Game.Menu = PreviousMenu;
                    } else
                    {
                        Game.Menu = null;
                        OnExit?.Invoke();
                    }
                }
                else if (item.Action != null)
                {
                    Sound.Tick.Play();
                    if (item.Updater is null)
                    {
                        Game.Menu = null;
                        OnExit?.Invoke();
                    }
                    item.Action.Invoke();
                }

            }

        }

    }

    /// <summary>
    /// Represents an item in a menu
    /// </summary>
    public class MenuItem
    {
        public string Text { get; set; }            // The text shown to the player.  Empty string is 
                                                    // a separator and can't be selected.  Will be overridden
                                                    // by any value returned from the Updater callback
        public Action Action { get; set; }          // Action to invoke when user presses enter/start/A
        public Menu NextMenu { get; set; }          // Menu to go to when user presses enter/start/A
        public bool PrevMenu { get; set; }          // Reference to the previous menu (set up in GoTo)
        public Func<string> Updater { get; set; }   // Optional delegate to update the text of this menu
        public Action Left { get; set; }            // Action to invoke when user presses left on this item
        public Action Right { get; set; }           // Action to invoke when user presses right on this item

        public MenuItem(string text, Action action, Func<string> updater)
        {
            Text = text;
            Action = action;
            Updater = updater;
        }
        public MenuItem(string text)
        {
            Text = text;
        }
        public MenuItem(string text, Action action)
        {
            Text = text;
            Action = action;
        }
        public MenuItem(string text, Action action, Action left, Action right, Func<string> updater)
        {
            Text = text;
            Action = action;
            Left = left;
            Right = right;
            Updater = updater;
        }
        public MenuItem(string text, Menu nextMenu)
        {
            Text = text;
            NextMenu = nextMenu;
        }
        public MenuItem(string text, bool prevMenu)
        {
            Text = text;
            PrevMenu = prevMenu;
        }

        public MenuItem() { }
    }
}
