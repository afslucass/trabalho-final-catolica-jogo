using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

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
    private int linesStepIndex = -1;

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
    private List<string> BuildLines(string text)
    {
        List<string> result = new();

        if (string.IsNullOrEmpty(text))
            return result;


        float maxWidth =
            this.graphicsDevice.PresentationParameters.BackBufferWidth * MAX_TEXT_WIDTH_RATIO;
        float spaceWidth = this.font.MeasureString(" ").X;

        foreach (string paragraph in text.Split('\n'))
        {
            StringBuilder line = new();
            float lineWidth = 0f;

            foreach (string word in paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                float wordWidth = this.font.MeasureString(word).X;

                if (lineWidth > 0f && lineWidth + spaceWidth + wordWidth > maxWidth)
                {
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
            this.linesStepIndex = this.currentStepIndex;
        }
        float lineHeight = this.font.LineSpacing;
        float blockHeight = this.lines.Count * lineHeight;
        float top =
            this.graphicsDevice.PresentationParameters.BackBufferHeight
                - TEXT_BOTTOM_MARGIN
                + lineHeight / 2f
                - blockHeight;

        for (int i = 0; i < this.lines.Count; i++)
        {
            string line = this.lines[i];
            if (line.Length == 0)
                continue;

            Vector2 lineSize = this.font.MeasureString(line);

            Vector2 linePosition = new Vector2(
                this.graphicsDevice.PresentationParameters.BackBufferWidth / 2f
                    - lineSize.X / 2f,
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