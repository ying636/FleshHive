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

        if (TryGetCharge(pawn, out HediffComp_TrispikeCharge charge))
        {
            charge.SetActive(false);
        }

        for (int i = 0; i < 2; i++)
        {
            SpawnSpikePawn(pawn, map);
        }
    }

    public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
    {
        Pawn pawn = this.parent.pawn;
        return pawn.MapHeld != null && TryGetCharge(pawn, out HediffComp_TrispikeCharge charge) && charge.Active;
    }

    internal static bool TryGetCharge(Pawn pawn, out HediffComp_TrispikeCharge charge)
    {
        if (pawn.health?.hediffSet?.hediffs == null)
        {
            charge = null;
            return false;
        }

        for (int i = 0; i < pawn.health.hediffSet.hediffs.Count; i++)
        {
            if (pawn.health.hediffSet.hediffs[i] is HediffWithComps hediffWithComps &&
                hediffWithComps.TryGetComp<HediffComp_TrispikeCharge>() is { } c)
            {
                charge = c;
                return true;
            }
        }

        charge = null;
        return false;
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

public class Ability_TrispikeRelease : Ability
{
    public Ability_TrispikeRelease(Pawn pawn) : base(pawn)
    {
    }

    public Ability_TrispikeRelease(Pawn pawn, AbilityDef def) : base(pawn, def)
    {
    }

    public override AcceptanceReport CanCast
    {
        get
        {
            AcceptanceReport baseReport = base.CanCast;
            if (!baseReport.Accepted)
            {
                return baseReport;
            }

            if (!CompAbilityEffect_TrispikeRelease.TryGetCharge(this.pawn, out HediffComp_TrispikeCharge charge))
            {
                return false;
            }

            return charge.Active ? baseReport : "FH_TrispikeNeedsTwistedFlesh".Translate();
        }
    }
}
