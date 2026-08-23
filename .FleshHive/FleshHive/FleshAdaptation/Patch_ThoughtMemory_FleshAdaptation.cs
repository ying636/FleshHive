using HarmonyLib;
using RimWorld;

namespace FleshHive;

[HarmonyPatch(typeof(Thought_Memory), nameof(Thought_Memory.MoodOffset))]
public static class Patch_ThoughtMemory_FleshAdaptation
{
    static void Postfix(Thought_Memory __instance, ref float __result)
    {
        if (FleshAdaptationUtility.IgnoresMoodThought(__instance.pawn, __instance.def))
        {
            __result = 0f;
        }
    }
}
