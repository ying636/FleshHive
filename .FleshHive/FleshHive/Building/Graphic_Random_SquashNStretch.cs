using UnityEngine;
using Verse;

namespace FleshHive;

public class Graphic_Random_SquashNStretch : Graphic_Random
{
    protected override System.Type SingleGraphicType => typeof(Graphic_Single_SquashNStretchSafe);

    public override Graphic GetColoredVersion(Shader newShader, Color newColor, Color newColorTwo)
    {
        if (newColorTwo != Color.white)
        {
            Log.ErrorOnce("Cannot use Graphic_Random_SquashNStretch.GetColoredVersion with a non-white colorTwo.", 9910251);
        }

        return GraphicDatabase.Get<Graphic_Random_SquashNStretch>(path, newShader, drawSize, newColor, Color.white, data);
    }
}
