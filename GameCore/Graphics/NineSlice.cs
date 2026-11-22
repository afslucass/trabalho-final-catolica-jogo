using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameCore.Graphics;

public class NineSlice
{
    private readonly Texture2D _texture;

    // Tamanho dos cantos na imagem original, em pixels.
    private readonly int _cornerWidth;
    private readonly int _cornerHeight;

    public Texture2D Texture => _texture;
    public int CornerWidth => _cornerWidth;
    public int CornerHeight => _cornerHeight;

    /// <param name="texture">Imagem da caixa (já recortada, sem margem transparente).</param>
    /// <param name="cornerWidth">Largura dos cantos na imagem (54 no text_box.png).</param>
    /// <param name="cornerHeight">Altura dos cantos na imagem (54 no text_box.png).</param>
    public NineSlice(Texture2D texture, int cornerWidth, int cornerHeight)
    {
        _texture = texture;
        _cornerWidth = cornerWidth;
        _cornerHeight = cornerHeight;
    }

    /// <summary>
    /// Desenha a caixa dentro do retângulo destination.
    /// Deve ser chamado entre um Begin e um End do SpriteBatch.
    /// </summary>
    /// <param name="destination">Onde a caixa vai ficar na tela.</param>
    /// <param name="scale">Escala dos cantos e das bordas (use 1 se a tela for 1080p;
    /// em outras resoluções, altura da tela / 1080, para a moldura ter a mesma proporção).</param>
    /// <param name="color">Cor de tinta (Color.White desenha como está; Color.White * 0.9f deixa levemente transparente).</param>
    public void Draw(SpriteBatch spriteBatch, Rectangle destination, float scale, Color color)
    {
        int sourceWidth = _texture.Width;
        int sourceHeight = _texture.Height;

        int cornerW = Math.Min((int)MathF.Round(_cornerWidth * scale), destination.Width / 2);
        int cornerH = Math.Min((int)MathF.Round(_cornerHeight * scale), destination.Height / 2);


        int[] dx = { destination.X, destination.X + cornerW, destination.Right - cornerW, destination.Right };
        int[] dy = { destination.Y, destination.Y + cornerH, destination.Bottom - cornerH, destination.Bottom };

        int[] sx = { 0, _cornerWidth, sourceWidth - _cornerWidth, sourceWidth };
        int[] sy = { 0, _cornerHeight, sourceHeight - _cornerHeight, sourceHeight };

        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                Rectangle dest = new Rectangle(dx[col], dy[row], dx[col + 1] - dx[col], dy[row + 1] - dy[row]);
                Rectangle src = new Rectangle(sx[col], sy[row], sx[col + 1] - sx[col], sy[row + 1] - sy[row]);

                if (dest.Width > 0 && dest.Height > 0)
                {
                    spriteBatch.Draw(_texture, dest, src, color);
                }
            }
        }
    }
}