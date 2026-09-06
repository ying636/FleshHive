using System.Collections.Generic;
using HiveCreatureFramework;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace FleshHive;

public class DeathActionProperties_FissionmeldDormancy : DeathActionProperties
{
    public DeathActionProperties_FissionmeldDormancy()
    {
        workerClass = typeof(DeathActionWorker_FissionmeldDormancy);
    }

    public ThingDef dormantThing;

    public List<PawnKindDef> spawnOptions;

    public IntRange spawnPointsRange = new IntRange(100, 300);

    public int spawnRadius = 5;

}

public class DeathActionWorker_FissionmeldDormancy : DeathActionWorker
{
    public DeathActionProperties_FissionmeldDormancy Props => (DeathActionProperties_FissionmeldDormancy)props;

    public override void PawnDied(Corpse corpse, Lord prevLord)
    {
        if (corpse?.MapHeld == null)
        {
            return;
        }

        Map map = corpse.MapHeld;
        IntVec3 position = corpse.PositionHeld;
        Faction faction = corpse.InnerPawn?.Faction;
        SpawnDormantFissionmeld(corpse, position, map, faction, prevLord);
        FleshHiveFleshbeastSpawnUtility.SpawnRandomByPoints(Props.spawnOptions, Props.spawnPointsRange, faction, position, map, Props.spawnRadius, tryAssignEnemyLord: true);
    }

    private void SpawnDormantFissionmeld(Corpse corpse, IntVec3 position, Map map, Faction faction, Lord prevLord)
    {
        if (Props.dormantThing == null)
        {
            return;
        }

        Thing dormant = ThingMaker.MakeThing(Props.dormantThing);
        GenSpawn.Spawn(dormant, position, map, WipeMode.VanishOrMoveAside);
        dormant.SetFaction(faction);
        CompFissionmeldDormant comp = dormant.TryGetComp<CompFissionmeldDormant>();
        if (comp != null)
        {
            CompFissionmeldState state = corpse.InnerPawn?.TryGetComp<CompFissionmeldState>();
            CompHiveGroup groupComp = corpse.InnerPawn?.TryGetComp<CompHiveGroup>();
            if (state != null && groupComp != null && state.PreservedGroups.Count == 0)
            {
                state.PreservedGroups.AddRange(groupComp.groups);
            }
            if (state != null && state.DormantHitPoints > 0)
            {
                dormant.HitPoints = System.Math.Min(state.DormantHitPoints, dormant.MaxHitPoints);
            }
            comp.StoreGroups(state?.PreservedGroups);
            comp.StoreCorpse(corpse);
            if (prevLord != null && dormant is Building building)
            {
                prevLord.AddBuilding(building);
            }
            if (dormant is Building dormantBuilding && state?.PreservedGroups != null)
            {
                foreach (UnitGroup group in state.PreservedGroups)
                {
                    if (group == null)
                    {
                        continue;
                    }

                    group.SetMode(HCFDefOf.HCF_GroupWorkMode_Defend, false);
                    group.SetTarget(new TargetInfo(dormantBuilding), false);
                }
            }
            if (corpse.Spawned)
            {
                corpse.Destroy(DestroyMode.Vanish);
            }
        }
        else
        {
            Log.Error("[FleshHive] Dormant fissionmeld is missing CompFissionmeldDormant; destroying the corpse to avoid leaving a duplicate.");
            corpse.Destroy(DestroyMode.Vanish);
        }
    }
}
