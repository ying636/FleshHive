using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive;

public class CompProperties_AbilityFleshSpread : CompProperties_AbilityEffect
{
    public CompProperties_AbilityFleshSpread()
    {
        this.compClass = typeof(CompAbilityEffect_FH_FleshSpread);
    }

    public float explosionRadius = 4.5f;

    public int baseDamage = 5;
}

public class CompAbilityEffect_FH_FleshSpread : CompAbilityEffect
{
    public new CompProperties_AbilityFleshSpread Props => (CompProperties_AbilityFleshSpread)this.props;

    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        base.Apply(target, dest);
        Pawn caster = this.parent.pawn;
        Map map = caster.MapHeld;
        if (map == null)
        {
            return;
        }

        IntVec3 cell = target.Cell;
        foreach (IntVec3 c in GenRadial.RadialCellsAround(cell, Props.explosionRadius, true))
        {
            if (c.InBounds(map) &&
                c.Walkable(map) &&
                !FleshTerrainUtility.IsFleshTerrain(map, c) &&
                !c.GetTerrain(map).IsRiver &&
                !c.GetTerrain(map).IsWater)
            {
                map.terrainGrid.SetTerrain(c, TerrainDefOf.Flesh);
            }
        }

        GenExplosion.DoExplosion(cell, map, Props.explosionRadius, DamageDefOf.Bomb, caster,
            damAmount: Props.baseDamage);
    }

    public override void DrawEffectPreview(LocalTargetInfo target)
    {
        GenDraw.DrawRadiusRing(target.Cell, Props.explosionRadius, Color.red);
    }

    public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
    {
        Pawn caster = this.parent.pawn;
        if (caster == null)
        {
            return false;
        }
        return base.CanApplyOn(target, dest);
    }
}
