using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using GameCore;
using GameCore.Camera;
using GameCore.CustomMouseCursor;
using GameCore.DialogCutscene;
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

    // O VIGIA (NPC que conversa com o jogador), usando o sprite do morcego.
    private AnimatedSprite _bat;
    private Vector2 _batPosition;

    // Map tiles and collision bounds.
    private Sprite _slimeWall;
    private Circle?[,] _mapSlimesBounds;

    // Mensagens de interação e posições da interface
    private const string PRESS_E_GAMESCENE = "Press E para entrar na GameScene";
    private const string PRESS_E_CALDEIRAO = "Press E para entrar na sala do Caldeirao";
    private const string PRESS_E_TALK = "Press E para conversar";

    private Vector2 _pressEInteractPos;
    private bool _isAtLeftExit;
    private bool _isAtRightExit;

    // Diálogo com o VIGIA
    private DialogCutsceneController dialogCutscene;
    private bool _canTalk;            // jogador perto o bastante do VIGIA para conversar
    private bool _isDialogRunning;    // o diálogo está tocando
    private double _dialogTotalMs;    // duração total do diálogo
    private double _dialogRemainingMs;

    // Largura máxima de uma linha de diálogo, como fração da largura da tela.
    private const float DIALOG_MAX_WIDTH_RATIO = 0.8f;

    readonly private int[,] map = {
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
        { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
        { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
        { 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1 },
        { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
        { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
        { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
        { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }
    };

    // UI and Sound.
    private SpriteFont _font;
    private SoundEffect _collectSoundEffect;

    public override void Initialize()
    {
        base.Initialize();

        _camera = new Camera2D();
        _slimePosition = new Vector2(120, 120);
    }

    public override void LoadContent()
    {
        base.LoadContent();

        TextureAtlas atlas = TextureAtlas.FromFile(Content, "images/atlas-definition.xml");
        MouseCursorAtlas cursorAtlas = MouseCursorAtlas.FromFile(Content, "cursor/cursor-atlas-definition.xml");

        _slime = atlas.CreateAnimatedSprite("slime-animation");
        _slime.Scale = new Vector2(4.0f, 4.0f);

        // Cria o VIGIA a partir do atlas e o posiciona no meio da sala (coluna 10, linha 4).
        _bat = atlas.CreateAnimatedSprite("bat-animation");
        _bat.Scale = new Vector2(4.0f, 4.0f);
        _batPosition = new Vector2(_bat.Width * 10, _bat.Height * 4);

        _slimeWall = atlas.CreateSprite("slime-2");
        _slimeWall.Scale = new Vector2(4.0f, 4.0f);

        _collectSoundEffect = Content.Load<SoundEffect>("audio/collect");
        _font = Content.Load<SpriteFont>("fonts/04B_30");

        _font.DefaultCharacter = _font.Characters.Contains('?') ? '?' : ' ';

        cursorAtlas.SetCursor("cheese");

        // Constrói a matriz de colisão das paredes
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
        List<DialogCutsceneSteps> steps = new();

        steps.Add(new DialogCutsceneSteps
        {
            caption = Wrap("AUTO LÁ! O que é isso que vejo? Um novo aperitivo? A grande Morte nunca falha... Bem-vindo! Bem-vindo! Bem-vindo ao meio termo! O Limbo abriga aqueles que estão de passagem."),
            hasCaptionFadeIn = true,
            durationInMillis = 8000,
            spriteAnimations = new List<DialogCutsceneSpriteAnimation>()
        });
        steps.Add(new DialogCutsceneSteps
        {
            caption = Wrap("Sou o grandioso VIGIA, tudo vejo, tudo sei. Eu te conheço de muito antes Alma, desdo seu nascimento, até o seu último suspiro..."),
            hasCaptionFadeIn = true,
            durationInMillis = 8000,
            spriteAnimations = new List<DialogCutsceneSpriteAnimation>()
        });
        steps.Add(new DialogCutsceneSteps
        {
            caption = Wrap("De certa forma, somos íntimos, mas não se deixe levar, não somos camaradas."),
            hasCaptionFadeIn = true,
            durationInMillis = 6500,
            spriteAnimations = new List<DialogCutsceneSpriteAnimation>()
        });
        steps.Add(new DialogCutsceneSteps
        {
            caption = Wrap("Seu corpo fraco te abandonou, você está aqui para seguir para a próxima fase do seu ciclo, assim como todos os outros antes, e como todos os próximos..."),
            hasCaptionFadeIn = true,
            durationInMillis = 8000,
            spriteAnimations = new List<DialogCutsceneSpriteAnimation>()
        });
        steps.Add(new DialogCutsceneSteps
        {
            caption = Wrap("Alma, seu destino te aguarda a direita desse vagão, não se surpreenda pelo o que está por vir, não irá durar por muito... Só precisamos ter certeza..."),
            hasCaptionFadeIn = true,
            durationInMillis = 8000,
            spriteAnimations = new List<DialogCutsceneSpriteAnimation>()
        });
        steps.Add(new DialogCutsceneSteps
        {
            caption = Wrap("VÁ CRIATURA, me deixe às minhas novelas, os mortos não me entretém tanto quanto os vivos."),
            hasCaptionFadeIn = true,
            durationInMillis = 7500,
            spriteAnimations = new List<DialogCutsceneSpriteAnimation>()
        });

        _dialogTotalMs = 0;
        foreach (var step in steps)
        {
            _dialogTotalMs += step.durationInMillis;
        }

        dialogCutscene = new DialogCutsceneController(steps, Color.Black, _font, Core.GraphicsDevice);
    }

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
                    // A palavra não cabe na linha atual, então começa uma nova.
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

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        // Atualiza animação e câmera
        _slime.Update(gameTime);
        _bat.Update(gameTime);
        _camera.Follow(_slimePosition, Core.GraphicsDevice.Viewport);

        CheckAudioInput();
        if (!_isDialogRunning)
        {
            CheckKeyboardInput();
        }
        CheckWallCollisions();

        // Círculo de colisão do jogador
        Circle slimeBounds = new Circle(
            (int)(_slimePosition.X + (_slime.Width * 0.5f)),
            (int)(_slimePosition.Y + (_slime.Height * 0.5f)),
            (int)(_slime.Width * 0.5f)
        );

        int tileWidth = (int)_slimeWall.Width;
        int tileHeight = (int)_slimeWall.Height;

        // Círculo da Saída Esquerda (GameScene)
        int leftExitCol = 1;
        int leftExitRow = 8;
        Circle leftExitBounds = new Circle(
            (int)(leftExitCol * tileWidth + tileWidth * 0.5f),
            (int)(leftExitRow * tileHeight + tileHeight * 0.5f),
            (int)(tileWidth * 0.5f)
        );

        // Círculo da Saída Direita (Caldeirão)
        int rightExitCol = 18;
        int rightExitRow = 8;
        Circle rightExitBounds = new Circle(
            (int)(rightExitCol * tileWidth + tileWidth * 0.5f),
            (int)(rightExitRow * tileHeight + tileHeight * 0.5f),
            (int)(tileWidth * 0.5f)
        );

        _isAtLeftExit = slimeBounds.Intersects(leftExitBounds);
        _isAtRightExit = slimeBounds.Intersects(rightExitBounds);
        if (_isDialogRunning)
        {
            _dialogRemainingMs -= gameTime.ElapsedGameTime.TotalMilliseconds;
            if (_dialogRemainingMs <= 0)
            {
                _isDialogRunning = false;
            }
        }

    
        Circle talkBounds = new Circle(
            (int)(_batPosition.X + (_bat.Width * 0.5f)),
            (int)(_batPosition.Y + (_bat.Height * 0.5f)),
            (int)(_bat.Width * 1.0f)
        );

        _canTalk = !_isDialogRunning && slimeBounds.Intersects(talkBounds);

        if (_canTalk && Core.Input.Keyboard.WasKeyJustPressed(Keys.E))
        {
            Core.Audio.PlaySoundEffect(_collectSoundEffect);

            _isDialogRunning = true;
            _dialogRemainingMs = _dialogTotalMs;
            dialogCutscene.Start();
        }

        // Trata a troca de cena
        if (_isAtLeftExit && Core.Input.Keyboard.WasKeyJustPressed(Keys.E))
        {
            Core.ChangeScene(new GameScene());
            return;
        }

        if (_isAtRightExit && Core.Input.Keyboard.WasKeyJustPressed(Keys.E))
        {
            Core.ChangeScene(new Scene_caldeirao());
            return;
        }
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
    }

    private void CheckAudioInput()
    {
        // Tecla M: liga/desliga o mudo.
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

        _bat.Draw(Core.SpriteBatch, _batPosition);
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

        // Renderiza o prompt de texto de acordo com a posição do jogador
        if (_canTalk)
        {
            Vector2 textSize = _font.MeasureString(PRESS_E_TALK);
            Vector2 pos = new Vector2(
                (Core.GraphicsDevice.PresentationParameters.BackBufferWidth - textSize.X) * 0.5f,
                Core.GraphicsDevice.PresentationParameters.BackBufferHeight - 100
            );
            Core.SpriteBatch.DrawString(_font, PRESS_E_TALK, pos, Color.Yellow);
        }
        else if (_isAtLeftExit)
        {
            Vector2 textSize = _font.MeasureString(PRESS_E_GAMESCENE);
            Vector2 pos = new Vector2(
                (Core.GraphicsDevice.PresentationParameters.BackBufferWidth - textSize.X) * 0.5f,
                Core.GraphicsDevice.PresentationParameters.BackBufferHeight - 100
            );
            Core.SpriteBatch.DrawString(_font, PRESS_E_GAMESCENE, pos, Color.Yellow);
        }
        else if (_isAtRightExit)
        {
            Vector2 textSize = _font.MeasureString(PRESS_E_CALDEIRAO);
            Vector2 pos = new Vector2(
                (Core.GraphicsDevice.PresentationParameters.BackBufferWidth - textSize.X) * 0.5f,
                Core.GraphicsDevice.PresentationParameters.BackBufferHeight - 100
            );
            Core.SpriteBatch.DrawString(_font, PRESS_E_CALDEIRAO, pos, Color.Yellow);
        }

        Core.SpriteBatch.End();

        Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
        dialogCutscene.Draw(Core.SpriteBatch, gameTime);
        Core.SpriteBatch.End();

        base.Draw(gameTime);
    }
}