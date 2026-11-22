using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameCore.Graphics;

/// <summary>
/// Desenha o balão de fala (images/text_box) atrás de um bloco de texto.
/// É usado pelo DialogCutsceneController (conversas) e pelo CutsceneController
/// (cutscenes), então o visual e as medidas ficam definidos num lugar só.
/// </summary>
public static class TextBoxRenderer
{
    private const string TextBoxAsset = "images/text_box";
    private const int CornerSize = 54;               // Tamanho dos cantos na imagem (nine-slice).
    private const float ReferenceHeight = 1080f;

    private const float BoxMinWidth = 1302f;         // Tamanho original da imagem: o balão nunca fica menor que isso...
    private const float BoxMinHeight = 262f;         // ...e só cresce quando o texto não cabe.
    private const float PaddingX = 80f;              // Espaço entre a borda do balão e o texto (horizontal).
    private const float PaddingY = 56f;              // Espaço entre a borda do balão e o texto (vertical).
    private const float BottomMargin = 60f;          // Distância do balão até a borda de baixo da tela.
    private const float SideMargin = 20f;            // Distância mínima do balão até as bordas laterais.

    // O balão é carregado uma única vez, pelo Content global do Core (que não é
    // descarregado ao trocar de cena), e compartilhado por tudo que o usa.
    private static NineSlice s_box;
    private static bool s_loadFailed;

    public static bool IsAvailable => Get() != null;

    // Carrega o balão na primeira vez que for preciso. Se a imagem não estiver no
    // Content (ou der qualquer erro), tudo continua funcionando, sem o balão.
    private static NineSlice Get()
    {
        if (s_box != null || s_loadFailed)
        {
            return s_box;
        }

        try
        {
            Texture2D texture = Core.Content.Load<Texture2D>(TextBoxAsset);
            s_box = new NineSlice(texture, CornerSize, CornerSize);
        }
        catch (Exception ex)
        {
            s_loadFailed = true;
            Debug.WriteLine($"[TextBox] Não foi possível carregar '{TextBoxAsset}': {ex.Message}");
        }

        return s_box;
    }

    /// <summary>
    /// Largura máxima que uma linha de texto pode ter para caber dentro do balão
    /// no tamanho original. 
    /// </summary>
    public static float GetMaxTextWidth(GraphicsDevice graphicsDevice)
    {
        float scale = graphicsDevice.PresentationParameters.BackBufferHeight / ReferenceHeight;
        float screenWidth = graphicsDevice.PresentationParameters.BackBufferWidth;

        float inner = (BoxMinWidth - PaddingX * 2f) * scale;
        float fitsScreen = screenWidth - (SideMargin + PaddingX) * 2f * scale;

        return MathF.Min(inner, fitsScreen);
    }

    /// <param name="textSize">Tamanho do bloco de texto (largura da linha mais larga e altura total).</param>
    /// <param name="opacity">0 a 1 (usado no fade de entrada).</param>
    /// <param name="bounds">Retângulo do balão na tela.</param>
    public static bool Draw(
        SpriteBatch spriteBatch,
        GraphicsDevice graphicsDevice,
        Vector2 textSize,
        float opacity,
        out Rectangle bounds)
    {
        bounds = Rectangle.Empty;

        NineSlice box = Get();
        if (box == null)
        {
            return false;
        }

        float screenWidth = graphicsDevice.PresentationParameters.BackBufferWidth;
        float screenHeight = graphicsDevice.PresentationParameters.BackBufferHeight;
        float uiScale = screenHeight / ReferenceHeight;

        // O balão tem o tamanho original da imagem e só cresce se o texto não couber.
        float boxWidth = MathF.Max(BoxMinWidth * uiScale, textSize.X + PaddingX * 2f * uiScale);
        float boxHeight = MathF.Max(BoxMinHeight * uiScale, textSize.Y + PaddingY * 2f * uiScale);

        // Nunca passa da largura da tela.
        boxWidth = MathF.Min(boxWidth, screenWidth - SideMargin * 2f * uiScale);

        bounds = new Rectangle(
            (int)((screenWidth - boxWidth) / 2f),
            (int)(screenHeight - BottomMargin * uiScale - boxHeight),
            (int)boxWidth,
            (int)boxHeight
        );

        box.Draw(spriteBatch, bounds, uiScale, Color.White * opacity);
        return true;
    }
}