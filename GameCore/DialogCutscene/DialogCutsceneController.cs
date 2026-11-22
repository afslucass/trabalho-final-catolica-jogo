using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GameCore.Graphics;

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

    // ---------- Balão de fala ----------
    // Todas as medidas abaixo estão em pixels de uma tela 1080p e são multiplicadas por
    // (altura da tela / 1080), então o balão mantém a proporção em qualquer resolução.
    private const string TextBoxAsset = "images/text_box";
    private const int TextBoxCornerSize = 54;        // Tamanho dos cantos na imagem (nine-slice).
    private const float ReferenceHeight = 1080f;

    private const float BoxMinWidth = 1302f;         // Tamanho original da imagem: o balão nunca fica menor que isso...
    private const float BoxMinHeight = 262f;         // ...e só cresce quando o texto não cabe.
    private const float BoxPaddingX = 80f;           // Espaço entre a borda do balão e o texto (horizontal).
    private const float BoxPaddingY = 56f;           // Espaço entre a borda do balão e o texto (vertical).
    private const float BoxBottomMargin = 60f;       // Distância do balão até a borda de baixo da tela.
    private const float BoxSideMargin = 20f;         // Distância mínima do balão até as bordas laterais.

    // O balão é carregado uma única vez, pelo Content global do Core (que não é
    // descarregado ao trocar de cena), e compartilhado por todos os diálogos.
    private static NineSlice s_textBox;
    private static bool s_textBoxLoadFailed;

    /// <summary>
    /// Liga ou desliga o balão de fala deste diálogo. Desligado, o texto é desenhado
    /// solto, como antes.
    /// </summary>
    public bool UseTextBox { get; set; } = true;

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

    // Carrega o balão na primeira vez que for preciso. Se a imagem não estiver no
    // Content (ou der qualquer erro), o diálogo continua funcionando, sem o balão.
    private static NineSlice GetTextBox()
    {
        if (s_textBox != null || s_textBoxLoadFailed)
            return s_textBox;

        try
        {
            Texture2D texture = Core.Content.Load<Texture2D>(TextBoxAsset);
            s_textBox = new NineSlice(texture, TextBoxCornerSize, TextBoxCornerSize);
        }
        catch (Exception ex)
        {
            s_textBoxLoadFailed = true;
            Debug.WriteLine($"[Dialog] Não foi possível carregar '{TextBoxAsset}': {ex.Message}");
        }

        return s_textBox;
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

        NineSlice textBox = this.UseTextBox ? GetTextBox() : null;

        Vector2 textPosition;

        if (textBox != null)
        {
            float uiScale = screenHeight / ReferenceHeight;

            // O balão tem o tamanho original da imagem e só cresce se o texto não couber.
            float boxWidth = MathF.Max(
                BoxMinWidth * uiScale,
                textSize.X + BoxPaddingX * 2f * uiScale
            );
            float boxHeight = MathF.Max(
                BoxMinHeight * uiScale,
                textSize.Y + BoxPaddingY * 2f * uiScale
            );

            // Nunca passa da largura da tela.
            boxWidth = MathF.Min(boxWidth, screenWidth - BoxSideMargin * 2f * uiScale);

            Rectangle bounds = new Rectangle(
                (int)((screenWidth - boxWidth) / 2f),
                (int)(screenHeight - BoxBottomMargin * uiScale - boxHeight),
                (int)boxWidth,
                (int)boxHeight
            );

            // O balão aparece com fade só no primeiro passo; nos seguintes ele fica
            // sempre visível, para não piscar a cada fala. O texto continua com o fade dele.
            float boxOpacity = this.currentStepIndex == 0 ? opacity : 1f;
            textBox.Draw(spriteBatch, bounds, uiScale, Color.White * boxOpacity);

            // Texto centralizado dentro do balão.
            textPosition = new Vector2(
                bounds.X + (bounds.Width - textSize.X) / 2f,
                bounds.Y + (bounds.Height - textSize.Y) / 2f
            );
        }
        else
        {
            // Sem balão: mesma posição de antes.
            textPosition = new Vector2(
                screenWidth / 2f - textSize.X / 2f,
                screenHeight - 120f - textSize.Y / 2f
            );
        }

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