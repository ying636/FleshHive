using HarmonyLib;
using UnityEngine;
using Verse;

namespace FleshHive;

[HarmonyPatch(typeof(PawnRenderer), "ParallelGetPreRenderResults")]
public static class Patch_PawnRenderer_ParallelGetPreRenderResults_FleshReplicaRenderColor
{
    public static void Prefix(Pawn ___pawn, ref bool disableCache)
    {
        if (___pawn is not FleshReplicaUnit { Active: true })
        {
            return;
        }

        disableCache = true;
    }
}

[HarmonyPatch(typeof(PawnRenderNodeWorker), nameof(PawnRenderNodeWorker.GetMaterialPropertyBlock))]
public static class Patch_PawnRenderNodeWorker_GetMaterialPropertyBlock_FleshReplicaRenderColor
{
    public static void Postfix(Material material, PawnDrawParms parms, ref MaterialPropertyBlock __result)
    {
        if (parms.pawn is not FleshReplicaUnit { Active: true } || __result == null || material == null)
        {
            return;
        }

        __result.SetColor(ShaderPropertyIDs.Color, FleshReplicaUnit.FleshColor * material.color);
    }
}
