using System;
using CombatExtended;
using HarmonyLib;
using Verse;

namespace FleshHive.CE;

[HarmonyPatch(typeof(GenExplosionCE), nameof(GenExplosionCE.DoExplosion))]
public static class Patch_FleshProjectileExplosionCE
{
    public static void Prefix(ThingDef projectile, ref bool doVisualEffects)
    {
        if (string.Equals(projectile?.modContentPack?.PackageIdPlayerFacing, "HaiLuan.FleshHive", StringComparison.OrdinalIgnoreCase))
        {
            doVisualEffects &= projectile.projectile.doExplosionVFX;
        }
    }
}
