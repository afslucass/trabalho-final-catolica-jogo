using System;
using System.Collections.Generic;
using System.Text;
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
using GameCore.DialogCutscene;

namespace TestGame.Scenes;

public class Scene_assombrado : Scene
{
    private Sprite _background_RECEPCAO;
    private float _cameraX;

    // Character foot position, measured in background pixels.
    private const float GROUND_Y = 1010f;

    // ---------- Player ----------
    private AnimatedSprite _player;
    private AnimatedSprite _playerLeft;
    private AnimatedSprite _playerRight;
    private float _playerX;
    private const float PLAYER_SPEED = 450f;
    private const float SPRINT_MULTIPLIER = 1.6f;
    private const float WORLD_MARGIN = 200f;

    private const string PLAYER_LEFT_ANIMATION = "player-2-animation";
    private const string PLAYER_RIGHT_ANIMATION = "player-1-animation";

    private Sprite _dollBase;
    private Sprite _dollHead;
    private const float DOLL_X = 1100f;
    private const float DOLL_INTERACTION_RANGE = 170f;
    private bool _maskCollected;

    // ---------- Raposo ----------
    private AnimatedSprite _raposo;
    private float _raposoX;
    private const string RAPOSO_ANIMATION = "hooded-animation";
    private const float TALK_RANGE = 330f;
    private const float RAPOSO_CHASE_SPEED = 270f; // Mais lento que o jogador (450 px/s).
    private bool _hasTalkedToRaposo;
    private bool _raposoChasing;

    // ---------- Dialogs ----------
    private class Talk
    {
        public DialogCutsceneController Dialog;
        public double TotalMs;
        public Action OnFinished;
    }

    private Talk _raposoTalk;
    private Talk _activeTalk;
    private double _talkRemainingMs;
    private bool _canTalk;
    private const float DIALOG_MAX_WIDTH_RATIO = 0.8f;

    private SpriteFont _font;

    // ---------- Exit ----------
    private const string PRESS_E_INTERECT = "Press E para ir à Recepção";
    private const string PRESS_E_TALK = "Press E para conversar";
    private const string PRESS_E_COLLECT_MASK = "Press E para coletar a máscara";
    private Vector2 _pressEInterectPos;
    private const float EXIT_RANGE_RECEPCAO = 150f;
    private const float EXIT_RATIO = 0.900f;
    private bool _isAtExit;
    private bool _isNearMask;
    private float _recepcaoExitX;

    public override void Initialize()
    {
        base.Initialize();

        // Jogador nasce próximo à porta do lado direito.
        _playerX = _background_RECEPCAO.Width * 0.90f;
        _player = _playerLeft;

        Vector2 size = _font.MeasureString(PRESS_E_INTERECT);
        _pressEInterectPos = new Vector2(
            Core.GraphicsDevice.PresentationParameters.BackBufferWidth / 2f,
            Core.GraphicsDevice.PresentationParameters.BackBufferHeight - 100f
        );

        UpdateCamera(0f, snap: true);
    }

    public override void LoadContent()
    {
        base.LoadContent();

        TextureAtlas charactersAtlas = TextureAtlas.FromFile(Content, "images/characters-atlas-definition.xml");
        TextureAtlas atlas = TextureAtlas.FromFile(Content, "images/atlas-definition.xml");

        _background_RECEPCAO = charactersAtlas.CreateSprite("bg-sala3");
        _background_RECEPCAO.Scale = Vector2.One;

        // A boneca começa sem a cabeça/máscara. Ao coletar, troca para doll-head.
        _dollBase = charactersAtlas.CreateSprite("doll-base");
        _dollBase.Scale = Vector2.One;
        _dollHead = charactersAtlas.CreateSprite("doll-head");
        _dollHead.Scale = Vector2.One;
        // A boneca sempre começa com doll-base ao entrar nesta sala.
        // Ela muda para doll-head somente depois que o jogador coleta a máscara.
        _maskCollected = false;

        _playerLeft = charactersAtlas.CreateAnimatedSprite(PLAYER_LEFT_ANIMATION);
        _playerLeft.Scale = Vector2.One;
        _playerRight = charactersAtlas.CreateAnimatedSprite(PLAYER_RIGHT_ANIMATION);
        _playerRight.Scale = Vector2.One;
        _player = _playerRight;

        _raposo = charactersAtlas.CreateAnimatedSprite(RAPOSO_ANIMATION);
        _raposo.Scale = Vector2.One;

        // Mantém a posição visual que o Raposo tinha no arquivo original.
        _raposoX = _raposo.Width;

        // A saída para a recepção fica no lado direito da sala.
        _recepcaoExitX = _background_RECEPCAO.Width * EXIT_RATIO;

        _font = Content.Load<SpriteFont>("fonts/KiwiSoda");
        _font.DefaultCharacter = _font.Characters.Contains('?') ? '?' : ' ';

        BuildDialogs();
    }

    // Cria a conversa do Raposo. Para alterar as falas, edite os textos abaixo.
    private void BuildDialogs()
    {
        List<DialogCutsceneSteps> steps = new();

        steps.Add(new DialogCutsceneSteps
        {
            caption = Wrap("Olá desconhecido… Poderia te chamar de amigo?"),
            hasCaptionFadeIn = true,
            durationInMillis = 3500,
            spriteAnimations = new List<DialogCutsceneSpriteAnimation>()
        });

        steps.Add(new DialogCutsceneSteps
        {
            caption = Wrap("Sabe quem eu sou?"),
            hasCaptionFadeIn = true,
            durationInMillis = 3500,
            spriteAnimations = new List<DialogCutsceneSpriteAnimation>()
        });

        steps.Add(new DialogCutsceneSteps
        {
            caption = Wrap("Só mais um dos estranhos que habitam esse lugar…"),
            hasCaptionFadeIn = true,
            durationInMillis = 4000,
            spriteAnimations = new List<DialogCutsceneSpriteAnimation>()
        });

        steps.Add(new DialogCutsceneSteps
        {
            caption = Wrap("Você não é como nós… Um mero humano… Há eras que não tenho algum petisco."),
            hasCaptionFadeIn = true,
            durationInMillis = 4000,
            spriteAnimations = new List<DialogCutsceneSpriteAnimation>()
        });

        steps.Add(new DialogCutsceneSteps
        {
            caption = Wrap("Me diga amigo… Gostaria de dar uma boa olhada em meu estômago?"),
            hasCaptionFadeIn = true,
            durationInMillis = 3500,
            spriteAnimations = new List<DialogCutsceneSpriteAnimation>()
        });

        // Ao terminar a última fala, Raposo começa a perseguir o jogador.
        _raposoTalk = CreateTalk(steps, StartRaposoChase);
    }

    private void StartRaposoChase()
    {
        _hasTalkedToRaposo = true;
        _raposoChasing = true;
    }

    private Talk CreateTalk(List<DialogCutsceneSteps> steps, Action onFinished)
    {
        double total = 0;
        foreach (DialogCutsceneSteps step in steps)
            total += step.durationInMillis;

        return new Talk
        {
            Dialog = new DialogCutsceneController(steps, Color.White, _font, Core.GraphicsDevice),
            TotalMs = total,
            OnFinished = onFinished
        };
    }

    // Quebra as falas em linhas para que não ultrapassem a largura da tela.
    private string Wrap(string text)
    {
        float maxWidth = Core.GraphicsDevice.PresentationParameters.BackBufferWidth * DIALOG_MAX_WIDTH_RATIO;
        float spaceWidth = _font.MeasureString(" ").X;
        StringBuilder result = new();

        string[] paragraphs = text.Split('\n');
        for (int p = 0; p < paragraphs.Length; p++)
        {
            if (p > 0)
                result.Append('\n');

            float lineWidth = 0f;
            string[] words = paragraphs[p].Split(' ', StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < words.Length; i++)
            {
                float wordWidth = _font.MeasureString(words[i]).X;
                float requiredWidth = lineWidth == 0f ? wordWidth : spaceWidth + wordWidth;

                if (lineWidth + requiredWidth > maxWidth && lineWidth > 0f)
                {
                    result.Append('\n');
                    lineWidth = 0f;
                }
                else if (lineWidth > 0f)
                {
                    result.Append(' ');
                    lineWidth += spaceWidth;
                }

                result.Append(words[i]);
                lineWidth += wordWidth;
            }
        }

        return result.ToString();
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        _player.Update(gameTime);
        _raposo.Update(gameTime);

        // Mantém os controles de áudio disponíveis mesmo durante a conversa.
        CheckAudioInput();

        // O jogador só fica impedido de andar durante o diálogo.
        if (_activeTalk == null)
            CheckKeyboardInput(dt);

        // Depois da conversa, Raposo persegue o jogador, mas é mais lento.
        if (_raposoChasing)
        {
            float distanceToPlayer = _playerX - _raposoX;
            if (MathF.Abs(distanceToPlayer) > 2f)
                _raposoX += MathF.Sign(distanceToPlayer) * RAPOSO_CHASE_SPEED * dt;

            _raposoX = MathHelper.Clamp(_raposoX, WORLD_MARGIN, _background_RECEPCAO.Width - WORLD_MARGIN);
        }

        UpdateCamera(dt, snap: false);

        // Atualiza o tempo da conversa. Ao terminar, o callback inicia a perseguição.
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

        _canTalk = !_hasTalkedToRaposo && _activeTalk == null && MathF.Abs(_playerX - _raposoX) < TALK_RANGE;
        float dollCenterX = DOLL_X + _dollBase.Width * 0.5f;
        _isNearMask = _raposoChasing && !_maskCollected && MathF.Abs(_playerX - dollCenterX) < DOLL_INTERACTION_RANGE;
        _isAtExit = MathF.Abs(_playerX - _recepcaoExitX) < EXIT_RANGE_RECEPCAO;

        // A tecla E coleta a máscara, inicia a conversa ou permite sair.
        // A saída continua disponível mesmo que a máscara não seja coletada.
        if (_activeTalk == null && Core.Input.Keyboard.WasKeyJustPressed(Keys.E))
        {
            if (_isNearMask)
            {
                CollectMask();
            }
            else if (_canTalk)
            {
                StartTalk(_raposoTalk);
            }
            else if (_isAtExit)
            {
                Core.ChangeScene(new Scene_recepcao(RecepcaoSpawn.FromAssombrado));
                return;
            }
        }
    }

    private void CollectMask()
    {
        if (_maskCollected)
            return;

        // Registra a máscara no inventário. A boneca passa a mostrar doll-head.
        GameSession.Inventory.CollectItem(GameItems.MaskId, GameItems.MaskIcon);
        _maskCollected = true;
    }

    private void StartTalk(Talk talk)
    {
        _activeTalk = talk;
        _talkRemainingMs = talk.TotalMs;
        talk.Dialog.Start();

        // Vira o jogador na direção de Raposo antes de iniciar a conversa.
        _player = _raposoX < _playerX ? _playerLeft : _playerRight;
    }

    private void CheckKeyboardInput(float dt)
    {
        float speed = PLAYER_SPEED;
        if (Core.Input.Keyboard.IsKeyDown(Keys.Space))
            speed *= SPRINT_MULTIPLIER;

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

        _playerX = MathHelper.Clamp(_playerX, WORLD_MARGIN, _background_RECEPCAO.Width - WORLD_MARGIN);

    }

    private void CheckAudioInput()
    {
        if (Core.Input.Keyboard.WasKeyJustPressed(Keys.M))
            Core.Audio.ToggleMute();

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

    private float GetWorldScale()
    {
        float zoom = 1.25f;
        return (Core.GraphicsDevice.PresentationParameters.BackBufferHeight / (float)_background_RECEPCAO.Height) * zoom;
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
        float cameraY = 150f;
        Matrix worldMatrix =
            Matrix.CreateTranslation(-_cameraX, -cameraY, 0f) *
            Matrix.CreateScale(scale, scale, 1f);

        Core.SpriteBatch.Begin(samplerState: SamplerState.LinearClamp, transformMatrix: worldMatrix);

        _background_RECEPCAO.Draw(Core.SpriteBatch, Vector2.Zero);
        Sprite dollSprite = _maskCollected ? _dollBase : _dollHead;
        dollSprite.Draw(Core.SpriteBatch, new Vector2(DOLL_X, GROUND_Y - dollSprite.Height - 100));
        _raposo.Draw(Core.SpriteBatch, new Vector2(_raposoX - _raposo.Width * 0.5f, GROUND_Y - _raposo.Height));
        _player.Draw(Core.SpriteBatch, new Vector2(_playerX - _player.Width * 0.5f, GROUND_Y - _player.Height));

        Core.SpriteBatch.End();

        // Interface: inventário e instruções de interação.
        Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
        GameSession.Inventory.Draw(Core.SpriteBatch, new Vector2(60, 60));

        if (_activeTalk == null)
        {
            string prompt = _canTalk
                ? PRESS_E_TALK
                : (_isNearMask ? PRESS_E_COLLECT_MASK : (_isAtExit ? PRESS_E_INTERECT : null));
            if (prompt != null)
            {
                Vector2 textSize = _font.MeasureString(prompt);
                Vector2 position = new Vector2(
                    (Core.GraphicsDevice.PresentationParameters.BackBufferWidth - textSize.X) * 0.5f,
                    Core.GraphicsDevice.PresentationParameters.BackBufferHeight - 100f
                );
                Core.SpriteBatch.DrawString(_font, prompt, position, Color.Yellow);
            }
        }

        Core.SpriteBatch.End();

        // Desenha somente o diálogo que está ativo.
        if (_activeTalk != null)
        {
            Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _activeTalk.Dialog.Draw(Core.SpriteBatch, gameTime);
            Core.SpriteBatch.End();
        }

        base.Draw(gameTime);
    }
}
