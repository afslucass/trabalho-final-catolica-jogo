using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GameCore;
using GameCore.Camera;
using GameCore.Graphics;
using GameCore.Scenes;

namespace TestGame.Scenes;

public class Scene_recepcao : Scene
{
    private Camera2D _camera;
    private AnimatedSprite _slime;
    private Vector2 _slimePosition;

    public override void Initialize()
    {
        base.Initialize();
        _camera = new Camera2D();
        _slimePosition = new Vector2(100, 100); // Posição inicial nesta nova sala
    }

    public override void LoadContent()
    {
        base.LoadContent();

        TextureAtlas atlas = TextureAtlas.FromFile(Content, "images/atlas-definition.xml");
        

        _slime = atlas.CreateAnimatedSprite("slime-animation");
        _slime.Scale = new Vector2(4.0f, 4.0f);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        _slime.Update(gameTime);
        _camera.Follow(_slimePosition, Core.GraphicsDevice.Viewport);
        
    }

    public override void Draw(GameTime gameTime)
    {
        Core.GraphicsDevice.Clear(Color.DarkSlateGray);

        Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: _camera.TransformMatrix);
        _slime.Draw(Core.SpriteBatch, _slimePosition);
        Core.SpriteBatch.End();

        base.Draw(gameTime);
    }
}