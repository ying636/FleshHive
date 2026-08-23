using RimWorld;
using Verse;

namespace FleshHive;

public class StatPart_FleshPack : StatPart
{
    public override void TransformValue(StatRequest req, ref float val)
    {
        Pawn pawn = req.Pawn;
        if (pawn?.apparel == null)
        {
            return;
        }
        foreach (Apparel ap in pawn.apparel.WornApparel)
        {
            if (ap.TryGetComp<CompFleshPack>() != null)
            {
                val += 2;
            }
        }
    }

    public override string ExplanationPart(StatRequest req)
    {
        Pawn pawn = req.Pawn;
        if (pawn?.apparel == null)
        {
            return null;
        }
        foreach (Apparel ap in pawn.apparel.WornApparel)
        {
            if (ap.TryGetComp<CompFleshPack>() != null)
            {
                return ap.def.LabelCap + ": +2";
            }
        }
        return null;
    }
}
