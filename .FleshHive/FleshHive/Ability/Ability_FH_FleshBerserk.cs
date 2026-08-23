using RimWorld;
using Verse;

namespace FleshHive;

public class CompProperties_AbilityFleshBerserk : CompProperties_AbilityEffect
{
    public CompProperties_AbilityFleshBerserk()
    {
        this.compClass = typeof(CompAbilityEffect_FH_FleshBerserk);
    }
}

public class CompAbilityEffect_FH_FleshBerserk : CompAbilityEffect
{
    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        base.Apply(target, dest);
        Pawn caster = this.parent.pawn;
        Map map = caster.MapHeld;
        if (map == null)
        {
            return;
        }

        MapComponent_FleshHive mapComp = map.GetComponent<MapComponent_FleshHive>();
        if (mapComp == null)
        {
            return;
        }

        foreach (Pawn pawn in mapComp.CachedFleshBeasts)
        {
            if (pawn.Faction != caster.Faction)
            {
                continue;
            }
            if (!pawn.Spawned || pawn.Dead)
            {
                continue;
            }
            pawn.health.AddHediff(FleshHiveDefOf.FH_Berserk);
        }
    }

    public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
    {
        return base.CanApplyOn(target, dest);
    }
}
