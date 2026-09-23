using RimWorld;
using Verse;

namespace FleshHive;

public class StatPart_FleshCarapacePsychicSensitivity : StatPart
{
    public override void TransformValue(StatRequest req, ref float val)
    {
        if (req.Thing is not Apparel apparel || apparel.Stuff != FleshHiveDefOf.FH_FleshCarapace)
        {
            return;
        }

        apparel.TryGetQuality(out QualityCategory quality);
        val *= 1.03f + (int)quality * (0.07f / (int)QualityCategory.Legendary);
    }

    public override string ExplanationPart(StatRequest req)
    {
        return string.Empty;
    }
}
