using HiveCreatureFramework;
using Verse;

namespace FleshHive;

public class FusionResult_PawnKindWithHediffs : FusionResult_PawnKind
{
    public override void Do(Thing fuser, List<Thing> materials)
    {
        Pawn pawn = HCFGameUtility.SpawnUnit(fuser, this.kind);
        if (pawn?.health == null)
        {
            return;
        }

        foreach (HediffDef hediff in this.hediffs)
        {
            if (hediff != null)
            {
                pawn.health.AddHediff(hediff);
            }
        }
    }

    public List<HediffDef> hediffs = new List<HediffDef>();
}
