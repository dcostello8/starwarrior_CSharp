using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Artemis;
using StarWarrior.Components;
using StarWarrior.Systems;
using StarWarrior.Primitives;
using StarWarrior.Templates;

namespace StarWarrior
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    /// 
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        private EntityWorld world;

        private EntitySystem renderSystem;
        private EntitySystem hudRenderSystem;
        private EntitySystem controlSystem;
        private EntitySystem movementSystem;
        private EntitySystem enemyShooterSystem;
        private EntitySystem enemyShipMovementSystem;
        private EntitySystem collisionSystem;
        private EntitySystem healthBarRenderSystem;
        private EntitySystem enemySpawnSystem;
        private EntitySystem expirationSystem;
        private SpriteFont font;
       
        int frameRate,frameCounter;
        TimeSpan elapsedTime = TimeSpan.Zero;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.SynchronizeWithVerticalRetrace = false;
            this.IsFixedTimeStep = false;
            graphics.IsFullScreen = false;
            graphics.PreferredBackBufferHeight = 700;
            graphics.PreferredBackBufferWidth = 1000;
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            Type[] types = new Type[] {typeof(Enemy),typeof(Expires),typeof(Health),typeof(SpatialForm),typeof(Transform),typeof(Velocity),typeof(Weapon)};

            spriteBatch = new SpriteBatch(GraphicsDevice);
            world = new EntityWorld();
            
            font = Content.Load<SpriteFont>("myFont");
            var systemManager = world.SystemManager;
            world.SetEntityTemplate("ShipExplosion", new ShipExplosionTemplate());
            world.SetEntityTemplate("BulletExplosion", new BulletExplosionTemplate());
            world.SetEntityTemplate("Missile", new MissileTemplate());
            world.SetEntityTemplate("EnemyShip", new EnemyShipTemplate());
            renderSystem = systemManager.SetSystem(new RenderSystem(GraphicsDevice,spriteBatch,Content),ExecutionType.DrawSyncronous);
            hudRenderSystem = systemManager.SetSystem(new HudRenderSystem(spriteBatch, font), ExecutionType.DrawSyncronous);
            controlSystem = systemManager.SetSystem(new MovementSystem(spriteBatch), ExecutionType.UpdateSyncronous,1);
            movementSystem = systemManager.SetSystem(new PlayerShipControlSystem(spriteBatch),ExecutionType.UpdateSyncronous);
            enemyShooterSystem = systemManager.SetSystem(new EnemyShipMovementSystem(spriteBatch), ExecutionType.UpdateSyncronous,1);
            enemyShipMovementSystem = systemManager.SetSystem(new EnemyShooterSystem(), ExecutionType.UpdateSyncronous);
            collisionSystem = systemManager.SetSystem(new CollisionSystem(), ExecutionType.UpdateSyncronous,1);
            healthBarRenderSystem = systemManager.SetSystem(new HealthBarRenderSystem(spriteBatch, font), ExecutionType.DrawSyncronous);
            enemySpawnSystem = systemManager.SetSystem(new EnemySpawnSystem(500, spriteBatch), ExecutionType.UpdateSyncronous);
            expirationSystem = systemManager.SetSystem(new ExpirationSystem(), ExecutionType.UpdateSyncronous);

            world.InitializeAll();

            InitPlayerShip();
            InitEnemyShips();

            base.Initialize();
        }

        private void InitEnemyShips() {
		    Random r = new Random();
		    for (int i = 0; 2 > i; i++) {
                Entity e = world.CreateEntityFromTemplate("EnemyShip");

			    e.GetComponent<Transform>().SetLocation(r.Next(GraphicsDevice.Viewport.Width), r.Next(400)+50);
			    e.GetComponent<Velocity>().Speed = 0.05f;
			    e.GetComponent<Velocity>().Angle = r.Next() % 2 == 0 ? 0 : 180;
			
			    e.Refresh();
		    }
	    }

	    private void InitPlayerShip() {
		    Entity e = world.CreateEntity();
		    e.Group = "SHIPS";

            e.AddComponent(new Transform());
		    e.AddComponent(new SpatialForm());
		    e.AddComponent(new Health());
            e.GetComponent<SpatialForm>().SpatialFormFile = "PlayerShip";
            e.GetComponent<Health>().HP = 30;
            e.GetComponent<Transform>().Coords = new Vector3(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height - 50, 0);
		    e.Refresh();
            e.Tag = "PLAYER";
	    }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
        }


        DateTime dt = DateTime.Now;
        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            TimeSpan elapsed = DateTime.Now - dt;
            dt = DateTime.Now;
            frameCounter++;

            
            world.Delta = elapsed.Milliseconds;
            world.Update(ExecutionType.UpdateSyncronous);                        
            elapsedTime += elapsed;

            if (elapsedTime > TimeSpan.FromSeconds(1))
            {
                elapsedTime -= TimeSpan.FromSeconds(1);
                frameRate = frameCounter;
                frameCounter = 0;
            }

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {            
            string fps = string.Format("fps: {0}", frameRate);
            string entityCount = string.Format("active entities: {0}", world.EntityManager.ActiveEntitiesCount);
			string removedEntityCount = string.Format("removed entities: {0}", world.EntityManager.TotalRemoved);
            string totalEntityCount = string.Format("total entities: {0}", world.EntityManager.TotalCreated);

            GraphicsDevice.Clear(Color.Black);
            spriteBatch.Begin();
            world.Update(ExecutionType.DrawSyncronous);
            spriteBatch.DrawString(font, fps, new Vector2(32,32), Color.Yellow);
            spriteBatch.DrawString(font, entityCount, new Vector2(32, 62), Color.Yellow);
            spriteBatch.DrawString(font, removedEntityCount, new Vector2(32, 92), Color.Yellow);
            spriteBatch.DrawString(font, totalEntityCount, new Vector2(32, 122), Color.Yellow);            
            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
