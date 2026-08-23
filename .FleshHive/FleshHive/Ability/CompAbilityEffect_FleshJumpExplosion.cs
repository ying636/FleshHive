using RimWorld;
using Verse;

namespace FleshHive;

public class CompProperties_AbilityFleshJumpExplosion : CompProperties_AbilityEffect
{
    public CompProperties_AbilityFleshJumpExplosion()
    {
        compClass = typeof(CompAbilityEffect_FleshJumpExplosion);
    }

    public int baseDamage = 5;

    public float explosionRadius = 4.5f;
}

public class CompAbilityEffect_FleshJumpExplosion : CompAbilityEffect
{
    public new CompProperties_AbilityFleshJumpExplosion Props => (CompProperties_AbilityFleshJumpExplosion)props;

    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        base.Apply(target, dest);
        Pawn pawn = parent.pawn;
        if (Find.Selector.IsSelected(pawn))
        {
            Find.Selector.Deselect(pawn);
        }
        Map map = pawn.MapHeld;
        if (map == null)
        {
            return;
        }

        IntVec3 position = pawn.PositionHeld;
        GenExplosion.DoExplosion(
            position,
            map,
            Props.explosionRadius,
            DamageDefOf.Bomb,
            pawn,
            Props.baseDamage,
            chanceToStartFire: 0f,
            ignoredThings: new List<Thing> { pawn });
    }
}
