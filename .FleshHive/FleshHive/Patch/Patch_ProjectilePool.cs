using HarmonyLib;
using Verse;

namespace FleshHive;

[HarmonyPatch(typeof(GenSpawn))]
[HarmonyPatch("Spawn", typeof(Thing), typeof(IntVec3), typeof(Map), typeof(Rot4), typeof(WipeMode), typeof(bool), typeof(bool))]
public static class Patch_ProjectilePool_Spawn
{
    [HarmonyPostfix]
    public static void Postfix(Thing __result)
    {
        if (__result is Projectile proj)
        {
            ProjectilePool.Register(proj);
        }
    }
}

[HarmonyPatch(typeof(Thing))]
[HarmonyPatch("DeSpawn", typeof(DestroyMode))]
public static class Patch_ProjectilePool_DeSpawn
{
    [HarmonyPrefix]
    public static void Prefix(Thing __instance)
    {
        if (__instance is Projectile proj)
        {
            ProjectilePool.Unregister(proj);
        }
    }
}

[HarmonyPatch(typeof(Thing))]
[HarmonyPatch("Destroy", typeof(DestroyMode))]
public static class Patch_ProjectilePool_Destroy
{
    [HarmonyPrefix]
    public static void Prefix(Thing __instance)
    {
        if (__instance is Projectile proj)
        {
            ProjectilePool.Unregister(proj);
        }
    }
}
