using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive;

[HarmonyPatch(typeof(Ability), nameof(Ability.StartCooldown), typeof(int))]
public static class Patch_Ability_StartCooldown_FleshTrait
{
    [HarmonyPrefix]
    public static void Prefix(Ability __instance, ref int ticks)
    {
        Pawn pawn = __instance.pawn;
        if (pawn?.health?.hediffSet?.HasHediff(FleshHiveDefOf.FH_Trait_BoneSpurGrowth) == true)
        {
            ticks = Mathf.RoundToInt(ticks * BoneSpurGrowthCooldownFactor);
        }
    }

    private const float BoneSpurGrowthCooldownFactor = 0.75f;
}
