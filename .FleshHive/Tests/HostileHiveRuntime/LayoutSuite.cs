using System.Reflection;
using FleshHive;
using HiveCreatureFramework;
using RimWorld;
using UnityEngine;
using Verse;

namespace FH.HostileHiveTests;

public static class LayoutSuite
{
    public static void Run(HostileFleshHiveColony colony, Action<bool, string> check)
    {
        HiveLord lord = colony.Lord;
        Map map = lord.Map;
        foreach (Thing hive in lord.hives)
            hive.TryGetComp<CompHiveContainer>().units.TryDropAll(hive.Position, map, ThingPlaceMode.Near);
        foreach (Pawn pawn in lord.groups.SelectMany(group => group.units).Distinct().ToArray())
        {
            foreach (Hediff injury in pawn.health.hediffSet.hediffs.OfType<Hediff_Injury>().ToArray())
                pawn.health.RemoveHediff(injury);
            if (pawn.GetNeed<Need_Maintenance>() is { } need)
                need.CurLevel = need.MaxLevel;
            if (pawn.Spawned)
                pawn.Position = colony.Origin + new IntVec3(-20, 0, -15);
        }
        SetLordField(lord, "threatenedUntilTick", 0);
        GrowSupports(colony);
        System.IO.File.WriteAllText(System.IO.Path.Combine(GenFilePaths.SaveDataFolderPath, "FH-HostileHiveTests", "mature-layout.txt"),
            string.Join("\n", colony.Slots.Select(slot => $"{slot.def.defName} Anchor={slot.anchor} Position={slot.position} Occupied={slot.Occupied} "
                + $"Fits={typeof(HostileFleshHiveColony).GetMethod("FitsLayout", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(colony, new object[] { slot, slot.anchor })} "
                + $"Native={lord.CanPlaceExpansion(slot.def, null, slot.anchor, slot.def.defaultPlacingRot)}"))
            + "\n" + string.Join("\n", colony.Slots.Where(slot => !slot.Occupied).Select(slot =>
            {
                object[] args = { slot.def, null, slot.anchor, slot.def.defaultPlacingRot, null };
                typeof(HiveLord).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                    .Single(method => method.Name == "CanPlaceExpansion" && method.GetParameters().Length == 5).Invoke(lord, args);
                return $"{slot.def.defName} {slot.anchor} Reason={args[4]} Terrain={slot.anchor.GetTerrain(map)} Foundation={map.terrainGrid.FoundationAt(slot.anchor)} "
                    + string.Join(", ", slot.anchor.GetThingList(map).Select(thing => $"{thing}({thing.def.category})"));
            }))
            + "\n" + lord.GetDebugHistory());
        check(colony.Slots.Where(slot => slot.def != HostileFleshHiveDefOf.FH_HostileFleshHive && slot.minHives <= 2)
            .All(slot => slot.Occupied), "all nerve defense and barricade organs grow in configured slots");
        check(colony.HasNerveCenter && colony.Slots.Count(slot => slot.def.defName == "FH_HostileFleshDigester" && slot.Occupied) == 2,
            "nerve center and two entrance digesters established");
        HostileHiveSlot target = colony.Slots.First(slot => slot.def == HostileFleshHiveDefOf.FH_HostileFleshHive && !slot.Occupied);
        List<Thing> walls = new();
        foreach (IntVec3 cell in GenRadial.RadialCellsAround(target.anchor, 3f, true))
            walls.Add(GenSpawn.Spawn(ThingMaker.MakeThing(ThingDefOf.Wall, GenStuff.DefaultStuffFor(ThingDefOf.Wall)), cell, map));
        colony.AddNutrition(400f);
        SetLordField(lord, "nextExpansionTick", Find.TickManager.TicksGame);
        check(!lord.TryExpand() && walls.All(wall => wall.Spawned), "fully blocked planned hive waits without overwriting obstacles or drifting");
        foreach (Thing wall in walls)
            wall.Destroy();
        for (int index = 0; index < 2; index++)
        {
            colony.AddNutrition(400f);
            SetLordField(lord, "nextExpansionTick", Find.TickManager.TicksGame);
            check(lord.TryExpand(), "cleared planned hive resumes growth " + index);
            SetLordField(lord, "expansionFinishTick", Find.TickManager.TicksGame);
            Find.TickManager.DebugSetTicksGame(Find.TickManager.TicksGame + 250);
            lord.Tick();
            foreach (Thing hive in lord.hives)
                hive.TryGetComp<CompProgressHolder>().progresses.Clear();
            colony.Tick();
            GrowSupports(colony);
        }
        check(lord.hives.Count == 4 && !lord.TryExpand(), "mature colony stops at four planned hives");
        check(colony.Slots.All(slot => slot.Occupied && slot.building.Faction == lord.faction),
            "every planned building belongs to enemy colony");
        check(colony.Slots.All(slot => slot.building.Position.DistanceToSquared(slot.anchor)
            <= (slot.def == HostileFleshHiveDefOf.FH_HostileFleshHive ? 9f : 4f)), "completed colony keeps bounded layout offsets");
        foreach (Thing hive in lord.hives)
            foreach (HiveResource resource in hive.TryGetComp<CompHiveResource>().resources)
                resource.DecreaseResource(resource.Amount);
        foreach (Thing hive in lord.hives)
            for (int tick = 0; tick < GenDate.TicksPerDay; tick++)
                hive.TryGetComp<CompHostileHiveIncome>().CompTick();
        check(Mathf.Abs(colony.Nutrition - 40f) < 0.001f, "four hives yield forty nutrition per day with no support income");
        Building_HostileFleshStore store = colony.Store;
        Pawn animal = PawnGenerator.GeneratePawn(PawnKindDefOf.Muffalo, null);
        GenSpawn.Spawn(animal, colony.Origin + new IntVec3(20, 0, 10), map);
        animal.Kill(null);
        check(store.Receive(animal.Corpse), "storage accepts a real wild animal corpse");
        int processed = store.ProcessedCorpses;
        for (int index = 0; index < 4; index++)
            store.TickRare();
        check(store.ProcessedCorpses == processed, "digestion respects time even with nerve center");
        store.TickRare();
        check(store.ProcessedCorpses == processed + 1 && store.HarvestedNutrition > 0f,
            "nerve center doubles actual corpse processing speed");
        for (int index = 0; index < 8; index++)
        {
            Pawn stored = PawnGenerator.GeneratePawn(PawnKindDefOf.Muffalo, null);
            GenSpawn.Spawn(stored, colony.Origin + new IntVec3(20, 0, 10), map);
            stored.Kill(null);
            if (!store.Receive(stored.Corpse))
                throw new InvalidOperationException("Test storage did not accept corpse " + index);
        }
        check(store.StoredCount == 8 && !store.CanReceive, "corpse storage has bounded capacity of eight");
        colony.Tick();
        check(!colony.Hunting, "full corpse storage pauses hunting");
        foreach (Thing corpse in store.GetDirectlyHeldThings().ToArray())
        {
            store.GetDirectlyHeldThings().Remove(corpse);
            corpse.Destroy();
        }
        colony.AddNutrition(200f);
        List<Pawn> units = lord.groups.SelectMany(group => group.units).Distinct().ToList();
        PawnKindDef kind = DefDatabase<UnitDef>.GetNamed("FH_Unit_HostileFingerspike").kind;
        List<Pawn> reinforcements = new();
        for (int index = units.Count; index < 16; index++)
        {
            Pawn pawn = PawnGenerator.GeneratePawn(kind, lord.faction);
            GenSpawn.Spawn(pawn, colony.Origin + new IntVec3(-20, 0, -15), map);
            if (pawn.GetNeed<Need_Maintenance>() is { } need)
                need.CurLevel = need.MaxLevel;
            reinforcements.Add(pawn);
        }
        lord.AcceptProducedUnits(lord.hives[0], reinforcements);
        if (!map.mapPawns.FreeColonistsSpawned.Any())
        {
            Pawn player = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
            GenSpawn.Spawn(player, colony.Origin + new IntVec3(60, 0, 60), map);
        }
        SetLordField(lord, "nextAttackTick", Find.TickManager.TicksGame);
        SetLordField(lord, "threatenedUntilTick", 0);
        check(lord.TryAttack() && colony.HealthyDefenders >= 4, "raid departs only with four healthy defenders retained");
        lord.Notify(TriggerType.BuildingDamaged, lord.hives[0]);
        Find.TickManager.DebugSetTicksGame(Find.TickManager.TicksGame + 250);
        lord.Tick();
        check(!lord.Attacking, "hive damage recalls an active raid");
        Thing originalCore = lord.hives.Single(hive => hive.Position == colony.Origin);
        originalCore.Destroy();
        SetLordField(lord, "threatenedUntilTick", 0);
        colony.AddNutrition(400f);
        colony.Tick();
        SetLordField(lord, "nextExpansionTick", Find.TickManager.TicksGame);
        check(lord.TryExpand(), "surviving sub hives can rebuild destroyed central hive");
        SetLordField(lord, "expansionFinishTick", Find.TickManager.TicksGame);
        Find.TickManager.DebugSetTicksGame(Find.TickManager.TicksGame + 250);
        lord.Tick();
        colony.Tick();
        check(lord.hives.Count == 4 && lord.hives.Any(hive => hive.Position.InHorDistOf(colony.Origin, 3f)),
            "replacement central hive occupies original planned zone");
        colony.AddNutrition(400f);
        foreach (Thing owned in lord.hives)
            owned.TryGetComp<CompProgressHolder>().progresses.Clear();
        int beforePopulation = lord.Population;
        int maxUnits = lord.def.maxUnits;
        lord.def.maxUnits = beforePopulation + 1;
        float beforeProduction = colony.Nutrition;
        Thing productionHive = lord.hives[0];
        check(lord.TryProduce(productionHive), "mature colony produces through native HCF spawner");
        UnitSpawnData progress = productionHive.TryGetComp<CompProgressHolder>().progresses.OfType<UnitSpawnData>().Single();
        float cost = progress.def.costs.Where(cost => cost.resource == HostileFleshHiveDefOf.FH_Resource_HostileNutrition).Sum(cost => cost.amount);
        check(Mathf.Abs(beforeProduction - colony.Nutrition - cost) < 0.001f, "production deducts actual recipe nutrition");
        check(!lord.TryProduce(lord.hives[1]), "queued births count toward population cap");
        productionHive.TryGetComp<CompProgressHolder>().CompTickInterval(GenDate.TicksPerDay);
        foreach (Pawn newborn in productionHive.TryGetComp<CompHiveContainer>().units.InnerListForReading.ToArray())
            for (int index = 0; index < 100; index++)
                productionHive.TryGetComp<CompHiveContainer>().IncreaseUnitMaintenance(newborn, false);
        Find.TickManager.DebugSetTicksGame(Find.TickManager.TicksGame + 250);
        lord.Tick();
        check(lord.Population == beforePopulation + 1 && lord.QueuedUnits == 0
            && lord.groups.SelectMany(group => group.units).All(pawn => pawn.Faction == lord.faction),
            "real produced fleshbeast joins enemy groups without an extra payment or free unit");
        lord.def.maxUnits = maxUnits;
        Thing separateHive = ThingMaker.MakeThing(HostileFleshHiveDefOf.FH_HostileFleshHive);
        separateHive.SetFaction(lord.faction);
        GenSpawn.Spawn(separateHive, colony.Origin + new IntVec3(60, 0, -10), map);
        HiveLord separate = map.GetComponent<MapComponent_HiveLord>().CreateLord(lord.def, new[] { separateHive });
        check(!productionHive.TryGetComp<CompHiveResource>().CanAdd(separate.groups[0])
            && !separateHive.TryGetComp<CompHiveResource>().CanAdd(lord.groups[0])
            && !productionHive.TryGetComp<CompHiveContainer>().CanAdd(separate.groups[0]),
            "same faction separate colonies cannot share resource pools or healing containers");
        separateHive.Destroy();
    }

    private static void GrowSupports(HostileFleshHiveColony colony)
    {
        for (int index = 0; index < 40; index++)
        {
            colony.AddNutrition(400f);
            colony.Tick();
            HostileHiveSlot pending = colony.Slots.FirstOrDefault(slot => slot.finishTick > 0);
            if (pending == null)
                return;
            Find.TickManager.DebugSetTicksGame(pending.finishTick);
            colony.Tick();
        }
        throw new InvalidOperationException("Supporting layout never completed.");
    }

    private static void SetLordField(HiveLord lord, string name, object value)
    {
        typeof(HiveLord).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(lord, value);
    }
}
