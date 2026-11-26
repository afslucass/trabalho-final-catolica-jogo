using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using GameCore;
using GameCore.Save;
using GameCore.Scenes;

namespace TestGame.Scenes;

public class TitleScene : Scene
    {
    private const string DUNGEON_TEXT = "";
    private const string SLIME_TEXT = "beyond death";

    // Opções do menu.
    private const string CONTINUE_TEXT = "Continuar";
    private const string NEW_GAME_TEXT = "Novo jogo";
    private const string LOAD_ERROR_TEXT = "Nao foi possivel carregar o save";

    // Posição vertical da primeira opção do menu e tempo que a mensagem de erro fica na tela.
    private const float MENU_TOP = 560f;
    private const double MESSAGE_MS = 2500;

    // The font to use to render normal text.
    private SpriteFont _font;

    // The font used to render the title text.
    private SpriteFont _font5x;

    // The position to draw the dungeon text at.
    private Vector2 _dungeonTextPos;

    // The origin to set for the dungeon text.
    private Vector2 _dungeonTextOrigin;

    // The position to draw the slime text at.
    private Vector2 _slimeTextPos;

    // The origin to set for the slime text.
    private Vector2 _slimeTextOrigin;

    // Menu: lista de opções (texto e ação) e a opção selecionada.
    private readonly List<(string Label, Action Action)> _options = new();
    private int _selected;

    // Se o save existe mas não pôde ser carregado (arquivo corrompido, por exemplo),
    // o "Continuar" some do menu até o jogo ser reaberto.
    private bool _saveUnusable;

    // Tempo restante da mensagem de erro de carregamento.
    private double _messageRemainingMs;

    public override bool CanPause => false;

    public override void Initialize()
    {
        // LoadContent is called during base.Initialize().
        base.Initialize();

        // While on the title screen, we can enable exit on escape so the player
        // can close the game by pressing the escape key.
        Core.ExitOnEscape = true;

        // Set the position and origin for the Dungeon text.
        Vector2 size = _font5x.MeasureString(DUNGEON_TEXT);
        _dungeonTextPos = new Vector2(Core.GraphicsDevice.PresentationParameters.BackBufferWidth / 2, 100);
        _dungeonTextOrigin = size * 0.5f;

        // Set the position and origin for the Slime text.
        size = _font5x.MeasureString(SLIME_TEXT);
        _slimeTextPos = new Vector2(Core.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 + 24, 207);
        _slimeTextOrigin = size * 0.5f;

        // Monta o menu (o "Continuar" só aparece se existir um save).
        BuildOptions();
    }

    public override void LoadContent()
    {
        // Load the font for the standard text.
        _font = Core.Content.Load<SpriteFont>("fonts/KiwiSoda");

        // Load the font for the title text.
        _font5x = Content.Load<SpriteFont>("fonts/04B_30_5x");
    }

    // Monta a lista de opções. Se existe um save utilizável, "Continuar" vem primeiro
    // (e já começa selecionado); senão, só "Novo jogo".
    private void BuildOptions()
    {
        _options.Clear();

        if (!_saveUnusable && SaveManager.HasSave())
        {
            _options.Add((CONTINUE_TEXT, OnContinue));
        }

        _options.Add((NEW_GAME_TEXT, OnNewGame));

        _selected = 0;
    }

    // Carrega o save e abre a cena em que o jogador estava.
    // O SceneRegistry.ContinueGame já troca de cena sozinho quando dá certo.
    private void OnContinue()
    {
        if (SceneRegistry.ContinueGame())
        {
            return;
        }

        // Falhou (o arquivo sumiu, está corrompido ou é de uma versão mais nova do jogo):
        // esconde o "Continuar" e avisa o jogador. O jogo continua normalmente.
        _saveUnusable = true;
        BuildOptions();
        _messageRemainingMs = MESSAGE_MS;
    }

    // Começa um jogo novo. O GameSession é estático e sobrevive a uma volta ao título,
    // então é preciso zerá-lo aqui para não herdar itens e estágio da partida anterior.
    // O save antigo não é apagado: ele só é substituído quando o jogador salvar de novo.
    private void OnNewGame()
    {
        GameSession.Reset();
        Core.ChangeScene(new Scene_recepcao());
    }

    public override void Update(GameTime gameTime)
    {
        if (_messageRemainingMs > 0)
        {
            _messageRemainingMs -= gameTime.ElapsedGameTime.TotalMilliseconds;
        }

        var keyboard = Core.Input.Keyboard;

        // Setas ou W/S movem a seleção.
        if (keyboard.WasKeyJustPressed(Keys.Up) || keyboard.WasKeyJustPressed(Keys.W))
        {
            _selected = (_selected - 1 + _options.Count) % _options.Count;
        }

        if (keyboard.WasKeyJustPressed(Keys.Down) || keyboard.WasKeyJustPressed(Keys.S))
        {
            _selected = (_selected + 1) % _options.Count;
        }

        // Enter (ou F) confirma a opção selecionada.
        if (keyboard.WasKeyJustPressed(Keys.Enter) || keyboard.WasKeyJustPressed(Keys.F))
        {
            _options[_selected].Action();
        }
    }

    public override void Draw(GameTime gameTime)
    {
        Core.GraphicsDevice.Clear(new Color(0, 0, 0, 0));

        // Begin the sprite batch to prepare for rendering.
        Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);

        // The color to use for the drop shadow text.
        Color dropShadowColor = Color.Black * 0.5f;

        // Draw the Dungeon text slightly offset from it is original position and
        // with a transparent color to give it a drop shadow.
        Core.SpriteBatch.DrawString(_font5x, DUNGEON_TEXT, _dungeonTextPos + new Vector2(10, 10), dropShadowColor, 0.0f, _dungeonTextOrigin, 1.0f, SpriteEffects.None, 1.0f);

        // Draw the Dungeon text on top of that at its original position.
        Core.SpriteBatch.DrawString(_font5x, DUNGEON_TEXT, _dungeonTextPos, Color.White, 0.0f, _dungeonTextOrigin, 1.0f, SpriteEffects.None, 1.0f);

        // Draw the Slime text slightly offset from it is original position and
        // with a transparent color to give it a drop shadow.
        Core.SpriteBatch.DrawString(_font5x, SLIME_TEXT, _slimeTextPos + new Vector2(10, 10), dropShadowColor, 0.0f, _slimeTextOrigin, 1.0f, SpriteEffects.None, 1.0f);

        // Draw the Slime text on top of that at its original position.
        Core.SpriteBatch.DrawString(_font5x, SLIME_TEXT, _slimeTextPos, Color.White, 0.0f, _slimeTextOrigin, 1.0f, SpriteEffects.None, 1.0f);

        // Desenha o menu: opções centralizadas, a selecionada em amarelo e com "> ".
        float centerX = Core.GraphicsDevice.PresentationParameters.BackBufferWidth / 2f;
        float y = MENU_TOP;
        float spacing = _font.LineSpacing + 20;

        for (int i = 0; i < _options.Count; i++)
        {
            bool isSelected = i == _selected;
            string text = isSelected ? $"> {_options[i].Label}" : _options[i].Label;
            Vector2 size = _font.MeasureString(text);

            Core.SpriteBatch.DrawString(
                _font,
                text,
                new Vector2(centerX - size.X / 2f, y),
                isSelected ? Color.Yellow : Color.White
            );

            y += spacing;
        }

        // Mensagem de erro, logo abaixo da última opção.
        if (_messageRemainingMs > 0)
        {
            Vector2 messageSize = _font.MeasureString(LOAD_ERROR_TEXT);
            Core.SpriteBatch.DrawString(
                _font,
                LOAD_ERROR_TEXT,
                new Vector2(centerX - messageSize.X / 2f, y + 20),
                Color.OrangeRed
            );
        }

        // Always end the sprite batch when finished.
        Core.SpriteBatch.End();
    }
}