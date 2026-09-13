using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GameCore;
using GameCore.Scenes;

namespace TestGame.Scenes;

public class Scene_recepcao : Scene
{
    public override void Initialize()
    {
        base.Initialize();
    }

    public override void LoadContent()
    {
        base.LoadContent();
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
    }

    public override void Draw(GameTime gameTime)
    {
        // Limpa a tela com uma cor diferente (ex: preto ou verde escuro)
        // para confirmar visualmente que a cena mudou
        Core.GraphicsDevice.Clear(Color.White);

        base.Draw(gameTime);
    }
}