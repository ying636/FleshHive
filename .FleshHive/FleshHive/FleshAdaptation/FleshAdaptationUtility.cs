using RimWorld;
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
        return thing?.def?.tradeTags?.Contains(FleshHiveTags.FleshBuilding) == true;
    }

    public static bool IgnoresMoodThought(Pawn pawn, ThoughtDef thoughtDef)
    {
        if (!HasAdaptation(pawn))
        {
            return false;
        }

        return thoughtDef == FleshHiveDefOf.FH_Thought_FleshParasitism ||
               thoughtDef.defName == "FH_AteRawFleshberry" ||
               thoughtDef.defName == "FH_AteCookedFleshberry";
    }
}
