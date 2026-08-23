using HarmonyLib;
using UnityEngine;
using Verse;

namespace FleshHive;

[HarmonyPatch(typeof(PawnRenderer), "ParallelGetPreRenderResults")]
public static class Patch_PawnRenderer_ParallelGetPreRenderResults_FleshReplicaRenderColor
{
    public static void Prefix(ref bool disableCache)
    {
        if (!FleshReplicaUnit.RenderingHost)
        {
            return;
        }

        disableCache = true;
    }
}

[HarmonyPatch(typeof(PawnRenderNodeWorker), nameof(PawnRenderNodeWorker.GetMaterialPropertyBlock))]
public static class Patch_PawnRenderNodeWorker_GetMaterialPropertyBlock_FleshReplicaRenderColor
{
    public static void Postfix(Material material, ref MaterialPropertyBlock __result)
    {
        if (!FleshReplicaUnit.RenderingHost || __result == null || material == null)
        {
            return;
        }

        __result.SetColor(ShaderPropertyIDs.Color, FleshReplicaUnit.FleshColor * material.color);
    }
}
