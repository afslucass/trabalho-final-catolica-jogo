using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using GameCore;
using GameCore.Graphics;
using GameCore.Scenes;
using GameCore.Camera;
using GameCore.CustomMouseCursor;
using GameCore.Input;
using GameCore.Inventory;

namespace TestGame.Scenes;

public class Scene_caldeirao : Scene
{
    private Sprite _background_RECEPCAO;
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
    private const float WORLD_MARGIN = 200f;
    
    // In this game, player-2 faces left and player-1 faces
    // right (verified). Update these names if the sprite sheet changes.
    private const string PLAYER_LEFT_ANIMATION = "player-2-animation";
    private const string PLAYER_RIGHT_ANIMATION = "player-1-animation";

    // ---------- Watcher ----------
    private AnimatedSprite _raposo;
    private float _raposoX;

    private const string RAPOSO_ANIMATION = "hooded-animation";

    private SpriteFont _font;

    Color Alloy = new Color(201, 149, 0);

    private const string PRESS_E_INTERECT = "Press E for go to Recepção";

    private Vector2 _pressEInterectPos;
    private const float EXIT_RANGE_RECEPCAO = 150f;
    private const float EXIT_RATIO = 0.100f;
    private bool _isAtExit;
    private float _recepcaoExitX;

    // Item 3 do disfarce (cabelo). Só pode ser coletado depois de iniciar o puzzle
    // (falar com o Estranho no vagão assombrado).
    private const float HAIR_X_RATIO = 0.55f;
    private const float ITEM_RANGE = 150f;
    private const string PRESS_E_COLLECT_ITEM = "Press E para pegar o item";
    private Sprite _hairSprite;
    private float _hairX;
    private bool _isNearHair;

    public override void Initialize()
    {
        base.Initialize();
        float worldWidth = _background_RECEPCAO.Width;
        _playerX = _recepcaoExitX + 100f;
        _player = _playerRight;
        
        // Set the position of the press E text
        Vector2 size = _font.MeasureString(PRESS_E_INTERECT);
        _pressEInterectPos = new Vector2(Core.GraphicsDevice.PresentationParameters.BackBufferWidth / 2, 800);

        UpdateCamera(0f, snap: true);       
    }

    public override void LoadContent()
    {
        base.LoadContent();

        TextureAtlas charactersAtlas = TextureAtlas.FromFile(Content, "images/characters-atlas-definition.xml");
        TextureAtlas atlas = TextureAtlas.FromFile(Content, "images/atlas-definition.xml");

        // Reception background (3840 x 1080), drawn at its native size.
        _background_RECEPCAO = charactersAtlas.CreateSprite("bg-caldeirao");
        _background_RECEPCAO.Scale = Vector2.One;


        // Player sprites: one for each direction (matching sizes prevent position jumps).
        _playerLeft = charactersAtlas.CreateAnimatedSprite(PLAYER_LEFT_ANIMATION);
        _playerLeft.Scale = Vector2.One;
        _playerRight = charactersAtlas.CreateAnimatedSprite(PLAYER_RIGHT_ANIMATION);
        _playerRight.Scale = Vector2.One;
        _player = _playerRight;

        _raposo = charactersAtlas.CreateAnimatedSprite(RAPOSO_ANIMATION);
        _raposo.Scale = Vector2.One;

        _recepcaoExitX = _background_RECEPCAO.Width * EXIT_RATIO;

        _hairSprite = charactersAtlas.CreateSprite(GameItems.HairIcon);
        _hairSprite.Scale = Vector2.One;
        _hairX = _background_RECEPCAO.Width * HAIR_X_RATIO;

        // Load the font
        _font = Content.Load<SpriteFont>("fonts/KiwiSoda");
        _font.DefaultCharacter = _font.Characters.Contains('?') ? '?' : ' ';
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        _player.Update(gameTime);
        _raposo.Update(gameTime);
        
        // Check for keyboard input and handle it.
        CheckKeyboardInput(dt);

        UpdateCamera(dt, snap: false);

        // Check for collision between the slime and the exit point to trigger a scene change.
        _isAtExit = MathF.Abs(_playerX - _recepcaoExitX) < EXIT_RANGE_RECEPCAO;

        // O item 3 só pode ser coletado depois de iniciar o puzzle.
        _isNearHair = GameSession.HauntedPuzzleStarted &&
                      !GameSession.Inventory.Has(GameItems.HairId) &&
                      MathF.Abs(_playerX - _hairX) < ITEM_RANGE;

        if (Core.Input.Keyboard.WasKeyJustPressed(Keys.E))
        {
            if (_isNearHair)
            {
                GameSession.Inventory.CollectItem(GameItems.HairId, GameItems.HairIcon);
            }
            else if (_isAtExit)
            {
                // Volta pela porta da direita da recepção (a Caldeira fica à direita dela).
                Core.ChangeScene(new Scene_recepcao(RecepcaoSpawn.FromCaldeira));
                return;
            }
        }
    }

    private void CheckKeyboardInput(float dt)
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
        _playerX = MathHelper.Clamp(_playerX, WORLD_MARGIN, _background_RECEPCAO.Width - WORLD_MARGIN);

        // If the M key is pressed, toggle mute state for audio.
        if (Core.Input.Keyboard.WasKeyJustPressed(Keys.M))
        {
            Core.Audio.ToggleMute();
        }

        // If the + button is pressed, increase the volume.
        if (Core.Input.Keyboard.WasKeyJustPressed(Keys.OemPlus))
        {
            Core.Audio.SongVolume += 0.1f;
            Core.Audio.SoundEffectVolume += 0.1f;
        }

        // If the - button was pressed, decrease the volume.
        if (Core.Input.Keyboard.WasKeyJustPressed(Keys.OemMinus))
        {
            Core.Audio.SongVolume -= 0.1f;
            Core.Audio.SoundEffectVolume -= 0.1f;
        }

        _playerX = MathHelper.Clamp(_playerX, WORLD_MARGIN, _background_RECEPCAO.Width - WORLD_MARGIN);
    }

    private float GetWorldScale()
    {
        return Core.GraphicsDevice.PresentationParameters.BackBufferHeight / (float)_background_RECEPCAO.Height;
    }
    private void UpdateCamera(float dt, bool snap)
    {
        float scale = GetWorldScale();
        float viewWidth = Core.GraphicsDevice.PresentationParameters.BackBufferWidth / scale;
        float maxCameraX = MathF.Max(0f, _background_RECEPCAO.Width - viewWidth);

        float target = MathHelper.Clamp(_playerX - viewWidth * 0.5f, 0f, maxCameraX);

        _cameraX = snap
            ? target
            : MathHelper.Lerp(_cameraX, target, MathHelper.Clamp(8f * dt, 0f, 1f));
    }

    public override void Draw(GameTime gameTime)
    {
        Core.GraphicsDevice.Clear(Color.DarkSlateGray);

        float scale = GetWorldScale();
        Matrix worldMatrix =
            Matrix.CreateTranslation(-_cameraX, 0f, 0f) *
            Matrix.CreateScale(scale, scale, 1f);
        
        Core.SpriteBatch.Begin(samplerState: SamplerState.LinearClamp, transformMatrix: worldMatrix);

        _background_RECEPCAO.Draw(Core.SpriteBatch, Vector2.Zero);

        if (GameSession.HauntedPuzzleStarted && !GameSession.Inventory.Has(GameItems.HairId))
        {
            _hairSprite.Draw(Core.SpriteBatch,
                new Vector2(_hairX - _hairSprite.Width * 0.5f, GROUND_Y - _hairSprite.Height));
        }

        _raposo.Draw(Core.SpriteBatch, new Vector2(_raposoX - _raposo.Width * -1.5f, GROUND_Y - _raposo.Height));
        _player.Draw(Core.SpriteBatch, new Vector2(_playerX - _player.Width * 0.5f, GROUND_Y - _player.Height));

        Core.SpriteBatch.End();

        Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);

        GameSession.Inventory.Draw(Core.SpriteBatch, new Vector2(60, 60));

        if (_isNearHair)
        {
            Core.SpriteBatch.DrawString(
                _font,
                PRESS_E_COLLECT_ITEM,
                _pressEInterectPos,
                Color.White,
                0.0f,
                _font.MeasureString(PRESS_E_COLLECT_ITEM) * 0.5f,
                1.0f,
                SpriteEffects.None,
                0.0f
            );
        }
        else if (_isAtExit)
        {
            Core.SpriteBatch.DrawString(
                _font,
                PRESS_E_INTERECT,
                _pressEInterectPos,
                Color.White,
                0.0f,
                _font.MeasureString(PRESS_E_INTERECT) * 0.5f,
                1.0f,
                SpriteEffects.None,
                0.0f
            );
        }
        Core.SpriteBatch.End();

        base.Draw(gameTime);
    }

}