using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GameCore.Graphics;

namespace GameCore.Cutscene;

public class CutsceneController
{
    private const float MAX_TEXT_WIDTH_RATIO = 0.8f;
    private const float TEXT_BOTTOM_MARGIN = 120f;

    private List<Step> steps;
    private Color backgroundColor;
    private Color textColor;
    private int clockInMillis;
    private bool hasStarted;
    private bool hasCalledOnStepEnter;
    private SpriteFont font;
    private int currentStepIndex;
    private GraphicsDevice graphicsDevice;

    private float imageOpacity = 1f;
    private float textOpacity = 1f;
    private readonly HashSet<char> supportedChars;

    private readonly char fallbackChar;

    private List<string> lines = new();
    private float blockWidth;
    private int linesStepIndex = -1;

    public bool UseTextBox { get; set; } = true;
    public bool IsPlaying => hasStarted;

    public CutsceneController(
        List<Step> steps,
        Color backgroundColor,
        Color textColor,
        SpriteFont font,
        GraphicsDevice graphicsDevice)
    {
        this.steps = steps;
        this.backgroundColor = backgroundColor;
        this.font = font;
        this.textColor = textColor;
        this.graphicsDevice = graphicsDevice;

        this.supportedChars = new HashSet<char>(font.Characters);
        this.fallbackChar = font.DefaultCharacter
            ?? (this.supportedChars.Contains('?') ? '?' : ' ');

        this.currentStepIndex = 0;
        this.clockInMillis = 0;
        this.hasStarted = false;
        this.hasCalledOnStepEnter = false;
    }

    public void Start()
    {
        this.clockInMillis = 0;
        this.currentStepIndex = 0;
        this.hasStarted = this.steps.Count > 0;
        this.hasCalledOnStepEnter = false;
        this.imageOpacity = 1f;
        this.textOpacity = 1f;
        this.linesStepIndex = -1;
    }

    private float CalculateOpacity(bool hasTransition, float fadeDuration)
    {
        if (!hasTransition)
            return 1f;

        return MathHelper.Clamp(
            this.clockInMillis / fadeDuration,
            0f,
            1f
        );
    }
    private string MakeDrawable(string text)
    {
        StringBuilder result = new(text.Length);

        foreach (char c in text)
        {
            if (c == '\n' || this.supportedChars.Contains(c))
            {
                result.Append(c);
                continue;
            }

            switch (c)
            {
                case '\r':
                    break; // ignora retorno de carro
                case '\u2026': // …
                    result.Append("...");
                    break;
                case '\u201C': // “
                case '\u201D': // ”
                    result.Append('"');
                    break;
                case '\u2018': // ‘
                case '\u2019': // ’
                    result.Append('\'');
                    break;
                case '\u2013': // –
                case '\u2014': // —
                    result.Append('-');
                    break;
                case '\u00A0': // espaço sem quebra
                    result.Append(' ');
                    break;
                default:
                    result.Append(this.fallbackChar);
                    break;
            }
        }

        return result.ToString();
    }

    // Largura máxima de uma linha: a que cabe dentro do balão ou, sem balão,
    // uma fração da largura da tela.
    private float GetMaxLineWidth()
    {
        if (this.UseTextBox && TextBoxRenderer.IsAvailable)
        {
            return TextBoxRenderer.GetMaxTextWidth(this.graphicsDevice);
        }

        return this.graphicsDevice.PresentationParameters.BackBufferWidth * MAX_TEXT_WIDTH_RATIO;
    }
    private List<string> BuildLines(string text)
    {
        List<string> result = new();

        if (string.IsNullOrEmpty(text))
            return result;

        string drawable = MakeDrawable(text);

        float maxWidth = GetMaxLineWidth();
        float spaceWidth = this.font.MeasureString(" ").X;

        foreach (string paragraph in drawable.Split('\n'))
        {
            StringBuilder line = new();
            float lineWidth = 0f;

            foreach (string word in paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                float wordWidth = this.font.MeasureString(word).X;

                if (lineWidth > 0f && lineWidth + spaceWidth + wordWidth > maxWidth)
                {
                    // A palavra não cabe na linha atual, então começa uma nova.
                    result.Add(line.ToString());
                    line.Clear();
                    lineWidth = 0f;
                }
                else if (lineWidth > 0f)
                {
                    line.Append(' ');
                    lineWidth += spaceWidth;
                }

                line.Append(word);
                lineWidth += wordWidth;
            }

            // Guarda a última linha do parágrafo (ou uma linha vazia, se for uma quebra manual).
            result.Add(line.ToString());
        }

        return result;
    }

    public void Draw(SpriteBatch spriteBatch, GameTime gameTime)
    {
        if (!this.hasStarted)
            return;

        if (this.currentStepIndex >= this.steps.Count)
        {
            this.hasStarted = false;
            return;
        }

        spriteBatch.GraphicsDevice.Clear(this.backgroundColor);

        if (!this.hasCalledOnStepEnter)
        {
            this.steps[this.currentStepIndex].onStepEnter?.Invoke();
            this.hasCalledOnStepEnter = true;
        }

        this.clockInMillis +=
            (int)gameTime.ElapsedGameTime.TotalMilliseconds;

        Step currentStep = this.steps[this.currentStepIndex];

        if (this.clockInMillis >= currentStep.durationInMillis)
        {
            currentStep.onStepFinish?.Invoke();

            this.currentStepIndex++;
            this.clockInMillis = 0;
            this.hasCalledOnStepEnter = false;

            if (this.currentStepIndex >= this.steps.Count)
            {
                this.hasStarted = false;
                return;
            }

            currentStep = this.steps[this.currentStepIndex];
            currentStep.onStepEnter?.Invoke();
            this.hasCalledOnStepEnter = true;
        }

        this.imageOpacity = CalculateOpacity(
            currentStep.hasImageTransition,
            this.steps[this.currentStepIndex].fadeDuration
        );

        this.textOpacity = CalculateOpacity(
            currentStep.hasTextTransition,
            this.steps[this.currentStepIndex].fadeDuration
        );

        Vector2 position = new Vector2(
            this.graphicsDevice.PresentationParameters.BackBufferWidth / 2f,
            this.graphicsDevice.PresentationParameters.BackBufferHeight / 2f
        );

        currentStep.image.CenterOrigin();
        currentStep.image.Color = Color.White * this.imageOpacity;
        currentStep.image.Draw(
            spriteBatch,
            position
        );

        // Quebra o texto do passo em linhas uma única vez, quando o passo muda.
        if (this.linesStepIndex != this.currentStepIndex)
        {
            this.lines = BuildLines(currentStep.text);

            this.blockWidth = 0f;
            foreach (string line in this.lines)
            {
                this.blockWidth = MathF.Max(this.blockWidth, this.font.MeasureString(line).X);
            }

            this.linesStepIndex = this.currentStepIndex;
        }

        if (this.lines.Count == 0)
            return;

        float screenWidth = this.graphicsDevice.PresentationParameters.BackBufferWidth;
        float screenHeight = this.graphicsDevice.PresentationParameters.BackBufferHeight;

        float lineHeight = this.font.LineSpacing;
        float blockHeight = this.lines.Count * lineHeight;

        float top;
        float centerX;

        // O balão aparece com fade só no primeiro passo; nos seguintes ele fica sempre
        // visível, para não piscar a cada fala. O texto continua com o fade dele.
        float boxOpacity = this.currentStepIndex == 0 ? this.textOpacity : 1f;

        if (this.UseTextBox &&
            TextBoxRenderer.Draw(
                spriteBatch,
                this.graphicsDevice,
                new Vector2(this.blockWidth, blockHeight),
                boxOpacity,
                out Rectangle bounds))
        {
            // Bloco de texto centralizado dentro do balão.
            top = bounds.Y + (bounds.Height - blockHeight) / 2f;
            centerX = bounds.X + bounds.Width / 2f;
        }
        else
        {
            // Sem balão: o bloco de texto cresce para cima a partir da posição original
            // (com uma linha só, fica exatamente onde ficava antes).
            top = screenHeight - TEXT_BOTTOM_MARGIN + lineHeight / 2f - blockHeight;
            centerX = screenWidth / 2f;
        }

        for (int i = 0; i < this.lines.Count; i++)
        {
            string line = this.lines[i];
            if (line.Length == 0)
                continue;

            Vector2 lineSize = this.font.MeasureString(line);

            Vector2 linePosition = new Vector2(
                centerX - lineSize.X / 2f,
                top + i * lineHeight
            );

            spriteBatch.DrawString(
                this.font,
                line,
                linePosition,
                this.textColor * this.textOpacity
            );
        }
    }
}