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

/// <summary>
/// Vagão assombrado (puzzle do Estranho / Raposo), conforme o storyboard:
///  1. O jogador entra pela direita e precisa falar com o Estranho para iniciar o puzzle.
///     Antes disso nenhum item pode ser coletado.
///  2. Ao fim da conversa o Estranho persegue o jogador, com metade da velocidade dele.
///  3. Item 1 (máscara da boneca) fica aqui, o item 2 na recepção e o item 3 na Caldeira.
///  4. Cenário 1: o jogador tenta passar por uma sala sem o item necessário. O Vigia o leva
///     de volta para cá (a recepção faz isso). Ele nasce no lado esquerdo, junto do Estranho,
///     e a velocidade do Estranho aumenta a cada volta.
///  5. Cenário 2: o Estranho alcança o jogador -> game over, que volta ao primeiro checkpoint
///     (o momento em que o Vigia colocou Alma no vagão).
///  6. Com o disfarce completo (3 itens) o Estranho deixa o jogador passar para o último vagão.
/// </summary>
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

    // ---------- Estranho (Raposo) ----------
    private AnimatedSprite _raposo;
    private Sprite _raposoHead;
    private float _raposoX;
    private const string RAPOSO_ANIMATION = "hooded-animation";
    private const string RAPOSO_HEAD_SPRITE = "hooded-head";
    private const float TALK_RANGE = 330f;

    // Velocidade da perseguição: no começo é a metade da velocidade do jogador.
    // A cada volta ao vagão sem o item necessário (cenário 1) ganha RAPOSO_SPEED_PER_RETRY,
    // até o limite RAPOSO_MAX_SPEED (abaixo do sprint do jogador, 720 px/s, para sempre
    // ser possível fugir).
    private const float RAPOSO_BASE_SPEED = PLAYER_SPEED * 0.5f; // 225 px/s
    private const float RAPOSO_SPEED_PER_RETRY = 90f;
    private const float RAPOSO_MAX_SPEED = 650f;
    private const float CATCH_DISTANCE = 100f; // Distância em que o Estranho pega o jogador.

    private float RaposoSpeed => MathF.Min(
        RAPOSO_BASE_SPEED + GameSession.HauntedRetries * RAPOSO_SPEED_PER_RETRY,
        RAPOSO_MAX_SPEED);

    // Tempo de espera entre o fim da conversa (ou o nascimento na sala) e o Estranho
    // realmente andar/pegar, para o jogador perceber que a perseguição começou.
    private const double CHASE_START_DELAY_MS = 1800;
    private double _chaseDelayMs;

    private bool _hasTalkedToRaposo; // O puzzle já foi iniciado (a conversa já aconteceu).
    private bool _raposoChasing;
    private bool _hasFullDisguise;   // Tem os 3 itens: o Estranho não persegue mais.
    private bool _passUnlocked;      // O Estranho já liberou a passagem para o último vagão.

    // true quando o jogador voltou à sala por ter tentado passar sem o item necessário:
    // nasce no lado esquerdo, junto do Estranho, e a perseguição começa logo em seguida.
    private readonly bool _isRetryChase;

    // ---------- Game over ----------
    private enum SceneState { Playing, GameOver }
    private SceneState _state = SceneState.Playing;
    private double _stateRemainingMs;
    private const double GAME_OVER_MS = 3000;
    private const string GAME_OVER_TEXT = "GAME OVER";
    private Texture2D _pixel;

    // ---------- Dialogs ----------
    private class Talk
    {
        public DialogCutsceneController Dialog;
        public double TotalMs;
        public Action OnFinished;
    }
    Color Alloy = new Color(201, 149, 0);

    private Talk _raposoTalk;    // Conversa que inicia o puzzle e a perseguição.
    private Talk _disguiseTalk;  // Disfarce completo: o Estranho deixa passar.
    private Talk _activeTalk;
    private double _talkRemainingMs;
    private bool _canTalk;
    private const float DIALOG_MAX_WIDTH_RATIO = 0.8f;

    private SpriteFont _font;

    // ---------- Exit ----------
    private const string PRESS_E_INTERECT = "Press E para ir à Recepção";
    private const string PRESS_E_TALK = "Press E para conversar";
    private const string PRESS_E_COLLECT_MASK = "Press E para coletar a máscara";
    private const string PRESS_E_NEXT = "Press E para ir ao próximo vagão";
    private Vector2 _pressEInterectPos;
    private const float EXIT_RANGE_RECEPCAO = 150f;
    private const float EXIT_RATIO = 0.900f;
    private const float EXIT_RANGE_NEXT = 150f; // Porta do último vagão: no lado esquerdo, atrás do Estranho.
    private bool _isAtExit;
    private bool _isAtNextDoor;
    private bool _isNearMask;
    private float _recepcaoExitX;

    public Scene_assombrado(bool isRetryChase = false)
    {
        _isRetryChase = isRetryChase;
    }

    public override void Initialize()
    {
        base.Initialize();

        _hasTalkedToRaposo = GameSession.HauntedPuzzleStarted;
        _hasFullDisguise = GameItems.HasFullDisguise;
        _passUnlocked = GameSession.Stage >= StoryStage.Final;

        if (_hasFullDisguise)
        {
            // Disfarce completo: nasce na porta da direita e o Estranho não persegue.
            _playerX = _recepcaoExitX;
            _player = _playerLeft;
        }
        else if (_isRetryChase)
        {
            // Cenário 1: nasce no lado esquerdo, junto do Estranho, e a perseguição começa
            // logo em seguida (mais rápida a cada volta).
            _playerX = _raposoX + 250f;
            _player = _playerRight;
            _hasTalkedToRaposo = true;
            _raposoChasing = true;
            _chaseDelayMs = CHASE_START_DELAY_MS;
        }
        else
        {
            // Jogador nasce próximo à porta do lado direito.
            _playerX = _recepcaoExitX;
            _player = _playerLeft;

            if (_hasTalkedToRaposo)
            {
                // Voltou à sala com o puzzle já iniciado: o Estranho persegue de novo.
                _raposoChasing = true;
                _chaseDelayMs = CHASE_START_DELAY_MS;
            }
            else
            {
                // Primeira visita: o checkpoint é o momento em que o Vigia colocou Alma aqui.
                GameSession.EnsureCheckpoint(_playerX);
            }
        }

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

        // A boneca começa com a cabeça/máscara. Ao coletar, troca para doll-base.
        _dollBase = charactersAtlas.CreateSprite("doll-base");
        _dollBase.Scale = Vector2.One;
        _dollHead = charactersAtlas.CreateSprite("doll-head");
        _dollHead.Scale = Vector2.One;
        // Se o jogador já tem a máscara no inventário, a boneca continua sem a cabeça.
        _maskCollected = GameSession.Inventory.Has(GameItems.MaskId);

        _pixel = new Texture2D(Core.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        _playerLeft = charactersAtlas.CreateAnimatedSprite(PLAYER_LEFT_ANIMATION);
        _playerLeft.Scale = Vector2.One;
        _playerRight = charactersAtlas.CreateAnimatedSprite(PLAYER_RIGHT_ANIMATION);
        _playerRight.Scale = Vector2.One;
        _player = _playerRight;

        _raposo = charactersAtlas.CreateAnimatedSprite(RAPOSO_ANIMATION);
        _raposo.Scale = Vector2.One;

        // Cabeça utilizada quando a perseguição começa.
        _raposoHead = charactersAtlas.CreateSprite(RAPOSO_HEAD_SPRITE);
        _raposoHead.Scale = Vector2.One;

        // Mantém a posição visual que o Raposo tinha no arquivo original (lado esquerdo).
        _raposoX = _raposo.Width;

        // A saída para a recepção fica no lado direito da sala.
        _recepcaoExitX = _background_RECEPCAO.Width * EXIT_RATIO;

        _font = Content.Load<SpriteFont>("fonts/KiwiSoda");
        _font.DefaultCharacter = _font.Characters.Contains('?') ? '?' : ' ';

        BuildDialogs();
    }

    public override void UnloadContent()
    {
        _pixel?.Dispose();
        base.UnloadContent();
    }

    // Cria as conversas do Estranho. Para alterar as falas, edite os textos abaixo.
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

        // Ao terminar a última fala, o puzzle começa e o Estranho persegue o jogador.
        _raposoTalk = CreateTalk(steps, StartRaposoChase);

        // Disfarce completo: o Estranho se engana e libera a passagem (texto provisório).
        List<DialogCutsceneSteps> disguiseSteps = new();

        disguiseSteps.Add(new DialogCutsceneSteps
        {
            caption = Wrap("Hmm… Esse rosto… essa roupa… esse cabelo…"),
            hasCaptionFadeIn = true,
            durationInMillis = 3500,
            spriteAnimations = new List<DialogCutsceneSpriteAnimation>()
        });

        disguiseSteps.Add(new DialogCutsceneSteps
        {
            caption = Wrap("Ora, ora… Um dos nossos! Perdoe a minha desconfiança, amigo."),
            hasCaptionFadeIn = true,
            durationInMillis = 3500,
            spriteAnimations = new List<DialogCutsceneSpriteAnimation>()
        });

        disguiseSteps.Add(new DialogCutsceneSteps
        {
            caption = Wrap("Pode passar. A próxima porta está aberta para você."),
            hasCaptionFadeIn = true,
            durationInMillis = 3500,
            spriteAnimations = new List<DialogCutsceneSpriteAnimation>()
        });

        _disguiseTalk = CreateTalk(disguiseSteps, UnlockPassage);
    }

    private void StartRaposoChase()
    {
        _hasTalkedToRaposo = true;
        GameSession.HauntedPuzzleStarted = true; // A partir daqui os itens podem ser coletados.
        _raposoChasing = true;
        _chaseDelayMs = CHASE_START_DELAY_MS;
    }

    private void UnlockPassage()
    {
        _passUnlocked = true;
        GameSession.Stage = StoryStage.Final;
    }

    private Talk CreateTalk(List<DialogCutsceneSteps> steps, Action onFinished)
    {
        double total = 0;
        foreach (DialogCutsceneSteps step in steps)
            total += step.durationInMillis;

        return new Talk
        {
            Dialog = new DialogCutsceneController(steps, Alloy, _font, Core.GraphicsDevice),
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

        // Game over: congela a cena até o tempo acabar e volta ao checkpoint.
        if (_state == SceneState.GameOver)
        {
            UpdateCamera(dt, snap: false);
            _stateRemainingMs -= gameTime.ElapsedGameTime.TotalMilliseconds;
            if (_stateRemainingMs <= 0)
                FinishGameOver();
            return;
        }

        // O jogador só fica impedido de andar durante o diálogo.
        if (_activeTalk == null)
            CheckKeyboardInput(dt);

        if (_raposoChasing && _chaseDelayMs > 0)
        {
            // Estranho ainda parado: o jogador tem um instante para se afastar.
            _chaseDelayMs -= gameTime.ElapsedGameTime.TotalMilliseconds;
        }
        else if (_raposoChasing)
        {
            float distanceToPlayer = _playerX - _raposoX;
            if (MathF.Abs(distanceToPlayer) > 2f)
                _raposoX += MathF.Sign(distanceToPlayer) * RaposoSpeed * dt;

            _raposoX = MathHelper.Clamp(_raposoX, WORLD_MARGIN, _background_RECEPCAO.Width - WORLD_MARGIN);

            // Cenário 2: o Estranho alcançou o jogador -> game over.
            if (MathF.Abs(_playerX - _raposoX) < CATCH_DISTANCE)
            {
                StartGameOver();
                return;
            }
        }

        UpdateCamera(dt, snap: false);

        // Atualiza o tempo da conversa. Ao terminar, o callback é executado.
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

        bool nearRaposo = MathF.Abs(_playerX - _raposoX) < TALK_RANGE;
        bool canStartPuzzle = !_hasTalkedToRaposo && !_hasFullDisguise;
        bool canShowDisguise = _hasFullDisguise && !_passUnlocked;
        _canTalk = _activeTalk == null && nearRaposo && (canStartPuzzle || canShowDisguise);

        float dollCenterX = DOLL_X + _dollBase.Width * 0.5f;
        // Itens só podem ser coletados depois de iniciar o puzzle (_raposoChasing).
        _isNearMask = _raposoChasing && !_maskCollected && MathF.Abs(_playerX - dollCenterX) < DOLL_INTERACTION_RANGE;
        _isAtNextDoor = _passUnlocked && MathF.Abs(_playerX - WORLD_MARGIN) < EXIT_RANGE_NEXT;
        _isAtExit = MathF.Abs(_playerX - _recepcaoExitX) < EXIT_RANGE_RECEPCAO;

        // A tecla E coleta a máscara, conversa com o Estranho ou permite sair.
        if (_activeTalk == null && Core.Input.Keyboard.WasKeyJustPressed(Keys.E))
        {
            if (_isNearMask)
            {
                CollectMask();
            }
            else if (_canTalk)
            {
                StartTalk(_hasFullDisguise ? _disguiseTalk : _raposoTalk);
            }
            else if (_isAtNextDoor)
            {
                Core.ChangeScene(new Scene_final());
                return;
            }
            else if (_isAtExit)
            {
                // Sair sem a máscara é permitido: quem barra o jogador é o Vigia, na
                // recepção (cenário 1), que o traz de volta para cá.
                Core.ChangeScene(new Scene_recepcao(RecepcaoSpawn.FromAssombrado));
                return;
            }
        }
    }

    // Cenário 2: o Estranho alcançou o jogador.
    private void StartGameOver()
    {
        _state = SceneState.GameOver;
        _stateRemainingMs = GAME_OVER_MS;
    }

    // O game over volta ao primeiro checkpoint: o momento em que o Vigia colocou Alma
    // no vagão assombrado. O puzzle recomeça do zero a partir dali.
    private void FinishGameOver()
    {
        GameSession.RestoreCheckpoint();
        Core.ChangeScene(new Scene_recepcao(RecepcaoSpawn.FromAssombrado));
    }

    private void CollectMask()
    {
        if (_maskCollected)
            return;

        // Registra a máscara no inventário. A boneca passa a mostrar doll-base.
        GameSession.Inventory.CollectItem(GameItems.MaskId, GameItems.MaskIcon);
        _maskCollected = true;
    }

    private void StartTalk(Talk talk)
    {
        _activeTalk = talk;
        _talkRemainingMs = talk.TotalMs;
        talk.Dialog.Start();

        // Vira o jogador na direção do Estranho antes de iniciar a conversa.
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

    private void DrawGameOverOverlay()
    {
        int w = Core.GraphicsDevice.PresentationParameters.BackBufferWidth;
        int h = Core.GraphicsDevice.PresentationParameters.BackBufferHeight;

        float elapsed = (float)((GAME_OVER_MS - _stateRemainingMs) / GAME_OVER_MS);
        float alpha = MathHelper.Clamp(elapsed * 3f, 0f, 1f) * 0.85f;

        Vector2 size = _font.MeasureString(GAME_OVER_TEXT);

        Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
        Core.SpriteBatch.Draw(_pixel, new Rectangle(0, 0, w, h), Color.Black * alpha);
        Core.SpriteBatch.DrawString(_font, GAME_OVER_TEXT, new Vector2((w - size.X) * 0.5f, (h - size.Y) * 0.5f), Color.Red);
        Core.SpriteBatch.End();
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
        if (_raposoChasing)
        {
            _raposoHead.Draw(
                Core.SpriteBatch,
                new Vector2(_raposoX - _raposoHead.Width * 0.5f, GROUND_Y - _raposoHead.Height));
        }
        else
        {
            _raposo.Draw(
                Core.SpriteBatch,
                new Vector2(_raposoX - _raposo.Width * 0.5f, GROUND_Y - _raposo.Height));
        }

        _player.Draw(Core.SpriteBatch, new Vector2(_playerX - _player.Width * 0.5f, GROUND_Y - _player.Height));

        Core.SpriteBatch.End();

        // Interface: inventário e instruções de interação.
        Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
        GameSession.Inventory.Draw(Core.SpriteBatch, new Vector2(60, 60));

        if (_activeTalk == null && _state == SceneState.Playing)
        {
            string prompt = null;
            if (_canTalk)
                prompt = PRESS_E_TALK;
            else if (_isNearMask)
                prompt = PRESS_E_COLLECT_MASK;
            else if (_isAtNextDoor)
                prompt = PRESS_E_NEXT;
            else if (_isAtExit)
                prompt = PRESS_E_INTERECT;

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

        // Tela de game over: escurece a sala e mostra a mensagem.
        if (_state == SceneState.GameOver)
            DrawGameOverOverlay();

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
