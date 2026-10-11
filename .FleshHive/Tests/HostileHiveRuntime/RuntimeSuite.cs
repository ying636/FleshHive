using System.IO;
using System.Reflection;
using System.Text;
using FleshHive;
using HiveCreatureFramework;
using RimWorld;
using UnityEngine;
using Verse;

namespace FH.HostileHiveTests;

public static class RuntimeSuite
{
    private static string OutputDirectory => Path.Combine(GenFilePaths.SaveDataFolderPath, "FH-HostileHiveTests");

    public static void Poll()
    {
        if (stage < 0)
            return;
        try
        {
            Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
            Directory.CreateDirectory(OutputDirectory);
            if (stage == 0)
                Begin();
            else if (stage == 1)
                SimulateHunt();
            else if (stage == 2)
                VerifyLoaded();
            File.WriteAllText(Path.Combine(OutputDirectory, "progress.txt"), $"Stage={stage} Tick={Find.TickManager.TicksGame} Checks={checks.Count}");
        }
        catch (Exception exception)
        {
            stage = -1;
            File.WriteAllText(Path.Combine(OutputDirectory, "failure.txt"), exception.ToString(), Encoding.UTF8);
            Log.Error("[FH Hostile Hive Tests] " + exception);
        }
    }

    private static void Begin()
    {
        Prefs.DevMode = true;
        File.WriteAllText(Path.Combine(OutputDirectory, "language-errors.txt"),
            string.Join("\n", LanguageDatabase.activeLanguage.loadErrors.Concat(
                LanguageDatabase.activeLanguage.defInjections.SelectMany(package => package.loadErrors))));
        Map map = Find.CurrentMap;
        definition = (HiveLordDef_HostileFleshHive)DefDatabase<HiveLordDef>.GetNamed("FH_HiveLord_SpreadingFleshHive");
        Check(!definition.ConfigErrors().Any(), "loaded hostile HiveLord definition validates");
        Check(definition.resourceIncomePerDay == 0f && definition.initialResourceFraction == 0f,
            "capacity income and free expanded hive resources disabled");
        Check(definition.layout.Count(slot => slot.building == HostileFleshHiveDefOf.FH_HostileFleshHive) == 3,
            "three peripheral hive slots configured");
        IncidentDef incident = DefDatabase<IncidentDef>.GetNamed("FH_Incident_SpreadingFleshHive");
        IncidentParms parms = new() { target = map, points = 200f };
        Check(incident.Worker.TryExecute(parms), "actual event creates hostile flesh hive on home map");
        HiveLord eventLord = map.GetComponent<MapComponent_HiveLord>().lords.Single(lord => lord.def == definition);
        HostileFleshHiveColony eventColony = map.GetComponent<MapComponent_HostileFleshHive>().GetColony(eventLord);
        Check(eventLord.Population >= 7 && eventLord.Population <= 10 && Mathf.Approximately(eventColony.Nutrition, 100f),
            "event supplies configured initial defenders and one startup budget");
        Check(!incident.Worker.CanFireNow(parms), "duplicate event cannot create overlapping active colony");
        Pawn[] eventUnits = eventLord.groups.SelectMany(group => group.units).Distinct().ToArray();
        foreach (Thing eventHive in eventLord.hives.ToArray())
            eventHive.Destroy();
        foreach (Pawn pawn in eventUnits)
            pawn.Destroy();
        IntVec3 center = new(55, 0, 55);
        List<IntVec3> arena = GenRadial.RadialCellsAround(center, 42f, true).Where(cell => cell.InBounds(map)).ToList();
        foreach (Thing thing in arena.SelectMany(cell => cell.GetThingList(map)).Distinct().ToArray())
        {
            if (thing.def.destroyable)
                thing.Destroy();
            else
                thing.DeSpawn();
        }
        foreach (IntVec3 cell in arena)
        {
            if (map.terrainGrid.FoundationAt(cell) != null)
                map.terrainGrid.RemoveFoundation(cell, false);
            map.terrainGrid.SetTerrain(cell, TerrainDefOf.Soil);
            map.roofGrid.SetRoof(cell, null);
        }
        map.fogGrid.ClearAllFog();
        foreach (Pawn animal in map.mapPawns.AllPawnsSpawned.Where(pawn => pawn.Faction == null).ToArray())
            animal.Destroy();
        hive = Spawn(HostileFleshHiveDefOf.FH_HostileFleshHive, center, Faction.OfEntities);
        lord = map.GetComponent<MapComponent_HiveLord>().CreateLord(definition, new[] { hive });
        colony = map.GetComponent<MapComponent_HostileFleshHive>().GetColony(lord);
        for (int seed = 0; seed < 20; seed++)
        {
            Rand.PushState(seed);
            HostileFleshHiveColony trial = new(lord);
            Rand.PopState();
            bool fits = trial.Slots.All(slot => GenRadial.RadialCellsAround(slot.anchor,
                slot.def == HostileFleshHiveDefOf.FH_HostileFleshBarricade ? 0f : 2f, true).Any(cell =>
                    (bool)typeof(HostileFleshHiveColony).GetMethod("FitsLayout", BindingFlags.Instance | BindingFlags.NonPublic)
                        .Invoke(trial, new object[] { slot, cell })));
            if (!fits)
                throw new InvalidOperationException("Rotated layout cannot maintain corridor and slot constraints for seed " + seed);
        }
        Check(true, "rotated layouts preserve functional zones and corridors across twenty seeds");
        Check(Mathf.Approximately(colony.Nutrition, 0f), "new hive receives zero automatic nutrition");
        colony.AddNutrition(100f);
        HiveResource resource = hive.TryGetComp<CompHiveResource>().resources[0];
        resource.DecreaseResource(resource.Amount);
        CompHostileHiveIncome income = hive.TryGetComp<CompHostileHiveIncome>();
        for (int index = 0; index < GenDate.TicksPerDay; index++)
            income.CompTick();
        Check(Mathf.Abs(resource.Amount - 10f) < 0.001f, "one hive receives exactly ten nutrition per day");
        resource.DecreaseResource(resource.Amount);
        resource.baseLimit = 400f;
        typeof(HiveResource).GetField("cacheLimit", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(resource, -1f);
        for (int index = 0; index < GenDate.TicksPerDay; index++)
            income.CompTick();
        Check(Mathf.Abs(resource.Amount - 10f) < 0.001f, "daily income independent of resource capacity");
        resource.baseLimit = 200f;
        typeof(HiveResource).GetField("cacheLimit", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(resource, -1f);
        resource.DecreaseResource(resource.Amount);
        colony.AddNutrition(100f);
        List<Pawn> defenders = new();
        PawnKindDef kind = DefDatabase<UnitDef>.GetNamed("FH_Unit_HostileFingerspike").kind;
        for (int index = 0; index < 9; index++)
        {
            Pawn pawn = PawnGenerator.GeneratePawn(kind, lord.faction);
            GenSpawn.Spawn(pawn, center + new IntVec3(index - 4, 0, -4), map);
            if (pawn.GetNeed<Need_Maintenance>() is { } need)
                need.CurLevel = need.MaxLevel;
            defenders.Add(pawn);
        }
        lord.AcceptProducedUnits(hive, defenders);
        Check(lord.Population == 9 && colony.HealthyCount == 9, "initial defenders belong to enemy HiveLord");
        CompHiveContainer_HostileFleshHive medical = hive.TryGetComp<CompHiveContainer_HostileFleshHive>();
        Pawn patient = defenders[0];
        foreach (Hediff_Injury existing in patient.health.hediffSet.hediffs.OfType<Hediff_Injury>().ToArray())
            patient.health.RemoveHediff(existing);
        Hediff_Injury injury = (Hediff_Injury)HediffMaker.MakeHediff(HediffDefOf.Cut, patient, patient.RaceProps.body.corePart);
        injury.Severity = 2f;
        patient.health.AddHediff(injury);
        float beforeHealing = colony.Nutrition;
        medical.Heal(patient, 1f);
        Check(Mathf.Abs(colony.Nutrition - beforeHealing + 0.2f) < 0.001f && Mathf.Abs(injury.Severity - 1f) < 0.001f,
            $"healing pays actual nutrition for restored injury points (nutrition={beforeHealing}/{colony.Nutrition}, severity={injury.Severity})");
        medical.Heal(patient, 1f);
        float afterHealing = colony.Nutrition;
        medical.Heal(patient, 1f);
        Check(Mathf.Abs(colony.Nutrition - afterHealing) < 0.001f, "healthy units incur no phantom healing charge");
        colony.AddNutrition(100f - colony.Nutrition);
        HostileHiveSlot storage = colony.Slots.Single(slot => slot.def == HostileFleshHiveDefOf.FH_HostileFleshStore);
        float before = colony.Nutrition;
        colony.Tick();
        File.WriteAllText(Path.Combine(OutputDirectory, "layout-state.txt"),
            $"Nutrition={before}/{colony.Nutrition} Threatened={lord.Threatened} Expanding={lord.Expanding} Finish={storage.finishTick} Anchor={storage.anchor} Size={storage.def.size}\n"
            + string.Join("\n", GenRadial.RadialCellsAround(storage.anchor, 2f, true).Select(cell =>
                $"{cell} Fits={typeof(HostileFleshHiveColony).GetMethod("FitsLayout", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(colony, new object[] { storage, cell })} "
                + $"Native={lord.CanPlaceExpansion(storage.def, null, cell, storage.def.defaultPlacingRot)} "
                + $"CanPlace={typeof(HostileFleshHiveColony).GetMethod("CanPlaceSupport", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(colony, new object[] { storage, cell })}")));
        Check(storage.finishTick > 0 && colony.Store == null && Mathf.Approximately(before, colony.Nutrition),
            "support organ grows over time with no premature payment");
        Find.TickManager.DebugSetTicksGame(Find.TickManager.TicksGame + definition.constructionTicks);
        colony.Tick();
        Check(colony.Store != null && Mathf.Abs(colony.Nutrition - (before - storage.cost)) < 0.001f,
            "completed storage organ pays its actual cost");
        Check(colony.Store.Position.DistanceToSquared(storage.anchor) <= 4f, "storage uses its planned functional zone");
        HostileHiveSlot subHive = colony.Slots.First(slot => slot.def == HostileFleshHiveDefOf.FH_HostileFleshHive && !slot.Occupied);
        Check(float.IsNegativeInfinity(definition.Worker.ScoreExpansionCell(lord, hive, hive.def, center + new IntVec3(18, 0, 0))),
            "unplanned expansion position rejected");
        SetLordField("nextExpansionTick", Find.TickManager.TicksGame);
        Check(lord.TryExpand(), "planned first sub hive expansion starts");
        Check(lord.ExpansionRect.CenterCell.DistanceToSquared(subHive.anchor) <= 9f, "expansion remains in planned triangle");
        Check(Mathf.Abs(colony.Nutrition - (before - storage.cost - 50f)) < 0.001f, "expansion pays fifty actual nutrition");
        SetLordField("expansionFinishTick", Find.TickManager.TicksGame);
        lord.Tick();
        Check(lord.hives.Count == 2 && !lord.Expanding, "planned expansion completes and joins original controller");
        Check(lord.hives[1].TryGetComp<CompHiveResource>().resources[0].Amount == 0f,
            "expanded hive does not mint starting resources");
        foreach (Thing owned in lord.hives)
            owned.TryGetComp<CompProgressHolder>().progresses.Clear();
        colony.Tick();
        Check(colony.Nutrition >= 40f, "construction and expansion preserve reserve");
        foreach (HiveResource value in lord.hives.SelectMany(owned => owned.TryGetComp<CompHiveResource>().resources))
            value.DecreaseResource(value.Amount);
        colony.AddNutrition(39f);
        Check(!definition.Worker.ShouldProduce(lord, hive) && !definition.Worker.ShouldExpand(lord),
            "low resources stop production and expansion");
        foreach (Pawn pawn in defenders)
            pawn.Position = center + new IntVec3(Rand.Range(-4, 5), 0, -4);
        prey = PawnGenerator.GeneratePawn(PawnKindDefOf.Muffalo, null);
        GenSpawn.Spawn(prey, center + new IntVec3(18, 0, 0), map);
        beforeHuntNutrition = colony.Nutrition;
        typeof(HostileFleshHiveColony).GetField("nextHuntTick", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(colony, 0);
        colony.Tick();
        Check(colony.Hunting && colony.Prey == prey && colony.HuntGroup.units.Count == 5,
            "five healthy hunters select reachable undesignated wild animal");
        Check(lord.groups.Where(group => group != colony.HuntGroup).SelectMany(group => group.units).Count() == 4,
            "four defenders stay home");
        lord.Notify(TriggerType.BuildingDamaged, colony.Store);
        colony.Tick();
        Check(!colony.Hunting && lord.groups.Where(group => group != colony.HuntGroup).SelectMany(group => group.units).Count() == 9,
            "support organ attack recalls all hunters into defense");
        SetLordField("threatenedUntilTick", 0);
        typeof(HostileFleshHiveColony).GetField("nextHuntTick", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(colony, 0);
        colony.Tick();
        Check(colony.Hunting, "hunt resumes after cooldown and threat clears");
        Check(!definition.Worker.ShouldAttack(lord), "active hunt cannot be recruited for simultaneous raid");
        huntStartTick = Find.TickManager.TicksGame;
        stage = 1;
    }

    private static void SimulateHunt()
    {
        for (int index = 0; index < 120; index++)
            Find.TickManager.DoSingleTick();
        File.WriteAllText(Path.Combine(OutputDirectory, "hunt-state.txt"),
            $"Prey={prey?.Spawned} Dead={prey?.Dead} Hunting={colony.Hunting} Gathered={colony.Gathered} Store={colony.Store?.StoredCount} Nutrition={colony.Nutrition}\n"
            + string.Join("\n", lord.groups.SelectMany(group => group.units).Select(pawn => $"{pawn} {pawn.Position} {pawn.CurJob} {pawn.mindState.duty?.def}")));
        string phase = $"{colony.Hunting}/{colony.Gathered}/{prey.Dead}/{colony.Store.StoredCount}/{colony.Store.ProcessedCorpses}";
        if (phase != lastHuntPhase)
        {
            lastHuntPhase = phase;
            File.AppendAllText(Path.Combine(OutputDirectory, "hunt-history.txt"),
                $"Tick={Find.TickManager.TicksGame} Phase={phase} Corpse={prey.Corpse?.PositionHeld} Holder={prey.Corpse?.ParentHolder} Healthy={colony.HealthyCount}\n"
                + File.ReadAllText(Path.Combine(OutputDirectory, "hunt-state.txt")) + "\n"
                + lord.GetDebugHistory() + "\n\n");
        }
        if (prey.Dead && !colony.Hunting && colony.Store.StoredCount == 0 && colony.Store.ProcessedCorpses > 0)
        {
            Check(true, "real tick simulation hunts kills hauls and processes corpse into nutrition");
            Check(prey.Corpse == null || prey.Corpse.Destroyed, "processed corpse cannot settle twice");
            float expected = colony.Nutrition;
            colony.Store.TickRare();
            Check(Mathf.Abs(colony.Nutrition - expected) < 0.001f, "empty storage organ produces no phantom resources");
            expectedLordId = lord.id;
            expectedNutrition = colony.Nutrition;
            expectedOrigin = colony.Origin;
            expectedStorage = colony.Store.Position;
            expectedPopulation = lord.Population;
            saveName = "FH_HostileHive_Test_" + Guid.NewGuid().ToString("N");
            GameDataSaveLoader.SaveGame(saveName);
            Check(File.Exists(GenFilePaths.FilePathForSavedGame(saveName)), "full game save created");
            stage = 2;
            GameDataSaveLoader.LoadGame(saveName);
        }
        else if (Find.TickManager.TicksGame - huntStartTick > 22000)
            throw new InvalidOperationException("Hunting did not complete. See hunt-state.txt.");
    }

    private static void VerifyLoaded()
    {
        lord = Find.CurrentMap.GetComponent<MapComponent_HiveLord>().lords.Single(lord => lord.id == expectedLordId);
        colony = Find.CurrentMap.GetComponent<MapComponent_HostileFleshHive>().GetColony(lord);
        Check(colony.Origin == expectedOrigin && colony.Store?.Position == expectedStorage,
            "full save load preserves colony origin layout and support references");
        Check(Mathf.Abs(colony.Nutrition - expectedNutrition) < 0.001f && lord.Population == expectedPopulation,
            "full save load preserves actual resources and population");
        Check(colony.HuntGroup != null && lord.groups.Contains(colony.HuntGroup), "saved hunting group remains registered");
        LayoutSuite.Run(colony, Check);
        SetLordField("threatenedUntilTick", 0);
        Building_HostileFleshStore store = colony.Store;
        IntVec3 oldPosition = store.Position;
        store.Destroy();
        colony.AddNutrition(60f);
        colony.Tick();
        Find.TickManager.DebugSetTicksGame(Find.TickManager.TicksGame + definition.constructionTicks);
        colony.Tick();
        Check(colony.Store?.Position == oldPosition, "destroyed support rebuilt at original slot");
        foreach (Thing owned in lord.hives.ToArray())
            owned.Destroy();
        Find.TickManager.DebugSetTicksGame((Find.TickManager.TicksGame / 250 + 1) * 250);
        Find.CurrentMap.GetComponent<MapComponent_HostileFleshHive>().MapComponentTick();
        Check(!Find.CurrentMap.GetComponent<MapComponent_HiveLord>().lords.Contains(lord)
            && !Find.CurrentMap.listerThings.ThingsOfDef(HostileFleshHiveDefOf.FH_HostileFleshStore).Any(),
            "last hive destruction cleans controller and supporting organs");
        stage = -1;
        File.WriteAllText(Path.Combine(OutputDirectory, "passed.txt"), string.Join(Environment.NewLine, checks), Encoding.UTF8);
        Log.Message("[FH Hostile Hive Tests] Passed " + checks.Count + " checks.");
    }

    private static Thing Spawn(ThingDef def, IntVec3 cell, Faction faction)
    {
        Thing thing = ThingMaker.MakeThing(def);
        thing.SetFaction(faction);
        return GenSpawn.Spawn(thing, cell, Find.CurrentMap);
    }

    private static void SetLordField(string name, object value)
    {
        typeof(HiveLord).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(lord, value);
    }

    private static void Check(bool success, string description)
    {
        if (!success)
            throw new InvalidOperationException("FAIL: " + description);
        checks.Add("PASS: " + description);
        File.WriteAllText(Path.Combine(OutputDirectory, "checks.txt"), string.Join(Environment.NewLine, checks), Encoding.UTF8);
    }

    private static List<string> checks = new();
    private static int stage;
    private static HiveLordDef_HostileFleshHive definition;
    private static HiveLord lord;
    private static HostileFleshHiveColony colony;
    private static Thing hive;
    private static Pawn prey;
    private static float beforeHuntNutrition;
    private static int huntStartTick;
    private static int expectedLordId;
    private static float expectedNutrition;
    private static IntVec3 expectedOrigin;
    private static IntVec3 expectedStorage;
    private static int expectedPopulation;
    private static string saveName;
    private static string lastHuntPhase;
}
