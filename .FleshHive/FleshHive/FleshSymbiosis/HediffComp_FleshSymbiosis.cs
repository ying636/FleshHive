using RimWorld;
using Verse;

namespace FleshHive;

public class HediffComp_FleshSymbiosis : HediffComp
{
    public override void CompPostTick(ref float severityAdjustment)
    {
        base.CompPostTick(ref severityAdjustment);
        if (!Pawn.IsHashIntervalTick(CheckIntervalTicks))
        {
            return;
        }

        TryFeedFromFleshHive();
    }

    private void TryFeedFromFleshHive()
    {
        Need_Food food = Pawn.needs?.food;
        Map map = Pawn.MapHeld;
        if (food == null || map == null || food.CurLevelPercentage >= TriggerThreshold)
        {
            return;
        }

        float nutritionNeeded = food.NutritionWanted;
        MapFleshHive fleshHive = MapComponent_FleshHive.GetMapFleshHive(map);
        if (nutritionNeeded <= 0f || fleshHive == null || fleshHive.nutrition < nutritionNeeded)
        {
            return;
        }

        fleshHive.nutrition -= nutritionNeeded;
        food.CurLevel = food.MaxLevel;

        if (!Pawn.health.hediffSet.HasHediff(FleshHiveDefOf.FH_FleshAdaptation) && !Pawn.Inhumanized())
        {
            Pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(FleshHiveDefOf.FH_Thought_FleshNutrition);
        }
    }

    private const int CheckIntervalTicks = 250;
    private const float TriggerThreshold = 0.25f;
}
