using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;
using Verse.AI.Group;

namespace FleshHive;

public class QuestPart_DistressCallAmbush_FleshHive : QuestPartActivable
{
    public QuestPart_DistressCallAmbush_FleshHive()
    {
    }

    public QuestPart_DistressCallAmbush_FleshHive(string inSignal, Site site, float points)
    {
        inSignalEnable = inSignal;
        this.site = site;
        this.points = points;
    }

    public override void QuestPartTick()
    {
        if (Find.TickManager.TicksGame % 60 != 0 || site?.Map is not Map map)
        {
            return;
        }

        List<Thing> burrows = map.listerThings.ThingsOfDef(ThingDefOf.PitBurrow)
            .Where(burrow => !burrow.Fogged()).ToList();
        if (burrows.Count == 0)
        {
            return;
        }

        FireAmbush(map, burrows.RandomElement());
        Complete();
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref site, "site");
        Scribe_Values.Look(ref points, "points");
    }

    private void FireAmbush(Map map, Thing burrow)
    {
        List<Pawn> attackers = PawnGroupMakerUtility.GeneratePawns(new PawnGroupMakerParms
        {
            groupKind = PawnGroupKindDefOf.Fleshbeasts,
            points = points,
            faction = Faction.OfEntities,
            raidStrategy = RaidStrategyDefOf.ImmediateAttack
        }).ToList();
        if (attackers.Count == 0)
        {
            Log.Error($"[FleshHive] Distress call ambush could not generate fleshbeasts at {points:F0} points.");
            return;
        }

        Lord lord = LordMaker.MakeNewLord(Faction.OfEntities, new LordJob_FleshbeastAssault(), map);
        List<IntVec3> spawnPositions = attackers.Select(pawn =>
            CellFinder.TryFindRandomCellNear(burrow.Position, map, 2,
                cell => cell.Walkable(map) && !cell.Fogged(map), out IntVec3 position)
                ? position
                : burrow.Position).ToList();
        map.deferredSpawner.AddRequest(new SpawnRequest(
            spawnPositions: spawnPositions,
            thingsToSpawn: attackers.Cast<Thing>().ToList(),
            batchSize: 1,
            intervalSeconds: 0.5f,
            lord: lord));
        Find.LetterStack.ReceiveLetter("DistressSignalAmbushLabel".Translate(),
            "DistressSignalAmbushText".Translate(), LetterDefOf.ThreatBig, burrow);

        Pawn mother = map.mapPawns.AllPawnsSpawned.FirstOrDefault(pawn =>
            pawn.kindDef == FleshHiveDefOf.FH_Nexusmeld
            || pawn.kindDef == FleshHiveDefOf.FH_Furiousmeld
            || pawn.kindDef == FleshHiveDefOf.FH_Bastionmeld
            || pawn.kindDef == FleshHiveDefOf.FH_Fissionmeld
            || pawn.kindDef == FleshHiveDefOf.FH_Dreadmeld);
        if (mother?.TryGetComp<CompHiveGroup_MotherBeast>() is { } groupComp)
        {
            SitePartWorker_DistressCall_Fleshbeasts_FleshHive.CaptureFleshbeastsForAmbush(mother, map);
            groupComp.SetAttackMode();
        }
    }

    private Site site;

    private float points;
}
