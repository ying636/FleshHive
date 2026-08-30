using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace FleshHive;

public class ScenPart_StartingFleshSurvivors : ScenPart
{
    public override void PostGameStart()
    {
        base.PostGameStart();
        GameComponent_FleshSurvivor? component = Current.Game?.GetComponent<GameComponent_FleshSurvivor>();
        if (component == null)
        {
            Log.Error("[FleshHive] Could not find GameComponent_FleshSurvivor while starting the flesh survivor scenario.");
            return;
        }

        component.DisableQuest();

        Map? map = Find.CurrentMap ?? Current.Game?.Maps.FirstOrDefault(candidate => candidate.IsPlayerHome);
        if (map == null)
        {
            Log.Error("[FleshHive] Could not find the starting map for the starting Hela pawn.");
            return;
        }

        LongEventHandler.ExecuteWhenFinished(() =>
        {
            SpawnStartingHela(map);
            QueueStartingUnits(map);
        });
    }

    public override IEnumerable<Thing> PlayerStartingThings()
    {
        return new List<Pawn>
        {
            PawnGenerator.GeneratePawn(FleshHiveDefOf.FH_Fingerspike, Faction.OfPlayer),
            PawnGenerator.GeneratePawn(FleshHiveDefOf.FH_Fingerspike, Faction.OfPlayer),
            PawnGenerator.GeneratePawn(FleshHiveDefOf.FH_Fingerspike, Faction.OfPlayer),
            PawnGenerator.GeneratePawn(FleshHiveDefOf.FH_Puffspike, Faction.OfPlayer),
            PawnGenerator.GeneratePawn(FleshHiveDefOf.FH_Puffspike, Faction.OfPlayer)
        };

    }

    private void SpawnStartingHela(Map map)
    {
        Pawn? existingHela = map.mapPawns.AllPawnsSpawned.FirstOrDefault(pawn =>
            pawn.health?.hediffSet?.HasHediff(FleshHiveDefOf.FH_Hela) == true);
        if (existingHela != null)
        {
            return;
        }

        Pawn helaPawn = FleshSurvivorHelaGenerator.Generate(
            map, PawnGenerationContext.NonPlayer, Faction.OfPlayer, includeDreadmeldSeed: false);
        helaPawn.SetFaction(Faction.OfPlayer);
        IntVec3 spawnCell = map.Center;
        if (!spawnCell.Standable(map)
            && !CellFinder.TryFindRandomCellNear(map.Center, map, 10,
                cell => cell.Standable(map) && !cell.Fogged(map), out spawnCell))
        {
            Log.Error("[FleshHive] Could not find a valid map cell for the starting Hela pawn.");
            helaPawn.Destroy();
            return;
        }

        GenSpawn.Spawn(helaPawn, spawnCell, map, WipeMode.VanishOrMoveAside);
    }

    private void QueueStartingUnits(Map map)
    {
        Pawn? hela = map.mapPawns.AllPawnsSpawned.FirstOrDefault(pawn =>
            pawn.health?.hediffSet?.HasHediff(FleshHiveDefOf.FH_Hela) == true);
        if (hela == null)
        {
            Log.Error("[FleshHive] Could not find the starting Hela pawn for the flesh survivor scenario.");
            return;
        }

        HediffComp_HelaNode? helaNode = hela.health?.hediffSet?.GetFirstHediffOfDef(FleshHiveDefOf.FH_Hela)
            ?.TryGetComp<HediffComp_HelaNode>();
        if (helaNode == null)
        {
            Log.Error("[FleshHive] Starting Hela has no Hela control node.");
            return;
        }

        List<Pawn> startingBeasts = map.mapPawns.AllPawnsSpawned
            .Where(pawn => pawn.Faction == Faction.OfPlayer
                && (pawn.kindDef == FleshHiveDefOf.FH_Fingerspike
                    || pawn.kindDef == FleshHiveDefOf.FH_Puffspike))
            .ToList();
        if (startingBeasts.Count != 5)
        {
            Log.Error("[FleshHive] Expected five starting fleshbeasts, found " + startingBeasts.Count + ".");
        }

        helaNode.QueueStartingUnits(startingBeasts);
    }
}
