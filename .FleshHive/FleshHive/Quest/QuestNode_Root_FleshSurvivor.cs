using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;

namespace FleshHive;

public class QuestNode_Root_FleshSurvivor : QuestNode
{
    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;
        Quest quest = QuestGen.quest;
        Map map = slate.Get<Map>("map") ?? QuestGen_Get.GetMap();
        slate.Set("map", map);
        Pawn hela = FleshSurvivorHelaGenerator.Generate(map);
        List<Pawn> pursuers = CreatePursuers();

        slate.Set("hela", hela);
        slate.Set("resolvedQuestName", "FH_Quest_FleshSurvivor_Name".Translate().ToString());
        slate.Set("resolvedQuestDescription", "FH_Quest_FleshSurvivor_Description".Translate().ToString());

        QuestGen.AddToGeneratedPawns(hela);
        Find.WorldPawns.PassToWorld(hela);
        foreach (Pawn pursuer in pursuers)
        {
            QuestGen.AddToGeneratedPawns(pursuer);
            Find.WorldPawns.PassToWorld(pursuer);
        }

        quest.PawnsArrive(Gen.YieldSingle(hela), mapParent: map.Parent, arrivalMode: PawnsArrivalModeDefOf.EdgeWalkIn,
            joinPlayer: true, sendStandardLetter: false);
        quest.Delay(AttackDelayTicks, delegate
        {
            quest.PawnsArrive(pursuers, mapParent: map.Parent, arrivalMode: PawnsArrivalModeDefOf.EdgeWalkIn,
                sendStandardLetter: false);
            quest.AssaultColony(Faction.OfEntities, map.Parent, pursuers);
            quest.Letter(LetterDefOf.ThreatBig, lookTargets: pursuers,
                text: "FH_Quest_FleshSurvivor_AttackText".Translate().ToString(),
                label: "FH_Quest_FleshSurvivor_AttackLabel".Translate().ToString());
            QuestGen_End.End(quest, QuestEndOutcome.Success);
        }, inspectStringTargets: new ISelectable[] { hela },
            inspectString: "FH_Quest_FleshSurvivor_AttackIn".Translate().ToString(),
            expiryInfoPart: "FH_Quest_FleshSurvivor_AttackIn".Translate().ToString(),
            debugLabel: "FleshSurvivorAttack");
    }

    protected override bool TestRunInt(Slate slate)
    {
        return (slate.Get<Map>("map") ?? QuestGen_Get.GetMap()) != null && Faction.OfEntities != null;
    }


    private List<Pawn> CreatePursuers()
    {
        List<PawnKindDef> smallKinds = new List<PawnKindDef>
        {
            FleshHiveDefOf.FH_Fingerspike,
            FleshHiveDefOf.FH_Puffspike,
            FleshHiveDefOf.FH_Whipspike,
            FleshHiveDefOf.FH_Gutspike,
            FleshHiveDefOf.FH_Paraspike
        };
        List<PawnKindDef> mediumKinds = new List<PawnKindDef>
        {
            FleshHiveDefOf.FH_Shatterspike,
            FleshHiveDefOf.FH_Toughspike
        };
        int count = Rand.RangeInclusive(4, 6);
        List<Pawn> pursuers = new List<Pawn>(count)
        {
            FleshHiveFleshbeastSpawnUtility.GeneratePawn(mediumKinds.RandomElement(), Faction.OfEntities)
        };
        for (int i = 1; i < count; i++)
        {
            pursuers.Add(FleshHiveFleshbeastSpawnUtility.GeneratePawn(smallKinds.RandomElement(), Faction.OfEntities));
        }
        return pursuers;
    }

    private const int AttackDelayTicks = 2500;
}
