using System.Collections.Generic;

namespace GameCore.DialogCutscene;

public class DialogCutsceneSteps
{
    public List<DialogCutsceneSpriteAnimation> spriteAnimations;
    public string caption;
    public bool hasCaptionFadeIn;
    public int durationInMillis;
}