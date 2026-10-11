using HarmonyLib;
using Verse;

namespace FH.HostileHiveTests;

[StaticConstructorOnStartup]
[HarmonyPatch(typeof(Root_Play), "Update")]
public static class TestLoop
{
    static TestLoop()
    {
        if (Environment.CommandLine.Contains("-quicktest"))
            new Harmony("FH.HostileHiveTests").PatchAll(typeof(TestLoop).Assembly);
    }

    [HarmonyPostfix]
    public static void Postfix()
    {
        if (Current.ProgramState == ProgramState.Playing && Find.CurrentMap != null && !LongEventHandler.AnyEventNowOrWaiting)
            RuntimeSuite.Poll();
    }
}
