using System.Collections.Generic;
using HiveCreatureFramework;
using RimWorld;
using Verse;

namespace FleshHive;

public class ItemSpawnWorker_FleshHopper : ItemSpawnWorker
{
    public override AcceptanceReport CanProduce(CompHiveSpawner comp)
    {
        if (comp?.parent?.Map == null)
        {
            return "FH_RequireFleshHopper".Translate();
        }

        if (comp.parent is Building_FleshHopper)
        {
            return base.CanProduce(comp);
        }

        List<CompHiveSpawner> hopperSpawners = GetHopperSpawners(comp.parent.Map).ToList();
        if (hopperSpawners.Count == 0)
        {
            return "FH_RequireFleshHopper".Translate();
        }

        AcceptanceReport firstReport = base.CanProduce(hopperSpawners[0]);
        return hopperSpawners.Any(spawner => base.CanProduce(spawner).Accepted) ? true : firstReport;
    }

    public override bool AddProgress(CompHiveSpawner comp)
    {
        if (comp?.parent is Building_FleshHopper)
        {
            if (!CanProduce(comp).Accepted)
            {
                return false;
            }

            Consume(comp);
            MapComponent_FleshHive mapComp = comp.parent.Map.GetComponent<MapComponent_FleshHive>();
            float speedFactor = mapComp?.GetUpgradeEffectFactor(FleshHiveUpgradeEffect.FleshShaping) ?? 1f;
            ItemSpawnData_FleshShaping progress = new ItemSpawnData_FleshShaping(def, speedFactor);
            comp.ProgressHolder.progresses.Add(progress);
            comp.SendProgressAddedMessage(progress);
            return true;
        }

        if (comp?.parent?.Map == null)
        {
            return false;
        }

        List<CompHiveSpawner> candidates = GetHopperSpawners(comp.parent.Map)
            .Where(spawner => base.CanProduce(spawner).Accepted)
            .ToList();
        if (candidates.TryRandomElement(out CompHiveSpawner targetSpawner))
        {
            return AddProgress(targetSpawner);
        }
        return false;
    }

    public override List<Thing> Spawn(CompProgressHolder comp)
    {
        Thing target = ThingMaker.MakeThing(def.thing);
        target.stackCount = def.stackCountRange.RandomInRange;
        if (!FleshHopperUtility.TryPlaceThingOnClosestHopper(comp.parent, target, out Thing placedThing))
        {
            target.Destroy();
            return [];
        }

        return
        [
            placedThing ?? target
        ];
    }

    private IEnumerable<CompHiveSpawner> GetHopperSpawners(Map map)
    {
        if (map == null)
        {
            yield break;
        }

        foreach (Building_FleshHopper hopper in FleshHopperUtility.GetCachedHoppers(map))
        {
            if (hopper.Faction != Faction.OfPlayer)
            {
                continue;
            }

            CompHiveSpawner spawner = hopper.TryGetComp<CompHiveSpawner>();
            if (spawner != null)
            {
                yield return spawner;
            }
        }
    }
}
