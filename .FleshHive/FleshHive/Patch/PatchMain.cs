using System.Reflection;
using HarmonyLib;
using Verse;

namespace FleshHive;

[StaticConstructorOnStartup]
public static class PatchMain
{
    static PatchMain()
    {
        Harmony harmony = new Harmony("FH_Patch");
        harmony.PatchAll(Assembly.GetExecutingAssembly());
    }
} 