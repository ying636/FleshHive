using System.Collections.Generic;
using System.Linq;
using HiveCreatureFramework;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace FleshHive;

public class SitePartWorker_DistressCall_Fleshbeasts_FleshHive : SitePartWorker_DistressCall_Fleshbeasts
{
    public override void PostMapGenerate(Map map)
    {
        base.PostMapGenerate(map);

        Caravan caravan = Find.WorldObjects.PlayerControlledCaravanAt(map.Tile);
        float escortPoints = caravan == null
            ? MinimumEscortPoints
            : StorytellerUtility.DefaultThreatPointsNow(caravan);
        if (escortPoints < MinimumEscortPoints)
        {
            escortPoints = MinimumEscortPoints;
        }
        List<Pawn> escorts = PawnGroupMakerUtility.GeneratePawns(new PawnGroupMakerParms
        {
            groupKind = PawnGroupKindDefOf.Fleshbeasts,
            points = escortPoints,
            faction = Faction.OfEntities,
            raidStrategy = RaidStrategyDefOf.ImmediateAttack,
            tile = map.Tile
        }).ToList();
        if (escorts.Count == 0)
        {
            Log.Error($"[FleshHive] 无法为求救信号生成随行血肉兽：远征队袭击点数为 {escortPoints:F0}，但血肉兽生成列表为空。");
            return;
        }

        PawnKindDef motherKind = GetMotherKindForSite(map);
        Pawn mother = PawnGenerator.GeneratePawn(motherKind, Faction.OfEntities);
        FleshParasiteUtility.TryApplyDefaultParasites(mother);
        List<Pawn> attackers = new List<Pawn>(escorts);

        List<Pawn> spawnedAttackers = SpawnAttackers(attackers, map);
        if (!TrySpawnMother(mother, map))
        {
            Log.Error("[FleshHive] 求救信号的随机母兽未能生成到地图，随行血肉兽将使用普通袭击逻辑。");
        }

        if (spawnedAttackers.Count == 0 && !mother.Spawned)
        {
            Log.Error("[FleshHive] 求救信号的母兽与随行血肉兽均未能生成到地图。");
            return;
        }

        CaptureFleshbeastsForAmbush(mother, map);
    }

    private List<Pawn> SpawnAttackers(IEnumerable<Pawn> attackers, Map map)
    {
        List<Pawn> spawnedAttackers = new List<Pawn>();
        foreach (Pawn attacker in attackers)
        {
            if (!RCellFinder.TryFindRandomCellNearWith(
                    map.Center,
                    cell => cell.Standable(map) && cell.GetEdifice(map) == null,
                    map,
                    out IntVec3 spawnCell,
                    SpawnRadius))
            {
                Log.Error($"[FleshHive] 求救信号地图中找不到可生成 {attacker.LabelShortCap} 的位置。");
                attacker.Destroy();
                continue;
            }

            GenSpawn.Spawn(attacker, spawnCell, map);
            spawnedAttackers.Add(attacker);
        }

        return spawnedAttackers;
    }

    private static bool TrySpawnMother(Pawn mother, Map map)
    {
        if (!RCellFinder.TryFindRandomCellNearWith(
                map.Center,
                cell => cell.Standable(map) && cell.GetEdifice(map) == null,
                map,
                out IntVec3 spawnCell,
                SpawnRadius))
        {
            mother.Destroy();
            return false;
        }

        GenSpawn.Spawn(mother, spawnCell, map);
        return true;
    }

    public static void CaptureFleshbeastsForAmbush(Pawn mother, Map map)
    {
        CompHiveGroup_MotherBeast groupComp = mother.TryGetComp<CompHiveGroup_MotherBeast>();
        if (groupComp == null)
        {
            return;
        }

        foreach (Pawn fleshbeast in map.mapPawns.AllPawnsSpawned.ToList())
        {
            if (fleshbeast == mother
                || fleshbeast.Faction != mother.Faction
                || !FleshBeastKindUtility.SizeOf(fleshbeast.kindDef).HasValue)
            {
                continue;
            }

            UnitGroup targetGroup = groupComp.groups.FirstOrDefault(group =>
                group != null && group.CanAccept(fleshbeast).Accepted);
            if (targetGroup == null)
            {
                continue;
            }

            targetGroup.AcceptUnit(fleshbeast);
        }
    }

    private const int SpawnRadius = 20;
    private const float MinimumEscortPoints = 1000f;

    private static PawnKindDef GetMotherKind()
    {
        motherKinds ??= new List<PawnKindDef>
        {
            DefDatabase<PawnKindDef>.GetNamed("FH_Nexusmeld"),
            DefDatabase<PawnKindDef>.GetNamed("FH_Furiousmeld"),
            DefDatabase<PawnKindDef>.GetNamed("FH_Bastionmeld"),
            DefDatabase<PawnKindDef>.GetNamed("FH_Fissionmeld"),
            DefDatabase<PawnKindDef>.GetNamed("FH_Dreadmeld")
        };
        return motherKinds.RandomElement();
    }

    private PawnKindDef GetMotherKindForSite(Map map)
    {
        Site site = map.Parent as Site;
        return (site as FleshHiveSite)?.motherKind ?? GetMotherKind();
    }

    private static List<PawnKindDef> motherKinds;
}
