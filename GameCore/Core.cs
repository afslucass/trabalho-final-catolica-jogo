using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using GameCore.Input;
using GameCore.Audio;
using GameCore.Scenes;


namespace GameCore;

public class Core : Game
{
    internal static Core s_instance;

    /// <summary>
    /// Gets a reference to the Core instance.
    /// </summary>
    public static Core Instance => s_instance;

    // The scene that is currently active.
    private static Scene s_activeScene;

    // The next scene to switch to, if there is one.
    private static Scene s_nextScene;

    // Total game time of the most recent Update, used to measure the pause.
    private static TimeSpan s_lastTotalTime;

    // When the current pause started.
    private static TimeSpan s_pauseStartedAt;

    private static TimeSpan s_totalPausedTime;
    public static bool IsPaused { get; private set; }
    public static PauseMenu Pause { get; private set; }

    /// <summary>
    /// Gets the graphics device manager to control the presentation of graphics.
    /// </summary>
    public static GraphicsDeviceManager Graphics { get; private set; }

    /// <summary>
    /// Gets the graphics device used to create graphical resources and perform primitive rendering.
    /// </summary>
    public static new GraphicsDevice GraphicsDevice { get; private set; }

    /// <summary>
    /// Gets the sprite batch used for all 2D rendering.
    /// </summary>
    public static SpriteBatch SpriteBatch { get; private set; }

    /// <summary>
    /// Gets the content manager used to load global assets.
    /// </summary>
    public static new ContentManager Content { get; private set; }

    /// <summary>
    /// Gets a reference to the input management system.
    /// </summary>
    public static InputManager Input { get; private set; }

    /// <summary>
    /// Gets or Sets a value that indicates if the game should exit when the esc key on the keyboard is pressed.
    /// This only applies to scenes that can't be paused (Scene.CanPause is false),
    /// because in the other scenes the esc key opens the pause menu.
    /// </summary>
    public static bool ExitOnEscape { get; set; }

    /// <summary>
    /// Gets a reference to the audio control system.
    /// </summary>
    public static AudioController Audio { get; private set; }

    /// <summary>
    /// Creates a new Core instance.
    /// </summary>
    /// <param name="title">The title to display in the title bar of the game window.</param>
    /// <param name="width">The initial width, in pixels, of the game window.</param>
    /// <param name="height">The initial height, in pixels, of the game window.</param>
    /// <param name="fullScreen">Indicates if the game should start in fullscreen mode.</param>
    public Core(string title, int width, int height, bool fullScreen)
    {
        // Ensure that multiple cores are not created.
        if (s_instance != null)
        {
            throw new InvalidOperationException($"Only a single Core instance can be created");
        }

        // Store reference to engine for global member access.
        s_instance = this;

        // Create a new graphics device manager.
        Graphics = new GraphicsDeviceManager(this);

        // Set the graphics defaults.
        Graphics.PreferredBackBufferWidth = width;
        Graphics.PreferredBackBufferHeight = height;
        Graphics.IsFullScreen = fullScreen;

        // Apply the graphic presentation changes.
        Graphics.ApplyChanges();

        // Set the window title.
        Window.Title = title;

        // Set the core's content manager to a reference of the base Game's
        // content manager.
        Content = base.Content;

        // Set the root directory for content.
        Content.RootDirectory = "Content";

        // Mouse is visible by default.
        IsMouseVisible = true;

        // Exit on escape is true by default
        ExitOnEscape = true;
    }

    protected override void Initialize()
    {
        base.Initialize();

        // Set the core's graphics device to a reference of the base Game's
        // graphics device.
        GraphicsDevice = base.GraphicsDevice;

        // Create the sprite batch instance.
        SpriteBatch = new SpriteBatch(GraphicsDevice);

        // Create a new input manager.
        Input = new InputManager();

        // Create a new audio controller.
        Audio = new AudioController();

        // Create the pause menu. It uses the same font as the scenes.
        Pause = new PauseMenu(Content.Load<SpriteFont>("fonts/04B_30"));
        Pause.OnResume = () => SetPaused(false);
        Pause.OnQuit = () => Exit();
    }

    protected override void UnloadContent()
    {
        // Dispose of the audio controller.
        Audio.Dispose();

        base.UnloadContent();
    }
    public static void SetPaused(bool paused)
    {
        if (paused == IsPaused)
        {
            return;
        }

        if (paused)
        {
            s_pauseStartedAt = s_lastTotalTime;
            Pause.Open();
        }
        else
        {
            // Add the time spent paused, so the scenes never see it.
            s_totalPausedTime += s_lastTotalTime - s_pauseStartedAt;
        }

        IsPaused = paused;
    }

    // Builds the GameTime that the scenes receive: it does not include the time
    // spent paused, and while paused it is frozen (no time elapses).
    private static GameTime SceneTime(GameTime real)
    {
        if (IsPaused)
        {
            return new GameTime(s_pauseStartedAt - s_totalPausedTime, TimeSpan.Zero);
        }

        return new GameTime(
            real.TotalGameTime - s_totalPausedTime,
            real.ElapsedGameTime,
            real.IsRunningSlowly
        );
    }

    protected override void Update(GameTime gameTime)
    {
        s_lastTotalTime = gameTime.TotalGameTime;

        // Update the input manager. It keeps running while paused,
        // so the pause menu can read the keyboard.
        Input.Update(gameTime);

        // Update the audio controller.
        Audio.Update();

        // Esc or P: pause/resume if the scene allows it. In scenes that can't be
        // paused (the title screen, for example) Esc keeps exiting the game.
        bool escPressed = Input.Keyboard.WasKeyJustPressed(Keys.Escape);
        if (escPressed || Input.Keyboard.WasKeyJustPressed(Keys.P))
        {
            if (s_activeScene != null && s_activeScene.CanPause)
            {
                SetPaused(!IsPaused);
            }
            else if (escPressed && ExitOnEscape)
            {
                Exit();
            }
        }

        // While paused, only the menu is updated. The scene is frozen.
        if (IsPaused)
        {
            Pause.Update();
            base.Update(gameTime);
            return;
        }

        // if there is a next scene waiting to be switch to, then transition
        // to that scene.
        if (s_nextScene != null)
        {
            TransitionScene();
        }

        // If there is an active scene, update it.
        if (s_activeScene != null)
        {
            s_activeScene.Update(SceneTime(gameTime));
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        // If there is an active scene, draw it. While paused it is still drawn
        // (frozen) behind the pause menu.
        if (s_activeScene != null)
        {
            s_activeScene.Draw(SceneTime(gameTime));
        }

        // Draw the pause menu on top of the frozen scene.
        if (IsPaused)
        {
            SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
            Pause.Draw(SpriteBatch);
            SpriteBatch.End();
        }

        base.Draw(gameTime);
    }

    public static void ChangeScene(Scene next)
    {
        // Only set the next scene value if it is not the same
        // instance as the currently active scene.
        if (s_activeScene != next)
        {
            s_nextScene = next;
        }
    }

    private static void TransitionScene()
    {
        // If there is an active scene, dispose of it.
        if (s_activeScene != null)
        {
            s_activeScene.Dispose();
        }

        // Force the garbage collector to collect to ensure memory is cleared.
        GC.Collect();

        // Change the currently active scene to the new scene.
        s_activeScene = s_nextScene;

        // Null out the next scene value so it does not trigger a change over and over.
        s_nextScene = null;

        // If the active scene now is not null, initialize it.
        // Remember, just like with Game, the Initialize call also calls the
        // Scene.LoadContent
        if (s_activeScene != null)
        {
            s_activeScene.Initialize();
        }
    }
}