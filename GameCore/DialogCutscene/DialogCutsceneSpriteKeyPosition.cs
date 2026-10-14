using System;
using Microsoft.Xna.Framework;

namespace GameCore.DialogCutscene;

public class DialogCutsceneSpriteKeyPosition
{
    public Vector2 spriteCurrentPosition;
    public int targetXPosition;
    public int targetYPosition;
    public int time;
    public Action<Vector2> onPositionChanged;

    public Action onStepEnter;
    public Action onStepFinish;
    public Vector2 startPosition;
}