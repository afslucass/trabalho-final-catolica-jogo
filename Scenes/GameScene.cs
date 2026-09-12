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

public class GameScene : Scene
{
    private Camera2D _camera;

 // Defines the slime animated sprite.
    private AnimatedSprite _slime;

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
    { 1, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 1 },
    { 1, 0, 0, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
    { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }
};

    private Circle?[,] mapSlimesBounds;

    // Defines the bat animated sprite.
    private AnimatedSprite _bat;

    private Sprite _batItemArthur;
    private Sprite _batItemPedro;
    private Vector2 _batItemPedroPosition;
    private Vector2 _batItemArthurPosition;

    // Tracks the position of the slime.
    private Vector2 _slimePosition;

    // Speed multiplier when moving.
    private const float MOVEMENT_SPEED = 5.0f;

    // Tracks the position of the bat.
    private Vector2 _batPosition;

    // Tracks the velocity of the bat.
    private Vector2 _batVelocity;

        // The sound effect to play when the bat bounces off the edge of the screen.
    private SoundEffect _bounceSoundEffect;

    // The sound effect to play when the slime eats a bat.
    private SoundEffect _collectSoundEffect;

    // The SpriteFont Description used to draw text.
    private SpriteFont _font;

    // Tracks the players score.
    private int _score;

    // Defines the position to draw the score text at.
    private Vector2 _scoreTextPosition;

    // Defines the origin used when drawing the score text.
    private Vector2 _scoreTextOrigin;

    private InventoryController _inventory;

    public override void Initialize()
    {
        // TODO: Add your initialization logic here

        base.Initialize();

        _inventory = new InventoryController();

        _camera = new Camera2D();

        // Set the initial position of the bat to be 10px
        // to the right of the slime.
        _batPosition = new Vector2(_slime.Width + 10, 0);

        // Assign the initial random velocity to the bat.
        AssignRandomBatVelocity();

        // Set the position of the score text to align to the left edge of the
        // room bounds, and to vertically be at the center of the first tile.
        _scoreTextPosition = new Vector2(60, 60);

        // Set the origin of the text so it is left-centered.
        float scoreTextYOrigin = _font.MeasureString("Score").Y * 0.5f;
        _scoreTextOrigin = new Vector2(0, scoreTextYOrigin);

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
        // Create the texture atlas from the XML configuration file
        TextureAtlas atlas = TextureAtlas.FromFile(Content, "images/atlas-definition.xml");
        MouseCursorAtlas cursorAtlas = MouseCursorAtlas.FromFile(Content, "cursor/cursor-atlas-definition.xml");

        // Create the slime animated sprite from the atlas.
        _slime = atlas.CreateAnimatedSprite("slime-animation");
        _slime.Scale = new Vector2(4.0f, 4.0f);
        _slimePosition = new Vector2(_slime.Width*2, _slime.Height*2);

        // Create the bat animated sprite from the atlas.
        _bat = atlas.CreateAnimatedSprite("bat-animation");
        _bat.Scale = new Vector2(4.0f, 4.0f);

        _slimeWall = atlas.CreateSprite("slime-2");
        _slimeWall.Scale = new Vector2(4.0f, 4.0f);

        _batItemArthur = atlas.CreateSprite("bat-1");
        _batItemArthur.Scale = new Vector2(4.0f, 4.0f);
        _batItemPedro = atlas.CreateSprite("bat-2");
        _batItemPedro.Scale = new Vector2(4.0f, 4.0f);
        _batItemArthurPosition = new Vector2(_batItemArthur.Width*10, _batItemArthur.Height*13);
        _batItemPedroPosition = new Vector2(_batItemPedro.Width*4, _batItemPedro.Height*7);

        // Load the bounce sound effect
        _bounceSoundEffect = Content.Load<SoundEffect>("audio/bounce");

        // Load the collect sound effect
        _collectSoundEffect = Content.Load<SoundEffect>("audio/collect");

        // Load the font
        _font = Content.Load<SpriteFont>("fonts/04B_30");
        
        cursorAtlas.SetCursor("cheese");
    }


    public override void Update(GameTime gameTime)
    {
        _camera.Follow(_slimePosition, Core.GraphicsDevice.Viewport);

        // Update the slime animated sprite.
        _slime.Update(gameTime);

        // Update the bat animated sprite.
        _bat.Update(gameTime);

        base.Update(gameTime);

        // Check for keyboard input and handle it.
        CheckKeyboardInput();

        // Create a bounding rectangle for the screen.
        Rectangle screenBounds = new(
            0,
            0,
            Core.GraphicsDevice.PresentationParameters.BackBufferWidth,
            Core.GraphicsDevice.PresentationParameters.BackBufferHeight
        );

        // Creating a bounding circle for the slime
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

        // Calculate the new position of the bat based on the velocity.
        Vector2 newBatPosition = _batPosition + _batVelocity;

        // Create a bounding circle for the bat.
        Circle batBounds = new Circle(
            (int)(newBatPosition.X + (_bat.Width * 0.5f)),
            (int)(newBatPosition.Y + (_bat.Height * 0.5f)),
            (int)(_bat.Width * 0.5f)
        );

        Vector2 normal = Vector2.Zero;

        // Use distance based checks to determine if the bat is within the
        // bounds of the game screen, and if it is outside that screen edge,
        // reflect it about the screen edge normal.
        if (batBounds.Left < screenBounds.Left)
        {
            normal.X = Vector2.UnitX.X;
            newBatPosition.X = screenBounds.Left;
        }
        else if (batBounds.Right > screenBounds.Right)
        {
            normal.X = -Vector2.UnitX.X;
            newBatPosition.X = screenBounds.Right - _bat.Width;
        }

        if (batBounds.Top < screenBounds.Top)
        {
            normal.Y = Vector2.UnitY.Y;
            newBatPosition.Y = screenBounds.Top;
        }
        else if (batBounds.Bottom > screenBounds.Bottom)
        {
            normal.Y = -Vector2.UnitY.Y;
            newBatPosition.Y = screenBounds.Bottom - _bat.Height;
        }

        // If the normal is anything but Vector2.Zero, this means the bat had
        // moved outside the screen edge so we should reflect it about the
        // normal.
        if (normal != Vector2.Zero)
        {
            normal.Normalize();
            _batVelocity = Vector2.Reflect(_batVelocity, normal);

            // Play the bounce sound effect.
            Core.Audio.PlaySoundEffect(_bounceSoundEffect);
        }

        _batPosition = newBatPosition;

        if (slimeBounds.Intersects(batBounds))
        {
            // Divide the width  and height of the screen into equal columns and
            // rows based on the width and height of the bat.
            int totalColumns = Core.GraphicsDevice.PresentationParameters.BackBufferWidth / (int)_bat.Width;
            int totalRows = Core.GraphicsDevice.PresentationParameters.BackBufferHeight / (int)_bat.Height;

            // Choose a random row and column based on the total number of each
            int column = Random.Shared.Next(0, totalColumns);
            int row = Random.Shared.Next(0, totalRows);

            // Change the bat position by setting the x and y values equal to
            // the column and row multiplied by the width and height.
            _batPosition = new Vector2(column * _bat.Width, row * _bat.Height);

            // Assign a new random velocity to the bat
            AssignRandomBatVelocity();

            // Play the collect sound effect.
            Core.Audio.PlaySoundEffect(_collectSoundEffect);

            // Increase the player's score.
            _score += 100;
        }

        if(Core.Input.Mouse.WasButtonJustPressed(MouseButton.Left))
        {
            Circle _mouseBounds = new Circle(
                (int)Core.Input.Mouse.GetWorldPosition(_camera).X, 
                (int)Core.Input.Mouse.GetWorldPosition(_camera).Y, 
                1
            );
            Circle _arthurBatBounds = new Circle(
                (int)(_batItemArthurPosition.X + (_batItemArthur.Width * 0.5f)),
                (int)(_batItemArthurPosition.Y + (_batItemArthur.Height * 0.5f)),
                (int)(_batItemArthur.Width * 0.5f)
            );
            Circle _pedroBatBounds = new Circle(
                (int)(_batItemPedroPosition.X + (_batItemPedro.Width * 0.5f)),
                (int)(_batItemPedroPosition.Y + (_batItemPedro.Height * 0.5f)),
                (int)(_batItemPedro.Width * 0.5f)
            );
            if (_mouseBounds.Intersects(_arthurBatBounds))
            {
                _inventory.CollectItem("arthur-bat", _batItemArthur);
                Core.Audio.PlaySoundEffect(_collectSoundEffect);
                _batItemArthur.Scale = new Vector2(0f, 0f);
            }
            if (_mouseBounds.Intersects(_pedroBatBounds))
            {
                _inventory.CollectItem("pedro-bat", _batItemPedro);
                _batItemPedro.Scale = new Vector2(0f, 0f);
                Core.Audio.PlaySoundEffect(_collectSoundEffect);
            }
        }
    }

    private void AssignRandomBatVelocity()
    {
        // Generate a random angle.
        float angle = (float)(Random.Shared.NextDouble() * Math.PI * 2);

        // Convert angle to a direction vector.
        float x = (float)Math.Cos(angle);
        float y = (float)Math.Sin(angle);
        Vector2 direction = new Vector2(x, y);

        // Multiply the direction vector by the movement speed.
        _batVelocity = direction * MOVEMENT_SPEED;
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
        Core.GraphicsDevice.Clear(Color.CornflowerBlue);

        // Begin the sprite batch to prepare for rendering.
        Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: _camera.TransformMatrix);

        // Draw the slime sprite.
        _slime.Draw(Core.SpriteBatch, _slimePosition);

        // Draw the bat sprite.
        _bat.Draw(Core.SpriteBatch, _batPosition);

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

        _batItemArthur.Draw(Core.SpriteBatch, _batItemArthurPosition);
        _batItemPedro.Draw(Core.SpriteBatch, _batItemPedroPosition);

        // Always end the sprite batch when finished.
        Core.SpriteBatch.End();

        // Renders GUI
        Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
        // Draw the score
        Core.SpriteBatch.DrawString(
            _font,              // spriteFont
            $"Score: {_score}", // text
            _scoreTextPosition, // position
            Color.White,        // color
            0.0f,               // rotation
            _scoreTextOrigin,   // origin
            1.0f,               // scale
            SpriteEffects.None, // effects
            0.0f                // layerDepth
        );
        _inventory.Draw(Core.SpriteBatch, _scoreTextPosition);
        Core.SpriteBatch.End();

        base.Draw(gameTime);
    }
}
