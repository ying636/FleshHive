using System.Linq;
using LudeonTK;
using RimWorld;
using Verse;

namespace FleshHive;

public static class DebugActions_Parasitism
{
    [DebugAction("FleshHive", "Add flesh hive activity 10%", false, false, false, false, false, 0, false,
        allowedGameStates = AllowedGameStates.PlayingOnMap)]
    public static void AddFleshHiveActivity10()
    {
        MapComponent_FleshHive mapComp = Find.CurrentMap?.GetComponent<MapComponent_FleshHive>();
        if (mapComp == null)
        {
            return;
        }

        mapComp.DebugAddActivity(mapComp.ActivityLimit * 0.1f);
        Messages.Message("FH_DebugActivity".Translate(mapComp.ActivityPercent.ToStringPercent("0")), MessageTypeDefOf.NeutralEvent, false);
    }

    [DebugAction("FleshHive", "Set flesh hive activity full", false, false, false, false, false, 0, false,
        allowedGameStates = AllowedGameStates.PlayingOnMap)]
    public static void SetFleshHiveActivityFull()
    {
        MapComponent_FleshHive mapComp = Find.CurrentMap?.GetComponent<MapComponent_FleshHive>();
        if (mapComp == null)
        {
            return;
        }

        mapComp.DebugSetActivityFull();
        Messages.Message("FH_DebugActivity".Translate(mapComp.ActivityPercent.ToStringPercent("0")), MessageTypeDefOf.NeutralEvent, false);
    }

    [DebugAction("FleshHive", "Trigger flesh hive riot", false, false, false, false, false, 0, false,
        allowedGameStates = AllowedGameStates.PlayingOnMap)]
    public static void TriggerFleshHiveRiot()
    {
        MapComponent_FleshHive mapComp = Find.CurrentMap?.GetComponent<MapComponent_FleshHive>();
        if (mapComp == null)
        {
            Messages.Message("FH_DebugNoHiveComponent".Translate(), MessageTypeDefOf.RejectInput, false);
            return;
        }

        mapComp.DebugStartRiot();
        Messages.Message("FH_DebugRiotTriggered".Translate(), MessageTypeDefOf.ThreatBig, false);
    }

    [DebugAction("FleshHive", "Add parasite", false, false, false, false, false, 0, false,
        allowedGameStates = AllowedGameStates.PlayingOnMap,
        actionType = DebugActionType.ToolMapForPawns)]
    public static void AddParasiteIgnoreSpace(Pawn pawn)
    {
        ParasitismSystem sys = pawn.health.hediffSet.hediffs
            .OfType<ParasitismSystem>()
            .FirstOrDefault();
        if (sys == null)
        {
            sys = (ParasitismSystem)pawn.health.AddHediff(FleshHiveDefOf.FH_ParasitismSystem);
            if (sys == null)
            {
                return;
            }
        }
        ParasitismSystem.EnsureAbilityTracker(pawn);
        Find.WindowStack.Add(new FloatMenu(
            DefDatabase<PawnKindDef>.AllDefsListForReading
                .Where(k => k.race?.comps?.Any(c => c is ParasitismCompProperties) == true)
                .Select(k => new FloatMenuOption(k.label, () =>
                {
                    sys.DebugParasite(k);
                }))
                .ToList()
        ));
    }

    [DebugAction("FleshHive", "View flesh replica special RenderTree", false, false, false, false, false, 0, false,
        allowedGameStates = AllowedGameStates.PlayingOnMap,
        actionType = DebugActionType.ToolMapForPawns)]
    public static void ViewFleshReplicaSpecialRenderTree(Pawn pawn)
    {
        if (pawn is not FleshReplicaUnit replica)
        {
            Messages.Message("Selected pawn is not a flesh replica.", MessageTypeDefOf.RejectInput, false);
            return;
        }

        Find.WindowStack.Add(new Dialog_DebugFleshReplicaRenderTree(replica));
    }
}
