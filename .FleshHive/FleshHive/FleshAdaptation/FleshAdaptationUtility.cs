using Verse;

namespace FleshHive;

public static class FleshAdaptationUtility
{
    public static bool HasAdaptation(Pawn pawn)
    {
        return pawn?.health?.hediffSet?.HasHediff(FleshHiveDefOf.FH_FleshAdaptation) == true;
    }

    public static bool IsFleshBeautyThing(Thing thing)
    {
        return thing?.def?.tradeTags?.Contains(FleshHiveTags.FleshAdaptationBeauty) == true
            || thing?.Stuff?.tradeTags?.Contains(FleshHiveTags.FleshAdaptationBeauty) == true;
    }

}
