using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using GameCore;
using GameCore.Scenes;
using GameCore.DialogCutscene;

namespace TestGame.Scenes;

/// <summary>
/// Cutscene final: o vagão da Morte (depois que o Estranho deixou o jogador passar).
/// O texto da Morte ainda é provisório ("Diálogo da Morte vem aqui", como no storyboard).
/// </summary>
public class Scene_final : Scene
{
    private const string BACK_TEXT = "Press Enter para voltar ao início";
    private const float DIALOG_MAX_WIDTH_RATIO = 0.8f;
    Color Alloy = new Color(201, 149, 0);
    private Texture2D _art;
    private SpriteFont _font;
    private DialogCutsceneController _dialog;
    private double _totalMs;
    private double _remainingMs;
    private bool _finished;

    // Música ambiente da cena final (audio/Final.ogg, carregada como Song).
    private Song _music;
    private const float MUSIC_VOLUME = 0.7f;

    public override bool CanPause => false;

    public override void LoadContent()
    {
        base.LoadContent();

        _art = Content.Load<Texture2D>("images/final");
        _font = Content.Load<SpriteFont>("fonts/KiwiSoda");
        _font.DefaultCharacter = _font.Characters.Contains('?') ? '?' : ' ';

        _music = Content.Load<Song>("audio/Final");

        // Troque os textos abaixo pelo diálogo da Morte.
        List<DialogCutsceneSteps> steps = new();
        steps.Add(new DialogCutsceneSteps
        {
            caption = Wrap("Alma… Que bom conhecê-lo, estava te aguardando. Muito esperto, se utilizar de um disfarce para enganar meus funcionários…"),
            hasCaptionFadeIn = true,
            durationInMillis = 5500,
            spriteAnimations = new List<DialogCutsceneSpriteAnimation>()   
        });
        steps.Add(new DialogCutsceneSteps
        {
            caption = Wrap("Eu sabia que esse dia chegaria, não tenho arrependimentos. Por favor, perdoe minhas criações, assim como eu, são imperfeitos, eles temem."),
            hasCaptionFadeIn = true,
            durationInMillis = 5500,
            spriteAnimations = new List<DialogCutsceneSpriteAnimation>()   
        });
        steps.Add(new DialogCutsceneSteps
        {
            caption = Wrap("Você deve estar confuso, certo?"),
            hasCaptionFadeIn = true,
            durationInMillis = 5500,
            spriteAnimations = new List<DialogCutsceneSpriteAnimation>()   
        });
        steps.Add(new DialogCutsceneSteps
        {
            caption = Wrap("Bom… Assim como você Alma, já fui um relés humano, como você, também acordei naquela estação, também adentrei o Limbo."),
            hasCaptionFadeIn = true,
            durationInMillis = 5500,
            spriteAnimations = new List<DialogCutsceneSpriteAnimation>()   
        });
        steps.Add(new DialogCutsceneSteps
        {
            caption = Wrap("Naquela época, porém, fui recebido por outras entidades, esses vagões também tinham outra aparência."),
            hasCaptionFadeIn = true,
            durationInMillis = 5500,
            spriteAnimations = new List<DialogCutsceneSpriteAnimation>()   
        });
        steps.Add(new DialogCutsceneSteps
        {
            caption = Wrap("Assim como você, fui julgado com a pesagem de uma pena, e, impressionantemente, também tive o mesmo peso que a  pena."),
            hasCaptionFadeIn = true,
            durationInMillis = 5500,
            spriteAnimations = new List<DialogCutsceneSpriteAnimation>()   
        });
        steps.Add(new DialogCutsceneSteps
        {
            caption = Wrap("E então, a Morte veio me receber. Entenda Alma, eu não sou um ser infinito, a Morte precisa de uma sucessão, e o escolhido sempre pesará o mesmo que a pena."),
            hasCaptionFadeIn = true,
            durationInMillis = 7500,
            spriteAnimations = new List<DialogCutsceneSpriteAnimation>()   
        });

        steps.Add(new DialogCutsceneSteps
        {
            caption = Wrap("A antiga Morte teve essa mesma conversa comigo… E, ao aceitar meu novo papel, uma nova era se iniciou. "),
            hasCaptionFadeIn = true,
            durationInMillis = 5500,
            spriteAnimations = new List<DialogCutsceneSpriteAnimation>()   
        });

        steps.Add(new DialogCutsceneSteps
        {
            caption = Wrap("Entendo que errei em alguns pontos com a humanidade, mas como disse, não tenho arrependimentos, estou preparada para o meu fim."),
            hasCaptionFadeIn = true,
            durationInMillis = 6500,
            spriteAnimations = new List<DialogCutsceneSpriteAnimation>()   
        });
        
        steps.Add(new DialogCutsceneSteps
        {
            caption = Wrap("E então Alma, você aceita seu destino? Tomar meu lugar como morte e dar início a nova era?"),
            hasCaptionFadeIn = true,
            durationInMillis = 5500,
            spriteAnimations = new List<DialogCutsceneSpriteAnimation>()   
        });

        _totalMs = 0;
        foreach (DialogCutsceneSteps step in steps)
            _totalMs += step.durationInMillis;

        _dialog = new DialogCutsceneController(steps, Alloy, _font, Core.GraphicsDevice);
    }

    public override void Initialize()
    {
        base.Initialize();

        _remainingMs = _totalMs;
        _dialog.Start();

        // Toca a música ambiente em loop. O volume é definido aqui porque a cena anterior
        // (vagão assombrado) deixa o volume do MediaPlayer reduzido. Se o som estiver
        // mudo (tecla M), o setter ignora e continua mudo.
        Core.Audio.SongVolume = MUSIC_VOLUME;
        Core.Audio.PlaySong(_music);
    }

    public override void UnloadContent()
    {
        // Para a música ao sair da cena final (volta ao título).
        MediaPlayer.Stop();
        base.UnloadContent();
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (!_finished)
        {
            _remainingMs -= gameTime.ElapsedGameTime.TotalMilliseconds;
            if (_remainingMs <= 0)
                _finished = true;
            return;
        }

        // Fim do jogo: recomeça do zero e volta para a tela inicial.
        if (Core.Input.Keyboard.WasKeyJustPressed(Keys.Enter))
        {
            GameSession.Reset();
            Core.ChangeScene(new TitleScene());
        }
    }

    public override void Draw(GameTime gameTime)
    {
        Core.GraphicsDevice.Clear(Color.Black);

        int w = Core.GraphicsDevice.PresentationParameters.BackBufferWidth;
        int h = Core.GraphicsDevice.PresentationParameters.BackBufferHeight;

        // A arte é vertical: ajusta à altura da tela e centraliza.
        float scale = h / (float)_art.Height;
        Vector2 position = new Vector2((w - _art.Width * scale) * 0.5f, 0f);

        Core.SpriteBatch.Begin(samplerState: SamplerState.LinearClamp);
        Core.SpriteBatch.Draw(_art, position, null, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        Core.SpriteBatch.End();

        Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
        if (!_finished)
        {
            _dialog.Draw(Core.SpriteBatch, gameTime);
        }
        else
        {
            Vector2 size = _font.MeasureString(BACK_TEXT);
            Core.SpriteBatch.DrawString(_font, BACK_TEXT, new Vector2((w - size.X) * 0.5f, h - 100f), Color.Yellow);
        }
        Core.SpriteBatch.End();

        base.Draw(gameTime);
    }

    // Quebra a fala em linhas para que não ultrapasse a largura da tela.
    private string Wrap(string text)
    {
        float maxWidth = Core.GraphicsDevice.PresentationParameters.BackBufferWidth * DIALOG_MAX_WIDTH_RATIO;
        float spaceWidth = _font.MeasureString(" ").X;
        StringBuilder result = new();

        float lineWidth = 0f;
        string[] words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (string word in words)
        {
            float wordWidth = _font.MeasureString(word).X;
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

            result.Append(word);
            lineWidth += wordWidth;
        }

        return result.ToString();
    }
}
