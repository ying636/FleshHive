using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;
using Verse.AI.Group;

namespace FleshHive;

[HarmonyPatch(typeof(QuestPart_DistressCallAmbush), nameof(QuestPart_DistressCallAmbush.Notify_QuestSignalReceived))]
public static class Patch_QuestPart_DistressCallAmbush_FleshHive
{
    public static void Postfix(Signal signal, string ___inSignal, Site ___site)
    {
        if (signal.tag != ___inSignal || ___site?.Map == null)
        {
            return;
        }

        if (!___site.Map.listerThings.ThingsOfDef(ThingDefOf.PitBurrow)
            .Any(burrow => !burrow.Fogged()))
        {
            return;
        }

        foreach (Lord lord in ___site.Map.lordManager.lords.ToList())
        {
            if (lord.LordJob is not LordJob_DefendPoint)
            {
                continue;
            }

            Pawn leader = lord.ownedPawns.FirstOrDefault(IsMotherBeast);
            if (leader == null)
            {
                continue;
            }

            lord.SetJob(new LordJob_GiantFleshbeastAssault(leader));
            lord.GotoToil(lord.Graph.StartingToil);
        }
    }

    private static bool IsMotherBeast(Pawn pawn)
    {
        return pawn?.kindDef == FleshHiveDefOf.FH_Nexusmeld
            || pawn?.kindDef == FleshHiveDefOf.FH_Furiousmeld
            || pawn?.kindDef == FleshHiveDefOf.FH_Bastionmeld
            || pawn?.kindDef == FleshHiveDefOf.FH_Fissionmeld
            || pawn?.kindDef == FleshHiveDefOf.FH_Dreadmeld;
    }
}
