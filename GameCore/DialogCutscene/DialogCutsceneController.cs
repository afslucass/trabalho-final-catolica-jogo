
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameCore.DialogCutscene;

public class DialogCutsceneController
{
    private readonly List<DialogCutsceneSteps> dialogCutsceneSteps;
    private readonly Color textColor;
    private readonly SpriteFont font;
    private readonly GraphicsDevice graphicsDevice;

    private int clockInMillis;
    private int currentStepIndex;

    private bool hasStarted;
    private bool hasCalledOnStepEnter;

    private const float CaptionFadeDuration = 500f;

    public DialogCutsceneController(
        List<DialogCutsceneSteps> dialogCutsceneSteps,
        Color textColor,
        SpriteFont font,
        GraphicsDevice graphicsDevice)
    {
        this.dialogCutsceneSteps = dialogCutsceneSteps
            ?? throw new ArgumentNullException(nameof(dialogCutsceneSteps));

        this.textColor = textColor;
        this.font = font;
        this.graphicsDevice = graphicsDevice;

        this.clockInMillis = 0;
        this.currentStepIndex = 0;
        this.hasStarted = false;
        this.hasCalledOnStepEnter = false;
    }

    public void Start()
    {
        clockInMillis = 0;
        currentStepIndex = 0;
        hasStarted = dialogCutsceneSteps.Count > 0;
        hasCalledOnStepEnter = false;

        foreach (DialogCutsceneSteps step in dialogCutsceneSteps)
        {
            if (step.spriteAnimations == null)
                continue;

            foreach (DialogCutsceneSpriteAnimation animation in step.spriteAnimations)
            {
                if (animation?.keyPositions == null ||
                    animation.keyPositions.Count == 0)
                    continue;

                var keyframes = animation.keyPositions;

                // O primeiro keyframe define a posição inicial.
                keyframes[0].startPosition =
                    keyframes[0].spriteCurrentPosition;

                // Cada keyframe seguinte começa no destino do anterior.
                for (int i = 1; i < keyframes.Count; i++)
                {
                    keyframes[i].startPosition = new Vector2(
                        keyframes[i - 1].targetXPosition,
                        keyframes[i - 1].targetYPosition
                    );
                }
            }
        }
    }

    private float CalculateOpacity(
        bool hasTransition,
        float fadeDuration)
    {
        if (!hasTransition)
            return 1f;

        if (fadeDuration <= 0f)
            return 1f;

        return MathHelper.Clamp(
            this.clockInMillis / fadeDuration,
            0f,
            1f
        );
    }

    private void UpdateSpritePositions(DialogCutsceneSteps step)
    {
        if (step?.spriteAnimations == null)
            return;

        foreach (DialogCutsceneSpriteAnimation animation in step.spriteAnimations)
        {
            if (animation?.keyPositions == null ||
                animation.keyPositions.Count == 0)
                continue;

            var keyframes = animation.keyPositions;

            // O primeiro keyframe representa o ponto de partida.
            for (int i = 1; i < keyframes.Count; i++)
            {
                var current = keyframes[i];
                var previous = keyframes[i - 1];

                // Ainda não chegou ao tempo deste destino.
                if (clockInMillis < current.time)
                {
                    float duration = current.time - previous.time;

                    if (duration <= 0)
                        break;

                    float progress = MathHelper.Clamp(
                        (float)(clockInMillis - previous.time) / duration,
                        0f,
                        1f
                    );

                    progress = MathHelper.SmoothStep(0f, 1f, progress);

                    Vector2 targetPosition = new Vector2(
                        current.targetXPosition,
                        current.targetYPosition
                    );

                    Vector2 newPosition = Vector2.Lerp(
                        current.startPosition,
                        targetPosition,
                        progress
                    );

                    current.spriteCurrentPosition = newPosition;
                    current.onPositionChanged?.Invoke(newPosition);

                    break;
                }

                // O destino deste keyframe já foi alcançado.
                Vector2 reachedPosition = new Vector2(
                    current.targetXPosition,
                    current.targetYPosition
                );

                current.spriteCurrentPosition = reachedPosition;
                current.onPositionChanged?.Invoke(reachedPosition);
            }
        }
    }

    private void DrawCaption(
        SpriteBatch spriteBatch,
        DialogCutsceneSteps step)
    {
        if (string.IsNullOrEmpty(step.caption))
            return;

        float opacity = this.CalculateOpacity(
            step.hasCaptionFadeIn,
            CaptionFadeDuration
        );

        Vector2 textSize = this.font.MeasureString(step.caption);

        float screenWidth =
            this.graphicsDevice.PresentationParameters.BackBufferWidth;

        float screenHeight =
            this.graphicsDevice.PresentationParameters.BackBufferHeight;

        Vector2 textPosition = new Vector2(
            screenWidth / 2f - textSize.X / 2f,
            screenHeight - 120f - textSize.Y / 2f
        );

        spriteBatch.DrawString(
            this.font,
            step.caption,
            textPosition,
            this.textColor * opacity
        );
    }

    private void InvokeStepCallbacks(
        DialogCutsceneSteps step,
        bool isEntering)
    {
        if (step.spriteAnimations == null)
            return;

        foreach (DialogCutsceneSpriteAnimation animation in step.spriteAnimations)
        {
            if (animation?.keyPositions == null)
                continue;

            foreach (DialogCutsceneSpriteKeyPosition keyframe in animation.keyPositions)
            {
                if (isEntering)
                    keyframe.onStepEnter?.Invoke();
                else
                    keyframe.onStepFinish?.Invoke();
            }
        }
    }

    public void Draw(SpriteBatch spriteBatch, GameTime gameTime)
    {
        if (!this.hasStarted)
            return;

        if (this.currentStepIndex >= this.dialogCutsceneSteps.Count)
        {
            this.hasStarted = false;
            return;
        }

        DialogCutsceneSteps currentStep =
            this.dialogCutsceneSteps[this.currentStepIndex];

        if (!this.hasCalledOnStepEnter)
        {
            this.InvokeStepCallbacks(currentStep, true);
            this.hasCalledOnStepEnter = true;
        }

        this.clockInMillis +=
            (int)gameTime.ElapsedGameTime.TotalMilliseconds;

        this.UpdateSpritePositions(currentStep);

        this.DrawCaption(spriteBatch, currentStep);

        if (this.clockInMillis >= currentStep.durationInMillis)
        {
            this.InvokeStepCallbacks(currentStep, false);

            this.currentStepIndex++;
            this.clockInMillis = 0;
            this.hasCalledOnStepEnter = false;

            if (this.currentStepIndex >= this.dialogCutsceneSteps.Count)
                this.hasStarted = false;
        }
    }
}
