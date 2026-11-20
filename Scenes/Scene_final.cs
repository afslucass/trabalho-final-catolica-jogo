using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
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

    private Texture2D _art;
    private SpriteFont _font;
    private DialogCutsceneController _dialog;
    private double _totalMs;
    private double _remainingMs;
    private bool _finished;

    public override bool CanPause => false;

    public override void LoadContent()
    {
        base.LoadContent();

        _art = Content.Load<Texture2D>("images/final");
        _font = Content.Load<SpriteFont>("fonts/KiwiSoda");
        _font.DefaultCharacter = _font.Characters.Contains('?') ? '?' : ' ';

        // Troque os textos abaixo pelo diálogo da Morte.
        List<DialogCutsceneSteps> steps = new();
        steps.Add(new DialogCutsceneSteps
        {
            caption = Wrap("Diálogo da Morte vem aqui"),
            hasCaptionFadeIn = true,
            durationInMillis = 5000,
            spriteAnimations = new List<DialogCutsceneSpriteAnimation>()
        });

        _totalMs = 0;
        foreach (DialogCutsceneSteps step in steps)
            _totalMs += step.durationInMillis;

        _dialog = new DialogCutsceneController(steps, Color.White, _font, Core.GraphicsDevice);
    }

    public override void Initialize()
    {
        base.Initialize();

        _remainingMs = _totalMs;
        _dialog.Start();
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
