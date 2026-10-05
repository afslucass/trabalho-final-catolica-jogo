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
    private Camera2D _camera;
    private AnimatedSprite _slime;
    private Vector2 _slimePosition;
    private Sprite _slimeWall;

    readonly private int[,] map = {
    { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
    { 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
    { 1, 0, 1, 1, 1, 0, 1, 0, 1, 1, 1, 1, 1, 0, 0, 1 },
    { 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1 },
    { 1, 0, 1, 0, 1, 1, 1, 1, 1, 1, 0, 0, 1, 0, 0, 1 },
    { 1, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 1 },
    { 1, 0, 0, 0, 1, 0, 1, 1, 0, 1, 0, 1, 1, 1, 0, 1 },
    { 1, 1, 1, 0, 0, 0, 1, 1, 0, 0, 0, 1, 0, 0, 0, 1 },
    { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 1, 1, 1 },
    { 1, 0, 1, 1, 1, 1, 0, 0, 1, 1, 0, 0, 0, 0, 0, 1 },
    { 1, 0, 1, 0, 0, 1, 0, 0, 1, 1, 1, 1, 1, 0, 0, 1 },
    { 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1 },
    { 1, 0, 1, 1, 0, 1, 1, 1, 1, 1, 0, 0, 1, 0, 0, 1 },
    { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
    { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
    { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }
    };

    private Circle?[,] mapSlimesBounds;

    private const float MOVEMENT_SPEED = 5.0f;

    private SpriteFont _font;

    private const string PRESS_E_INTERECT = "Press E for go to the next scene";

    private Vector2 _pressEInterectPos;
    private bool _isAtExit;

    public override void Initialize()
    {
        base.Initialize();
        _camera = new Camera2D();
        
        // Set the position of the press E text
        Vector2 size = _font.MeasureString(PRESS_E_INTERECT);
        _pressEInterectPos = new Vector2(Core.GraphicsDevice.PresentationParameters.BackBufferWidth / 2, 800);

        mapSlimesBounds = new Circle?[map.GetLength(0), map.GetLength(1)];
        for (int row = 0; row < map.GetLength(0); row++)
        {
            for (int col = 0; col < map.GetLength(1); col++)
            {
                if (map[row, col] == 1)
                {
                    float x = col * _slime.Width;
                    float y = row * _slime.Height;
                    mapSlimesBounds[row, col] = new Circle(
                        (int)(x + _slime.Width * 0.5f),
                        (int)(y + _slime.Height * 0.5f),
                        (int)(_slime.Width * 0.5f)
                    );
                }
                else
                {
                    mapSlimesBounds[row, col] = null;
                }
            }
        }
    }

    public override void LoadContent()
    {
        base.LoadContent();

        TextureAtlas atlas = TextureAtlas.FromFile(Content, "images/atlas-definition.xml");

        _slime = atlas.CreateAnimatedSprite("slime-animation");
        _slime.Scale = new Vector2(4.0f, 4.0f);
        _slimePosition = new Vector2(_slime.Width*7, _slime.Height*14);

        _slimeWall = atlas.CreateSprite("slime-2");
        _slimeWall.Scale = new Vector2(4.0f, 4.0f);

        // Load the font
        _font = Content.Load<SpriteFont>("fonts/04B_30");
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        _slime.Update(gameTime);
        _camera.Follow(_slimePosition, Core.GraphicsDevice.Viewport);
        
        // Check for keyboard input and handle it.
        CheckKeyboardInput();

        Circle slimeBounds = new(
            (int)(_slimePosition.X + (_slime.Width * 0.5f)),
            (int)(_slimePosition.Y + (_slime.Height * 0.5f)),
            (int)(_slime.Width * 0.5f)
        );

        foreach (Circle? mapSlimeBoundsNullable in mapSlimesBounds)
        {
            if (!mapSlimeBoundsNullable.HasValue)
                continue;

            Circle wall = mapSlimeBoundsNullable.Value;
            Vector2 slimeCenter = new Vector2(
                _slimePosition.X + _slime.Width * 0.5f,
                _slimePosition.Y + _slime.Height * 0.5f
            );
            Vector2 wallCenter = new Vector2(
                wall.X,
                wall.Y
            );

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

        // Define the column and row indices for the map exit tile.
        int exitCol = 01; 
        int exitRow = 14; 
        // Get the tile dimensions based on the wall sprite size.
        int tileWidth = (int)_slimeWall.Width;
        int tileHeight = (int)_slimeWall.Height;
        // Create a bounding circle for the exit tile.
        Circle exitBounds = new Circle(
            (int)(exitCol * tileWidth + tileWidth * 0.5f),
            (int)(exitRow * tileHeight + tileHeight * 0.5f),
            (int)(tileWidth * 0.5f)
        );
        // Check for collision between the slime and the exit point to trigger a scene change.
        _isAtExit = slimeBounds.Intersects(exitBounds);
        if (_isAtExit && Core.Input.Keyboard.WasKeyJustPressed(Keys.E))
        {
            Core.ChangeScene(new GameScene());
            return;
        }
    }

    private void CheckKeyboardInput()
    {
        // If the space key is held down, the movement speed increases by 1.5
        float speed = MOVEMENT_SPEED;
        if (Core.Input.Keyboard.IsKeyDown(Keys.Space))
        {
            speed *= 1.5f;
        }

        // If the W or Up keys are down, move the slime up on the screen.
        if (Core.Input.Keyboard.IsKeyDown(Keys.W) || Core.Input.Keyboard.IsKeyDown(Keys.Up))
        {
            _slimePosition.Y -= speed;
        }

        // if the S or Down keys are down, move the slime down on the screen.
        if (Core.Input.Keyboard.IsKeyDown(Keys.S) || Core.Input.Keyboard.IsKeyDown(Keys.Down))
        {
            _slimePosition.Y += speed;
        }

        // If the A or Left keys are down, move the slime left on the screen.
        if (Core.Input.Keyboard.IsKeyDown(Keys.A) || Core.Input.Keyboard.IsKeyDown(Keys.Left))
        {
            _slimePosition.X -= speed;
        }

        // If the D or Right keys are down, move the slime right on the screen.
        if (Core.Input.Keyboard.IsKeyDown(Keys.D) || Core.Input.Keyboard.IsKeyDown(Keys.Right))
        {
            _slimePosition.X += speed;
        }

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
    }

    public override void Draw(GameTime gameTime)
    {
        Core.GraphicsDevice.Clear(Color.DarkSlateGray);

        Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: _camera.TransformMatrix);
        _slime.Draw(Core.SpriteBatch, _slimePosition);
        // Draw the slime sprite.
        _slime.Draw(Core.SpriteBatch, _slimePosition);

        for (int row = 0; row < map.GetLength(0); row++)
        {
            for (int col = 0; col < map.GetLength(1); col++)
            {
                if(map[row, col] == 1)
                {
                    Vector2 _slimeWallPosition = new(col * _slimeWall.Height, row * _slimeWall.Width);
                    _slimeWall.Draw(Core.SpriteBatch, _slimeWallPosition);
                }
            }
        }

        Core.SpriteBatch.End();

        Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);

        if (_isAtExit)
        {
            Core.SpriteBatch.DrawString(
                _font,
                PRESS_E_INTERECT,
                _pressEInterectPos,
                Color.Black,
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