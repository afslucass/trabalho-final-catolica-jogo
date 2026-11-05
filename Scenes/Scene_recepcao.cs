using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using GameCore;
using GameCore.CustomMouseCursor;
using GameCore.DialogCutscene;
using GameCore.Graphics;
using GameCore.Scenes;

namespace TestGame.Scenes;

/// <summary>
/// Defines the player's spawn point based on the previous scene.
/// </summary>
public enum RecepcaoSpawn
{
    Start,           // First visit, coming from the introduction
    FromCaldeira,    // Returning from the Boiler (spawns near the right arch)
    FromAssombrado   // Returning from the Haunted Train (spawns near the left arch)
}

public class Scene_recepcao : Scene
{
    // ---------- World ----------
    // Uses the background's native resolution (3840 x 1080). The camera applies a
    // scale so the background height matches the window height, allowing the game
    // to support any resolution. Background areas outside the window width
    // remain off-screen and are revealed as the camera scrolls.
    private Sprite _background;
    private float _cameraX;

    // Character foot position, measured in background pixels.
    // The floor (bottom black strip) spans y=955 to y=1080.
    private const float GROUND_Y = 1010f;

    // ---------- Player ----------
    // Two mirrored sprites, one facing each direction.
    // _player always references the active sprite.
    private AnimatedSprite _player;
    private AnimatedSprite _playerLeft;
    private AnimatedSprite _playerRight;

    // Horizontal position of the player's foot center, in background pixels.
    private float _playerX;

    // Movement speed in background pixels per second (Space applies the sprint multiplier).
    private const float PLAYER_SPEED = 450f;
    private const float SPRINT_MULTIPLIER = 1.6f;

    // Minimum distance from the world boundaries.
    private const float WORLD_MARGIN = 150f;

    // In this game, player-2 faces left and player-1 faces
    // right (verified). Update these names if the sprite sheet changes.
    private const string PLAYER_LEFT_ANIMATION = "player-2-animation";
    private const string PLAYER_RIGHT_ANIMATION = "player-1-animation";

    // ---------- Watcher ----------
    // owl-1 faces left and owl-2 faces right.
    // The Watcher always faces the player.
    private AnimatedSprite _vigia;
    private AnimatedSprite _vigiaLeft;
    private AnimatedSprite _vigiaRight;
    private float _vigiaX;

    private const string VIGIA_LEFT_ANIMATION = "owl-1-animation";
    private const string VIGIA_RIGHT_ANIMATION = "owl-2-animation";

    // Maximum distance (in background pixels) for interacting with the Watcher.
    private const float TALK_RANGE = 330f;

    // ---------- Exits ----------
    // The two background arches: the left one leads to the Haunted Train,
    // and the right one leads to the Boiler. Positions are ratios of background width.
    private const float LEFT_EXIT_RATIO = 0.215f;
    private const float RIGHT_EXIT_RATIO = 0.785f;
    private const float EXIT_RANGE = 220f;

    private float _leftExitX;
    private float _rightExitX;
    private bool _isAtLeftExit;
    private bool _isAtRightExit;
    private bool _leftExitOpen;
    private bool _rightExitOpen;

    // ---------- UI Text ----------
    private const string PRESS_E_TALK = "Press E para conversar";
    private const string PRESS_E_LEFT = "Press E para ir ao vagao assombrado";
    private const string PRESS_E_RIGHT = "Press E para entrar na Caldeira";
    private const string LOCKED_LEFT = "A porta esta trancada";
    private const string LOCKED_RIGHT = "Converse com o Vigia antes de seguir";

    // ---------- Dialogs ----------
    // Stores the dialog controller, total duration, and completion callback.
    private class Talk
    {
        public DialogCutsceneController Dialog;
        public double TotalMs;
        public Action OnFinished;
    }

    private Talk _firstTalk;      // Intro stage: Watcher's welcome
    private Talk _reminderTalk;   // TalkedToWatcher stage: reminder to go right
    private Talk _secondTalk;     // Judged stage: agitated Watcher guides Alma to the Haunted Train
    private Talk _idleTalk;       // Other stages

    private Talk _activeTalk;     // Currently playing dialog (null = none)
    private double _talkRemainingMs;
    private bool _canTalk;

    // Maximum dialog line width, as a fraction of screen width.
    private const float DIALOG_MAX_WIDTH_RATIO = 0.8f;

    // UI and audio resources.
    private SpriteFont _font;
    private SoundEffect _collectSoundEffect;

    private readonly RecepcaoSpawn _spawn;

    public Scene_recepcao(RecepcaoSpawn spawn = RecepcaoSpawn.Start)
    {
        _spawn = spawn;
    }

    public override void Initialize()
    {
        // The base Initialize method also calls LoadContent.
        base.Initialize();

        // Sets the player's spawn position based on the previous scene.
        float worldWidth = _background.Width;
        switch (_spawn)
        {
            case RecepcaoSpawn.FromCaldeira:
                _playerX = _rightExitX - 300f;
                _player = _playerLeft;
                break;

            case RecepcaoSpawn.FromAssombrado:
                _playerX = _leftExitX + 300f;
                _player = _playerRight;
                break;

            default:
                _playerX = worldWidth * 0.30f;
                _player = _playerRight;
                break;
        }

        // Places the camera immediately, without an initial slide.
        UpdateCamera(0f, snap: true);
    }

    public override void LoadContent()
    {
        base.LoadContent();

        // Atlas containing characters, items, and backgrounds from spritesheet_final.
        TextureAtlas charactersAtlas = TextureAtlas.FromFile(Content, "images/characters-atlas-definition.xml");
        MouseCursorAtlas cursorAtlas = MouseCursorAtlas.FromFile(Content, "cursor/cursor-atlas-definition.xml");

        // Reception background (3840 x 1080), drawn at its native size.
        _background = charactersAtlas.CreateSprite("bg-recepcao");
        _background.Scale = Vector2.One;

        // Player sprites: one for each direction (matching sizes prevent position jumps).
        _playerLeft = charactersAtlas.CreateAnimatedSprite(PLAYER_LEFT_ANIMATION);
        _playerLeft.Scale = Vector2.One;
        _playerRight = charactersAtlas.CreateAnimatedSprite(PLAYER_RIGHT_ANIMATION);
        _playerRight.Scale = Vector2.One;
        _player = _playerRight;

        // Watcher: positioned at the center of the train, in front of the tree corridor.
        _vigiaLeft = charactersAtlas.CreateAnimatedSprite(VIGIA_LEFT_ANIMATION);
        _vigiaLeft.Scale = Vector2.One;
        _vigiaRight = charactersAtlas.CreateAnimatedSprite(VIGIA_RIGHT_ANIMATION);
        _vigiaRight.Scale = Vector2.One;
        _vigia = _vigiaLeft;

        _vigiaX = _background.Width * 0.5f;
        _leftExitX = _background.Width * LEFT_EXIT_RATIO;
        _rightExitX = _background.Width * RIGHT_EXIT_RATIO;

        _collectSoundEffect = Content.Load<SoundEffect>("audio/collect");
        _font = Content.Load<SpriteFont>("fonts/04B_30");

        // Fallback: unsupported font characters are replaced with '?'
        // (or a space) to prevent ArgumentException in MeasureString/DrawString.
        // Add accented characters to the .spritefont CharacterRegion to render them correctly.
        _font.DefaultCharacter = _font.Characters.Contains('?') ? '?' : ' ';

        cursorAtlas.SetCursor("cheese");

        BuildDialogs();
    }

    // Builds the Watcher's dialogs. Characters remain still during conversations,
    // so each step contains only text and duration, with no sprite animations.
    private void BuildDialogs()
    {
        // ----- Intro stage: welcome -----
        List<DialogCutsceneSteps> firstSteps = new();

        firstSteps.Add(new DialogCutsceneSteps
        {
            caption = Wrap("AUTO LÁ! O que é isso que vejo? Um novo aperitivo? A grande Morte nunca falha... Bem-vindo! Bem-vindo! Bem-vindo ao meio termo! O Limbo abriga aqueles que estão de passagem."),
            hasCaptionFadeIn = true,
            durationInMillis = 12000,
            spriteAnimations = new List<DialogCutsceneSpriteAnimation>()
        });
        firstSteps.Add(new DialogCutsceneSteps
        {
            caption = Wrap("Sou o grandioso VIGIA, tudo vejo, tudo sei. Eu te conheço de muito antes Alma, desdo seu nascimento, até o seu último suspiro..."),
            hasCaptionFadeIn = true,
            durationInMillis = 10000,
            spriteAnimations = new List<DialogCutsceneSpriteAnimation>()
        });
        firstSteps.Add(new DialogCutsceneSteps
        {
            caption = Wrap("De certa forma, somos íntimos, mas não se deixe levar, não somos camaradas."),
            hasCaptionFadeIn = true,
            durationInMillis = 6500,
            spriteAnimations = new List<DialogCutsceneSpriteAnimation>()
        });
        firstSteps.Add(new DialogCutsceneSteps
        {
            caption = Wrap("Seu corpo fraco te abandonou, você está aqui para seguir para a próxima fase do seu ciclo, assim como todos os outros antes, e como todos os próximos..."),
            hasCaptionFadeIn = true,
            durationInMillis = 11000,
            spriteAnimations = new List<DialogCutsceneSpriteAnimation>()
        });
        firstSteps.Add(new DialogCutsceneSteps
        {
            caption = Wrap("Alma, seu destino te aguarda a direita desse vagão, não se surpreenda pelo o que está por vir, não irá durar por muito... Só precisamos ter certeza..."),
            hasCaptionFadeIn = true,
            durationInMillis = 11000,
            spriteAnimations = new List<DialogCutsceneSpriteAnimation>()
        });
        firstSteps.Add(new DialogCutsceneSteps
        {
            caption = Wrap("VÁ CRIATURA, me deixe às minhas novelas, os mortos não me entretém tanto quanto os vivos."),
            hasCaptionFadeIn = true,
            durationInMillis = 7500,
            spriteAnimations = new List<DialogCutsceneSpriteAnimation>()
        });

        // Unlocks the right exit (Boiler) when the first dialog ends.
        _firstTalk = CreateTalk(firstSteps, () => GameSession.Stage = StoryStage.FalouComVigia);

        // ----- TalkedToWatcher stage: reminder (replace placeholder text) -----
        List<DialogCutsceneSteps> reminderSteps = new();

        reminderSteps.Add(new DialogCutsceneSteps
        {
            caption = Wrap("Siga pela direita, Alma. A Caldeira te aguarda."),
            hasCaptionFadeIn = true,
            durationInMillis = 4000,
            spriteAnimations = new List<DialogCutsceneSpriteAnimation>()
        });

        _reminderTalk = CreateTalk(reminderSteps, null);

        // ----- Judged stage: the Watcher becomes agitated (replace placeholder text) -----
        List<DialogCutsceneSteps> secondSteps = new();

        secondSteps.Add(new DialogCutsceneSteps
        {
            caption = Wrap("Texto do Vigia depois da Justiça 1"),
            hasCaptionFadeIn = true,
            durationInMillis = 4000,
            spriteAnimations = new List<DialogCutsceneSpriteAnimation>()
        });
        secondSteps.Add(new DialogCutsceneSteps
        {
            caption = Wrap("Texto do Vigia depois da Justiça 2"),
            hasCaptionFadeIn = true,
            durationInMillis = 4000,
            spriteAnimations = new List<DialogCutsceneSpriteAnimation>()
        });
        secondSteps.Add(new DialogCutsceneSteps
        {
            caption = Wrap("Texto do Vigia depois da Justiça 3"),
            hasCaptionFadeIn = true,
            durationInMillis = 4000,
            spriteAnimations = new List<DialogCutsceneSpriteAnimation>()
        });

        // When finished, the Watcher guides Alma to the Haunted Train and unlocks the left exit.
        _secondTalk = CreateTalk(secondSteps, () => GameSession.Stage = StoryStage.VagaoAssombrado);

        // ----- Other stages (replace placeholder text) -----
        List<DialogCutsceneSteps> idleSteps = new();

        idleSteps.Add(new DialogCutsceneSteps
        {
            caption = Wrap("Não tenho mais nada a dizer, Alma. Siga o seu caminho."),
            hasCaptionFadeIn = true,
            durationInMillis = 4000,
            spriteAnimations = new List<DialogCutsceneSpriteAnimation>()
        });

        _idleTalk = CreateTalk(idleSteps, null);
    }

    // Creates a dialog controller and calculates the total duration of its steps.
    private Talk CreateTalk(List<DialogCutsceneSteps> steps, Action onFinished)
    {
        double total = 0;
        foreach (var step in steps)
        {
            total += step.durationInMillis;
        }

        return new Talk
        {
            Dialog = new DialogCutsceneController(steps, Color.Black, _font, Core.GraphicsDevice),
            TotalMs = total,
            OnFinished = onFinished
        };
    }

    // Selects the Watcher's dialog based on the current story stage.
    private Talk ChooseTalk()
    {
        return GameSession.Stage switch
        {
            StoryStage.Intro => _firstTalk,
            StoryStage.FalouComVigia => _reminderTalk,
            StoryStage.Julgada => _secondTalk,
            _ => _idleTalk
        };
    }

    // Wraps dialog text to keep it within the screen boundaries.
    // Uses the dialog font to ensure accurate text measurements.
    // Preserves manual line breaks written as "\n" in the text.
    private string Wrap(string text)
    {
        float maxWidth = Core.GraphicsDevice.PresentationParameters.BackBufferWidth * DIALOG_MAX_WIDTH_RATIO;
        float spaceWidth = _font.MeasureString(" ").X;

        System.Text.StringBuilder result = new();

        string[] paragraphs = text.Split('\n');
        for (int p = 0; p < paragraphs.Length; p++)
        {
            if (p > 0)
            {
                result.Append('\n');
            }

            float lineWidth = 0f;
            string[] words = paragraphs[p].Split(' ', StringSplitOptions.RemoveEmptyEntries);

            foreach (string word in words)
            {
                float wordWidth = _font.MeasureString(word).X;

                if (lineWidth > 0f && lineWidth + spaceWidth + wordWidth > maxWidth)
                {
                    // Moves the word to a new line when it exceeds the current line width.
                    result.Append('\n');
                    lineWidth = 0f;
                }
                else if (lineWidth > 0f)
                {
                    result.Append(' ');
                    lineWidth += spaceWidth;
                }

                result.Append(word);
                lineWidth += wordWidth;
            }
        }

        return result.ToString();
    }

    // ---------- Update ----------

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        _player.Update(gameTime);
        _vigia.Update(gameTime);

        // Audio input remains active, but player movement is disabled during dialogs.
        CheckAudioInput();
#if DEBUG
        CheckDebugKeys();
#endif
        if (_activeTalk == null)
        {
            MovePlayer(dt);
        }

        UpdateCamera(dt, snap: false);

        // Turns the Watcher toward the player's position.
        _vigia = _playerX < _vigiaX ? _vigiaLeft : _vigiaRight;

        // Updates the active dialog timer and ends the dialog when it reaches zero.
        // If DialogCutsceneController exposes an IsPlaying/IsFinished property,
        // use it instead of this timer.
        if (_activeTalk != null)
        {
            _talkRemainingMs -= gameTime.ElapsedGameTime.TotalMilliseconds;
            if (_talkRemainingMs <= 0)
            {
                Talk finished = _activeTalk;
                _activeTalk = null;
                finished.OnFinished?.Invoke();
            }
        }

        // Checks the player's proximity to the Watcher and exit arches.
        _canTalk = _activeTalk == null && MathF.Abs(_playerX - _vigiaX) < TALK_RANGE;
        _isAtLeftExit = MathF.Abs(_playerX - _leftExitX) < EXIT_RANGE;
        _isAtRightExit = MathF.Abs(_playerX - _rightExitX) < EXIT_RANGE;

        // The right arch (Boiler) unlocks after speaking to the Watcher; the
        // left arch (Haunted Train) unlocks after the Watcher guides Alma there.
        _rightExitOpen = GameSession.Stage >= StoryStage.FalouComVigia;
        _leftExitOpen = GameSession.Stage >= StoryStage.VagaoAssombrado;

        if (_activeTalk == null && Core.Input.Keyboard.WasKeyJustPressed(Keys.E))
        {
            if (_canTalk)
            {
                StartTalk(ChooseTalk());
            }
            else if (_isAtLeftExit && _leftExitOpen)
            {
                // TODO: Replace with the Haunted Train scene when it is available.
                Core.ChangeScene(new GameScene());
                return;
            }
            else if (_isAtRightExit && _rightExitOpen)
            {
                Core.ChangeScene(new Scene_caldeirao());
                return;
            }
        }
    }

    private void StartTalk(Talk talk)
    {
        Core.Audio.PlaySoundEffect(_collectSoundEffect);

        // Turns the player toward the Watcher.
        _player = _vigiaX < _playerX ? _playerLeft : _playerRight;

        _activeTalk = talk;
        _talkRemainingMs = talk.TotalMs;
        talk.Dialog.Start();
    }

    // Moves the player horizontally only; vertical movement is not supported.
    private void MovePlayer(float dt)
    {
        float speed = PLAYER_SPEED;
        if (Core.Input.Keyboard.IsKeyDown(Keys.Space))
        {
            speed *= SPRINT_MULTIPLIER;
        }

        // Switches to the matching sprite when moving left or right.
        if (Core.Input.Keyboard.IsKeyDown(Keys.A) || Core.Input.Keyboard.IsKeyDown(Keys.Left))
        {
            _playerX -= speed * dt;
            _player = _playerLeft;
        }

        if (Core.Input.Keyboard.IsKeyDown(Keys.D) || Core.Input.Keyboard.IsKeyDown(Keys.Right))
        {
            _playerX += speed * dt;
            _player = _playerRight;
        }

        // Clamps the player within the background boundaries.
        _playerX = MathHelper.Clamp(_playerX, WORLD_MARGIN, _background.Width - WORLD_MARGIN);
    }

    // Calculates the scale needed to match the background height to the window.
    private float GetWorldScale()
    {
        return Core.GraphicsDevice.PresentationParameters.BackBufferHeight / (float)_background.Height;
    }

    // Follows the player horizontally while staying within the background.
    private void UpdateCamera(float dt, bool snap)
    {
        float scale = GetWorldScale();
        float viewWidth = Core.GraphicsDevice.PresentationParameters.BackBufferWidth / scale;
        float maxCameraX = MathF.Max(0f, _background.Width - viewWidth);

        float target = MathHelper.Clamp(_playerX - viewWidth * 0.5f, 0f, maxCameraX);

        _cameraX = snap
            ? target
            : MathHelper.Lerp(_cameraX, target, MathHelper.Clamp(8f * dt, 0f, 1f));
    }

#if DEBUG
    // Debug shortcuts only (not included in the final game):
    // F1-F4 change the story stage; F5-F7 add items to the inventory.
    private void CheckDebugKeys()
    {
        var keyboard = Core.Input.Keyboard;

        if (keyboard.WasKeyJustPressed(Keys.F1)) GameSession.Stage = StoryStage.Intro;
        if (keyboard.WasKeyJustPressed(Keys.F2)) GameSession.Stage = StoryStage.FalouComVigia;
        if (keyboard.WasKeyJustPressed(Keys.F3)) GameSession.Stage = StoryStage.Julgada;
        if (keyboard.WasKeyJustPressed(Keys.F4)) GameSession.Stage = StoryStage.VagaoAssombrado;

        if (keyboard.WasKeyJustPressed(Keys.F5)) GameSession.Inventory.CollectItem(GameItems.MaskId, GameItems.MaskIcon);
        if (keyboard.WasKeyJustPressed(Keys.F6)) GameSession.Inventory.CollectItem(GameItems.NecklaceId, GameItems.NecklaceIcon);
        if (keyboard.WasKeyJustPressed(Keys.F7)) GameSession.Inventory.CollectItem(GameItems.HairId, GameItems.HairIcon);
    }
#endif

    // NOTE: If M, +, and - are already handled by GlobalControls in
    // Core.Update, remove this method and its Update call to avoid handling
    // each key twice (for example, toggling mute twice cancels itself).
    private void CheckAudioInput()
    {
        // M key: toggles mute.
        if (Core.Input.Keyboard.WasKeyJustPressed(Keys.M))
        {
            Core.Audio.ToggleMute();
        }

        if (Core.Input.Keyboard.WasKeyJustPressed(Keys.OemPlus))
        {
            Core.Audio.SongVolume += 0.1f;
            Core.Audio.SoundEffectVolume += 0.1f;
        }

        if (Core.Input.Keyboard.WasKeyJustPressed(Keys.OemMinus))
        {
            Core.Audio.SongVolume -= 0.1f;
            Core.Audio.SoundEffectVolume -= 0.1f;
        }
    }

    // ---------- Draw ----------

    public override void Draw(GameTime gameTime)
    {
        Core.GraphicsDevice.Clear(Color.Black);

        // World layer: draws the background, Watcher, and player with horizontal camera scrolling.
        // The high-resolution artwork is scaled down, so it uses
        // LinearClamp (PointClamp would make thin edges look jagged).
        float scale = GetWorldScale();
        Matrix worldMatrix =
            Matrix.CreateTranslation(-_cameraX, 0f, 0f) *
            Matrix.CreateScale(scale, scale, 1f);

        Core.SpriteBatch.Begin(samplerState: SamplerState.LinearClamp, transformMatrix: worldMatrix);

        _background.Draw(Core.SpriteBatch, Vector2.Zero);

        // Sprites are drawn from the top-left corner, keeping their feet aligned at GROUND_Y.
        _vigia.Draw(Core.SpriteBatch, new Vector2(_vigiaX - _vigia.Width * 0.5f, GROUND_Y - _vigia.Height));
        _player.Draw(Core.SpriteBatch, new Vector2(_playerX - _player.Width * 0.5f, GROUND_Y - _player.Height));

        Core.SpriteBatch.End();

        // Screen / UI layer
        Core.SpriteBatch.Begin(samplerState: SamplerState.LinearClamp);

        GameSession.Inventory.Draw(Core.SpriteBatch, new Vector2(60, 60));

        // Displays the interaction prompt based on player position (hidden during dialogs).
        string prompt = null;
        Color promptColor = Color.Yellow;

        if (_activeTalk == null)
        {
            if (_canTalk)
            {
                prompt = PRESS_E_TALK;
            }
            else if (_isAtLeftExit)
            {
                prompt = _leftExitOpen ? PRESS_E_LEFT : LOCKED_LEFT;
                promptColor = _leftExitOpen ? Color.Yellow : Color.LightGray;
            }
            else if (_isAtRightExit)
            {
                prompt = _rightExitOpen ? PRESS_E_RIGHT : LOCKED_RIGHT;
                promptColor = _rightExitOpen ? Color.Yellow : Color.LightGray;
            }
        }

        if (prompt != null)
        {
            Vector2 textSize = _font.MeasureString(prompt);
            Vector2 pos = new Vector2(
                (Core.GraphicsDevice.PresentationParameters.BackBufferWidth - textSize.X) * 0.5f,
                Core.GraphicsDevice.PresentationParameters.BackBufferHeight - 100
            );
            Core.SpriteBatch.DrawString(_font, prompt, pos, promptColor);
        }

        Core.SpriteBatch.End();

        // Draws dialogs above all other layers; each controller renders only while active.
        Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
        _firstTalk.Dialog.Draw(Core.SpriteBatch, gameTime);
        _reminderTalk.Dialog.Draw(Core.SpriteBatch, gameTime);
        _secondTalk.Dialog.Draw(Core.SpriteBatch, gameTime);
        _idleTalk.Dialog.Draw(Core.SpriteBatch, gameTime);
        Core.SpriteBatch.End();

        base.Draw(gameTime);
    }
}