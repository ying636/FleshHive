using RimWorld;
using Verse;

namespace FleshHive.CE;

public class CompAbilityEffect_FleshPulseCE : CompAbilityEffect_BastionmeldFleshPulse
{
    public new CompProperties_AbilityFleshPulseCE Props => (CompProperties_AbilityFleshPulseCE)props;

    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        Pawn caster = parent.pawn;
        Map map = caster.MapHeld;
        if (map == null)
        {
            return;
        }

        GenExplosion.DoExplosion(caster.PositionHeld, map, Props.radius, DamageDefOf.EMP, caster,
            Props.damageAmount, -1f, Props.explosionSound, ignoredThings: new List<Thing> { caster });
    }
}
