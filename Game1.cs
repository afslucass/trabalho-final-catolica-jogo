using Microsoft.Xna.Framework.Media;
using GameCore;
using TestGame.Scenes;

namespace TestGame;

public class Game1 : Core
{
    // The background theme song.
    public Game1() : base("Jogo", 1920, 1080, true)
    {

    }

    protected override void Initialize()
    {
        base.Initialize();

        // Start the game with the title scene.
        ChangeScene(new TitleScene());
    }

}
