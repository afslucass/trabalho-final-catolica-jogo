using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameCore.Camera;

public class Camera2D
{
    public Matrix TransformMatrix { get; private set; }
    public Vector2 Position { get; set; }

    public void Follow(Vector2 targetPosition, Viewport viewport)
    {
        Position = new Vector2(
            targetPosition.X - viewport.Width / 2f,
            targetPosition.Y - viewport.Height / 2f
        );

        TransformMatrix = Matrix.CreateTranslation(new Vector3(-Position, 0f));
    }
}
