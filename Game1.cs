using Microsoft.Xna.Framework.Media;
using GameCore;
using TestGame.Scenes;

namespace TestGame;

public class Game1 : Core
{
    // The background theme song.
    private Song _themeSong;

    public Game1() : base("Jogo", 1920, 1080, true)
    {

    }

    protected override void Initialize()
    {
        base.Initialize();

        // Start playing the background music.
        Audio.PlaySong(_themeSong);

        // Start the game with the title scene.
        ChangeScene(new TitleScene());
    }

    protected override void LoadContent()
    {   
        // Load the background theme music.
        _themeSong = Content.Load<Song>("audio/theme");
    }
}
