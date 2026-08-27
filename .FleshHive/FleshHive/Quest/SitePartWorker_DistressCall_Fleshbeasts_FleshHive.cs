using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI.Group;

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

        Pawn mother = PawnGenerator.GeneratePawn(MotherKinds.RandomElement(), Faction.OfEntities);
        List<Pawn> attackers = new List<Pawn>(escorts.Count + 1)
        {
            mother
        };
        attackers.AddRange(escorts);

        List<Pawn> spawnedAttackers = SpawnAttackers(attackers, map);
        if (!spawnedAttackers.Contains(mother))
        {
            Log.Error("[FleshHive] 求救信号的随机母兽未能生成到地图，随行血肉兽将使用普通袭击逻辑。");
        }

        if (spawnedAttackers.Count == 0)
        {
            Log.Error("[FleshHive] 求救信号的母兽与随行血肉兽均未能生成到地图。");
            return;
        }

        LordJob lordJob = spawnedAttackers.Contains(mother)
            ? new LordJob_DefendPoint(mother.Position, 28f, 12f)
            : new LordJob_FleshbeastAssault();
        LordMaker.MakeNewLord(Faction.OfEntities, lordJob, map, spawnedAttackers);
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

    private const int SpawnRadius = 20;
    private const float MinimumEscortPoints = 1000f;

    private static readonly List<PawnKindDef> MotherKinds = new List<PawnKindDef>
    {
        FleshHiveDefOf.FH_Nexusmeld,
        FleshHiveDefOf.FH_Furiousmeld,
        FleshHiveDefOf.FH_Bastionmeld,
        FleshHiveDefOf.FH_Fissionmeld,
        FleshHiveDefOf.FH_Dreadmeld
    };
}
