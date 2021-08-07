using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Design;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace SKX
{
    /// <summary>
    /// World is an abstract class used by the Game to determine what to draw/do.
    /// Probably should be called GameMode.
    /// Examples of World implementations would be Level, TitleScreen, Ending, etc.
    /// </summary>
    public abstract class World
    {
        public int WorldWidth { get; protected set; }               // The width of the world
        public int WorldHeight { get; protected set; }              // The height of the world
        public int TileWidth { get; protected set; }              // Width of the world in tiles
        public int TileHeight { get; protected set; }             // Height of the world in tiles

        public double Gravity = 0.2;            // Acceleration of gravity per tick
        public double TermVelocity = 1.5;       // Terminal velocity (max fall speed)

        public Color BackgroundColor = new Color(86, 29, 0);    // Background color
        public Tile BackgroundTile = Tile.BrickBackground;      // Background tile
        public bool Flash;                                      // For time over
        public List<GameObject> Objects
                = new List<GameObject>();           // currently active GameObjects

        public RenderTarget2D Surface { get; private set; }  // The render target for the world
        public Point NativeTileSize => Game.NativeTileSize; // Reference to keep things short
        public uint Ticks = 0;              // How many ticks we've been on this world
        public int Life = 10_000;           // Remaining life for levels;  countdown timer for other screens
        public Objects.Dana Dana;           // Reference to the player -- MAY BE NULL for non-level screens
        protected SpriteBatch Batch;                            // Used in rendering

        public virtual void RenderHUD(SpriteBatch batch) { }
        public virtual Menu PauseMenu => Game.PauseMenu;

        // Used to avoid changing Objects as it's being enumerated without cloning it or writing a
        // resilient linked list
        protected List<GameObject> ObjectsToRemove = new List<GameObject>();
        protected List<GameObject> ObjectsToAdd = new List<GameObject>();

        /// <summary>
        /// Renders the current set of active objects
        /// </summary>
        protected virtual void RenderObjects(SpriteBatch batch)
        {
            foreach(var o in Objects)
            {
                o.Render(batch);
            }
        }

        /// <summary>
        /// Manages the Objects collections (processes adds, removes, etc.)
        /// </summary>
        protected void ObjectMaintenance()
        {
            foreach (var o in ObjectsToAdd)
            {
                Objects.Add(o);
            }
            ObjectsToAdd.Clear();
            foreach (var o in ObjectsToRemove)
            {
                Objects.Remove(o);
            }
            ObjectsToRemove.Clear();

        }

        /// <summary>
        /// Updates all of the active game objects
        /// </summary>
        protected virtual void UpdateObjects(GameTime gameTime)
        {
            foreach(var o in Objects)
            {
                o.Update(gameTime);
            }
        }

        /// <summary>
        /// Renders a single tile to the world in a specific X/Y (grid space) cell
        /// </summary>
        public void RenderTile(SpriteBatch batch, int x, int y, Tile tile, Color color = default,
        SpriteEffects spriteEffects = SpriteEffects.None)
        {
            batch.Draw(texture: Game.Assets.Blocks, 
                       destinationRectangle: new Rectangle(x * NativeTileSize.X, y * NativeTileSize.Y, NativeTileSize.X, NativeTileSize.Y),
                       sourceRectangle: Game.Assets.BlockSourceTileRect(tile),
                       color: color == default ? Color.White : color, 
                       rotation: 0f, 
                       origin: Vector2.Zero, 
                       effects: spriteEffects, 
                       layerDepth: 0f);
        }

        public void RenderSmallTileWorld(SpriteBatch batch, uint frame, int x, int y, Tile tile, Color color = default,
            SpriteEffects spriteEffects = SpriteEffects.None, uint degRotate = 0, bool wrapped = false)
        {
            // Calculate rotation only if degrees != 0
            float rotation = degRotate == 0 ? 0 : MathHelper.ToRadians(degRotate);
            var srect = Game.Assets.BlockSourceTileRect(tile);
            srect.Width = 8;
            srect.Height = 8;
            switch(frame)
            {
                case 0:  // 0, 0 
                    break;
                case 1: // 8, 0
                    srect.X += 8;
                    break;
                case 2: // 0, 8
                    srect.Y += 8;
                    break;
                default: // 8, 8
                    srect.Y += 8;
                    srect.X += 8;
                    break;
            }

            batch.Draw(Game.Assets.Blocks, new Rectangle(x, y, 8, 8),
            srect, color, rotation, Vector2.Zero, spriteEffects, 0f);

        }

        /// <summary>
        /// Renders a single tile to the world in a specific X/Y world coordinate
        /// </summary>
        public void RenderTileWorld(SpriteBatch batch, int x, int y, Tile tile, Color color = default,
            SpriteEffects spriteEffects = SpriteEffects.None, uint degRotate = 0, bool wrapped = false)
        {
            // Calculate rotation only if degrees != 0
            float rotation = degRotate == 0 ? 0 : MathHelper.ToRadians(degRotate);


            batch.Draw(Game.Assets.Blocks, new Rectangle(x, y, NativeTileSize.X, NativeTileSize.Y),
                Game.Assets.BlockSourceTileRect(tile), color, rotation, Vector2.Zero, spriteEffects, 0f);

            if (wrapped)
            {
                var y2 = ((y + NativeTileSize.Y) % WorldHeight) - NativeTileSize.Y;
                var x2 = ((x + NativeTileSize.X) % WorldWidth) - NativeTileSize.X;
                if (y2 != y)
                {
                    batch.Draw(Game.Assets.Blocks, new Rectangle(x, y2, NativeTileSize.X, NativeTileSize.Y),
                        Game.Assets.BlockSourceTileRect(tile), color == default ? Color.White : color, rotation, Vector2.Zero, spriteEffects, 0f);
                }
                if (x2 != x)
                {
                    batch.Draw(Game.Assets.Blocks, new Rectangle(x2, y, NativeTileSize.X, NativeTileSize.Y),
                        Game.Assets.BlockSourceTileRect(tile), color == default ? Color.White : color, rotation, Vector2.Zero, spriteEffects, 0f);
                }
                if (x2 != x && y2 != y)
                {
                    batch.Draw(Game.Assets.Blocks, new Rectangle(x2, y2, NativeTileSize.X, NativeTileSize.Y),
                    Game.Assets.BlockSourceTileRect(tile), color == default ? Color.White : color, rotation, Vector2.Zero, spriteEffects, 0f);
                }
            }
        
        }

        /// <summary>
        /// Renders a single tile to the world in a specific X/Y screen coordinates regardless of camera location
        /// </summary>
        public void RenderTileScreen(SpriteBatch batch, int x, int y, Tile tile, Color color = default,
            SpriteEffects spriteEffects = SpriteEffects.None, uint degRotate = 0, bool wrapped = false)
        {

            RenderTileWorld(batch, x + Game.CameraPos.X, 
                                   y + Game.CameraPos.Y,
                                   tile, color, spriteEffects, degRotate, wrapped);

        }

        /// <summary>
        /// Adds a GameObject to the level
        /// </summary>
        public void AddObject(GameObject o)
        {
            if (o is null) return;
            if (!o.Initialized) o.Init();
            ObjectsToAdd.Add(o);
        }

        /// <summary>
        /// Renders a continuous run of a single tile (e.g. repeating background) to the world
        /// </summary>
        public void RenderTileMultiDest(SpriteBatch batch, Tile tile, int startX, int startY, int destTiles)
        {
            int x = startX;
            int y = startY;
            for (int n = 0; n < destTiles; n++)
            {
                RenderTile(batch, x, y, tile);
                x++;
                if (x * NativeTileSize.X >= WorldWidth)
                {
                    x = 0;
                    y++;
                }
            }
        }

 
        /// <summary>
        /// Called by the Game on every update even when we're paused
        /// </summary>
        /// <param name="gameTime"></param>
        public virtual void PauseUpdate(GameTime gameTime)
        {
            
        }


        /// <summary>
        /// Called by the Game on every update
        /// </summary>
        public virtual void Update(GameTime gameTime)
        {
            unchecked
            {
                Ticks++;
            }
        }

        /* Constructor */
        public World(int tWidth, int tHeight)
        {
            TileWidth = tWidth;
            TileHeight = tHeight;
            WorldWidth = TileWidth * Game.NativeTileSize.X;
            WorldHeight = TileHeight * Game.NativeTileSize.Y;
            Surface = new RenderTarget2D(Game.Instance.GraphicsDevice, WorldWidth, WorldHeight);
            Batch = new SpriteBatch(Game.Instance.GraphicsDevice);
        }

        /// <summary>
        /// Resizes the current level and associated layout
        /// </summary>
        public virtual void Resize(int width, int height)
        {
            if (width < 17) return;
            if (height < 14) return;

            // Update the level
            TileWidth = width;
            TileHeight = height;
            WorldWidth = TileWidth * Game.NativeTileSize.X;
            WorldHeight = TileHeight * Game.NativeTileSize.Y;
            Surface = new RenderTarget2D(Game.Instance.GraphicsDevice, WorldWidth, WorldHeight);

        }

        /// <summary>
        /// Called when the World is first loaded;  used to set up things that happen once
        /// </summary>
        public virtual void Init()
        {
            
        }

        /// <summary>
        /// Called when the World's background needs to be drawn.
        /// </summary>
        protected abstract void RenderBackground(SpriteBatch batch);

        /// <summary>
        /// Called when the World's foreground needs to be drawn.
        /// </summary>
        protected abstract void RenderForeground(SpriteBatch batch);

        public virtual void Unload() {  }

        /// <summary>
        /// Called by Game to render the entire world -- may be overridden if needed but be sure
        /// to SetRenderTarget to this.Surface
        /// </summary>
        public virtual void Render()
        {

            // Render to the world surface (which is the size of our level)
            Game.Instance.GraphicsDevice.SetRenderTarget(Surface);

            /* Background color */
            Game.Instance.GraphicsDevice.Clear(BackgroundColor);

            // Render background
            Batch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp);
            RenderBackground(Batch);
            Batch.End();

            // Render foreground and objects
            Batch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp);
            RenderForeground(Batch);
            
            Batch.End();

        }

    }


}
