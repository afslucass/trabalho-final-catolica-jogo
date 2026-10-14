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
using GameCore.Cutscene;
using System.Collections.Generic;
using GameCore.DialogCutscene;

namespace TestGame.Scenes;

public class GameSceneWithDialog : Scene
{
    private Camera2D _camera;

 // Defines the slime animated sprite.
    private AnimatedSprite _slime;

    private Sprite _slimeWall;

    readonly private int[,] map = {
    { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
    { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
    { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
    { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
    { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
    { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
    { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
    { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
    { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
    { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
    { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
    { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
    { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
    { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
    { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
    { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }
};

    private Circle?[,] mapSlimesBounds;

    // Defines the bat animated sprite.
    private AnimatedSprite _bat;

    // Tracks the position of the slime.
    private Vector2 _slimePosition;

    // Speed multiplier when moving.
    private const float MOVEMENT_SPEED = 5.0f;

    // Tracks the position of the bat.
    private Vector2 _batPosition;

    // The sound effect to play when the bat bounces off the edge of the screen.
    private SoundEffect _bounceSoundEffect;

    // The sound effect to play when the slime eats a bat.
    private SoundEffect _collectSoundEffect;

    private SpriteFont _font;
    private DialogCutsceneController dialogCutscene;

    public override void Initialize()
    {
        // TODO: Add your initialization logic here

        base.Initialize();

        _camera = new Camera2D();

        // Set the initial position of the bat to be 10px
        // to the right of the slime.
        _batPosition = new Vector2(500, 500);

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

        // Load the bounce sound effect
        _bounceSoundEffect = Content.Load<SoundEffect>("audio/bounce");

        // Load the collect sound effect
        _collectSoundEffect = Content.Load<SoundEffect>("audio/collect");

        // Load the font
        _font = Content.Load<SpriteFont>("fonts/04B_30");

        List<DialogCutsceneSteps> steps = new();

        steps.Add(new DialogCutsceneSteps
        {
            caption = "O personagem e o morcego comecam a caminhar",
            hasCaptionFadeIn = true,
            durationInMillis = 3000,
            spriteAnimations = new List<DialogCutsceneSpriteAnimation>
            {
                // Animação do slime
                new DialogCutsceneSpriteAnimation
                {
                    keyPositions = new List<DialogCutsceneSpriteKeyPosition>
                    {
                        new DialogCutsceneSpriteKeyPosition
                        {
                            spriteCurrentPosition = _slimePosition,
                            targetXPosition = 200,
                            targetYPosition = 200,
                            time = 0,
                            onPositionChanged = position => _slimePosition = position
                        },
                        new DialogCutsceneSpriteKeyPosition
                        {
                            spriteCurrentPosition = _slimePosition,
                            targetXPosition = 500,
                            targetYPosition = 200,
                            time = 2000,
                            onPositionChanged = position => _slimePosition = position
                        },
                        new DialogCutsceneSpriteKeyPosition
                        {
                            spriteCurrentPosition = _slimePosition,
                            targetXPosition = 500,
                            targetYPosition = 300,
                            time = 3000,
                            onPositionChanged = position => _slimePosition = position
                        }
                    }
                },
                // Animação do morcego
                new DialogCutsceneSpriteAnimation
                {
                    keyPositions = new List<DialogCutsceneSpriteKeyPosition>
                    {
                        new DialogCutsceneSpriteKeyPosition
                        {
                            spriteCurrentPosition = _batPosition,
                            targetXPosition = 400,
                            targetYPosition = 400,
                            time = 0,
                            onPositionChanged = position => _batPosition = position
                        },
                        new DialogCutsceneSpriteKeyPosition
                        {
                            spriteCurrentPosition = _batPosition,
                            targetXPosition = 500,
                            targetYPosition = 400,
                            time = 2000,
                            onPositionChanged = position => _batPosition = position
                        },
                        new DialogCutsceneSpriteKeyPosition
                        {
                            spriteCurrentPosition = _batPosition,
                            targetXPosition = 900,
                            targetYPosition = 400,
                            time = 3000,
                            onPositionChanged = position => _batPosition = position
                        }
                    }
                }
            }
        });
        steps.Add(new DialogCutsceneSteps
        {
            caption = "e dps ocorre alguma coisa.",
            hasCaptionFadeIn = true,
            durationInMillis = 3000,
            spriteAnimations = new List<DialogCutsceneSpriteAnimation>
            {
                // Animação do slime
                new DialogCutsceneSpriteAnimation
                {
                    keyPositions = new List<DialogCutsceneSpriteKeyPosition>
                    {
                        new DialogCutsceneSpriteKeyPosition
                        {
                            spriteCurrentPosition = _slimePosition,
                            targetXPosition = 500,
                            targetYPosition = 300,
                            time = 0,
                            onPositionChanged = position => _slimePosition = position
                        },
                        new DialogCutsceneSpriteKeyPosition
                        {
                            spriteCurrentPosition = _slimePosition,
                            targetXPosition = 500,
                            targetYPosition = 200,
                            time = 2000,
                            onPositionChanged = position => _slimePosition = position
                        },
                    }
                },
                // Animação do morcego
                new DialogCutsceneSpriteAnimation
                {
                    keyPositions = new List<DialogCutsceneSpriteKeyPosition>
                    {
                        new DialogCutsceneSpriteKeyPosition
                        {
                            spriteCurrentPosition = _batPosition,
                            targetXPosition = 400,
                            targetYPosition = 400,
                            time = 0,
                            onPositionChanged = position => _batPosition = position
                        },
                        new DialogCutsceneSpriteKeyPosition
                        {
                            spriteCurrentPosition = _batPosition,
                            targetXPosition = 500,
                            targetYPosition = 400,
                            time = 2000,
                            onPositionChanged = position => _batPosition = position
                        },
                    }
                }
            }
        });
        
        this.dialogCutscene = new DialogCutsceneController(steps, Color.Black, _font, Core.GraphicsDevice);
        
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

        Vector2 newBatPosition = _batPosition ;

        // Create a bounding circle for the bat.
        Circle batBounds = new Circle(
            (int)(newBatPosition.X + (_bat.Width * 0.5f)),
            (int)(newBatPosition.Y + (_bat.Height * 0.5f)),
            (int)(_bat.Width * 0.5f)
        );

        if (slimeBounds.Intersects(batBounds))
        {
            Core.Audio.PlaySoundEffect(_collectSoundEffect);
            dialogCutscene.Start();
        }
        // Define the column and row indices for the map exit tile.
        int exitCol = 15; 
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
        if (slimeBounds.Intersects(exitBounds))
        {
            Core.ChangeScene(new Scene_recepcao());
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

        // Always end the sprite batch when finished.
        Core.SpriteBatch.End();

        Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
        this.dialogCutscene.Draw(Core.SpriteBatch, gameTime);
        Core.SpriteBatch.End();

        base.Draw(gameTime);
    }
}
