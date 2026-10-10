using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace GameCore.Input;

public static class PlayerControls
{
    public const float SPRINT_MULTIPLIER = 1.5f;

    /// <summary>
    /// Lê o teclado e devolve o deslocamento do frame.
    /// </summary>
    public static Vector2 GetMovement(float baseSpeed)
    {
        float speed = baseSpeed;
        if (Core.Input.Keyboard.IsKeyDown(Keys.Space))
        {
            speed *= SPRINT_MULTIPLIER;
        }

        Vector2 movement = Vector2.Zero;    

        if (Core.Input.Keyboard.IsKeyDown(Keys.W) || Core.Input.Keyboard.IsKeyDown(Keys.Up))
            movement.Y -= speed;

        if (Core.Input.Keyboard.IsKeyDown(Keys.S) || Core.Input.Keyboard.IsKeyDown(Keys.Down))
            movement.Y += speed;

        if (Core.Input.Keyboard.IsKeyDown(Keys.A) || Core.Input.Keyboard.IsKeyDown(Keys.Left))
            movement.X -= speed;

        if (Core.Input.Keyboard.IsKeyDown(Keys.D) || Core.Input.Keyboard.IsKeyDown(Keys.Right))
            movement.X += speed;

        return movement;
    }
}