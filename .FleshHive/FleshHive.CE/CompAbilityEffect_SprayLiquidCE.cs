using RimWorld;
using Verse;
using System.Reflection;

namespace FleshHive.CE;

public class CompAbilityEffect_SprayLiquidCE : CompAbilityEffect
{
    private CompAbilityEffect_SprayLiquid Original => original ??= new CompAbilityEffect_SprayLiquid { parent = parent, props = props };

    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        CompProperties_AbilitySprayLiquidCE properties = (CompProperties_AbilitySprayLiquidCE)props;
        foreach (IntVec3 cell in affectedCells(Original, target))
        {
            new Verb_ShootFleshTentacleCE().Fire(parent.pawn, cell, properties.projectileDef, parent.def.verbProperties.range, parent.verb.preventFriendlyFire);
        }
        properties.sprayEffecter?.Spawn(parent.pawn.Position, target.Cell, parent.pawn.Map).Cleanup();
        base.Apply(target, dest);
    }

    public override void DrawEffectPreview(LocalTargetInfo target) => Original.DrawEffectPreview(target);

    public override bool AICanTargetNow(LocalTargetInfo target) => Original.AICanTargetNow(target);

    private CompAbilityEffect_SprayLiquid? original;
    private static readonly Func<CompAbilityEffect_SprayLiquid, LocalTargetInfo, List<IntVec3>> affectedCells =
        (Func<CompAbilityEffect_SprayLiquid, LocalTargetInfo, List<IntVec3>>)typeof(CompAbilityEffect_SprayLiquid)
        .GetMethod("AffectedCells", BindingFlags.Instance | BindingFlags.NonPublic)
        .CreateDelegate(typeof(Func<CompAbilityEffect_SprayLiquid, LocalTargetInfo, List<IntVec3>>));
}
