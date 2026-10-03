using System;
using GameCore.Graphics;

namespace GameCore.Cutscene;

public class Step
{
    public Sprite image;
    public string text;
    public int durationInMillis;
    public bool hasImageTransition;
    public bool hasTextTransition;
    public Action onStepEnter;
    public Action onStepFinish;
    public float fadeDuration;
}