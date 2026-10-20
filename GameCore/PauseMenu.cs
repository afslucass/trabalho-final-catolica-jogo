using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace GameCore;

public class PauseMenu
{
    private const string TITLE = "PAUSADO";

    private readonly SpriteFont _font;

    private readonly Texture2D _pixel;

    private List<(string Label, Action Action)> _options = new();
    private int _selected;

    /// <summary>Chamado quando o jogador escolhe "Continuar".</summary>
    public Action OnResume { get; set; }

    /// <summary>Chamado quando o jogador escolhe "Sair do jogo".</summary>
    public Action OnQuit { get; set; }

    /// <summary>
    /// Chamado quando o jogador escolhe "Voltar ao titulo". Se for null,
    /// a opção não aparece no menu. O jogo já é despausado antes de chamar.
    /// </summary>
    public Action OnReturnToTitle { get; set; }

    public PauseMenu(SpriteFont font)
    {
        _font = font;

        _pixel = new Texture2D(Core.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
    }

    /// <summary>
    /// Monta a lista de opções e volta a seleção para o topo.
    /// Deve ser chamado toda vez que o jogo é pausado.
    /// </summary>
    public void Open()
    {
        _selected = 0;

        _options = new List<(string, Action)>
        {
            ("Continuar", () => OnResume?.Invoke()),
            ("Som: ligar/desligar", () => Core.Audio.ToggleMute()),
        };

        if (OnReturnToTitle != null)
        {
            _options.Add(("Voltar ao titulo", () =>
            {
                OnResume?.Invoke();
                OnReturnToTitle?.Invoke();
            }));
        }

        _options.Add(("Sair do jogo", () => OnQuit?.Invoke()));
    }

    public void Update()
    {
        if (_options.Count == 0)
            return;

        var keyboard = Core.Input.Keyboard;

        if (keyboard.WasKeyJustPressed(Keys.Up) || keyboard.WasKeyJustPressed(Keys.W))
        {
            _selected = (_selected - 1 + _options.Count) % _options.Count;
        }

        if (keyboard.WasKeyJustPressed(Keys.Down) || keyboard.WasKeyJustPressed(Keys.S))
        {
            _selected = (_selected + 1) % _options.Count;
        }

        if (keyboard.WasKeyJustPressed(Keys.Enter))
        {
            _options[_selected].Action();
        }
    }

    /// <summary>
    /// Desenha o menu. Deve ser chamado entre um Begin e um End do SpriteBatch,
    /// depois de a cena congelada já ter sido desenhada.
    /// </summary>
    public void Draw(SpriteBatch spriteBatch)
    {
        int width = Core.GraphicsDevice.PresentationParameters.BackBufferWidth;
        int height = Core.GraphicsDevice.PresentationParameters.BackBufferHeight;

        // Película escura sobre a cena congelada.
        spriteBatch.Draw(_pixel, new Rectangle(0, 0, width, height), Color.Black * 0.6f);

        // Título centralizado.
        Vector2 titleSize = _font.MeasureString(TITLE);
        spriteBatch.DrawString(
            _font,
            TITLE,
            new Vector2((width - titleSize.X) * 0.5f, height * 0.25f),
            Color.White
        );

        float y = height * 0.40f;
        float spacing = _font.LineSpacing + 20;

        for (int i = 0; i < _options.Count; i++)
        {
            bool isSelected = i == _selected;
            string text = isSelected ? $"> {_options[i].Label}" : _options[i].Label;
            Vector2 size = _font.MeasureString(text);

            spriteBatch.DrawString(
                _font,
                text,
                new Vector2((width - size.X) * 0.5f, y),
                isSelected ? Color.Yellow : Color.White
            );

            y += spacing;
        }
    }
}