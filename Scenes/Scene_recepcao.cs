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

    // ---------- Jogador ----------
    // O jogador usa duas sprites espelhadas: uma olhando para cada lado.
    // _slime aponta sempre para a que está ativa (a do lado para onde ele anda).
    private AnimatedSprite _slime;
    private AnimatedSprite _slimeLeft;
    private AnimatedSprite _slimeRight;
    private Vector2 _slimePosition;
    private const float MOVEMENT_SPEED = 5.0f;

    private const string PLAYER_LEFT_ANIMATION = "player-2-animation";
    private const string PLAYER_RIGHT_ANIMATION = "player-1-animation";

    private const float PLAYER_SCALE = 0.33f;
    private const float VIGIA_SCALE = 0.18f;
    private const float SECOND_NPC_SCALE = 0.15f;

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

    // ---------- NPCs de conversa ----------
    // Um NPC guarda o sprite, a posição, o diálogo dele e a duração total desse diálogo.
    private class DialogNpc
    {
        public AnimatedSprite Sprite;
        public Vector2 Position;
        public DialogCutsceneController Dialog;
        public double TotalMs;
    }
    private const int NPC_ROW = 4;
    private const int VIGIA_COL = 4;
    private const int SECOND_NPC_COL = 14;

    // Animação do atlas de personagens usada por cada NPC.
    private const string VIGIA_ANIMATION = "hooded-animation";
    private const string SECOND_NPC_ANIMATION = "owl-1-animation";

    private DialogNpc _vigia;       
    private DialogNpc _secondNpc;   

    private DialogNpc _activeNpc;   
    private DialogNpc _talkNpc;     
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

        // Atlas antigo: continua fornecendo a parede ("slime-2") e os itens.
        TextureAtlas atlas = TextureAtlas.FromFile(Content, "images/atlas-definition.xml");

        // Atlas novo (spritesheet_final): jogador e NPCs.
        TextureAtlas charactersAtlas = TextureAtlas.FromFile(Content, "images/characters-atlas-definition.xml");

        MouseCursorAtlas cursorAtlas = MouseCursorAtlas.FromFile(Content, "cursor/cursor-atlas-definition.xml");

        _slimeWall = atlas.CreateSprite("slime-2");
        _slimeWall.Scale = new Vector2(4.0f, 4.0f);
        _slimeLeft = charactersAtlas.CreateAnimatedSprite(PLAYER_LEFT_ANIMATION);
        _slimeLeft.Scale = new Vector2(PLAYER_SCALE, PLAYER_SCALE);
        _slimeRight = charactersAtlas.CreateAnimatedSprite(PLAYER_RIGHT_ANIMATION);
        _slimeRight.Scale = new Vector2(PLAYER_SCALE, PLAYER_SCALE);
        _slime = _slimeRight;

        _collectSoundEffect = Content.Load<SoundEffect>("audio/collect");
        _font = Content.Load<SpriteFont>("fonts/04B_30");

        // Rede de segurança: um caractere que a fonte não desenha é trocado por '?'
         _font.DefaultCharacter = _font.Characters.Contains('?') ? '?' : ' ';

        cursorAtlas.SetCursor("cheese");

        //matriz de colisão das paredes
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
        _vigia = new DialogNpc
        {
            Sprite = CreateNpcSprite(charactersAtlas, VIGIA_ANIMATION, VIGIA_SCALE)
        };
        _vigia.Position = NpcPosition(VIGIA_COL, NPC_ROW, _vigia.Sprite);
        _secondNpc = new DialogNpc
        {
            Sprite = CreateNpcSprite(charactersAtlas, SECOND_NPC_ANIMATION, SECOND_NPC_SCALE)
        };
        _secondNpc.Position = NpcPosition(SECOND_NPC_COL, NPC_ROW, _secondNpc.Sprite);

        List<DialogCutsceneSteps> vigiaSteps = new();

        vigiaSteps.Add(new DialogCutsceneSteps
        {
            caption = Wrap("AUTO LÁ! O que é isso que vejo? Um novo aperitivo? A grande Morte nunca falha... Bem-vindo! Bem-vindo! Bem-vindo ao meio termo! O Limbo abriga aqueles que estão de passagem."),
            hasCaptionFadeIn = true,
            durationInMillis = 12000,
            spriteAnimations = new List<DialogCutsceneSpriteAnimation>()
        });
        vigiaSteps.Add(new DialogCutsceneSteps
        {
            caption = Wrap("Sou o grandioso VIGIA, tudo vejo, tudo sei. Eu te conheço de muito antes Alma, desdo seu nascimento, até o seu último suspiro..."),
            hasCaptionFadeIn = true,
            durationInMillis = 10000,
            spriteAnimations = new List<DialogCutsceneSpriteAnimation>()
        });
        vigiaSteps.Add(new DialogCutsceneSteps
        {
            caption = Wrap("De certa forma, somos íntimos, mas não se deixe levar, não somos camaradas."),
            hasCaptionFadeIn = true,
            durationInMillis = 6500,
            spriteAnimations = new List<DialogCutsceneSpriteAnimation>()
        });
        vigiaSteps.Add(new DialogCutsceneSteps
        {
            caption = Wrap("Seu corpo fraco te abandonou, você está aqui para seguir para a próxima fase do seu ciclo, assim como todos os outros antes, e como todos os próximos..."),
            hasCaptionFadeIn = true,
            durationInMillis = 11000,
            spriteAnimations = new List<DialogCutsceneSpriteAnimation>()
        });
        vigiaSteps.Add(new DialogCutsceneSteps
        {
            caption = Wrap("Alma, seu destino te aguarda a direita desse vagão, não se surpreenda pelo o que está por vir, não irá durar por muito... Só precisamos ter certeza..."),
            hasCaptionFadeIn = true,
            durationInMillis = 11000,
            spriteAnimations = new List<DialogCutsceneSpriteAnimation>()
        });
        vigiaSteps.Add(new DialogCutsceneSteps
        {
            caption = Wrap("VÁ CRIATURA, me deixe às minhas novelas, os mortos não me entretém tanto quanto os vivos."),
            hasCaptionFadeIn = true,
            durationInMillis = 7500,
            spriteAnimations = new List<DialogCutsceneSpriteAnimation>()
        });

        SetupDialog(_vigia, vigiaSteps);

        // ----- Diálogo do segundo NPC (só aparece depois de falar com o VIGIA) -----
        List<DialogCutsceneSteps> secondSteps = new();

        secondSteps.Add(new DialogCutsceneSteps
        {
            caption = Wrap("Texto do segundo NPC 1"),
            hasCaptionFadeIn = true,
            durationInMillis = 4000,
            spriteAnimations = new List<DialogCutsceneSpriteAnimation>()
        });
        secondSteps.Add(new DialogCutsceneSteps
        {
            caption = Wrap("Texto do segundo NPC 2"),
            hasCaptionFadeIn = true,
            durationInMillis = 4000,
            spriteAnimations = new List<DialogCutsceneSpriteAnimation>()
        });
        secondSteps.Add(new DialogCutsceneSteps
        {
            caption = Wrap("Texto do segundo NPC 3"),
            hasCaptionFadeIn = true,
            durationInMillis = 4000,
            spriteAnimations = new List<DialogCutsceneSpriteAnimation>()
        });

        SetupDialog(_secondNpc, secondSteps);
    }

    private AnimatedSprite CreateNpcSprite(TextureAtlas atlas, string animationName, float scale)
    {
        AnimatedSprite sprite = atlas.CreateAnimatedSprite(animationName);
        sprite.Scale = new Vector2(scale, scale);
        return sprite;
    }

    private Vector2 NpcPosition(int col, int row, AnimatedSprite sprite)
    {
        float tileX = col * _slimeWall.Width;
        float tileY = row * _slimeWall.Height;

        return new Vector2(
            tileX + (_slimeWall.Width - sprite.Width) * 0.5f,
            tileY + (_slimeWall.Height - sprite.Height)
        );
    }

    private void SetupDialog(DialogNpc npc, List<DialogCutsceneSteps> steps)
    {
        npc.TotalMs = 0;
        foreach (var step in steps)
        {
            npc.TotalMs += step.durationInMillis;
        }

        npc.Dialog = new DialogCutsceneController(steps, Color.Black, _font, Core.GraphicsDevice);
    }

    // Verifica se o slime está perto o bastante de um NPC para conversar.
    private bool IsNearNpc(Circle slimeBounds, DialogNpc npc)
    {
        Circle talkBounds = new Circle(
            (int)(npc.Position.X + (npc.Sprite.Width * 0.5f)),
            (int)(npc.Position.Y + (npc.Sprite.Height * 0.5f)),
            (int)(_slimeWall.Width * 1.0f)
        );

        return slimeBounds.Intersects(talkBounds);
    }

    // Quebra o texto em linhas para ele nunca passar das bordas da tela.
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
        _vigia.Sprite.Update(gameTime);
        _secondNpc.Sprite.Update(gameTime);
        _camera.Follow(_slimePosition, Core.GraphicsDevice.Viewport);

        // O áudio sempre funciona, mas o slime não anda enquanto um diálogo toca.
        CheckAudioInput();
        if (_activeNpc == null)
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
        if (_activeNpc != null)
        {
            _dialogRemainingMs -= gameTime.ElapsedGameTime.TotalMilliseconds;
            if (_dialogRemainingMs <= 0)
            {
                if (_activeNpc == _vigia)
                {
                    GameSession.TalkedToVigia = true;
                }

                _activeNpc = null;
            }
        }

        _talkNpc = null;
        if (_activeNpc == null)
        {
            if (IsNearNpc(slimeBounds, _vigia))
            {
                _talkNpc = _vigia;
            }
            else if (GameSession.TalkedToVigia && IsNearNpc(slimeBounds, _secondNpc))
            {
                _talkNpc = _secondNpc;
            }
        }

        // Inicia o diálogo quando a tecla E é pressionada perto de um NPC.
        if (_talkNpc != null && Core.Input.Keyboard.WasKeyJustPressed(Keys.E))
        {
            Core.Audio.PlaySoundEffect(_collectSoundEffect);

            // O jogador vira de frente para o NPC com quem vai conversar.
            float npcCenterX = _talkNpc.Position.X + (_talkNpc.Sprite.Width * 0.5f);
            float slimeCenterX = _slimePosition.X + (_slime.Width * 0.5f);
            _slime = npcCenterX < slimeCenterX ? _slimeLeft : _slimeRight;

            _activeNpc = _talkNpc;
            _dialogRemainingMs = _talkNpc.TotalMs;
            _talkNpc.Dialog.Start();
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
            _slime = _slimeLeft;
        }

        if (Core.Input.Keyboard.IsKeyDown(Keys.D) || Core.Input.Keyboard.IsKeyDown(Keys.Right))
        {
            _slimePosition.X += speed;
            _slime = _slimeRight;
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
        Core.GraphicsDevice.Clear(Color.Beige);

        // World Layer (Camera Matrix)
        Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: _camera.TransformMatrix);

        for (int row = 0; row < map.GetLength(0); row++)
        {
            for (int col = 0; col < map.GetLength(1); col++)
            {
                bool isNpcTile = row == NPC_ROW && (col == VIGIA_COL || col == SECOND_NPC_COL);

                if (map[row, col] == 1 && !isNpcTile)
                {
                    Vector2 wallPos = new Vector2(col * _slimeWall.Width, row * _slimeWall.Height);
                    _slimeWall.Draw(Core.SpriteBatch, wallPos);
                }
            }
        }

        _vigia.Sprite.Draw(Core.SpriteBatch, _vigia.Position);
        _secondNpc.Sprite.Draw(Core.SpriteBatch, _secondNpc.Position);
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
        if (_talkNpc != null)
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

        // Renderiza o diálogo por cima de tudo (cada controller só desenha enquanto o dele toca).
        Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
        _vigia.Dialog.Draw(Core.SpriteBatch, gameTime);
        _secondNpc.Dialog.Draw(Core.SpriteBatch, gameTime);
        Core.SpriteBatch.End();

        base.Draw(gameTime);
    }
}