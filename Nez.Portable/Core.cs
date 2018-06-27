namespace Nez
{
    using System;
    using System.Collections;

    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Microsoft.Xna.Framework.Input;

    using Nez.Analysis;
    using Nez.BitmapFonts;
    using Nez.Console;
    using Nez.Systems;
    using Nez.Textures;
    using Nez.Timers;
    using Nez.Tweens;

    public class Core : Game
    {
        /// <summary>
        ///     core emitter. emits only Core level events.
        /// </summary>
        public static Emitter<CoreEvents> emitter;

        /// <summary>
        ///     enables/disables if we should quit the app when escape is pressed
        /// </summary>
        public static bool ExitOnEscapeKeypress = true;

        /// <summary>
        ///     enables/disables pausing when focus is lost. No update or render methods will be called if true when not in focus.
        /// </summary>
        public static bool PauseOnFocusLost = true;

        /// <summary>
        ///     enables/disables debug rendering
        /// </summary>
        public static bool DebugRenderEnabled = false;

        /// <summary>
        ///     global access to the graphicsDevice
        /// </summary>
        public static GraphicsDevice graphicsDevice;

        /// <summary>
        ///     global content manager for loading any assets that should stick around between scenes
        /// </summary>
        public static NezContentManager content;

        /// <summary>
        ///     default SamplerState used by Materials. Note that this must be set at launch! Changing it after that time will
        ///     result in only
        ///     Materials created after it was set having the new SamplerState
        /// </summary>
        public static SamplerState defaultSamplerState = SamplerState.PointClamp;

        /// <summary>
        ///     default wrapped SamplerState. Determined by the Filter of the defaultSamplerState.
        /// </summary>
        /// <value>The default state of the wraped sampler.</value>
        public static SamplerState defaultWrappedSamplerState =>
            defaultSamplerState.Filter == TextureFilter.Point ? SamplerState.PointWrap : SamplerState.LinearWrap;

        /// <summary>
        ///     default GameServiceContainer access
        /// </summary>
        /// <value>The services.</value>
        public static GameServiceContainer services => _instance.Services;

        /// <summary>
        ///     internal flag used to determine if EntitySystems should be used or not
        /// </summary>
        internal static bool entitySystemsEnabled;

        /// <summary>
        ///     facilitates easy access to the global Content instance for internal classes
        /// </summary>
        internal static Core _instance;

#if DEBUG
        internal static long drawCalls;

        private TimeSpan _frameCounterElapsedTime = TimeSpan.Zero;

        private int _frameCounter;

        private readonly string _windowTitle;
#endif

        private Scene _scene;

        private Scene _nextScene;

        internal SceneTransition _sceneTransition;

        /// <summary>
        ///     used to coalesce GraphicsDeviceReset events
        /// </summary>
        private ITimer _graphicsDeviceChangeTimer;

        // globally accessible systems
        private readonly FastList<IUpdatableManager> _globalManagers = new FastList<IUpdatableManager>();

        private readonly CoroutineManager _coroutineManager = new CoroutineManager();

        private readonly TimerManager _timerManager = new TimerManager();

        /// <summary>
        ///     The currently active Scene. Note that if set, the Scene will not actually change until the end of the Update
        /// </summary>
        public static Scene Scene
        {
            get => _instance._scene;

            set => _instance._nextScene = value;
        }

        public Core(
            int width = 1280,
            int height = 720,
            bool isFullScreen = false,
            bool enableEntitySystems = true,
            string windowTitle = "Nez",
            string contentDirectory = "Content")
        {
#if DEBUG
            this._windowTitle = windowTitle;
#endif

            _instance = this;
            emitter = new Emitter<CoreEvents>(new CoreEventsComparer());

            var graphicsManager = new GraphicsDeviceManager(this);
            graphicsManager.PreferredBackBufferWidth = width;
            graphicsManager.PreferredBackBufferHeight = height;
            graphicsManager.IsFullScreen = isFullScreen;
            graphicsManager.SynchronizeWithVerticalRetrace = true;
            graphicsManager.DeviceReset += this.onGraphicsDeviceReset;
            graphicsManager.PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8;

            Screen.initialize(graphicsManager);
            this.Window.ClientSizeChanged += this.onGraphicsDeviceReset;
            this.Window.OrientationChanged += this.onOrientationChanged;

            this.Content.RootDirectory = contentDirectory;
            content = new NezGlobalContentManager(this.Services, this.Content.RootDirectory);
            this.IsMouseVisible = true;
            this.IsFixedTimeStep = false;

            entitySystemsEnabled = enableEntitySystems;

            // setup systems
            this._globalManagers.add(this._coroutineManager);
            this._globalManagers.add(new TweenManager());
            this._globalManagers.add(this._timerManager);
            this._globalManagers.add(new RenderTarget());
        }

        private void onOrientationChanged(object sender, EventArgs e)
        {
            emitter.emit(CoreEvents.OrientationChanged);
        }

        /// <summary>
        ///     this gets called whenever the screen size changes
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">E.</param>
        protected void onGraphicsDeviceReset(object sender, EventArgs e)
        {
            // we coalese these to avoid spamming events
            if (this._graphicsDeviceChangeTimer != null)
            {
                this._graphicsDeviceChangeTimer.reset();
            }
            else
            {
                this._graphicsDeviceChangeTimer = schedule(
                    0.05f,
                    false,
                    this,
                    t =>
                        {
                            (t.context as Core)._graphicsDeviceChangeTimer = null;
                            emitter.emit(CoreEvents.GraphicsDeviceReset);
                        });
            }
        }

        #region Passthroughs to Game

        public static void exit()
        {
            _instance.Exit();
        }

        #endregion

        #region Game overides

        protected override void Initialize()
        {
            base.Initialize();

            // prep the default Graphics system
            graphicsDevice = this.GraphicsDevice;
            var font = content.Load<BitmapFont>("nez://Nez.Content.NezDefaultBMFont.xnb");
            Graphics.instance = new Graphics(font);
        }

        protected override void Update(GameTime gameTime)
        {
            if (PauseOnFocusLost && !this.IsActive)
            {
                this.SuppressDraw();
                return;
            }

#if DEBUG
            TimeRuler.instance.startFrame();
            TimeRuler.instance.beginMark("update", Color.Green);
#endif

            // update all our systems and global managers
            Time.update((float)gameTime.ElapsedGameTime.TotalSeconds);
            Input.update();

            for (var i = this._globalManagers.length - 1; i >= 0; i--)
            {
                this._globalManagers.buffer[i].update();
            }

            if (ExitOnEscapeKeypress
                && (Input.isKeyDown(Keys.Escape) || Input.gamePads[0].isButtonReleased(Buttons.Back)))
            {
                this.Exit();
                return;
            }

            if (this._scene != null)
            {
                this._scene.Update();
            }

            if (this._scene != this._nextScene)
            {
                if (this._scene != null)
                {
                    this._scene.end();
                }

                this._scene = this._nextScene;
                this.onSceneChanged();

                if (this._scene != null)
                {
                    this._scene.begin();
                }
            }

#if DEBUG
            TimeRuler.instance.endMark("update");
            DebugConsole.instance.update();
            drawCalls = 0;
#endif

#if FNA

// MonoGame only updates old-school XNA Components in Update which we dont care about. FNA's core FrameworkDispatcher needs

// Update called though so we do so here.
			FrameworkDispatcher.Update();
			#endif
        }

        protected override void Draw(GameTime gameTime)
        {
            if (PauseOnFocusLost && !this.IsActive)
            {
                return;
            }

#if DEBUG
            TimeRuler.instance.beginMark("draw", Color.Gold);

            // fps counter
            this._frameCounter++;
            this._frameCounterElapsedTime += gameTime.ElapsedGameTime;
            if (this._frameCounterElapsedTime >= TimeSpan.FromSeconds(1))
            {
                var totalMemory = (GC.GetTotalMemory(false) / 1048576f).ToString("F");
                this.Window.Title = string.Format(
                    "{0} {1} fps - {2} MB",
                    this._windowTitle,
                    this._frameCounter,
                    totalMemory);
                this._frameCounter = 0;
                this._frameCounterElapsedTime -= TimeSpan.FromSeconds(1);
            }

#endif

            if (this._sceneTransition != null)
            {
                this._sceneTransition.preRender(Graphics.instance);
            }

            if (this._scene != null)
            {
                this._scene.render();

#if DEBUG
                if (DebugRenderEnabled)
                {
                    Debug.render();
                }
#endif

                // render as usual if we dont have an active SceneTransition
                if (this._sceneTransition == null)
                {
                    this._scene.postRender();
                }
            }

            // special handling of SceneTransition if we have one
            if (this._sceneTransition != null)
            {
                if (this._scene != null && this._sceneTransition.wantsPreviousSceneRender
                                        && !this._sceneTransition.hasPreviousSceneRender)
                {
                    this._scene.postRender(this._sceneTransition.previousSceneRender);
                    if (this._sceneTransition._loadsNewScene)
                    {
                        Scene = null;
                    }

                    startCoroutine(this._sceneTransition.onBeginTransition());
                }
                else if (this._scene != null)
                {
                    this._scene.postRender();
                }

                this._sceneTransition.render(Graphics.instance);
            }

#if DEBUG
            TimeRuler.instance.endMark("draw");
            DebugConsole.instance.render();

            // the TimeRuler only needs to render when the DebugConsole is not open
            if (!DebugConsole.instance.isOpen)
            {
                TimeRuler.instance.render();
            }

#if !FNA
            drawCalls = graphicsDevice.Metrics.DrawCount;
#endif
#endif
        }

        #endregion

        /// <summary>
        ///     Called after a Scene ends, before the next Scene begins
        /// </summary>
        private void onSceneChanged()
        {
            emitter.emit(CoreEvents.SceneChanged);
            Time.sceneChanged();
            GC.Collect();
        }

        /// <summary>
        ///     temporarily runs SceneTransition allowing one Scene to transition to another smoothly with custom effects.
        /// </summary>
        /// <param name="sceneTransition">Scene transition.</param>
        public static T startSceneTransition<T>(T sceneTransition)
            where T : SceneTransition
        {
            Assert.isNull(
                _instance._sceneTransition,
                "You cannot start a new SceneTransition until the previous one has completed");
            _instance._sceneTransition = sceneTransition;
            return sceneTransition;
        }

        #region Global Managers

        /// <summary>
        ///     adds a global manager object that will have its update method called each frame before Scene.update is called
        /// </summary>
        /// <returns>The global manager.</returns>
        /// <param name="manager">Manager.</param>
        public static void registerGlobalManager(IUpdatableManager manager)
        {
            _instance._globalManagers.add(manager);
        }

        /// <summary>
        ///     removes the global manager object
        /// </summary>
        /// <returns>The global manager.</returns>
        /// <param name="manager">Manager.</param>
        public static void unregisterGlobalManager(IUpdatableManager manager)
        {
            _instance._globalManagers.remove(manager);
        }

        /// <summary>
        ///     gets the global manager of type T
        /// </summary>
        /// <returns>The global manager.</returns>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public static T getGlobalManager<T>()
            where T : class, IUpdatableManager
        {
            for (var i = 0; i < _instance._globalManagers.length; i++)
            {
                if (_instance._globalManagers.buffer[i] is T)
                {
                    return _instance._globalManagers.buffer[i] as T;
                }
            }

            return null;
        }

        #endregion

        #region Systems access

        /// <summary>
        ///     starts a coroutine. Coroutines can yeild ints/floats to delay for seconds or yeild to other calls to
        ///     startCoroutine.
        ///     Yielding null will make the coroutine get ticked the next frame.
        /// </summary>
        /// <returns>The coroutine.</returns>
        /// <param name="enumerator">Enumerator.</param>
        public static ICoroutine startCoroutine(IEnumerator enumerator)
        {
            return _instance._coroutineManager.startCoroutine(enumerator);
        }

        /// <summary>
        ///     schedules a one-time or repeating timer that will call the passed in Action
        /// </summary>
        /// <param name="timeInSeconds">Time in seconds.</param>
        /// <param name="repeats">If set to <c>true</c> repeats.</param>
        /// <param name="context">Context.</param>
        /// <param name="onTime">On time.</param>
        public static ITimer schedule(float timeInSeconds, bool repeats, object context, Action<ITimer> onTime)
        {
            return _instance._timerManager.schedule(timeInSeconds, repeats, context, onTime);
        }

        /// <summary>
        ///     schedules a one-time timer that will call the passed in Action after timeInSeconds
        /// </summary>
        /// <param name="timeInSeconds">Time in seconds.</param>
        /// <param name="context">Context.</param>
        /// <param name="onTime">On time.</param>
        public static ITimer schedule(float timeInSeconds, object context, Action<ITimer> onTime)
        {
            return _instance._timerManager.schedule(timeInSeconds, false, context, onTime);
        }

        /// <summary>
        ///     schedules a one-time or repeating timer that will call the passed in Action
        /// </summary>
        /// <param name="timeInSeconds">Time in seconds.</param>
        /// <param name="repeats">If set to <c>true</c> repeats.</param>
        /// <param name="onTime">On time.</param>
        public static ITimer schedule(float timeInSeconds, bool repeats, Action<ITimer> onTime)
        {
            return _instance._timerManager.schedule(timeInSeconds, repeats, null, onTime);
        }

        /// <summary>
        ///     schedules a one-time timer that will call the passed in Action after timeInSeconds
        /// </summary>
        /// <param name="timeInSeconds">Time in seconds.</param>
        /// <param name="onTime">On time.</param>
        public static ITimer schedule(float timeInSeconds, Action<ITimer> onTime)
        {
            return _instance._timerManager.schedule(timeInSeconds, false, null, onTime);
        }

        #endregion
    }
}