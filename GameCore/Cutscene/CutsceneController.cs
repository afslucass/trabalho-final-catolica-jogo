
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameCore.Cutscene;

public class CutsceneController
{
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

        Vector2 textSize = this.font.MeasureString(currentStep.text);

        Vector2 textPosition = new Vector2(
            this.graphicsDevice.PresentationParameters.BackBufferWidth / 2f
                - textSize.X / 2f,
            this.graphicsDevice.PresentationParameters.BackBufferHeight
                - 120f
                - textSize.Y / 2f
        );

        spriteBatch.DrawString(
            this.font,
            currentStep.text,
            textPosition,
            this.textColor * this.textOpacity
        );
    }
}