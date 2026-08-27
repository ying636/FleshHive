using RimWorld;
using Verse;

namespace FleshHive;

public class GenStep_UndercaveInterest_FleshHive : GenStep_UndercaveInterest
{
    public override void Generate(Map map, GenStepParams parms)
    {
        PawnKindDef vanillaDreadmeld = PawnKindDefOf.Dreadmeld;
        PawnKindDefOf.Dreadmeld = FleshHiveDefOf.FH_Dreadmeld;
        try
        {
            base.Generate(map, parms);
        }
        finally
        {
            PawnKindDefOf.Dreadmeld = vanillaDreadmeld;
        }
    }
}
