using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using GameCore;
using GameCore.CustomMouseCursor;
using GameCore.DialogCutscene;
using GameCore.Graphics;
using GameCore.Scenes;
using GameCore.Cutscene;
using GameCore.Save;

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

public class Scene_recepcao : Scene, ISaveableScene
{
    // ---------- World ----------
    // Uses the background's native resolution (3840 x 1080). The camera applies a
    // scale so the background height matches the window height, allowing the game
    // to support any resolution. Background areas outside the window width
    // remain off-screen and are revealed as the camera scrolls.
    public string SaveId => "recepcao";

    private CutsceneController cutscene;
    private bool _showIntroCutscene;
    public Vector2 SavePlayerPosition => new Vector2(_playerX, GROUND_Y);
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
    private AnimatedSprite _vigiaItch;   // Vigia coçando (depois da 2ª parte)
    private const string VIGIA_ITCH_ANIMATION = "owl-item-animation";
    private float _vigiaX;

    private const string VIGIA_LEFT_ANIMATION = "owl-1-animation";
    private const string VIGIA_RIGHT_ANIMATION = "owl-2-animation";

    // Maximum distance (in background pixels) for interacting with the Watcher.
    private const float TALK_RANGE = 200f;

    // ---------- Vigia acompanhando ----------
    // Depois da primeira conversa (estágio FalouComVigia) o Vigia acompanha Alma pela
    // recepção. Nos outros estágios ele fica no posto (centro do vagão) e, se estiver
    // longe dele, volta andando para lá.
    private float _vigiaPostX;
    private const float VIGIA_FOLLOW_DISTANCE = 400f;  // Distância que mantém de Alma (menor que TALK_RANGE, para ainda dar para conversar).
    private const float VIGIA_FOLLOW_SPEED = 520f;     // Um pouco mais rápido que o andar de Alma, para alcançá-la.

    // Pequeno "pulinho" enquanto anda, já que a sprite do Vigia é uma imagem só.
    private const float VIGIA_BOB_HEIGHT = 14f;
    private const float VIGIA_BOB_SPEED = 9f;
    private float _vigiaWalkTime;
    private float _vigiaBobOffset;
    private readonly bool _vigiaCanFollow;

    private AnimatedSprite _justica;
    private float _justicaX;

    private const string JUSTICA_ANIMATION = "doctor-animation";

    private const float JUSTICA_FOLLOW_DISTANCE = 180f;
    private const float JUSTICA_FOLLOW_SPEED = 800f;
    private bool _justicaVisivel;
    private bool _justicaAcompanhando;

    // ---------- Exits ----------
    // The two background arches: the left one leads to the Haunted Train,
    // and the right one leads to the Boiler. Positions are ratios of background width.
    private const float LEFT_EXIT_RATIO = 0.015f;
    private const float RIGHT_EXIT_RATIO = 0.985f;
    private const float EXIT_RANGE = 320f;

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

    Color Alloy = new Color(201, 149, 0);

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
    private Talk _secondTalk;     // Judged stage: Justiça brings Alma back; the agitated Watcher sends her to the Haunted Train
    private Talk _maskTalk;       // Alma com a máscara: o Vigia a confunde com um ajudante do Estranho
    private Talk _idleTalk;       // Other stages

    private Talk _activeTalk;     // Currently playing dialog (null = none)
    private double _talkRemainingMs;
    private bool _canTalk;

    // Maximum dialog line width, as a fraction of screen width.
    private const float DIALOG_MAX_WIDTH_RATIO = 0.58f;

    // UI and audio resources.
    private SpriteFont _font;
    private SoundEffect _DoorSoundEffect;
    private bool _ambienceStarted;
    private Song _introSong;
    private const float INTRO_VOLUME = 0.45f;
    private Song _ambienceSong;
    private const float AMBIENCE_VOLUME = 1.0f;
    private bool _introMusicFinished;

    // ---------- Puzzle do Estranho ----------
    // Item 2 do disfarce (colar), no lado direito da recepção. Só pode ser coletado
    // depois de iniciar o puzzle (falar com o Estranho no vagão assombrado).
    private const float NECKLACE_X_RATIO = 0.72f;
    private const float ITEM_RANGE = 150f;
    private const string PRESS_E_COLLECT_ITEM = "Press E para pegar o item";
    private Sprite _necklaceSprite;
    private float _necklaceX;
    private bool _isNearNecklace;
    // Cenário 1: o jogador tentou passar sem o item necessário. O Vigia o reconhece e o
    // leva de volta ao vagão assombrado.
    private const float VIGIA_BLOCK_RANGE = 500f;  // Sem o item 1, não passa daqui em direção ao Vigia.
    private const double CAUGHT_MS = 3000;
    private const string CAUGHT_TEXT = "ALMA! MALDIÇÃO! Você não passará por mim, MORRA!";
    private bool _caught;
    private double _caughtRemainingMs;
    private Texture2D _pixel;

    private readonly RecepcaoSpawn _spawn;
    private readonly float? _startX;

    public Scene_recepcao(RecepcaoSpawn spawn = RecepcaoSpawn.Start, float? startX = null)
    {
        _spawn = spawn;
        _startX = startX;

        // Só acompanha na primeira visita, vindo da introdução (e não ao carregar um save).
        _vigiaCanFollow = spawn == RecepcaoSpawn.Start && !startX.HasValue;
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
                _playerX = _rightExitX - 350f;
                _player = _playerLeft;

                _justicaX = _rightExitX - 100f;

                _justicaAcompanhando = GameSession.Stage == StoryStage.Julgada;
                _justicaVisivel = _justicaAcompanhando;
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
        if (_startX.HasValue)
        {
            _playerX = MathHelper.Clamp(_startX.Value, WORLD_MARGIN, worldWidth - WORLD_MARGIN);
        }

        // Places the camera immediately, without an initial slide.
        UpdateCamera(0f, snap: true);
    }

    public override void LoadContent()
    {
        base.LoadContent();

        // Atlas containing characters, items, and backgrounds from spritesheet_final.
        TextureAtlas charactersAtlas = TextureAtlas.FromFile(Content, "images/characters-atlas-definition.xml");
        TextureAtlas cutsceneAtlas = TextureAtlas.FromFile(Content, "images/cutscene-atlas-definition.xml");
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

        // Watcher: his post is the center of the train, in front of the tree corridor.
        _vigiaLeft = charactersAtlas.CreateAnimatedSprite(VIGIA_LEFT_ANIMATION);
        _vigiaLeft.Scale = Vector2.One;
        _vigiaRight = charactersAtlas.CreateAnimatedSprite(VIGIA_RIGHT_ANIMATION);
        _vigiaRight.Scale = Vector2.One;
        _vigia = _vigiaLeft;

        _vigiaItch = charactersAtlas.CreateAnimatedSprite(VIGIA_ITCH_ANIMATION);
        _vigiaItch.Scale = Vector2.One;
        _vigiaPostX = _background.Width * 0.5f;
        _vigiaX = _vigiaPostX;
        _leftExitX = _background.Width * LEFT_EXIT_RATIO;
        _rightExitX = _background.Width * RIGHT_EXIT_RATIO;
        _justica = charactersAtlas.CreateAnimatedSprite(JUSTICA_ANIMATION);
        _justica.Scale = Vector2.One;

        _necklaceSprite = charactersAtlas.CreateSprite(GameItems.NecklaceIcon);
        _necklaceSprite.Scale = Vector2.One;
        _necklaceX = _background.Width * NECKLACE_X_RATIO;

        _pixel = new Texture2D(Core.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        _DoorSoundEffect = Content.Load<SoundEffect>("audio/portas");
        _font = Content.Load<SpriteFont>("fonts/KiwiSoda");
        _ambienceSong = Content.Load<Song>("audio/recepcao_ambience");
        _introSong = Content.Load<Song>("audio/intro_ambience");
        if (_startX.HasValue || GameSession.IntroCutscenePlayed)
{
        StartAmbienceAudio();
}
        // Fallback: unsupported font characters are replaced with '?')         
        // (or a space) to prevent ArgumentException in MeasureString/DrawString.
        // Add accented characters to the .spritefont CharacterRegion to render them correctly.
        _font.DefaultCharacter = _font.Characters.Contains('?') ? '?' : ' ';


        List<Step> steps = new List<Step>();
        steps.Add(new Step
        {
            text = "Você nem repara o quão precioso o levantar do peito, a leve indicação de respiração de algo ainda vivo, na maioria dos casos pelo menos.",
            image = cutsceneAtlas.CreateSprite("image-1"),
            fadeDuration = 1000,
            hasImageTransition = true,
            hasTextTransition = true,
            durationInMillis = 6000,
        });
        steps.Add(new Step
        {
            text = "Enquanto deitado na maca de um hospital, você consegue sentir cada parte do seu corpo parar, a dor só dura um momento.",
            image = cutsceneAtlas.CreateSprite("image-1"),
            fadeDuration = 1000,
            hasImageTransition = false,
            hasTextTransition = false,
            durationInMillis = 6000,
        });
        steps.Add(new Step
        {
            text = "Você fecha os olhos, com medo, seus lábios já incapazes de expressar suas últimas preces.",
            image = cutsceneAtlas.CreateSprite("image-1"),
            fadeDuration = 1000,
            hasImageTransition = false,
            hasTextTransition = false,
            durationInMillis = 6000,
        });
        steps.Add(new Step
        {
            text = "No painel, apenas uma parada, Limbo. Ao longe, luzes fortes, mas não é nenhum tipo de maquinário. ",
            image = cutsceneAtlas.CreateSprite("image-1"),
            fadeDuration = 1000,
            hasImageTransition = false,
            hasTextTransition = false,
            durationInMillis = 6000,
        });
        steps.Add(new Step
        {
            text = "A visão do que se parece um inseto gigante, com seu corpo formado por vagões, não te assusta, na realidade, você não parece sentir absolutamente nada. ",
            image = cutsceneAtlas.CreateSprite("image-1"),
            fadeDuration = 1000,
            hasImageTransition = false,
            hasTextTransition = false,
            durationInMillis = 8000,
        });
        steps.Add(new Step
        {
            text = "A criatura olha em sua direção, e apesar de nenhuma palavra ser dita, você entende o seu chamado.",
            image = cutsceneAtlas.CreateSprite("image-1"),
            fadeDuration = 1000,
            hasImageTransition = false,
            hasTextTransition = false,
            durationInMillis = 6000,
        });
        steps.Add(new Step
        {
            text = "Por meio de forças maiores, você sente seu corpo deslizar pelo ar. Uma atitude inconsciente, o anseio por uma curiosidade que não parece existir, sentimentos complicados de se descrever.",
            image = cutsceneAtlas.CreateSprite("image-1"),
            fadeDuration = 1000,
            hasImageTransition = false,
            hasTextTransition = false,
            durationInMillis = 8000,
        });
        steps.Add(new Step
        {
            text = "O que há dentro deste trem? Você deve se perguntar.",
            image = cutsceneAtlas.CreateSprite("image-1"),
            fadeDuration = 1000,
            hasImageTransition = false,
            hasTextTransition = false,
            durationInMillis = 6000,
        });
        steps.Add(new Step
        {
            text = "Uma única porta se abre para recebê-lo.",
            image = cutsceneAtlas.CreateSprite("image-1"),
            fadeDuration = 1000,
            hasImageTransition = false,
            hasTextTransition = false,
            durationInMillis = 6000,
        });
        steps.Add(new Step
        {
            text = "Você embarca no Limbo.",
            image = cutsceneAtlas.CreateSprite("image-1"),
            fadeDuration = 1000,
            hasImageTransition = false,
            hasTextTransition = false,
            durationInMillis = 6000,
        });

        _showIntroCutscene = !GameSession.IntroCutscenePlayed;


        cutscene = new CutsceneController(
            steps,
            Color.Black,
            Alloy,
            _font,
            Core.GraphicsDevice
        );

        if (!_startX.HasValue && (_showIntroCutscene || !GameSession.IntroCutscenePlayed))
        {
            MediaPlayer.Stop();
            MediaPlayer.IsRepeating = false;
            MediaPlayer.Play(_introSong);
            cutscene.Start();
            GameSession.IntroCutscenePlayed = true;
        }


        cursorAtlas.SetCursor("cheese");

        BuildDialogs();
    }
    private void StartAmbienceAudio()
    {
    if (_ambienceSong == null || _ambienceStarted)
        return;
    _ambienceStarted = true;
    MediaPlayer.Stop();
    MediaPlayer.IsRepeating = true;
    Core.Audio.SetSongVolumeScale(AMBIENCE_VOLUME);
    MediaPlayer.Play(_ambienceSong);
    }
    public override void UnloadContent()
    {
        MediaPlayer.Stop();
        _pixel?.Dispose();
        base.UnloadContent();
    }

    // Scenario 1: the player was caught trying to go on without the needed item.
    // Each return to the haunted car makes the Estranho faster.
    private void StartCaught()
    {
        GameSession.HauntedRetries++;
        _caught = true;
        _caughtRemainingMs = CAUGHT_MS;
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
            durationInMillis = 8000,
            spriteAnimations = new List<DialogCutsceneSpriteAnimation>()
        });
        firstSteps.Add(new DialogCutsceneSteps
        {
            caption = Wrap("Sou o grandioso VIGIA, tudo vejo, tudo sei. Eu te conheço de muito antes Alma, desdo seu nascimento, até o seu último suspiro..."),
            hasCaptionFadeIn = true,
            durationInMillis = 7000,
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
            durationInMillis = 8000,
            spriteAnimations = new List<DialogCutsceneSpriteAnimation>()
        });
        firstSteps.Add(new DialogCutsceneSteps
        {
            caption = Wrap("Alma, seu destino te aguarda a direita desse vagão, não se surpreenda pelo o que está por vir, não irá durar por muito... Só precisamos ter certeza..."),
            hasCaptionFadeIn = true,
            durationInMillis = 8000,
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
        // A partir daqui (estágio FalouComVigia) o Vigia passa a acompanhar Alma.
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

        // ----- Judged stage: Justiça brings Alma back and the Watcher becomes agitated -----
        List<DialogCutsceneSteps> secondSteps = new();

        secondSteps.Add(new DialogCutsceneSteps
        {
            caption = Wrap("Justiça: Vigia, como havia me dito, aconteceu."),
            hasCaptionFadeIn = true,
            durationInMillis = 4300,
            spriteAnimations = new List<DialogCutsceneSpriteAnimation>()
        });
        secondSteps.Add(new DialogCutsceneSteps
        {
            caption = Wrap("Vigia: Eu sabia, não me restavam dúvidas, ALMA, DESTRUIDORA, USURPADORA!"),
            hasCaptionFadeIn = true,
            durationInMillis = 5300,
            spriteAnimations = new List<DialogCutsceneSpriteAnimation>()
        });
        secondSteps.Add(new DialogCutsceneSteps
        {
            caption = Wrap("Justiça: Talvez pudesse haver algum engano… A velha morte certamente teria algo a dizer."),
            hasCaptionFadeIn = true,
            durationInMillis = 6700,
            spriteAnimations = new List<DialogCutsceneSpriteAnimation>()
        });
        secondSteps.Add(new DialogCutsceneSteps
        {
            caption = Wrap("Vigia: TRAIDOR! Não entende o que isso significa? Minhas palavras não alcançam suas orelhas? É O NOSSO FIM! EU REFORÇO! O NOSSO FIM"),
            hasCaptionFadeIn = true,
            durationInMillis = 9700,
            spriteAnimations = new List<DialogCutsceneSpriteAnimation>()
        });
        secondSteps.Add(new DialogCutsceneSteps
        {
            caption = Wrap("Justiça: O que deveria ser feito?"),
            hasCaptionFadeIn = true,
            durationInMillis = 4000,
            spriteAnimations = new List<DialogCutsceneSpriteAnimation>()
        });
        secondSteps.Add(new DialogCutsceneSteps
        {
            caption = Wrap("Vigia: Criatura sombria, o mal encarnado, deixe Alma a mercê de nosso amigo, o Estranho há de dar-lhe uma boa lição…"),
            hasCaptionFadeIn = true,
            durationInMillis = 9000,
            spriteAnimations = new List<DialogCutsceneSpriteAnimation>()
        });

        // When finished, the Watcher guides Alma to the Haunted Train and unlocks the left exit.
        _secondTalk = CreateTalk(secondSteps, () =>
        {
            GameSession.Stage = StoryStage.VagaoAssombrado;
            // Primeiro checkpoint do jogo: o Vigia coloca Alma no vagão assombrado.
            GameSession.SaveCheckpoint(_playerX);
        });
        // ----- Com a máscara: o Vigia confunde Alma com um ajudante do Estranho -----
        List<DialogCutsceneSteps> maskSteps = new();

        maskSteps.Add(new DialogCutsceneSteps
        {
            caption = Wrap("Coça… Coça tanto…"),
            hasCaptionFadeIn = true,
            durationInMillis = 3000,
            spriteAnimations = new List<DialogCutsceneSpriteAnimation>()
        });
        maskSteps.Add(new DialogCutsceneSteps
        {
            caption = Wrap("Não me interrompa… O que seria você? Algum dos ajudantes do Estranho?"),
            hasCaptionFadeIn = true,
            durationInMillis = 6000,
            spriteAnimations = new List<DialogCutsceneSpriteAnimation>()
        });
        maskSteps.Add(new DialogCutsceneSteps
        {
            caption = Wrap("Pobre criatura, a fome de seu mestre sempre ultrapassa a sua necessidade de auxílio…"),
            hasCaptionFadeIn = true,
            durationInMillis = 6700,
            spriteAnimations = new List<DialogCutsceneSpriteAnimation>()
        });
        maskSteps.Add(new DialogCutsceneSteps
        {
            caption = Wrap("Boa sorte criatura."),
            hasCaptionFadeIn = true,
            durationInMillis = 3000,
            spriteAnimations = new List<DialogCutsceneSpriteAnimation>()
        });

        _maskTalk = CreateTalk(maskSteps, null);

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
            Dialog = new DialogCutsceneController(steps, Alloy, _font, Core.GraphicsDevice),
            TotalMs = total,
            OnFinished = onFinished
        };
    }

    // Selects the Watcher's dialog based on the inventory and the current story stage.
    private Talk ChooseTalk()
    {
        // Com a máscara, o Vigia confunde Alma com um ajudante do Estranho.
        if (GameSession.Inventory.Has(GameItems.MaskId))
        {
            return _maskTalk;
        }

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
        if (_showIntroCutscene && !_introMusicFinished && !cutscene.IsPlaying)
    {
    _introMusicFinished = true;
    StartAmbienceAudio();
    }

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        _player.Update(gameTime);
        _vigia.Update(gameTime);
        _justica?.Update(gameTime);

        // Audio input remains active, but player movement is disabled during dialogs.
        CheckAudioInput();
#if DEBUG
        CheckDebugKeys();
#endif
        // Cenário 1: o Vigia leva o jogador de volta ao vagão assombrado.
        if (_caught)
        {
            UpdateCamera(dt, snap: false);
            _caughtRemainingMs -= gameTime.ElapsedGameTime.TotalMilliseconds;
            if (_caughtRemainingMs <= 0)
            {
                Core.ChangeScene(new Scene_assombrado(isRetryChase: true));
            }
            return;
        }

        if (_activeTalk == null)
        {
            MovePlayer(dt);
        }

        // Com o puzzle iniciado, sem o item 1 (máscara) o jogador não passa pela recepção:
        // ao se aproximar do Vigia (do posto dele) ele é reconhecido e levado de volta.
        if (GameSession.HauntedPuzzleStarted &&
            !GameSession.Inventory.Has(GameItems.MaskId) &&
            _playerX > _vigiaPostX - VIGIA_BLOCK_RANGE)
        {
            StartCaught();
            return;
        }

        UpdateCamera(dt, snap: false);

        // O Vigia acompanha Alma (estágio FalouComVigia) ou volta ao posto.
        UpdateVigia(dt);
        UpdateJustica(dt);

        // Turns the Watcher toward the player's position.
        if (GameSession.VigiaDesbloqueouSegundoSprite)
        {
        _vigia = _vigiaItch;
        }
        else
        {
        _vigia = _playerX < _vigiaX ? _vigiaLeft : _vigiaRight;
        }

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
        if (_justicaAcompanhando &&
            _activeTalk == null &&
            MathF.Abs(_playerX - _vigiaX) < TALK_RANGE)
        {
            _justicaAcompanhando = false;
            StartTalk(_secondTalk);
        }

        // Checks the player's proximity to the Watcher and exit arches.
        _canTalk = !_justicaAcompanhando &&
           _activeTalk == null &&
           MathF.Abs(_playerX - _vigiaX) < TALK_RANGE;
        _isAtLeftExit = MathF.Abs(_playerX - _leftExitX) < EXIT_RANGE;
        _isAtRightExit = MathF.Abs(_playerX - _rightExitX) < EXIT_RANGE;

        // O item 2 só pode ser coletado depois de iniciar o puzzle.
        _isNearNecklace = GameSession.HauntedPuzzleStarted &&
                          !GameSession.Inventory.Has(GameItems.NecklaceId) &&
                          MathF.Abs(_playerX - _necklaceX) < ITEM_RANGE;

        // The right arch (Boiler) unlocks after speaking to the Watcher; the
        // left arch (Haunted Train) stays locked until Alma has gone to the Boiler
        // for the first time (Scene_caldeirao sets the stage to Julgada).
        _rightExitOpen = GameSession.Stage >= StoryStage.FalouComVigia;
        _leftExitOpen = GameSession.Stage >= StoryStage.Julgada;

        if (_activeTalk == null && Core.Input.Keyboard.WasKeyJustPressed(Keys.E))
        {
            if (_canTalk)
            {
                StartTalk(ChooseTalk());
            }
            else if (_isNearNecklace)
            {
                GameSession.Inventory.CollectItem(GameItems.NecklaceId, GameItems.NecklaceIcon);
            }
            else if (_isAtLeftExit && _leftExitOpen)
            {
                Core.Audio.PlaySoundEffect(_DoorSoundEffect);
                Core.ChangeScene(new Scene_assombrado());
                return;
            }
            else if (_isAtRightExit && _rightExitOpen)
            {
                Core.Audio.PlaySoundEffect(_DoorSoundEffect);
                Core.ChangeScene(new Scene_caldeirao());

                return;
            }
        }
    }

    private void StartTalk(Talk talk)
    {
        // Turns the player toward the Watcher.
        _player = _vigiaX < _playerX ? _playerLeft : _playerRight;

        _activeTalk = talk;
        _talkRemainingMs = talk.TotalMs;
        talk.Dialog.Start();
    }

    // O Vigia acompanha Alma depois da primeira conversa. Nos outros estágios
    // ele volta ao posto. Fica parado enquanto uma conversa está tocando.
    private void UpdateVigia(float dt)
    {
        float step = 0f;
        float direction = 0f;

        if (_activeTalk == null)
        {
            if (_vigiaCanFollow && GameSession.Stage == StoryStage.FalouComVigia)
            {
                // Só anda se Alma estiver mais longe que a distância de acompanhamento.
                // Para exatamente nela, sem encostar e sem recuar quando Alma se aproxima.
                float distance = _playerX - _vigiaX;
                float excess = MathF.Abs(distance) - VIGIA_FOLLOW_DISTANCE;
                if (excess > 0f)
                {
                    direction = MathF.Sign(distance);
                    step = MathF.Min(VIGIA_FOLLOW_SPEED * dt, excess);
                }
            }
            else
            {
                // Fora do estágio de acompanhar, volta andando para o posto.
                float toPost = _vigiaPostX - _vigiaX;
                if (MathF.Abs(toPost) > 1f)
                {
                    direction = MathF.Sign(toPost);
                    step = MathF.Min(VIGIA_FOLLOW_SPEED * dt, MathF.Abs(toPost));
                }
            }
        }

        _vigiaX = MathHelper.Clamp(_vigiaX + direction * step, WORLD_MARGIN, _background.Width - WORLD_MARGIN);

        // Pulinho só enquanto anda.
        if (step > 0f)
        {
            _vigiaWalkTime += dt;
            _vigiaBobOffset = -MathF.Abs(MathF.Sin(_vigiaWalkTime * VIGIA_BOB_SPEED)) * VIGIA_BOB_HEIGHT;
        }
        else
        {
            _vigiaWalkTime = 0f;
            _vigiaBobOffset = 0f;
        }
    }
        private void UpdateJustica(float dt)
        {
            if (!_justicaAcompanhando || _activeTalk != null)
        return;

            float targetX = _playerX + JUSTICA_FOLLOW_DISTANCE;

            if (_justicaX > targetX)
        {
            float distance = _justicaX - targetX;
            float step = MathF.Min(JUSTICA_FOLLOW_SPEED * dt, distance);

            _justicaX -= step;
        }
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
        if (_justicaVisivel)
        {
        float justiceLeft = _justicaX - _justica.Width * 0.1f;
        float playerHalfWidth = _player.Width * 0.5f;

        float maxPlayerX = justiceLeft - playerHalfWidth;

        _playerX = MathF.Min(_playerX, maxPlayerX);
        }
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

        if (GameSession.HauntedPuzzleStarted && !GameSession.Inventory.Has(GameItems.NecklaceId))
        {
            _necklaceSprite.Draw(Core.SpriteBatch,
                new Vector2(_necklaceX - _necklaceSprite.Width * 0.5f, GROUND_Y - _necklaceSprite.Height));
        }

        // Sprites are drawn from the top-left corner, keeping their feet aligned at GROUND_Y.
        // O Vigia ganha um pulinho (_vigiaBobOffset) enquanto anda.
        if (_justicaVisivel)
        {
            _justica.Draw(
            Core.SpriteBatch,
            new Vector2(
            _justicaX - _justica.Width * 0.1f,
            GROUND_Y - _justica.Height
        )
        );
        }
        _vigia.Draw(Core.SpriteBatch,
            new Vector2(_vigiaX - _vigia.Width * 0.5f, GROUND_Y - _vigia.Height + _vigiaBobOffset));
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
            else if (_isNearNecklace)
            {
                prompt = PRESS_E_COLLECT_ITEM;
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

        // Cenário 1: escurece a tela e mostra a mensagem enquanto o Vigia leva o jogador de volta.
        if (_caught)
        {
            int w = Core.GraphicsDevice.PresentationParameters.BackBufferWidth;
            int h = Core.GraphicsDevice.PresentationParameters.BackBufferHeight;
            float elapsed = (float)((CAUGHT_MS - _caughtRemainingMs) / CAUGHT_MS);
            float alpha = MathHelper.Clamp(elapsed * 3f, 0f, 1f) * 0.7f;
            Vector2 textSize = _font.MeasureString(CAUGHT_TEXT);

            Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
            Core.SpriteBatch.Draw(_pixel, new Rectangle(0, 0, w, h), Color.Black * alpha);
            Core.SpriteBatch.DrawString(_font, CAUGHT_TEXT, new Vector2((w - textSize.X) * 0.5f, (h - textSize.Y) * 0.5f), Alloy);
            Core.SpriteBatch.End();
        }

        // Draws dialogs above all other layers; each controller renders only while active.
        Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
        _firstTalk.Dialog.Draw(Core.SpriteBatch, gameTime);
        _reminderTalk.Dialog.Draw(Core.SpriteBatch, gameTime);
        _secondTalk.Dialog.Draw(Core.SpriteBatch, gameTime);
        _maskTalk.Dialog.Draw(Core.SpriteBatch, gameTime);
        _idleTalk.Dialog.Draw(Core.SpriteBatch, gameTime);
        if (!GameSession.IntroCutscenePlayed || _showIntroCutscene)
        {
            cutscene.Draw(Core.SpriteBatch, gameTime);
        }
        Core.SpriteBatch.End();

        base.Draw(gameTime);
    }
}