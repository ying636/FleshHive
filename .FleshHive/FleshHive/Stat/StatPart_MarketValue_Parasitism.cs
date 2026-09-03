using RimWorld;
using Verse;

namespace FleshHive;

public class StatPart_MarketValue_Parasitism : StatPart
{
    public override void TransformValue(StatRequest req, ref float val)
    {
        if (req.Thing is not Pawn pawn || pawn.health?.hediffSet == null)
        {
            return;
        }

        if (pawn.health.hediffSet.GetFirstHediffOfDef(FleshHiveDefOf.FH_ParasitismSystem) is not ParasitismSystem system)
        {
            return;
        }

        foreach (ParasitismHediff hediff in system.ParasitismHediffs)
        {
            if (hediff.flesh == null)
            {
                continue;
            }

            val += FleshBeastKindUtility.SizeOf(hediff.flesh.kindDef) switch
            {
                FleshBeastSize.Small => 120f,
                FleshBeastSize.Medium => 300f,
                FleshBeastSize.Large => 800f,
                FleshBeastSize.Giant => 1800f,
                _ => 0f
            };
        }
    }

    public override string ExplanationPart(StatRequest req)
    {
        return null;
    }
}
