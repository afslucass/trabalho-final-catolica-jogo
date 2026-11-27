using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using GameCore;
using GameCore.CustomMouseCursor;
using GameCore.DialogCutscene;
using GameCore.Graphics;
using GameCore.Scenes;
using GameCore.Cutscene;
using GameCore.Save;
using Microsoft.Xna.Framework.Media;

namespace TestGame.Scenes;

public class Scene_caldeirao : Scene, ISaveableScene
{
    private Sprite _background_RECEPCAO;
    private float _cameraX;
    private Song _caldeiraoSong;
    public string SaveId => "caldeirao";


    // Character foot position, measured in background pixels.
    // The floor (bottom black strip) spans y=955 to y=1080.
    private const float GROUND_Y = 1010f;
    public Vector2 SavePlayerPosition => new Vector2(_playerX, GROUND_Y);


    // ---------- Player ----------
    // Two mirrored sprites, one facing each direction.
    // _player always references the active sprite.
    private AnimatedSprite _player;
    private AnimatedSprite _playerLeft;
    private AnimatedSprite _playerRight;

    // ---------- Expulsão por falta do colar ----------
    private bool _isBeingExpelled;
    private float _expulsionRemainingMs;

    private const float EXPULSION_MS = 3500f;

    private const string EXPULSION_TEXT =
        "Alma? O Vigia não ficará contente… Por favor, desista e pereça.";
    
    private bool HasNecklace()
    {
        return GameSession.Inventory.Has(GameItems.NecklaceId);
    }
    // Textura utilizada para escurecer a tela.
    private Texture2D _pixel;
    // Horizontal position of the player's foot center, in background pixels.
    private float _playerX;
    // Movement speed in background pixels per second (Space applies the sprint multiplier).
    private const float PLAYER_SPEED = 450f;
    private const float SPRINT_MULTIPLIER = 1.6f;

    // Minimum distance from the world boundaries.
    private const float WORLD_MARGIN = 100f;
    
    // In this game, player-2 faces left and player-1 faces
    // right (verified). Update these names if the sprite sheet changes.
    private const string PLAYER_LEFT_ANIMATION = "player-2-animation";
    private const string PLAYER_RIGHT_ANIMATION = "player-1-animation";

    // ---------- Doctor ----------
    private AnimatedSprite _doctor;
    private AnimatedSprite _doctor1;
    private AnimatedSprite _doctor2;
    private float _doctorX;

    private const string DOCTOR_ANIMATION1 = "doctor-animation";
    private const string DOCTOR_ANIMATION2 = "doctor-seated-animation";

    private SpriteFont _font;
    // Balence
    private AnimatedSprite _balence1;
    private AnimatedSprite _balence2;
    private AnimatedSprite _balence3;
    private float _balenceX;
    private const string BALENCE_ANIMATION1 = "scale-1-animation";
    private const string BALENCE_ANIMATION2 = "scale-2-animation";
    private const string BALENCE_ANIMATION3 = "scale-3-animation";

    Color Alloy = new Color(201, 149, 0);

    // ---------- Dialogue ----------
    private DialogCutsceneController _doctor1Dialog1;
    private DialogCutsceneController _doctor1Dialog2;
    private DialogCutsceneController _doctor1Dialog3;
    private DialogCutsceneController _doctorRepeatDialog;
    private DialogCutsceneController _doctorRepeatDialog2;
    private DialogCutsceneController _doctorsemNecklaceDialog;
    private DialogCutsceneController _hairWithoutNecklaceDialog;
    private DialogCutsceneController _hairWithNecklaceDialog;
    private enum DoctorSequence
    {
        None,
        FirstDialog,
        MovingToBalance,
        RisingOnBalance,
        SecondDialog,
        ReturningToGround,
        RepeatDialog,
        ThirdDialog,
        RepeatDialog2,
        semNecklaceDialog,
        HairWithoutNecklaceDialog,
        HairWithNecklaceDialog,
        Completed
    }

    private DoctorSequence _doctor1Sequence = DoctorSequence.None;

    // Posição X onde o personagem vai parar, depois da balança.
    private float _balanceTargetX;

    // Posição inicial da caminhada.
    private float _balanceStartX;

    // Deslocamento vertical do personagem durante a cutscene.
    private float _cutscenePlayerYOffset;
    private float _cutscenePlayerXOffset;

    // Controle da subida.
    private int _riseStage = 0;
    private float _riseTimer;

    private const float CUTSCENE_PLAYER_SPEED = 220f;

    // Distância depois do centro da balança.
    private const float BALANCE_TARGET_OFFSET = 180f;

    // Quantos pixels o personagem sobe.
    private const float PLAYER_RISE_HEIGHT1 = 240f;
    private const float PLAYER_RISE_HEIGHT2 = 300f;
    private const float PLAYER_RISE_HEIGHT3 = 350f;
    private const float PLAYER_RISE_X1 = 5f;
    private const float PLAYER_RISE_X2 = 70f;
    private const float PLAYER_RISE_X3 = 90f;

    // Duração da subida em segundos.
    private const float PLAYER_RISE_DURATION = 1.5f;

    // Sprite da balança que será desenhado no momento.
    private AnimatedSprite _activeBalence;

    private float _animationTimer;

    private bool _dialogActive;
    private bool _isNearDoctor;

    private const float DOCTOR_TALK_RANGE = 150f;
    private const string PRESS_E_TALK = "Press E para conversar";

    private const string PRESS_E_INTERECT = "Press E for go to Recepção";

    private Vector2 _pressEInterectPos;
    private const float EXIT_RANGE_RECEPCAO = 150f;
    private const float EXIT_RATIO = 0.100f;
    private bool _isAtExit;
    private float _recepcaoExitX;

    // Item 3 do disfarce (cabelo). Só pode ser coletado depois de iniciar o puzzle
    // (falar com o Estranho no vagão assombrado).
    private const float HAIR_X_RATIO = 0.20f;
    private const float ITEM_RANGE = 150f;
    private const string PRESS_E_COLLECT_ITEM = "Press E para pegar o item";
    private Sprite _hairSprite;
    private float _hairX;
    private bool _isNearHair;

    public override void Initialize()
    {
        base.Initialize();

        // Primeira ida à Caldeira (pesagem da alma): a partir daqui a porta do vagão
        // assombrado fica liberada na recepção. Só avança de FalouComVigia; nunca volta atrás.
        if (GameSession.Stage == StoryStage.FalouComVigia)
        {
            GameSession.Stage = StoryStage.Julgada;
        }

        float worldWidth = _background_RECEPCAO.Width;
        _playerX = _recepcaoExitX + 100f;
        _player = _playerRight;
        
        // Set the position of the press E text
        Vector2 size = _font.MeasureString(PRESS_E_INTERECT);
        _pressEInterectPos = new Vector2(Core.GraphicsDevice.PresentationParameters.BackBufferWidth / 2, 1000);

        UpdateCamera(0f, snap: true);       
    }

    public override void LoadContent()
    {
        base.LoadContent();

        TextureAtlas charactersAtlas = TextureAtlas.FromFile(Content, "images/characters-atlas-definition.xml");
        TextureAtlas atlas = TextureAtlas.FromFile(Content, "images/atlas-definition.xml");

        _caldeiraoSong = Content.Load<Song>("Audio/caldeira");

        MediaPlayer.IsRepeating = true;
        MediaPlayer.Volume = 0.5f;

        MediaPlayer.Play(_caldeiraoSong);

        // Reception background (3840 x 1080), drawn at its native size.
        _background_RECEPCAO = charactersAtlas.CreateSprite("bg-caldeirao");
        _background_RECEPCAO.Scale = Vector2.One;


        // Player sprites: one for each direction (matching sizes prevent position jumps).
        _playerLeft = charactersAtlas.CreateAnimatedSprite(PLAYER_LEFT_ANIMATION);
        _playerLeft.Scale = Vector2.One;
        _playerRight = charactersAtlas.CreateAnimatedSprite(PLAYER_RIGHT_ANIMATION);
        _playerRight.Scale = Vector2.One;
        _player = _playerRight;

        // Sprite do médico na primeira entrada.
        _doctor1 = charactersAtlas.CreateAnimatedSprite(DOCTOR_ANIMATION1);
        _doctor1.Scale = Vector2.One;

        // Sprite do médico na segunda entrada.
        _doctor2 = charactersAtlas.CreateAnimatedSprite(DOCTOR_ANIMATION2);
        _doctor2.Scale = Vector2.One;

        // Define qual médico será utilizado.
        if (GameSession.CaldeiraoSequenceCompleted)
        {
            _doctor = _doctor2;

            // Nova posição do médico.
            _doctorX = _background_RECEPCAO.Width * 0.36f;
        }
        else
        {
            _doctor = _doctor1;

            // Posição original do médico.
            _doctorX = _background_RECEPCAO.Width * -0.1f;
        }

        _balence1 = charactersAtlas.CreateAnimatedSprite(BALENCE_ANIMATION1);
        _balence1.Scale = Vector2.One;
        _balence2 = charactersAtlas.CreateAnimatedSprite(BALENCE_ANIMATION2);
        _balence2.Scale = Vector2.One;
        _balence3 = charactersAtlas.CreateAnimatedSprite(BALENCE_ANIMATION3);
        _balence3.Scale = Vector2.One;
        _balenceX = _background_RECEPCAO.Width * 0.5f;

        _activeBalence = _balence1;

        _recepcaoExitX = _background_RECEPCAO.Width * EXIT_RATIO;

        _hairSprite = charactersAtlas.CreateSprite(GameItems.HairIcon);
        _hairSprite.Scale = Vector2.One;
        _hairX = _background_RECEPCAO.Width * HAIR_X_RATIO;

        // Load the font
        _font = Content.Load<SpriteFont>("fonts/KiwiSoda");
        _font.DefaultCharacter = _font.Characters.Contains('?') ? '?' : ' ';
        // Textura branca de 1x1 para desenhar o fundo escuro.
        _pixel = new Texture2D(Core.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        List<DialogCutsceneSteps> doctorSteps = new List<DialogCutsceneSteps>
        {
            new DialogCutsceneSteps
            {
                caption = "Bem-vindo, você me conhecerá por Justiça.",
                hasCaptionFadeIn = true,
                durationInMillis = 3500
            },

            new DialogCutsceneSteps
            {
                caption = "Meu dever é simples, o seu destino nas palmas de minhas mãos.",
                hasCaptionFadeIn = true,
                durationInMillis = 3500
            },

            new DialogCutsceneSteps
            {
                caption = "Suba a balança, revele sua verdade, seu peso contra o peso da pena.",
                hasCaptionFadeIn = true,
                durationInMillis = 4000
            }
        };

        _doctor1Dialog1 = new DialogCutsceneController(
            doctorSteps,
            Alloy,
            _font,
            Core.GraphicsDevice
        );

        List<DialogCutsceneSteps> secondSteps = new()
        {
            new DialogCutsceneSteps
            {
                caption = "...",
                hasCaptionFadeIn = true,
                durationInMillis = 3500
            },
            new DialogCutsceneSteps
            {
                caption = "Como pode alguém ter o mesmo peso da pena?",
                hasCaptionFadeIn = true,
                durationInMillis = 4000
            },
            new DialogCutsceneSteps
            {
                caption = "...",
                hasCaptionFadeIn = true,
                durationInMillis = 4000
            },
            new DialogCutsceneSteps
            {
                caption = "Estranho… Nunca me ocorreu algo assim… Vigia havia me dito…",
                hasCaptionFadeIn = true,
                durationInMillis = 4000
            },
            new DialogCutsceneSteps
            {
                caption = "Venha comigo Alma, devemos tirar isso a limpo.",
                hasCaptionFadeIn = true,
                durationInMillis = 4000
            }
        };

        _doctor1Dialog2 = new DialogCutsceneController(
            secondSteps,
            Alloy,
            _font,
            Core.GraphicsDevice
        );

        List<DialogCutsceneSteps> repeatSteps = new()
        {
            new DialogCutsceneSteps
            {
                caption = "Vá na frente Alma, estarei logo atrás de você.",
                hasCaptionFadeIn = true,
                durationInMillis = 4000
            }
        };

        _doctorRepeatDialog = new DialogCutsceneController(
            repeatSteps,
            Alloy,
            _font,
            Core.GraphicsDevice
        );
        
        List<DialogCutsceneSteps> thirdSteps = new()
        {
            new DialogCutsceneSteps
            {
                caption = "...",
                hasCaptionFadeIn = true,
                durationInMillis = 3000
            },
            new DialogCutsceneSteps
            {
                caption = "Hum... Que cheiro...",
                hasCaptionFadeIn = true,
                durationInMillis = 3500
            },
            new DialogCutsceneSteps
            {
                caption = "Que cheiro familiar. Como que você tem esse cheiro?",
                hasCaptionFadeIn = true,
                durationInMillis = 4000
            },
        };
        _doctor1Dialog3 = new DialogCutsceneController(
            thirdSteps,
            Alloy,
            _font,
            Core.GraphicsDevice
        );
        List<DialogCutsceneSteps> repeatSteps2 = new()
        {
            new DialogCutsceneSteps
            {
                caption = "Que cheiro familiar...",
                hasCaptionFadeIn = true,
                durationInMillis = 4000
            }
        };

        _doctorRepeatDialog2 = new DialogCutsceneController(
            repeatSteps2,
            Alloy,
            _font,
            Core.GraphicsDevice
        );
        List<DialogCutsceneSteps> semMaskSteps = new()
        {
            new DialogCutsceneSteps
            {
                caption = "Quem é você? O estranho te mandou aqui?",
                hasCaptionFadeIn = true,
                durationInMillis = 4000
            },
            new DialogCutsceneSteps
            {
                caption = "Saia criado, não quero conversar.",
                hasCaptionFadeIn = true,
                durationInMillis = 4000
            }
        };

        _doctorsemNecklaceDialog = new DialogCutsceneController(
            semMaskSteps,
            Alloy,
            _font,
            Core.GraphicsDevice
        );
        List<DialogCutsceneSteps> hairWithoutNecklaceSteps = new()
        {
            new DialogCutsceneSteps
            {
                caption = "Ei cria do Estranho, deixe o cabelo onde está, saia daqui.",
                hasCaptionFadeIn = true,
                durationInMillis = 4000
            }
        };

        _hairWithoutNecklaceDialog = new DialogCutsceneController(
            hairWithoutNecklaceSteps,
            Alloy,
            _font,
            Core.GraphicsDevice
        );
        List<DialogCutsceneSteps> hairWithNecklaceSteps = new()
        {
            new DialogCutsceneSteps
            {
                caption = "Você cheira como meu amigo… Vigia nunca me disse que havia dado cria…",
                hasCaptionFadeIn = true,
                durationInMillis = 4000
            },
            new DialogCutsceneSteps
            {
                caption = "...",
                hasCaptionFadeIn = true,
                durationInMillis = 3000
            },
            new DialogCutsceneSteps
            {
                caption = "Somos capazes de ter herdeiros?",
                hasCaptionFadeIn = true,
                durationInMillis = 4000
            },
            new DialogCutsceneSteps
            {
                caption = "...",
                hasCaptionFadeIn = true,
                durationInMillis = 3000
            },
            new DialogCutsceneSteps
            {
                caption = "Não, não estou pelado.",
                hasCaptionFadeIn = true,
                durationInMillis = 4000
            },
        };

        _hairWithNecklaceDialog = new DialogCutsceneController(
            hairWithNecklaceSteps,
            Alloy,
            _font,
            Core.GraphicsDevice
        );
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        // Controla a expulsão do jogador.
        if (_isBeingExpelled)
        {
            _expulsionRemainingMs -=
                (float)gameTime.ElapsedGameTime.TotalMilliseconds;

            if (_expulsionRemainingMs <= 0)
            {
                MediaPlayer.Stop();

                Core.ChangeScene(
                    new Scene_assombrado(isRetryChase: true)
                );

                return;
            }

            return;
        }

        // Verifica se o jogador não possui o colar.
        if (!HasNecklace() && GameSession.HauntedPuzzleStarted)
        {
            StartExpulsion();
            return;
        }

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        _player.Update(gameTime);
        _doctor.Update(gameTime);
        _activeBalence?.Update(gameTime);

        
        // Só permite movimentação quando não há cutscene.
        if (_doctor1Sequence == DoctorSequence.None ||
            _doctor1Sequence == DoctorSequence.Completed)
        {
            CheckKeyboardInput(dt);
        }

        UpdateCamera(dt, snap: false);

        // Check for collision between the slime and the exit point to trigger a scene change.
        _isAtExit = MathF.Abs(_playerX - _recepcaoExitX) < EXIT_RANGE_RECEPCAO;

        float balenceDrawX = _balenceX - _balence1.Width * -2.0f;

        // Calcula a posição do médico ativo.
        float doctorDrawX = _doctorX + _doctor.Width * 1.5f;
        // Calcula o centro do médico ativo.
        float doctorCenterX = doctorDrawX + _doctor.Width / 2f;
        // Verifica se o jogador está próximo do médico.
        _isNearDoctor = MathF.Abs(_playerX - doctorCenterX) < 400f;

        // O item 3 só pode ser coletado depois de iniciar o puzzle.
        _isNearHair = GameSession.HauntedPuzzleStarted &&
                !GameSession.Inventory.Has(GameItems.HairId) &&
                MathF.Abs(_playerX - _hairX) < ITEM_RANGE;

        if (Core.Input.Keyboard.WasKeyJustPressed(Keys.E) &&
            (_doctor1Sequence == DoctorSequence.None ||
            _doctor1Sequence == DoctorSequence.Completed))
        {
            if (_isNearHair)
                {
                    if (GameSession.Inventory.Has(GameItems.NecklaceId))
                    {
                        _hairWithNecklaceDialog.Start();

                        _doctor1Sequence = DoctorSequence.HairWithNecklaceDialog;

                        _dialogActive = true;
                    }
                    else
                    {
                        _hairWithoutNecklaceDialog.Start();

                        _doctor1Sequence = DoctorSequence.HairWithoutNecklaceDialog;
                        _dialogActive = true;
                    }
                }
            else if (_isAtExit && GameSession.CaldeiraoSequenceCompleted)
            {
                MediaPlayer.Stop();

                // Registra que Alma saiu da Caldeira pela primeira vez.
                GameSession.SaiuDaCaldeiraPelaPrimeiraVez = true;

                Core.ChangeScene(
                    new Scene_recepcao(RecepcaoSpawn.FromCaldeira)
                );

                return;
            }
            else if (_isNearDoctor && !_dialogActive)
            {
                // Puzzle iniciado, mas Alma ainda não possui a máscara.
                if (GameSession.HauntedPuzzleStarted &&
                    !GameSession.Inventory.Has(GameItems.NecklaceId))
                {
                    _doctorsemNecklaceDialog.Start();

                    _doctor1Sequence = DoctorSequence.semNecklaceDialog;
                    _dialogActive = true;
                }
                // Primeira visita: inicia a sequência da balança.
                else if (!GameSession.CaldeiraoSequenceCompleted)
                {
                    _doctor1Dialog1.Start();

                    _doctor1Sequence = DoctorSequence.FirstDialog;
                    _dialogActive = true;
                }
                // Segunda visita: inicia o terceiro diálogo.
                else if (!GameSession.SaiuDaCaldeiraPelaPrimeiraVez)
                {
                    _doctorRepeatDialog.Start();

                    _doctor1Sequence = DoctorSequence.RepeatDialog;
                    _dialogActive = true;
                }
                else if (!GameSession.ThirdJusticeDialogCompleted)
                {
                    _doctor1Dialog3.Start();

                    _doctor1Sequence = DoctorSequence.ThirdDialog;
                    _dialogActive = true;
                }
                else
                {
                    _doctorRepeatDialog2.Start();

                    _doctor1Sequence = DoctorSequence.RepeatDialog2;
                    _dialogActive = true;
                }
            }
        }
        if (_doctor1Sequence != DoctorSequence.None)
        {
            switch (_doctor1Sequence)
            {
                case DoctorSequence.FirstDialog:

                    if (!_doctor1Dialog1.IsRunning)
                        {
                            _balanceStartX = _playerX;

                            // Ponto X depois da balança.
                            _balanceTargetX = MathHelper.Clamp(
                                _balenceX + BALANCE_TARGET_OFFSET,
                                WORLD_MARGIN,
                                _background_RECEPCAO.Width - WORLD_MARGIN
                            );

                            _cutscenePlayerYOffset = 0f;
                            _riseTimer = 0f;

                            SetBalanceStage(1);

                            _doctor1Sequence = DoctorSequence.MovingToBalance;
                        }

                    break;

                case DoctorSequence.MovingToBalance:
                    UpdatePlayerCutsceneMovement(dt);
                    break;
                
                case DoctorSequence.RisingOnBalance:
                    UpdatePlayerRiseAnimation(dt);
                    break;
                case DoctorSequence.SecondDialog:
                    if (!_doctor1Dialog2.IsRunning)
                    {
                        _doctor1Sequence = DoctorSequence.ReturningToGround;
                        _riseTimer = 0f;
                    }
                    break;
                case DoctorSequence.ReturningToGround:
                    UpdatePlayerReturnAnimation(dt);
                    break;
                case DoctorSequence.RepeatDialog:
                    if (!_doctorRepeatDialog.IsRunning)
                    {
                        _doctor1Sequence = DoctorSequence.Completed;
                        _dialogActive = false;
                    }
                    break;               
                case DoctorSequence.ThirdDialog:
                    if (!_doctor1Dialog3.IsRunning)
                    {
                        GameSession.ThirdJusticeDialogCompleted = true;

                        _doctor1Sequence = DoctorSequence.Completed;
                        _dialogActive = false;
                    }
                    break;
                case DoctorSequence.RepeatDialog2:
                    if (!_doctorRepeatDialog2.IsRunning)
                    {
                        _doctor1Sequence = DoctorSequence.Completed;
                        _dialogActive = false;
                    }
                    break;
                case DoctorSequence.semNecklaceDialog:
                    if (!_doctorsemNecklaceDialog.IsRunning)
                    {
                        _doctor1Sequence = DoctorSequence.Completed;
                        _dialogActive = false;
                    }

                    break;
                case DoctorSequence.HairWithoutNecklaceDialog:
                    if (!_hairWithoutNecklaceDialog.IsRunning)
                    {
                        _doctor1Sequence = DoctorSequence.Completed;
                        _dialogActive = false;
                    }
                    break;
                case DoctorSequence.HairWithNecklaceDialog:               
                    if (!_hairWithNecklaceDialog.IsRunning)
                    {
                        GameSession.Inventory.CollectItem(
                            GameItems.HairId,
                            GameItems.HairIcon
                        );

                        _doctor1Sequence = DoctorSequence.Completed;
                        _dialogActive = false;
                    }
                    break;
                case DoctorSequence.Completed:
                    break;
            }

            UpdateCamera(dt, snap: false);
            return;
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
    private void StartExpulsion()
    {
        if (_isBeingExpelled)
            return;

        _isBeingExpelled = true;
        _expulsionRemainingMs = EXPULSION_MS;

        _dialogActive = true;
    }
    private void UpdatePlayerCutsceneMovement(float dt)
    {
        float distance = _balanceTargetX - _playerX;

        // Chegou ao ponto X depois da balança.
        if (MathF.Abs(distance) <= CUTSCENE_PLAYER_SPEED * dt)
        {
            _playerX = _balanceTargetX;

            // Começa a animação de subir.
            _riseTimer = 0f;
            _cutscenePlayerYOffset = 0f;

            _doctor1Sequence = DoctorSequence.RisingOnBalance;

            return;
        }

        // Define a direção do personagem.
        if (distance > 0)
        {
            _player = _playerRight;
            _playerX += CUTSCENE_PLAYER_SPEED * dt;
        }
        else
        {
            _player = _playerLeft;
            _playerX -= CUTSCENE_PLAYER_SPEED * dt;
        }
    }

    private void UpdatePlayerRiseAnimation(float dt)
    {
        _riseTimer += dt;

        float progress = _riseTimer / PLAYER_RISE_DURATION;
        progress = MathHelper.Clamp(progress, 0f, 1f);

        // Suaviza o movimento da animação.
        float easedProgress = progress * progress * (3f - 2f * progress);

        switch (_riseStage)
        {
            case 0:
                // Primeiro movimento: sobe até a altura 1.
                _cutscenePlayerYOffset =
                    MathHelper.Lerp(0f, PLAYER_RISE_HEIGHT1, easedProgress);
                _cutscenePlayerXOffset =
                    MathHelper.Lerp(0f, PLAYER_RISE_X1, easedProgress);

                if (progress >= 1f)
                {
                    _cutscenePlayerYOffset = PLAYER_RISE_HEIGHT1;
                    _cutscenePlayerXOffset = PLAYER_RISE_X1;

                    SetBalanceStage(2);

                    _riseStage = 1;
                    _riseTimer = 0f;
                }
                break;

            case 1:
                // Segundo movimento: sobe até a altura 2.
                _cutscenePlayerYOffset =
                    MathHelper.Lerp(
                        PLAYER_RISE_HEIGHT1,
                        PLAYER_RISE_HEIGHT2,
                        easedProgress
                    );
                _cutscenePlayerXOffset =
                    MathHelper.Lerp(
                        PLAYER_RISE_X1,
                        PLAYER_RISE_X2,
                        easedProgress
                    );

                if (progress >= 1f)
                {
                    _cutscenePlayerYOffset = PLAYER_RISE_HEIGHT2;
                    _cutscenePlayerXOffset = PLAYER_RISE_X2;

                    SetBalanceStage(3);

                    _riseStage = 2;
                    _riseTimer = 0f;
                }
                break;

            case 2:
                // Terceiro movimento: sobe até a altura 3.
                _cutscenePlayerYOffset =
                    MathHelper.Lerp(
                        PLAYER_RISE_HEIGHT2,
                        PLAYER_RISE_HEIGHT3,
                        easedProgress
                    );
                _cutscenePlayerXOffset =
                    MathHelper.Lerp(
                        PLAYER_RISE_X2,
                        PLAYER_RISE_X3,
                        easedProgress
                    );

                if (progress >= 1f)
                {
                    _cutscenePlayerYOffset = PLAYER_RISE_HEIGHT3;
                    _cutscenePlayerXOffset = PLAYER_RISE_X3;

                    _riseStage = 3;

                    _doctor1Dialog2.Start();
                    _doctor1Sequence = DoctorSequence.SecondDialog;
                }
                break;
        }
    }
    private void UpdatePlayerReturnAnimation(float dt)
    {
        _riseTimer += dt;

        float progress = _riseTimer / PLAYER_RISE_DURATION;
        progress = MathHelper.Clamp(progress, 0f, 1f);

        // Suaviza o movimento.
        float easedProgress = progress * progress * (3f - 2f * progress);

        // O personagem desce até o chão.
        _cutscenePlayerYOffset = MathHelper.Lerp(
            PLAYER_RISE_HEIGHT3,
            0f,
            easedProgress
        );

        // A animação terminou.
        if (progress >= 1f)
        {
            _cutscenePlayerYOffset = 0f;

            _riseTimer = 0f;

            // Registra que a primeira sequência foi concluída.
            GameSession.CaldeiraoSequenceCompleted = true;

            _doctor1Sequence = DoctorSequence.Completed;
            // Libera novamente a interação com o médico.
            _dialogActive = false;
        }
    }

    private void SetBalanceStage(int stage)
    {
        switch (stage)
        {
            case 1:
                _activeBalence = _balence1;
                break;

            case 2:
                _activeBalence = _balence2;
                break;

            case 3:
                _activeBalence = _balence3;
                break;
        }
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

        _activeBalence?.Draw(Core.SpriteBatch, new Vector2(_balenceX - _activeBalence.Width * 0.5f, GROUND_Y - _activeBalence.Height));
        _doctor.Draw(Core.SpriteBatch, new Vector2(_doctorX - _doctor.Width * -1.5f, GROUND_Y - _doctor.Height));
        _player.Draw(
            Core.SpriteBatch,
            new Vector2(
                _playerX - _player.Width * 0.5f + _cutscenePlayerXOffset,
                GROUND_Y - _player.Height - _cutscenePlayerYOffset
            )
        );

        Core.SpriteBatch.End();

        Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);

        GameSession.Inventory.Draw(Core.SpriteBatch, new Vector2(60, 60));

        if (_isNearHair)
        {
            Core.SpriteBatch.DrawString(
                _font,
                PRESS_E_COLLECT_ITEM,
                _pressEInterectPos,
                Alloy,
                0.0f,
                _font.MeasureString(PRESS_E_COLLECT_ITEM) * 0.5f,
                1.0f,
                SpriteEffects.None,
                0.0f
            );
        }
        else if (_isAtExit && GameSession.CaldeiraoSequenceCompleted)
        {
            Core.SpriteBatch.DrawString(
                _font,
                PRESS_E_INTERECT,
                _pressEInterectPos,
                Alloy,
                0.0f,
                _font.MeasureString(PRESS_E_INTERECT) * 0.5f,
                1.0f,
                SpriteEffects.None,
                0.0f
            );
        }
        else if (_isNearDoctor && !_dialogActive)
        {
            Core.SpriteBatch.DrawString(
                _font,
                PRESS_E_TALK,
                _pressEInterectPos,
                Alloy,
                0.0f,
                _font.MeasureString(PRESS_E_TALK) * 0.5f,
                1.0f,
                SpriteEffects.None,
                0.0f
            );
        }
        Core.SpriteBatch.End();

        Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
        if (_doctor1Sequence == DoctorSequence.FirstDialog)
        {
            _doctor1Dialog1?.Draw(Core.SpriteBatch, gameTime);
        }
        else if (_doctor1Sequence == DoctorSequence.SecondDialog)
        {
            _doctor1Dialog2?.Draw(Core.SpriteBatch, gameTime);
        }
        else if (_doctor1Sequence == DoctorSequence.RepeatDialog)
        {
            _doctorRepeatDialog?.Draw(Core.SpriteBatch, gameTime);
        }
        else if (_doctor1Sequence == DoctorSequence.ThirdDialog)
        {
            _doctor1Dialog3?.Draw(Core.SpriteBatch, gameTime);
        }
        else if (_doctor1Sequence == DoctorSequence.RepeatDialog2)
        {
            _doctorRepeatDialog2?.Draw(Core.SpriteBatch, gameTime);
        }
        else if (_doctor1Sequence == DoctorSequence.semNecklaceDialog)
        {
            _doctorsemNecklaceDialog?.Draw(Core.SpriteBatch, gameTime);
        }
        else if (_doctor1Sequence == DoctorSequence.HairWithoutNecklaceDialog)
        {
            _hairWithoutNecklaceDialog?.Draw(Core.SpriteBatch, gameTime);
        }
        else if (_doctor1Sequence == DoctorSequence.HairWithNecklaceDialog)
        {
            _hairWithNecklaceDialog?.Draw(Core.SpriteBatch, gameTime);
        }
        Core.SpriteBatch.End();

        // Mensagem exibida durante a expulsão.
        if (_isBeingExpelled)
        {
            int w = Core.GraphicsDevice.PresentationParameters.BackBufferWidth;
            int h = Core.GraphicsDevice.PresentationParameters.BackBufferHeight;

            float elapsed = (EXPULSION_MS - _expulsionRemainingMs) / EXPULSION_MS;
            float alpha = MathHelper.Clamp(elapsed * 3f, 0f, 1f) * 0.7f;

            Vector2 textSize = _font.MeasureString(EXPULSION_TEXT);

            Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);

            Core.SpriteBatch.Draw(
                _pixel,
                new Rectangle(0, 0, w, h),
                Color.Black * alpha
            );

            Core.SpriteBatch.DrawString(
                _font,
                EXPULSION_TEXT,
                new Vector2(
                    (w - textSize.X) * 0.5f,
                    (h - textSize.Y) * 0.5f
                ),
                Alloy
            );

            Core.SpriteBatch.End();
        }


        base.Draw(gameTime);
    }

}