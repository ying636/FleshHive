using RimWorld;
using RimWorld.Utility;
using Verse;

namespace FleshHive.Effect;

public class CompProperties_AbilityTrispikeRelease : CompProperties_AbilityEffect
{
    public CompProperties_AbilityTrispikeRelease()
    {
        this.compClass = typeof(CompAbilityEffect_TrispikeRelease);
    }
}

public class CompAbilityEffect_TrispikeRelease : CompAbilityEffect
{
    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        base.Apply(target, dest);
        Pawn pawn = this.parent.pawn;
        Map map = pawn.MapHeld;
        if (map == null)
        {
            return;
        }

        for (int i = 0; i < 2; i++)
        {
            SpawnSpikePawn(pawn, map);
        }
    }

    public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
    {
        Pawn pawn = this.parent.pawn;
        return pawn.MapHeld != null && base.CanApplyOn(target, dest);
    }

    private static void SpawnSpikePawn(Pawn parent, Map map)
    {
        IntVec3 position = parent.Position;
        for (int i = 0; i < GenAdj.AdjacentCellsAndInside.Length; i++)
        {
            IntVec3 c = position + GenAdj.AdjacentCellsAndInside[i];
            if (c.InBounds(map) && c.Walkable(map))
            {
                position = c;
                break;
            }
        }

        Pawn pawn = FleshHive.FleshHiveFleshbeastSpawnUtility.GenerateRandomPawn(FleshBeastSize.Small, parent.Faction);
        FleshHive.FleshHiveFleshbeastSpawnUtility.SpawnPawnAsFlyer(pawn, position, map, 5, parent);
    }
}
