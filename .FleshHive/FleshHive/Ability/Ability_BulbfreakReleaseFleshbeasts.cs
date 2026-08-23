using RimWorld;
using Verse;

namespace FleshHive;

public class CompProperties_AbilityBulbfreakReleaseFleshbeasts : CompProperties_AbilityEffect
{
    public CompProperties_AbilityBulbfreakReleaseFleshbeasts()
    {
        this.compClass = typeof(CompAbilityEffect_BulbfreakReleaseFleshbeasts);
    }

    public int spawnCount = 2;
    public int spawnRadius = 5;
}

public class CompAbilityEffect_BulbfreakReleaseFleshbeasts : CompAbilityEffect
{
    public new CompProperties_AbilityBulbfreakReleaseFleshbeasts Props =>
        (CompProperties_AbilityBulbfreakReleaseFleshbeasts)this.props;

    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        base.Apply(target, dest);
        Pawn caster = this.parent.pawn;
        Map map = caster.MapHeld;
        if (map == null || FleshBeastKindUtility.MediumKinds.Count == 0)
        {
            return;
        }

        FleshHiveFleshbeastSpawnUtility.SpawnRandomBySize(
            FleshBeastSize.Medium,
            Props.spawnCount,
            caster.Faction,
            caster.PositionHeld,
            map,
            Props.spawnRadius,
            caster,
            true,
            true);
    }

    public override bool AICanTargetNow(LocalTargetInfo target)
    {
        return this.parent.pawn.MapHeld != null && FleshBeastKindUtility.MediumKinds.Count > 0;
    }

    public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
    {
        return this.parent.pawn.MapHeld != null
               && FleshBeastKindUtility.MediumKinds.Count > 0
               && base.CanApplyOn(target, dest);
    }
}
