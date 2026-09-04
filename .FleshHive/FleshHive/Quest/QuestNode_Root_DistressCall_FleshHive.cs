using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;

namespace FleshHive;

public class QuestNode_Root_DistressCall_FleshHive : QuestNode_Root_DistressCall
{
    protected override void RunInt()
    {
        Quest quest = QuestGen.quest;
        Slate slate = QuestGen.slate;
        float points = slate.Get("points", 0f);
        if (points < MinPoints)
        {
            points = MinPoints;
        }

        if (!TryFindSiteTile(out PlanetTile tile) || !TryFindFaction(points, out Faction faction))
        {
            Log.Error("[FleshHive] 无法为母兽召唤仪式生成求救信号站点：找不到有效地点或派系。");
            return;
        }

        slate.Set("faction", faction);
        SitePartDef sitePartDef = DefDatabase<SitePartDef>.GetNamed("DistressCall_Fleshbeasts");
        if (sitePartDef == null)
        {
            Log.Error("[FleshHive] 无法为母兽召唤仪式生成求救信号站点：DistressCall_Fleshbeasts 不存在。");
            return;
        }

        Site site = QuestGen_Sites.GenerateSite(
            new[]
            {
                new SitePartDefWithParams(sitePartDef, new SitePartParams
                {
                    threatPoints = points
                })
            },
            tile,
            faction,
            worldObjectDef: FleshHiveDefOf.FH_WorldObject_FleshHiveSite);
        if (site is not FleshHiveSite fleshHiveSite)
        {
            Log.Error("[FleshHive] 母兽召唤仪式生成的求救信号站点类型错误，无法保存母兽种类。");
            return;
        }

        fleshHiveSite.motherKind = slate.Get<PawnKindDef>("motherKind");
        quest.SpawnWorldObject(site);
        slate.Set("site", site);

        string siteMapGeneratedSignal = QuestGenUtility.HardcodedSignalWithQuestID("site.MapGenerated");
        string siteNoActiveThreatsSignal = QuestGenUtility.HardcodedSignalWithQuestID("site.NoActiveThreats");
        string siteMapRemovedSignal = QuestGenUtility.HardcodedSignalWithQuestID("site.MapRemoved");
        string ambushSignal = QuestGenUtility.HardcodedSignalWithQuestID("ambush");
        quest.Letter(
            LetterDefOf.NeutralEvent,
            null,
            null,
            label: "DistressSignalLabel".Translate(),
            text: "DistressSignalText".Translate(site.Faction.Named("FACTION")).Resolve(),
            lookTargets: Gen.YieldSingle(site),
            relatedFaction: site.Faction);

        QuestPart_Choice rewardChoice = quest.RewardChoice();
        rewardChoice.choices.Add(new QuestPart_Choice.Choice
        {
            rewards = { (Reward)new Reward_CampLoot() }
        });

        if (Rand.Chance(AmbushChance))
        {
            quest.Delay(AmbushDelayTicks.RandomInRange, delegate
            {
                quest.SignalPass(null, null, ambushSignal);
            }, siteMapGeneratedSignal);
            quest.AddPart(new QuestPart_DistressCallAmbush(ambushSignal, site, AmbushPointsCurve.Evaluate(points)));
        }

        quest.WorldObjectTimeout(site, TimeoutTicks);
        quest.Delay(TimeoutTicks, delegate
        {
            QuestGen_End.End(quest, QuestEndOutcome.Fail);
        });
        quest.End(QuestEndOutcome.Success, 0, null, siteNoActiveThreatsSignal);
        quest.End(QuestEndOutcome.Fail, 0, null, siteMapRemovedSignal);
    }

    private bool TryFindSiteTile(out PlanetTile tile)
    {
        return TileFinder.TryFindNewSiteTile(out tile, MinDistanceFromColony, MaxDistanceFromColony,
            allowCaravans: false, allowedLandmarks: AllowedLandmarks, selectLandmarkChance: 0.5f,
            canSelectComboLandmarks: true, tileFinderMode: TileFinderMode.Near);
    }

    private bool TryFindFaction(float points, out Faction faction)
    {
        return Find.FactionManager.AllFactionsListForReading
            .Where(candidate => FactionUsable(candidate, points))
            .TryRandomElement(out faction);
    }

    private static bool FactionUsable(Faction faction, float points)
    {
        if (ModsConfig.RoyaltyActive && points < EmpireSitePointsThreshold && faction == Faction.OfEmpire)
        {
            return false;
        }

        if (!faction.def.canGenerateQuestSites)
        {
            return false;
        }

        if (faction.def.humanlikeFaction && !faction.def.pawnGroupMakers.NullOrEmpty())
        {
            return !faction.def.permanentEnemy;
        }

        return false;
    }

    private const int MaxDistanceFromColony = 9;
    private const int MinDistanceFromColony = 3;
    private const float MinPoints = 100f;
    private const int TimeoutTicks = 900000;
    private const float EmpireSitePointsThreshold = 2000f;
    private const float AmbushChance = 0.75f;
    private static readonly IntRange AmbushDelayTicks = new(2400, 4800);
    private static readonly SimpleCurve AmbushPointsCurve = new()
    {
        new CurvePoint(100f, 100f),
        new CurvePoint(1000f, 400f),
        new CurvePoint(5000f, 1000f)
    };
}
