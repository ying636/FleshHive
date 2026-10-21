using RimWorld;
using Verse;
using Verse.AI;

namespace FleshHive;

public class WorkGiver_HarvestBulbplump : WorkGiver_Milk
{
    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
    {
        return pawn.Map.GetComponent<MapComponent_FleshHive>().CachedFleshBeasts
            .Where(target => target.def.defName == "FH_Bulbplump" && target.Faction == pawn.Faction);
    }

    public override bool ShouldSkip(Pawn pawn, bool forced = false)
    {
        return !PotentialWorkThingsGlobal(pawn).Any(target => target.TryGetComp<CompMilkable>()?.ActiveAndFull == true);
    }

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        return t is Pawn target && target.def.defName == "FH_Bulbplump" && target.Faction == pawn.Faction
            && target.TryGetComp<CompMilkable>()?.ActiveAndFull == true
            && !target.Downed && (target.roping == null || !target.roping.IsRopedByPawn)
            && target.CanCasuallyInteractNow() && pawn.CanReserve(target, 1, -1, null, forced);
    }
}
