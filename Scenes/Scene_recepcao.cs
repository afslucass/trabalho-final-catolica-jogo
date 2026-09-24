using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using GameCore;
using GameCore.Camera;
using GameCore.CustomMouseCursor;
using GameCore.Graphics;
using GameCore.Input;
using GameCore.Scenes;

namespace TestGame.Scenes;

public class Scene_recepcao : Scene
{
    private Camera2D _camera;

    // Defines the slime animated sprite and position.
    private AnimatedSprite _slime;
    private Vector2 _slimePosition;
    private const float MOVEMENT_SPEED = 5.0f;

    // Map tiles and collision bounds.
    private Sprite _slimeWall;
    private Circle?[,] _mapSlimesBounds;

    readonly private int[,] map = {
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
        { 1, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
        { 1, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
        { 1, 0, 0, 0, 1, 0, 0, 0, 0, 1 },
        { 1, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
        { 1, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
        { 1, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
        { 1, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }
    };

    // UI and Sound.
    private SpriteFont _font;
    private SoundEffect _collectSoundEffect;

    public override void Initialize()
    {
        base.Initialize();

        _camera = new Camera2D();
        _slimePosition = new Vector2(120, 120);

        // Build collision circles for all solid tiles in this room
        _mapSlimesBounds = new Circle?[map.GetLength(0), map.GetLength(1)];
        for (int row = 0; row < map.GetLength(0); row++)
        {
            for (int col = 0; col < map.GetLength(1); col++)
            {
                if (map[row, col] == 1)
                {
                    float x = col * _slimeWall.Width;
                    float y = row * _slimeWall.Height;
                    _mapSlimesBounds[row, col] = new Circle(
                        (int)(x + _slimeWall.Width * 0.5f),
                        (int)(y + _slimeWall.Height * 0.5f),
                        (int)(_slimeWall.Width * 0.5f)
                    );
                }
                else
                {
                    _mapSlimesBounds[row, col] = null;
                }
            }
        }
    }

    public override void LoadContent()
    {
        base.LoadContent();

        TextureAtlas atlas = TextureAtlas.FromFile(Content, "images/atlas-definition.xml");
        MouseCursorAtlas cursorAtlas = MouseCursorAtlas.FromFile(Content, "cursor/cursor-atlas-definition.xml");

        _slime = atlas.CreateAnimatedSprite("slime-animation");
        _slime.Scale = new Vector2(4.0f, 4.0f);

        _slimeWall = atlas.CreateSprite("slime-2");
        _slimeWall.Scale = new Vector2(4.0f, 4.0f);

        _collectSoundEffect = Content.Load<SoundEffect>("audio/collect");
        _font = Content.Load<SpriteFont>("fonts/04B_30");

        cursorAtlas.SetCursor("cheese");
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        // Update player movement and global audio keys
        CheckKeyboardInput();

        // Update animations and camera position
        _slime.Update(gameTime);
        _camera.Follow(_slimePosition, Core.GraphicsDevice.Viewport);

        // Check collisions between the player and walls
        CheckWallCollisions();
    }

    private void CheckWallCollisions()
    {
        Circle slimeBounds = new Circle(
            (int)(_slimePosition.X + (_slime.Width * 0.5f)),
            (int)(_slimePosition.Y + (_slime.Height * 0.5f)),
            (int)(_slime.Width * 0.5f)
        );

        foreach (Circle? wallNullable in _mapSlimesBounds)
        {
            if (!wallNullable.HasValue)
                continue;

            Circle wall = wallNullable.Value;
            Vector2 slimeCenter = new Vector2(_slimePosition.X + _slime.Width * 0.5f, _slimePosition.Y + _slime.Height * 0.5f);
            Vector2 wallCenter = new Vector2(wall.X, wall.Y);

            Vector2 direction = slimeCenter - wallCenter;
            float distance = direction.Length();
            float minDistance = slimeBounds.Radius + wall.Radius;

            if (distance < minDistance)
            {
                if (distance == 0)
                {
                    direction = Vector2.UnitX;
                    distance = 1;
                }

                direction.Normalize();
                float overlap = minDistance - distance;
                _slimePosition += direction * overlap;
            }
        }
    }

    private void CheckKeyboardInput()
    {
        float speed = MOVEMENT_SPEED;
        if (Core.Input.Keyboard.IsKeyDown(Keys.Space))
        {
            speed *= 1.5f;
        }

        if (Core.Input.Keyboard.IsKeyDown(Keys.W) || Core.Input.Keyboard.IsKeyDown(Keys.Up))
        {
            _slimePosition.Y -= speed;
        }

        if (Core.Input.Keyboard.IsKeyDown(Keys.S) || Core.Input.Keyboard.IsKeyDown(Keys.Down))
        {
            _slimePosition.Y += speed;
        }

        if (Core.Input.Keyboard.IsKeyDown(Keys.A) || Core.Input.Keyboard.IsKeyDown(Keys.Left))
        {
            _slimePosition.X -= speed;
        }

        if (Core.Input.Keyboard.IsKeyDown(Keys.D) || Core.Input.Keyboard.IsKeyDown(Keys.Right))
        {
            _slimePosition.X += speed;
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

    public override void Draw(GameTime gameTime)
    {
        Core.GraphicsDevice.Clear(Color.CornflowerBlue);

        // World Layer (Camera Matrix)
        Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: _camera.TransformMatrix);

        for (int row = 0; row < map.GetLength(0); row++)
        {
            for (int col = 0; col < map.GetLength(1); col++)
            {
                if (map[row, col] == 1)
                {
                    Vector2 wallPos = new Vector2(col * _slimeWall.Width, row * _slimeWall.Height);
                    _slimeWall.Draw(Core.SpriteBatch, wallPos);
                }
            }
        }

        _slime.Draw(Core.SpriteBatch, _slimePosition);

        Core.SpriteBatch.End();

        // Screen / GUI Layer
        Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);

        Core.SpriteBatch.DrawString(
            _font,
            $"Score: {GameSession.Score}",
            new Vector2(60, 60),
            Color.White
        );

        GameSession.Inventory.Draw(Core.SpriteBatch, new Vector2(60, 60));

        Core.SpriteBatch.End();

        base.Draw(gameTime);
    }
}