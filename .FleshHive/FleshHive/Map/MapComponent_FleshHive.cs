using System.Collections.Generic;
using System.Linq;
using HiveCreatureFramework;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace FleshHive;

public class MapComponent_FleshHive : MapComponent
{
    public MapComponent_FleshHive(Map map) : base(map)
    {
    }

    public static MapFleshHive GetMapFleshHive(Map map)
    {
        if (map == null)
        {
            return null;
        }
        if (mapFleshHives.TryGetValue(map, out MapFleshHive mapFleshHive) && mapFleshHive != null)
        {
            return mapFleshHive;
        }
        MapComponent_FleshHive component = map.GetComponent<MapComponent_FleshHive>();
        if (component == null)
        {
            return null;
        }
        mapFleshHive = component.MapFleshHive;
        mapFleshHives[map] = mapFleshHive;
        return mapFleshHive;
    }

    public static float AddNutrition(Map map, float amount)
    {
        if (map == null || amount <= 0f)
        {
            return 0f;
        }

        MapFleshHive mapFleshHive = GetMapFleshHive(map);
        MapComponent_FleshHive component = map.GetComponent<MapComponent_FleshHive>();
        if (mapFleshHive == null || component == null)
        {
            return 0f;
        }

        float oldNutrition = mapFleshHive.nutrition;
        mapFleshHive.nutrition = Mathf.Min(component.NutritionLimit, mapFleshHive.nutrition + amount);
        return mapFleshHive.nutrition - oldNutrition;
    }

    public HashSet<Pawn> CachedFleshBeasts
    {
        get
        {
            if (cachedFleshBeasts == null)
            {
                cachedFleshBeasts = new HashSet<Pawn>();
            }
            return cachedFleshBeasts;
        }
    }

    public HashSet<Pawn> CachedNeedsTwistedFlesh
    {
        get
        {
            if (cachedNeedsTwistedFlesh == null)
            {
                cachedNeedsTwistedFlesh = new HashSet<Pawn>();
            }
            return cachedNeedsTwistedFlesh;
        }
    }

    public HashSet<Building> CachedFleshBuildings
    {
        get
        {
            if (cachedFleshBuildings == null)
            {
                cachedFleshBuildings = new HashSet<Building>();
            }
            return cachedFleshBuildings;
        }
    }

    public int HiveScale => MapFleshHive.hiveScale + ExtraHiveScale;
    public int HiveGroupCostLimit => HiveScale / HiveScalePerGroupCost + ExtraHiveGroupCostLimit;
    public bool HasFleshHive => cachedFleshHives?.Count > 0;
    public bool IsHiveHungry => cachedFleshHives?.Any(hive => hive?.Spawned == true
        && hive.Faction == Faction.OfPlayer
        && FleshHiveHungerUtility.IsHungry(hive)) == true;
    public int ExtraHiveGroupCostLimit => hiveCapacityProviders?.Where(comp => comp?.parent?.Spawned == true).Sum(comp => comp.Capacity) ?? 0;
    public int ExtraHiveScale => hiveScaleProviders?.Where(comp => comp?.parent?.Spawned == true).Sum(comp => comp.Scale) ?? 0;
    public bool NutritionAllowedToFill
    {
        get => MapFleshHive.nutritionAllowedToFill;
        set => MapFleshHive.nutritionAllowedToFill = value;
    }
    public float NutritionLimit => HiveScale * NutritionLimitPerHiveScale;
    public float NutritionTargetValue
    {
        get => Mathf.Clamp01(MapFleshHive.nutritionTargetValue);
        set => MapFleshHive.nutritionTargetValue = Mathf.Clamp01(value);
    }
    public float Activity
    {
        get => MapFleshHive.activity;
        set => MapFleshHive.activity = Mathf.Clamp(value, 0f, ActivityLimit);
    }
    public bool AutoSuppressActivity
    {
        get => MapFleshHive.autoSuppressActivity;
        set => MapFleshHive.autoSuppressActivity = value;
    }
    public float AutoSuppressActivityThreshold
    {
        get => Mathf.Clamp01(MapFleshHive.autoSuppressActivityThreshold);
        set => MapFleshHive.autoSuppressActivityThreshold = Mathf.Clamp01(value);
    }
    public bool ShouldAutoSuppressActivity => AutoSuppressActivity && ActivityPercent >= AutoSuppressActivityThreshold;
    public float ActivityLimit => 100f;
    public float ActivityPercent => ActivityLimit > 0f ? Mathf.Clamp01(Activity / ActivityLimit) : 0f;
    public int CurrentHiveGroupCost => GetHiveGroupCost();
    public int FleshTerrainHiveScale => MapFleshHive.hiveScale;
    public FleshHiveUpgradeDef ActiveUpgrade => MapFleshHive.activeUpgrade;
    public float ActiveUpgradeProgressPercent => MapFleshHive.activeUpgradeTotalTime > 0f
        ? Mathf.Clamp01(MapFleshHive.activeUpgradeProgress / MapFleshHive.activeUpgradeTotalTime)
        : 0f;
    public float ActiveUpgradeRemainingTicks => Mathf.Max(0f,
        MapFleshHive.activeUpgradeTotalTime - MapFleshHive.activeUpgradeProgress);
    public int PlanCheckIntervalTicks => planCheckIntervalTicks > 0
        ? planCheckIntervalTicks
        : DefaultPlanCheckIntervalTicks;

    public List<HiveResourcer> HiveResourcers
    {
        get
        {
            if (hiveResourcers == null)
            {
                hiveResourcers = new List<HiveResourcer>();
            }
            return hiveResourcers;
        }
    }

    public MapFleshHive MapFleshHive
    {
        get
        {
            if (mapFleshHive == null)
            {
                mapFleshHive = new MapFleshHive();
            }
            return mapFleshHive;
        }
    }

    public List<CompHiveSpawner_FleshTrait> GetFleshbeastSpawners()
    {
        return map.listerThings.ThingsOfDef(FleshHiveDefOf.FH_FleshHive)
            .Concat(map.listerThings.ThingsOfDef(FleshHiveDefOf.FH_FleshPrimaryNest))
            .Where(thing => thing.Faction == Faction.OfPlayer)
            .OfType<ThingWithComps>()
            .Select(thing => thing.TryGetComp<CompHiveSpawner_FleshTrait>())
            .Where(spawner => spawner != null)
            .ToList();
    }

    public List<UnitDef> GetAvailableFleshbeastUnits()
    {
        return GetFleshbeastSpawners()
            .SelectMany(GetAvailableUnits)
            .Distinct()
            .OrderBy(unit => unit.index)
            .ToList();
    }

    public int GetCurrentUnitCount(UnitDef unit)
    {
        if (unit?.kind == null)
        {
            return 0;
        }

        CleanupCachedFleshBeasts();
        IEnumerable<Pawn> containedUnits = GameComponent_UnitGroup.Instance.groups
            .Where(group => group?.Map == map && group.units != null)
            .SelectMany(group => group.units)
            .Where(pawn => pawn?.ParentHolder is CompHiveContainer container
                && container.parent.Map == map
                && container.parent.Faction == Faction.OfPlayer);
        return CachedFleshBeasts
            .Concat(containedUnits)
            .Where(pawn => pawn != null && !pawn.Destroyed && !pawn.Dead
                && pawn.Faction == Faction.OfPlayer
                && pawn.kindDef == unit.kind)
            .Distinct()
            .Count();
    }

    public int GetQueuedUnitCount(UnitDef unit)
    {
        if (unit == null)
        {
            return 0;
        }

        return GetFleshbeastSpawners()
            .SelectMany(spawner => spawner.ProgressHolder.progresses)
            .Count(progress => GetProgressUnitDef(progress) == unit);
    }

    public int GetUnitMaintainTarget(UnitDef unit)
    {
        return unit != null && unitMaintainTargets.TryGetValue(unit, out int target) ? target : 0;
    }

    public void SetUnitMaintainTarget(UnitDef unit, int target)
    {
        if (unit == null)
        {
            return;
        }

        int maximum = GetUnitMaximumTarget(unit);
        target = Mathf.Max(0, target);
        if (maximum >= 0)
        {
            target = Mathf.Min(target, maximum);
        }

        if (target == 0)
        {
            unitMaintainTargets.Remove(unit);
        }
        else
        {
            unitMaintainTargets[unit] = target;
        }
    }

    public int GetUnitMaximumTarget(UnitDef unit)
    {
        return unit != null && unitMaximumTargets.TryGetValue(unit, out int target) ? target : -1;
    }

    public void SetUnitMaximumTarget(UnitDef unit, int target)
    {
        if (unit == null)
        {
            return;
        }

        if (target < 0)
        {
            unitMaximumTargets.Remove(unit);
            return;
        }

        unitMaximumTargets[unit] = target;
        if (GetUnitMaintainTarget(unit) > target)
        {
            SetUnitMaintainTarget(unit, target);
        }
    }

    public void RegisterFleshBeast(Pawn pawn)
    {
        if (pawn.TryGetComp<UnitComp>() == null)
        {
            return;
        }

        if (pawn is FleshReplicaUnit { Active: true } replica
            && replica.SourceHediff?.parentChildParasite == true)
        {
            cachedFleshBeasts?.Remove(pawn);
            return;
        }

        CachedFleshBeasts.Add(pawn);
    }

    public void GrantFleshBeastUpgradeHediffs(Pawn pawn)
    {
        if (pawn == null || pawn.Destroyed || pawn.Dead || pawn.health?.hediffSet == null)
        {
            return;
        }

        if (pawn.Faction != Faction.OfPlayer || pawn.TryGetComp<UnitComp>() == null)
        {
            return;
        }

        int tamingLevel = 0;
        if (MapFleshHive.completedUpgrades.Contains(FleshHiveDefOf.FH_Upgrade_FleshbeastTaming1))
        {
            tamingLevel++;
        }
        if (MapFleshHive.completedUpgrades.Contains(FleshHiveDefOf.FH_Upgrade_FleshbeastTaming2))
        {
            tamingLevel++;
        }

        SetFleshBeastUpgradeHediff(pawn, FleshHiveDefOf.FH_Hediff_Upgrade_Reactivation,
            HasUpgradeEffect(FleshHiveUpgradeEffect.Reactivation));
        SetFleshBeastUpgradeHediff(pawn, FleshHiveDefOf.FH_Hediff_Upgrade_Agility,
            HasUpgradeEffect(FleshHiveUpgradeEffect.Agility) && FleshBeastKindUtility.IsSmall(pawn.kindDef));
        SetFleshBeastUpgradeHediff(pawn, FleshHiveDefOf.FH_Hediff_Upgrade_BoneSpikePenetration,
            HasUpgradeEffect(FleshHiveUpgradeEffect.BoneSpikePenetration) && HasModBoneSpikeSkill(pawn));
        SetFleshBeastUpgradeHediff(pawn, FleshHiveDefOf.FH_Hediff_Upgrade_ParasiticSpace,
            HasUpgradeEffect(FleshHiveUpgradeEffect.ParasiticSpace)
            && (FleshBeastKindUtility.IsLarge(pawn.kindDef) || FleshBeastKindUtility.IsGiant(pawn.kindDef)));
        SetFleshBeastUpgradeHediff(pawn, FleshHiveDefOf.FH_Hediff_Upgrade_NestMasterCarapace,
            HasUpgradeEffect(FleshHiveUpgradeEffect.NestMasterCarapace)
            && FleshBeastKindUtility.IsGiant(pawn.kindDef));
        SetFleshBeastUpgradeHediff(pawn, FleshHiveDefOf.FH_Hediff_Upgrade_FastHealing,
            HasUpgradeEffect(FleshHiveUpgradeEffect.NestHealing));
        SetFleshBeastUpgradeHediff(pawn, FleshHiveDefOf.FH_Hediff_Upgrade_FleshbeastTaming,
            tamingLevel > 0, tamingLevel);
        SetFleshBeastUpgradeHediff(pawn, FleshHiveDefOf.FH_Hediff_Upgrade_Robust,
            HasUpgradeEffect(FleshHiveUpgradeEffect.Robust)
            && FleshBeastKindUtility.IsLarge(pawn.kindDef));
    }

    public void RegisterFleshHive(Building_RenameableFleshHive hive)
    {
        if (hive == null)
        {
            return;
        }

        cachedFleshHives ??= new HashSet<Building_RenameableFleshHive>();
        cachedFleshHives.Add(hive);
    }

    public void UnregisterFleshHive(Building_RenameableFleshHive hive)
    {
        if (cachedFleshHives == null || hive == null)
        {
            return;
        }

        cachedFleshHives.Remove(hive);
    }

    public void UnregisterFleshBeast(Pawn pawn)
    {
        if (cachedFleshBeasts == null)
        {
            return;
        }
        cachedFleshBeasts.Remove(pawn);
    }

    public void RegisterTwistedFlesh(Pawn pawn)
    {
        CachedNeedsTwistedFlesh.Add(pawn);
    }

    public void RegisterFleshBlueprint(Blueprint_FleshBuild blueprint)
    {
        if (blueprint == null)
        {
            return;
        }

        MapFleshHive.CachedFleshBlueprints.Add(blueprint);
    }

    public float AddNutrition(float amount)
    {
        if (amount <= 0f)
        {
            return 0f;
        }

        float oldNutrition = MapFleshHive.nutrition;
        MapFleshHive.nutrition = Mathf.Min(NutritionLimit, MapFleshHive.nutrition + amount);
        return MapFleshHive.nutrition - oldNutrition;
    }

    public bool IsUpgradeCompleted(FleshHiveUpgradeDef upgrade)
    {
        return upgrade != null && MapFleshHive.completedUpgrades.Contains(upgrade);
    }

    public bool IsUpgradeProcessing(FleshHiveUpgradeDef upgrade)
    {
        return upgrade != null && MapFleshHive.activeUpgrade == upgrade;
    }

    public bool TryStartUpgrade(FleshHiveUpgradeDef upgrade)
    {
        if (upgrade == null)
        {
            Log.Error("[FleshHive] Cannot start a null hive upgrade.");
            return false;
        }

        if (MapFleshHive.activeUpgrade != null)
        {
            Messages.Message("FH_Upgrade_AlreadyProcessing".Translate(MapFleshHive.activeUpgrade.label),
                MessageTypeDefOf.RejectInput, false);
            return false;
        }

        if (IsUpgradeCompleted(upgrade))
        {
            Messages.Message("FH_Upgrade_AlreadyCompleted".Translate(upgrade.label),
                MessageTypeDefOf.RejectInput, false);
            return false;
        }

        if (FleshHiveDefOf.FH_Research_ComplexFleshHive?.IsFinished != true)
        {
            Messages.Message("FH_Upgrade_RequiresResearch".Translate(
                    FleshHiveDefOf.FH_Research_ComplexFleshHive?.LabelCap ?? "FH_Upgrade_PageTitle".Translate()),
                MessageTypeDefOf.RejectInput, false);
            return false;
        }

        if (!upgrade.prerequisites.NullOrEmpty() && !upgrade.prerequisites.All(IsUpgradeCompleted))
        {
            Messages.Message("FH_Upgrade_MissingPrerequisite".Translate(upgrade.label),
                MessageTypeDefOf.RejectInput, false);
            return false;
        }

        if (upgrade.requiresPrimaryNest && !HasPrimaryNest)
        {
            Messages.Message("FH_Upgrade_RequiresPrimaryNest".Translate(upgrade.label),
                MessageTypeDefOf.RejectInput, false);
            return false;
        }

        if (MapFleshHive.nutrition < upgrade.nutritionCost)
        {
            Messages.Message("FH_Upgrade_InsufficientNutrition".Translate(upgrade.nutritionCost),
                MessageTypeDefOf.RejectInput, false);
            return false;
        }

        SpecialResourceConsume_FleshBoxRequirement materialCost = MakeUpgradeMaterialCost(upgrade);
        AcceptanceReport materialReport = materialCost.Satisfied(map);
        if (!materialReport.Accepted)
        {
            Messages.Message("FH_Upgrade_InsufficientNerveFlesh".Translate(upgrade.nerveFleshCost),
                MessageTypeDefOf.RejectInput, false);
            return false;
        }

        if (!materialCost.TryConsume(map, map.Center))
        {
            Log.Error($"[FleshHive] Failed to consume nerve flesh for upgrade {upgrade.defName} after validation.");
            Messages.Message("FH_Upgrade_InsufficientNerveFlesh".Translate(upgrade.nerveFleshCost),
                MessageTypeDefOf.RejectInput, false);
            return false;
        }

        MapFleshHive.nutrition -= upgrade.nutritionCost;
        MapFleshHive.activeUpgrade = upgrade;
        MapFleshHive.activeUpgradeProgress = 0f;
        MapFleshHive.activeUpgradeTotalTime = Mathf.Max(1, upgrade.processingTicks);
        Messages.Message("FH_Upgrade_Started".Translate(upgrade.label), MessageTypeDefOf.PositiveEvent, false);
        return true;
    }

    public void DebugUnlockAllUpgrades()
    {
        if (!Prefs.DevMode)
        {
            Log.Error("[FleshHive] DebugUnlockAllUpgrades called while developer mode is disabled.");
            return;
        }

        MapFleshHive.completedUpgrades = DefDatabase<FleshHiveUpgradeDef>.AllDefsListForReading.ToHashSet();
        MapFleshHive.activeUpgrade = null;
        MapFleshHive.activeUpgradeProgress = 0f;
        MapFleshHive.activeUpgradeTotalTime = 0f;
        GrantAllFleshBeastUpgradeHediffs();
        Messages.Message("FH_Upgrade_DebugUnlocked".Translate(), MessageTypeDefOf.PositiveEvent, false);
    }

    public static float GetFleshExpansionSpeedFactor(Map map)
    {
        return GetUpgradeEffectFactor(map, FleshHiveUpgradeEffect.FleshExpansion);
    }

    public static float GetNutritionAbsorptionFactor(Map map)
    {
        return GetUpgradeEffectFactor(map, FleshHiveUpgradeEffect.NutritionAbsorption);
    }

    public static float GetCellDivisionSpeedFactor(Map map)
    {
        return GetUpgradeEffectFactor(map, FleshHiveUpgradeEffect.CellDivision);
    }

    public bool HasAutoRepairUpgrade => HasUpgradeEffect(FleshHiveUpgradeEffect.SelfRepair);

    public int AvailableNerveFlesh => FleshBoxUtility.GetCachedBoxes(map)
        .SelectMany(FleshBoxUtility.GetStoredThings)
        .Where(thing => thing.def == FleshHiveDefOf.FH_NerveFlesh)
        .Sum(thing => thing.stackCount);

    public bool AutoRepairFleshBuildings
    {
        get => MapFleshHive.autoRepairFleshBuildings;
        set => MapFleshHive.autoRepairFleshBuildings = value;
    }

    public bool HasPrimaryNest => map.listerThings.ThingsOfDef(FleshHiveDefOf.FH_FleshPrimaryNest)
        .Any(thing => thing.Spawned && thing.Faction == Faction.OfPlayer);

    public float ActivityGrowthFactor => GetUpgradeEffectFactor(FleshHiveUpgradeEffect.NestTaming);

    public float GetUpgradeEffectValue(FleshHiveUpgradeEffect effect)
    {
        return MapFleshHive.completedUpgrades
            .Where(upgrade => upgrade != null && upgrade.effect == effect)
            .Sum(upgrade => upgrade.effectValue);
    }

    public void SuppressActivity(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        Activity -= amount;
        if (Activity < ActivityLimit)
        {
            MapFleshHive.fullActivityTicks = 0;
        }
    }

    public void DebugAddActivity(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        float oldActivity = Activity;
        Activity += amount;
        if (oldActivity < ActivityLimit && Activity >= ActivityLimit)
        {
            MapFleshHive.fullActivityTicks = 0;
        }
    }

    public void DebugSetActivityFull()
    {
        if (Activity < ActivityLimit)
        {
            MapFleshHive.fullActivityTicks = 0;
        }
        Activity = ActivityLimit;
    }

    public void DebugStartRiot()
    {
        StartRiot();
    }

    public void RegisterFleshHopper(Building_FleshHopper hopper)
    {
        if (hopper == null)
        {
            return;
        }

        MapFleshHive.CachedFleshHoppers.Add(hopper);
    }

    public void RegisterFleshBox(Building_FleshBox box)
    {
        if (box == null)
        {
            return;
        }

        MapFleshHive.CachedFleshBoxes.Add(box);
    }

    public void RegisterFleshBuilding(Building building)
    {
        if (building == null)
        {
            return;
        }

        CachedFleshBuildings.Add(building);
    }

    public void UnregisterFleshBuilding(Building building)
    {
        cachedFleshBuildings?.Remove(building);
    }

    public void RegisterHiveCapacityProvider(CompHiveGroupCapacityProvider provider)
    {
        if (provider == null)
        {
            return;
        }

        hiveCapacityProviders ??= new List<CompHiveGroupCapacityProvider>();
        if (!hiveCapacityProviders.Contains(provider))
        {
            hiveCapacityProviders.Add(provider);
            EnforceHiveGroupCapacity();
        }
    }

    public void RegisterHiveScaleProvider(CompHiveScaleProvider provider)
    {
        if (provider == null)
        {
            return;
        }

        hiveScaleProviders ??= new List<CompHiveScaleProvider>();
        if (!hiveScaleProviders.Contains(provider))
        {
            hiveScaleProviders.Add(provider);
            ClampNutritionToHiveScaleLimit();
            EnforceHiveGroupCapacity();
        }
    }

    public void UnregisterTwistedFlesh(Pawn pawn)
    {
        if (cachedNeedsTwistedFlesh == null)
        {
            return;
        }
        cachedNeedsTwistedFlesh.Remove(pawn);
    }

    public void UnregisterFleshBlueprint(Blueprint_FleshBuild blueprint)
    {
        if (mapFleshHive == null || blueprint == null)
        {
            return;
        }

        mapFleshHive.CachedFleshBlueprints.Remove(blueprint);
    }

    public void UnregisterFleshHopper(Building_FleshHopper hopper)
    {
        if (mapFleshHive == null || hopper == null)
        {
            return;
        }

        mapFleshHive.CachedFleshHoppers.Remove(hopper);
    }

    public void UnregisterFleshBox(Building_FleshBox box)
    {
        if (mapFleshHive == null || box == null)
        {
            return;
        }

        mapFleshHive.CachedFleshBoxes.Remove(box);
    }

    public void UnregisterHiveCapacityProvider(CompHiveGroupCapacityProvider provider)
    {
        if (hiveCapacityProviders == null || provider == null)
        {
            return;
        }

        if (hiveCapacityProviders.Remove(provider))
        {
            EnforceHiveGroupCapacity();
        }
    }

    public void UnregisterHiveScaleProvider(CompHiveScaleProvider provider)
    {
        if (hiveScaleProviders == null || provider == null)
        {
            return;
        }

        if (hiveScaleProviders.Remove(provider))
        {
            ClampNutritionToHiveScaleLimit();
            EnforceHiveGroupCapacity();
        }
    }

    public void Notify_FleshTerrainChanged(int delta)
    {
        MapFleshHive.fleshTerrainCount = Mathf.Max(0, MapFleshHive.fleshTerrainCount + delta);
        MapFleshHive.hiveScale = CalculateHiveScale(MapFleshHive.fleshTerrainCount);
        ClampNutritionToHiveScaleLimit();
        EnforceHiveGroupCapacity();
    }

    public void RecalculateHiveScale()
    {
        int fleshTerrainCount = 0;
        foreach (IntVec3 cell in map.AllCells)
        {
            if (FleshTerrainUtility.IsFleshTerrain(map, cell))
            {
                fleshTerrainCount++;
            }
        }
        MapFleshHive.fleshTerrainCount = fleshTerrainCount;
        MapFleshHive.hiveScale = CalculateHiveScale(fleshTerrainCount);
        ClampNutritionToHiveScaleLimit();
        EnforceHiveGroupCapacity();
    }

    public bool CanAcceptIntoHiveGroup(UnitGroup targetGroup, Pawn unit)
    {
        if (!IsHiveContainerGroup(targetGroup) || unit?.TryGetComp<UnitComp>() is not { } unitComp)
        {
            return true;
        }

        int currentCost = GetHiveGroupCost(unit);
        return currentCost + unitComp.Props.groupCost <= HiveGroupCostLimit;
    }

    public void EnforceHiveGroupCapacity()
    {
        if (group == null || GameComponent_UnitGroup.Instance == null)
        {
            return;
        }

        int excessCost = GetHiveGroupCost() - HiveGroupCostLimit;
        if (excessCost <= 0)
        {
            return;
        }

        foreach (Pawn unit in GetOverflowCandidates().ToList())
        {
            UnitGroup sourceGroup = unit.TryGetComp<UnitComp>()?.group;
            if (sourceGroup == null || sourceGroup == group)
            {
                continue;
            }

            if (!group.CanAccept(unit).Accepted)
            {
                continue;
            }

            group.AcceptUnit(unit);
            excessCost -= unit.TryGetComp<UnitComp>()?.Props.groupCost ?? 0;
            if (excessCost <= 0)
            {
                break;
            }
        }
    }

    public override void FinalizeInit()
    {
        base.FinalizeInit();
        RebuildFleshHiveCache();
        if (group is not UnitGroup_TemporaryFleshHive)
        {
            return;
        }

        foreach (Pawn unit in group.units)
        {
            if (unit != null)
            {
                unit.forceNoDeathNotification = true;
            }
        }
    }

    public override void MapRemoved()
    {
        base.MapRemoved();
        if (group != null)
        {
            UnitGroup removedGroup = group;
            group = null;
            foreach (Pawn unit in removedGroup.units.ToList())
            {
                removedGroup.RemoveUnit(unit);
            }

            removedGroup.lord = null;
            removedGroup.Destroy();
        }

        mapFleshHives.Remove(map);
    }

    public override void MapGenerated()
    {
        base.MapGenerated();
        RecalculateHiveScale();
        var group = GameComponent_UnitGroup.Instance.MakeNewGroup(typeof(UnitGroup_TemporaryFleshHive));
        group.name = "TemporaryFleshGroup".Translate();
        group.color =GenColor.RandomColorOpaque(); 
        group.tags = [FleshHiveTags.Flesh];
        group.unitLimit = 240;
        RegisterGroupTags(group);
        Faction faction =  Faction.OfPlayer;
        group.map = this.map;
        group.lord = LordMaker.MakeNewLord(faction, new LordJob_HiveGroup(group),this.map);  
        group.SetMode(HCFDefOf.HCF_GroupWorkMode_Attack);
        this.group = group;
        ThingDef? fleshHiveDef = DefDatabase<ThingDef>.GetNamedSilentFail("FH_FleshHive");
        CompPropertiesHiveGroup? groupProperties = fleshHiveDef?.GetCompProperties<CompPropertiesHiveGroup>();
        if (groupProperties != null)
        {
            this.group.gizmoBackgroundColor = groupProperties.gizmoBackgroundColor;
        }
        else
        {
            Log.Error("[FleshHive] FH_FleshHive is missing CompPropertiesHiveGroup; temporary group Gizmo color cannot be synchronized.");
        }
        this.group.Spawn(this.map);
        this.group.SetTarget(map.Center,map);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref group, "group");
        Scribe_Deep.Look(ref mapFleshHive, "mapFleshHive");
        Scribe_Collections.Look(ref hiveResourcers, "hiveResourcers", LookMode.Deep);
        Scribe_Collections.Look(ref unitMaintainTargets, "unitMaintainTargets", LookMode.Def, LookMode.Value);
        Scribe_Collections.Look(ref unitMaximumTargets, "unitMaximumTargets", LookMode.Def, LookMode.Value);
        Scribe_Values.Look(ref fleshBushCycleIndex, "fleshBushCycleIndex");
        Scribe_Values.Look(ref planCheckIntervalTicks, "planCheckIntervalTicks");
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            if (mapFleshHive != null)
            {
                mapFleshHives[map] = mapFleshHive;
            } 
            RecalculateHiveScale();
            hiveResourcers ??= new List<HiveResourcer>();
            hiveResourcers.RemoveAll(resourcer => resourcer == null);
            NormalizeUnitTargets();
        }
        Scribe_Collections.Look(ref cachedFleshBeasts, "cachedFleshBeasts", LookMode.Reference);
        if (Scribe.mode == LoadSaveMode.PostLoadInit && cachedFleshBeasts == null)
        {
            cachedFleshBeasts = new HashSet<Pawn>();
        }
        Scribe_Collections.Look(ref cachedNeedsTwistedFlesh, "cachedNeedsTwistedFlesh", LookMode.Reference);
        if (Scribe.mode == LoadSaveMode.PostLoadInit && cachedNeedsTwistedFlesh == null)
        {
            cachedNeedsTwistedFlesh = new HashSet<Pawn>();
        }
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            hiveCapacityProviders = new List<CompHiveGroupCapacityProvider>();
        }
    }

    public override void MapComponentTick()
    {
        base.MapComponentTick();
        int ticksGame = Find.TickManager.TicksGame;
        TickUpgrade(ticksGame);
        UpdateHiveResourcers();
        TickFleshBushSpawner();
        if (ticksGame % ActivityTickInterval == 0)
        {
            TickActivity(ActivityTickInterval);
        }
        if (ticksGame % SelfRepairTickInterval == 0)
        {
            TickSelfRepairFleshBuildings();
        }
        if (ticksGame % ResourceTransportInterval == 0)
        {
            DispatchHiveResourcers();
        }
        if (ticksGame % UnitQuotaTickInterval == 0)
        {
            TickUnitQuotas();
        }
    }

    public override void MapComponentDraw()
    {
        base.MapComponentDraw();
        if (hiveResourcers == null)
        {
            return;
        }

        foreach (HiveResourcer hiveResourcer in hiveResourcers)
        {
            hiveResourcer?.Draw();
        }
    }

    private void ClampNutritionToHiveScaleLimit()
    {
        MapFleshHive.nutrition = Mathf.Min(MapFleshHive.nutrition, NutritionLimit);
    } 

    private static int CalculateHiveScale(int fleshTerrainCount)
    {
        return Mathf.Max(0, fleshTerrainCount / FleshTerrainPerHiveScale);
    }

    private void TickUpgrade(int ticksGame)
    {
        if (MapFleshHive.activeUpgrade == null || ticksGame % UpgradeTickInterval != 0)
        {
            return;
        }

        MapFleshHive.activeUpgradeProgress += UpgradeTickInterval;
        if (MapFleshHive.activeUpgradeProgress < MapFleshHive.activeUpgradeTotalTime)
        {
            return;
        }

        FleshHiveUpgradeDef completedUpgrade = MapFleshHive.activeUpgrade;
        MapFleshHive.completedUpgrades.Add(completedUpgrade);
        MapFleshHive.activeUpgrade = null;
        MapFleshHive.activeUpgradeProgress = 0f;
        MapFleshHive.activeUpgradeTotalTime = 0f;
        GrantAllFleshBeastUpgradeHediffs();
        Messages.Message("FH_Upgrade_CompletedMessage".Translate(completedUpgrade.label),
            MessageTypeDefOf.PositiveEvent, false);
    }

    private void GrantAllFleshBeastUpgradeHediffs()
    {
        HashSet<Pawn> pawns = map.mapPawns.AllPawnsSpawned
            .Where(pawn => pawn != null && pawn.TryGetComp<UnitComp>() != null)
            .ToHashSet();
        if (cachedFleshBeasts != null)
        {
            pawns.UnionWith(cachedFleshBeasts.Where(pawn => pawn != null));
        }

        if (GameComponent_UnitGroup.Instance?.groups != null)
        {
            pawns.UnionWith(GameComponent_UnitGroup.Instance.groups
                .Where(group => group?.Map == map && group.units != null)
                .SelectMany(group => group.units)
                .Where(pawn => pawn != null));
        }

        foreach (Pawn pawn in pawns)
        {
            GrantFleshBeastUpgradeHediffs(pawn);
        }
    }

    private void SetFleshBeastUpgradeHediff(Pawn pawn, HediffDef hediffDef, bool enabled, float severity = 1f)
    {
        if (hediffDef == null)
        {
            Log.ErrorOnce("[FleshHive] A fleshbeast upgrade HediffDef is missing.", 728451936);
            return;
        }

        List<Hediff> matchingHediffs = pawn.health.hediffSet.hediffs
            .Where(hediff => hediff.def == hediffDef)
            .ToList();
        if (!enabled)
        {
            return;
        }

        Hediff activeHediff = matchingHediffs.FirstOrDefault();
        if (activeHediff == null)
        {
            activeHediff = HediffMaker.MakeHediff(hediffDef, pawn);
            activeHediff.Severity = severity;
            pawn.health.AddHediff(activeHediff);
        }
        else if (!Mathf.Approximately(activeHediff.Severity, severity))
        {
            activeHediff.Severity = severity;
            pawn.health.Notify_HediffChanged(activeHediff);
        }

    }

    private bool HasModBoneSpikeSkill(Pawn pawn)
    {
        return pawn.abilities?.GetAbility(FleshHiveDefOf.FH_SpikeLaunch_Fingerspike) != null
            || pawn.abilities?.GetAbility(FleshHiveDefOf.FH_SpikeLaunch_Toughspike) != null
            || pawn.abilities?.GetAbility(FleshHiveDefOf.FH_SpikeLaunch_Whipspike) != null
            || pawn.abilities?.GetAbility(FleshHiveDefOf.FH_SpikeLaunch_Paraspike) != null
            || pawn.abilities?.GetAbility(FleshHiveDefOf.FH_SpikeLaunch_Shatterspike) != null;
    }

    private void RebuildFleshHiveCache()
    {
        cachedFleshHives = map.listerThings.AllThings
            .OfType<Building_RenameableFleshHive>()
            .Where(hive => hive.Spawned && !hive.Destroyed && hive.Map == map)
            .ToHashSet();
    }

    private IEnumerable<UnitDef> GetAvailableUnits(CompHiveSpawner_FleshTrait spawner)
    {
        foreach (SpawnCategoryDef category in spawner.GetUnitCategories())
        {
            if (category == null || !spawner.CanShowUnitCategory(category))
            {
                continue;
            }

            foreach (UnitDef unit in category.GetUnits(spawner))
            {
                if (unit?.kind != null)
                {
                    yield return unit;
                }
            }
        }
    }

    private void NormalizeUnitTargets()
    {
        unitMaintainTargets ??= new Dictionary<UnitDef, int>();
        unitMaximumTargets ??= new Dictionary<UnitDef, int>();

        unitMaintainTargets = unitMaintainTargets
            .Where(pair => pair.Key != null && pair.Value > 0)
            .ToDictionary(pair => pair.Key, pair => pair.Value);
        unitMaximumTargets = unitMaximumTargets
            .Where(pair => pair.Key != null && pair.Value >= 0)
            .ToDictionary(pair => pair.Key, pair => pair.Value);

        foreach (UnitDef unit in unitMaintainTargets.Keys.ToList())
        {
            SetUnitMaintainTarget(unit, unitMaintainTargets[unit]);
        }
    }

    private void TickUnitQuotas()
    {
        EnforceUnitMaximumTargets();
        if (IsHiveHungry)
        {
            return;
        }

        QueueMissingMaintainedUnits();
    }

    private void EnforceUnitMaximumTargets()
    {
        foreach (KeyValuePair<UnitDef, int> pair in unitMaximumTargets.ToList())
        {
            UnitDef unit = pair.Key;
            int maximum = pair.Value;
            int currentCount = GetCurrentUnitCount(unit);
            int queuedCount = GetQueuedUnitCount(unit);
            int excessProjectedCount = currentCount + queuedCount - maximum;
            if (excessProjectedCount > 0)
            {
                CancelQueuedUnitTasks(unit, excessProjectedCount);
            }

            currentCount = GetCurrentUnitCount(unit);
            if (currentCount > maximum)
            {
                ReclaimUnits(unit, currentCount - maximum);
            }
        }
    }

    private void CancelQueuedUnitTasks(UnitDef unit, int count)
    {
        if (count <= 0)
        {
            return;
        }

        foreach (CompHiveSpawner_FleshTrait spawner in GetFleshbeastSpawners().InRandomOrder())
        {
            CompProgressHolder holder = spawner.ProgressHolder;
            foreach (Progress progress in holder.progresses
                         .Where(progress => GetProgressUnitDef(progress) == unit)
                         .Reverse()
                         .ToList())
            {
                progress.Cancel(holder);
                holder.progresses.Remove(progress);
                count--;
                if (count <= 0)
                {
                    return;
                }
            }
        }
    }

    private void ReclaimUnits(UnitDef unit, int count)
    {
        if (count <= 0 || unit?.kind == null)
        {
            return;
        }

        List<Pawn> units = map.listerThings.AllThings
            .OfType<ThingWithComps>()
            .Where(thing => thing.Faction == Faction.OfPlayer)
            .Select(thing => thing.TryGetComp<CompHiveContainer>())
            .OfType<CompHiveContainer>()
            .Where(container => container.parent.Map == map)
            .SelectMany(container => container.units.InnerListForReading)
            .Where(pawn => pawn != null && !pawn.Destroyed && !pawn.Dead
                && pawn.Faction == Faction.OfPlayer
                && pawn.kindDef == unit.kind)
            .Distinct()
            .Take(count)
            .ToList();
        float nutritionRefund = GetUnitNutritionCost(unit);
        foreach (Pawn pawn in units)
        {
            if (pawn.ParentHolder is CompHiveContainer container)
            {
                container.units.Remove(pawn);
            }
            CachedFleshBeasts.Remove(pawn);
            pawn.Destroy(DestroyMode.Vanish);
            AddNutrition(nutritionRefund);
        }
    }

    private void QueueMissingMaintainedUnits()
    {
        int tasksAdded = 0;
        List<CompHiveSpawner_FleshTrait> spawners = GetFleshbeastSpawners();
        foreach (KeyValuePair<UnitDef, int> pair in unitMaintainTargets.InRandomOrder())
        {
            UnitDef unit = pair.Key;
            int missingCount = pair.Value - GetCurrentUnitCount(unit) - GetQueuedUnitCount(unit);
            while (missingCount > 0 && tasksAdded < MaxAutomaticUnitTasksPerInterval)
            {
                IEnumerable<CompHiveSpawner_FleshTrait> candidates = spawners
                    .Where(spawner => GetAvailableUnits(spawner).Contains(unit) && unit.CanProduce(spawner).Accepted);
                if (!candidates.TryRandomElement(out CompHiveSpawner_FleshTrait spawner))
                {
                    break;
                }

                AcceptanceReport report = spawner.TryStartUnitProduction(unit, sendMessage: false);
                if (!report.Accepted)
                {
                    break;
                }

                missingCount--;
                tasksAdded++;
            }

            if (tasksAdded >= MaxAutomaticUnitTasksPerInterval)
            {
                break;
            }
        }
    }

    private float GetUnitNutritionCost(UnitDef unit)
    {
        return unit.costs.NullOrEmpty()
            ? 0f
            : unit.costs.Where(cost => cost.resource == FleshHiveDefOf.FH_Resource_Nutrition).Sum(cost => cost.amount);
    }

    private UnitDef? GetProgressUnitDef(Progress? progress)
    {
        if (progress is UnitSpawnData_FleshTrait unitProgress)
        {
            return unitProgress.Def;
        }
        return progress is FormulaProgress formulaProgress ? formulaProgress.formula?.unit : null;
    }

    private void TickActivity(int intervalTicks)
    {
        float oldActivity = Activity;
        float change = GetActivityGrowthPerDay() * intervalTicks / GenDate.TicksPerDay;
        Activity += change;

        if (Activity >= ActivityLimit)
        {
            if (oldActivity < ActivityLimit)
            {
                MapFleshHive.fullActivityTicks = 0;
            }
            MapFleshHive.fullActivityTicks += intervalTicks;
            TryStartRiot(intervalTicks);
        }
        else
        {
            MapFleshHive.fullActivityTicks = 0;
        }
    }

    private float GetActivityGrowthPerDay()
    {
        int fleshBeastCost = GetMapFleshBeastCost();
        // float pressure = HiveScale * ActivityPerHiveScalePerDay + fleshBeastCost * ActivityPerGroupCostPerDay;
        // if (MapFleshHive.nutrition <= 0f)
        // {
        //     pressure *= HungryActivityGrowthMultiplier;
        // }
        float pressure = HiveScale * ActivityPerHiveScalePerDay + fleshBeastCost * ActivityPerGroupCostPerDay;
        pressure *= ActivityGrowthFactor;
        return pressure;
    }

    public bool HasUpgradeEffect(FleshHiveUpgradeEffect effect)
    {
        return MapFleshHive.completedUpgrades.Any(upgrade => upgrade != null && upgrade.effect == effect);
    }

    public float GetUpgradeEffectFactor(FleshHiveUpgradeEffect effect)
    {
        return Mathf.Max(0f, 1f + GetUpgradeEffectValue(effect));
    }

    private static float GetUpgradeEffectFactor(Map map, FleshHiveUpgradeEffect effect)
    {
        return map?.GetComponent<MapComponent_FleshHive>()?.GetUpgradeEffectFactor(effect) ?? 1f;
    }

    private void TickSelfRepairFleshBuildings()
    {
        if (!HasAutoRepairUpgrade || !AutoRepairFleshBuildings || MapFleshHive.nutrition <= 0f)
        {
            return;
        }

        int remainingRepair = SelfRepairHitPointsPerTick;
        float nutritionPerHitPoint = SelfRepairNutritionPerHitPoint;
        if (MapFleshHive.completedUpgrades.Contains(FleshHiveDefOf.FH_Upgrade_SelfRepair2))
        {
            remainingRepair *= 2;
        }

        foreach (Building thing in CachedFleshBuildings)
        {
            if (remainingRepair <= 0 || thing.Destroyed || thing.HitPoints >= thing.MaxHitPoints)
            {
                continue;
            }

            int repairAmount = Mathf.Min(remainingRepair, thing.MaxHitPoints - thing.HitPoints);
            repairAmount = Mathf.Min(repairAmount, Mathf.FloorToInt(MapFleshHive.nutrition / nutritionPerHitPoint));
            if (repairAmount <= 0)
            {
                continue;
            }

            float nutritionCost = repairAmount * nutritionPerHitPoint;
            MapFleshHive.nutrition -= nutritionCost;
            thing.HitPoints += repairAmount;
            remainingRepair -= repairAmount;
        }
    }

    private static SpecialResourceConsume_FleshBoxRequirement MakeUpgradeMaterialCost(FleshHiveUpgradeDef upgrade)
    {
        SpecialResourceConsume_FleshBoxRequirement materialCost = new();
        if (upgrade?.nerveFleshCost > 0)
        {
            materialCost.requirements.Add(new ThingDefCountClass(FleshHiveDefOf.FH_NerveFlesh,
                upgrade.nerveFleshCost));
        }
        return materialCost;
    }

    private static bool IsFleshBuilding(Thing thing)
    {
        return thing?.def?.tradeTags?.Contains(FleshBuildingTradeTag) == true;
    }

    private int GetMapFleshBeastCost()
    {
        CleanupCachedFleshBeasts();
        return CachedFleshBeasts.Sum(pawn => pawn.TryGetComp<UnitComp>()?.Props.groupCost ?? 1);
    }

    private void CleanupCachedFleshBeasts()
    {
        if (cachedFleshBeasts == null)
        {
            return;
        }

        cachedFleshBeasts.RemoveWhere(pawn => pawn == null || pawn.Destroyed || pawn.Dead || pawn.Map != map);
    }

    private void TryStartRiot(int intervalTicks)
    {
        if (MapFleshHive.fullActivityTicks < MinFullActivityTicksBeforeRiot)
        {
            return;
        }

        float fullDays = MapFleshHive.fullActivityTicks / (float)GenDate.TicksPerDay;
        float mtbDays = Mathf.Lerp(MaxRiotMtbDays, MinRiotMtbDays, Mathf.Clamp01(fullDays / RiotDangerRampDays));
        if (!Rand.MTBEventOccurs(mtbDays, GenDate.TicksPerDay, intervalTicks))
        {
            return;
        }

        StartRiot();
    }

    private void StartRiot()
    {
        List<Pawn> rioters = ReleaseHiveContainerFleshBeasts().ToList();
        if (rioters.Count == 0)
        {
            MapFleshHive.fullActivityTicks = 0;
            return;
        }

        Lord lord = LordMaker.MakeNewLord(Faction.OfEntities, new LordJob_FleshbeastAssault(), map);
        foreach (Pawn pawn in rioters)
        {
            if (pawn.DestroyedOrNull() || pawn.Dead)
            {
                continue;
            }

            pawn.SetFaction(Faction.OfEntities);
            pawn.health?.AddHediff(FleshHiveDefOf.FH_Berserk);
            lord.AddPawn(pawn);
        }

        Activity = ActivityLimit * RiotActivityAftermathPercent;
        MapFleshHive.fullActivityTicks = 0;
        Find.LetterStack.ReceiveLetter(
            "FH_FleshHiveRiot_Label".Translate(),
            "FH_FleshHiveRiot_Text".Translate(rioters.Count),
            LetterDefOf.ThreatBig,
            rioters
        );
    }

    private IEnumerable<Pawn> ReleaseHiveContainerFleshBeasts()
    {
        foreach (UnitGroup unitGroup in GetHiveContainerGroups().ToList())
        {
            foreach (Pawn unit in unitGroup.units.ToList())
            {
                if (unit == null || unit.Dead || !unit.Spawned)
                {
                    continue;
                }

                unitGroup.RemoveUnit(unit);
                yield return unit;
            }
        }
    }

    private bool AddMissingGroupTag(UnitGroup group, string tag)
    {
        if (group.tags.Contains(tag))
        {
            return false;
        }

        group.tags.Add(tag);
        return true;
    }

    private void RegisterGroupTags(UnitGroup group)
    {
        if (GameComponent_UnitGroup.Instance == null || group?.tags == null)
        {
            return;
        }

        foreach (string unitGroupTag in group.tags)
        {
            GameComponent_UnitGroup.Instance.groupsByTag.AddOrSetObjectToListFromDictionary(unitGroupTag, group);
        }
    }

    private int GetHiveGroupCost(Pawn excludingUnit = null)
    {
        int cost = 0;
        foreach (UnitGroup unitGroup in GetHiveContainerGroups())
        {
            foreach (Pawn unit in unitGroup.units)
            {
                if (unit == null || unit == excludingUnit)
                {
                    continue;
                }

                cost += unit.TryGetComp<UnitComp>()?.Props.groupCost ?? 0;
            }
        }

        return cost;
    }

    private IEnumerable<Pawn> GetOverflowCandidates()
    {
        return GetHiveContainerGroups()
            .SelectMany(unitGroup => unitGroup.units)
            .Where(unit => unit?.TryGetComp<UnitComp>() != null)
            .OrderByDescending(unit => unit.TryGetComp<UnitComp>().Props.groupCost);
    }

    private IEnumerable<UnitGroup> GetHiveContainerGroups()
    {
        if (GameComponent_UnitGroup.Instance == null)
        {
            yield break;
        }

        foreach (UnitGroup unitGroup in GameComponent_UnitGroup.Instance.groups)
        {
            if (IsHiveContainerGroup(unitGroup))
            {
                yield return unitGroup;
            }
        }
    }

    private bool IsHiveContainerGroup(UnitGroup unitGroup)
    {
        return unitGroup != null && unitGroup.Map == map && unitGroup.tags?.Contains(FleshHiveTags.FleshContainer) == true;
    }

    private void DispatchHiveResourcers()
    {
        foreach (Blueprint_FleshBuild blueprint in GetCachedFleshBlueprints())
        {
            if (!blueprint.Spawned || !blueprint.HasPendingMaterials || HasIncomingResourcer(blueprint))
            {
                continue;
            }

            TryCreateResourcer(blueprint);
        }
    }

    private void TickFleshBushSpawner()
    {
        int mapArea = map.Area;
        if (!FleshTerrainUtility.HasLargeFleshEcosystem(map) || mapArea <= 0)
        {
            return;
        }

        int cellsPerTick = Mathf.CeilToInt(mapArea * WildPlantSpawnerMapFractionCheckPerTick);
        int tickInterval = Mathf.CeilToInt(WildPlantSpawnerTickInterval);
        for (int i = 0; i < cellsPerTick; i++)
        {
            if (fleshBushCycleIndex >= mapArea)
            {
                fleshBushCycleIndex = 0;
            }

            IntVec3 cell = map.cellsInRandomOrder.Get(fleshBushCycleIndex);
            TrySpawnWildFleshBushAt(cell, tickInterval);
            fleshBushCycleIndex++;
        }
    }

    private void TrySpawnWildFleshBushAt(IntVec3 cell, int tickInterval)
    {
        if (!CanSpawnWildFleshBushAt(cell))
        {
            return;
        }

        float chanceFromDensity = Mathf.Clamp01((float)HiveScale / map.Area);
        float regrowDays = map.BiomeAt(cell).wildPlantRegrowDays;
        if (!Rand.Chance(chanceFromDensity) || !Rand.MTBEventOccurs(regrowDays, GenDate.TicksPerDay, tickInterval))
        {
            return;
        }

        Plant plant = WildPlantSpawner.SpawnPlant(FleshHiveDefOf.FH_FleshBush, map, cell, true);
        plant.Growth = Mathf.Clamp01(WildPlantSpawner.InitialGrowthRandomRange.RandomInRange);
    }

    private bool CanSpawnWildFleshBushAt(IntVec3 cell)
    {
        if (!FleshTerrainUtility.IsFleshTerrain(map, cell))
        {
            return false;
        }

        if (cell.GetPlant(map) != null || cell.GetCover(map) != null || cell.GetEdifice(map) != null)
        {
            return false;
        }

        if (!PlantUtility.SnowAllowsPlanting(cell, map) || !PlantUtility.SandAllowsPlanting(cell, map))
        {
            return false;
        }

        if (!FleshHiveDefOf.FH_FleshBush.CanEverPlantAt(cell, map))
        {
            return false;
        }

        return !FleshBushSaturatedNear(cell);
    }

    private bool FleshBushSaturatedNear(IntVec3 cell)
    {
        int fleshCells = 0;
        int fleshBushes = 0;
        int cellsToScan = GenRadial.NumCellsInRadius(FleshBushSaturationScanRadius);
        for (int i = 0; i < cellsToScan; i++)
        {
            IntVec3 scanCell = cell + GenRadial.RadialPattern[i];
            if (!FleshTerrainUtility.IsFleshTerrain(map, scanCell))
            {
                continue;
            }

            fleshCells++;
            if (scanCell.GetPlant(map)?.def == FleshHiveDefOf.FH_FleshBush)
            {
                fleshBushes++;
            }
        }

        int desiredBushes = Mathf.CeilToInt(fleshCells * FleshBushDesiredDensity);
        return desiredBushes > 0 && fleshBushes >= desiredBushes;
    }

    private CompHiveResource FindClosestHiveWithResource(IntVec3 targetCell, HiveResourceDef resourceDef)
    {
        CompHiveResource closest = null;
        float closestDistance = float.MaxValue;
        foreach (CompHiveResource comp in map.listerThings.AllThings
                     .OfType<ThingWithComps>()
                     .Select(thing => thing.TryGetComp<CompHiveResource>())
                     .Where(comp => comp != null && comp.parent.Faction == Faction.OfPlayer))
        {
            HiveResource hiveResource = comp.resources.FirstOrDefault(resource => resource.def == resourceDef && resource.Amount > 0f);
            if (hiveResource == null)
            {
                continue;
            }

            float distance = comp.parent.Position.DistanceToSquared(targetCell);
            if (distance >= closestDistance)
            {
                continue;
            }

            closest = comp;
            closestDistance = distance;
        }

        return closest;
    }

    private bool HasIncomingResourcer(Blueprint_FleshBuild blueprint)
    {
        return hiveResourcers != null && hiveResourcers.Any(resourcer => resourcer?.targetBlueprint == blueprint);
    }

    private void TryCreateResourcer(Blueprint_FleshBuild blueprint)
    {
        ResourceCount needed = blueprint.GetNextNeededResource();
        if (needed != null && needed.amount > 0f)
        {
            CompHiveResource sourceComp = FindClosestHiveWithResource(blueprint.Position, needed.resource);
            HiveResource hiveResource = sourceComp?.resources.FirstOrDefault(resource => resource.def == needed.resource && resource.Amount > 0f);
            if (hiveResource == null)
            {
                return;
            }

            float carriedAmount = Mathf.Min(ResourceCarryCapacity, Mathf.Min(needed.amount, hiveResource.Amount));
            if (carriedAmount <= 0f)
            {
                return;
            }

            hiveResource.DecreaseResource(carriedAmount);
            HiveResourcers.Add(new HiveResourcer(sourceComp.parent, blueprint, needed.resource, carriedAmount));
            return;
        }

        ThingDefCountClass neededThing = blueprint.GetNextNeededThing();
        if (neededThing == null || neededThing.count <= 0)
        {
            return;
        }

        Thing sourceThing = FleshBoxUtility.FindThingOnClosestBox(map, blueprint.Position, neededThing.thingDef, out Building_FleshBox sourceBox);
        if (sourceThing == null || sourceBox == null)
        {
            return;
        }

        int carriedCount = Mathf.Min((int)ResourceCarryCapacity, Mathf.Min(neededThing.count, sourceThing.stackCount));
        if (carriedCount <= 0)
        {
            return;
        }

        Thing carriedThing = sourceThing.SplitOff(carriedCount);
        HiveResourcers.Add(new HiveResourcer(sourceBox, blueprint, carriedThing.def, carriedThing.stackCount));
        carriedThing.Destroy();
    }

    private void UpdateHiveResourcers()
    {
        if (hiveResourcers == null || hiveResourcers.Count == 0)
        {
            return;
        }

        for (int i = hiveResourcers.Count - 1; i >= 0; i--)
        {
            HiveResourcer hiveResourcer = hiveResourcers[i];
            if (hiveResourcer == null || hiveResourcer.Tick())
            {
                hiveResourcers.RemoveAt(i);
            }
        }
    }

    private IEnumerable<Blueprint_FleshBuild> GetCachedFleshBlueprints()
    {
        if (mapFleshHive == null)
        {
            yield break;
        }

        CleanupInvalidBlueprints();
        HashSet<Blueprint_FleshBuild> blueprints = mapFleshHive.CachedFleshBlueprints;
        if (blueprints.Count == 0)
        {
            yield break;
        }

        foreach (Blueprint_FleshBuild blueprint in blueprints)
        {
            if (blueprint != null)
            {
                yield return blueprint;
            }
        }
    }

    public void CleanupInvalidBlueprints()
    {
        if (mapFleshHive == null)
        {
            return;
        }

        mapFleshHive.CachedFleshBlueprints.RemoveWhere(blueprint => blueprint == null || blueprint.Destroyed || blueprint.Map != map);
    }

    public void CleanupInvalidHoppers()
    {
        if (mapFleshHive == null)
        {
            return;
        }

        mapFleshHive.CachedFleshHoppers.RemoveWhere(hopper => hopper == null || hopper.Destroyed || hopper.Map != map);
    }

    public void CleanupInvalidFleshBoxes()
    {
        if (mapFleshHive == null)
        {
            return;
        }

        mapFleshHive.CachedFleshBoxes.RemoveWhere(box => box == null || box.Destroyed || box.Map != map);
    }

    public void PreparePlanCheckInterval(CompHivePlan_FleshHive plan)
    {
        if (plan == null)
        {
            return;
        }

        if (planCheckIntervalTicks <= 0)
        {
            planCheckIntervalTicks = plan.CurrentCheckIntervalTicks;
        }

        if (plan.CurrentCheckIntervalTicks != PlanCheckIntervalTicks)
        {
            plan.ApplySharedCheckInterval(PlanCheckIntervalTicks);
        }
    }

    public void AdoptPlanCheckInterval(CompHivePlan_FleshHive sourcePlan)
    {
        if (sourcePlan == null)
        {
            return;
        }

        int requestedInterval = sourcePlan.CurrentCheckIntervalTicks;
        if (requestedInterval <= 0 || requestedInterval == PlanCheckIntervalTicks)
        {
            return;
        }

        planCheckIntervalTicks = requestedInterval;
        foreach (CompHivePlan_FleshHive plan in map.listerThings.AllThings
                     .OfType<ThingWithComps>()
                     .Select(thing => thing.TryGetComp<CompHivePlan_FleshHive>())
                     .Where(plan => plan != null))
        {
            plan.ApplySharedCheckInterval(planCheckIntervalTicks);
            plan.ResetSharedCheckSchedule();
        }
    }

    public void CancelFleshHopperItemProgressesIfNeeded()
    {
        CleanupInvalidHoppers();
        if (FleshHopperUtility.HasAvailableHopper(map))
        {
            return;
        }

        foreach (CompProgressHolder progressHolder in map.listerThings.AllThings
                     .OfType<ThingWithComps>()
                     .Select(thing => thing.TryGetComp<CompProgressHolder>())
                     .Where(comp => comp != null && comp.parent.Faction == Faction.OfPlayer && comp.progresses != null))
        {
            List<ItemSpawnData> progresses = progressHolder.progresses
                .OfType<ItemSpawnData>()
                .Where(progress => progress?.item?.worker is ItemSpawnWorker_FleshHopper)
                .ToList();
            foreach (ItemSpawnData progress in progresses)
            {
                progress.Cancel(progressHolder);
                progressHolder.progresses.Remove(progress);
            }
        }
    }

    public UnitGroup group;

    private HashSet<Pawn> cachedFleshBeasts;
    private HashSet<Pawn> cachedNeedsTwistedFlesh;
    private HashSet<Building_RenameableFleshHive> cachedFleshHives;
    private HashSet<Building> cachedFleshBuildings;
    private List<HiveResourcer> hiveResourcers;
    private List<CompHiveGroupCapacityProvider> hiveCapacityProviders;
    private List<CompHiveScaleProvider> hiveScaleProviders;
    private MapFleshHive mapFleshHive;
    private Dictionary<UnitDef, int> unitMaintainTargets = new Dictionary<UnitDef, int>();
    private Dictionary<UnitDef, int> unitMaximumTargets = new Dictionary<UnitDef, int>();
    private int fleshBushCycleIndex;
    private int planCheckIntervalTicks;
    private const int ResourceTransportInterval = 250;
    private const int ActivityTickInterval = 2500;
    private const int UnitQuotaTickInterval = 2500;
    private const int MaxAutomaticUnitTasksPerInterval = 50;
    public const int FleshTerrainPerHiveScale = 50;
    public const float NutritionLimitPerHiveScale = 50f;
    private const int HiveScalePerGroupCost = 5;
    private const float ResourceCarryCapacity = 75f;
    private const float ActivityPerHiveScalePerDay = 0.05f;
    private const float ActivityPerGroupCostPerDay = 0.5f;
    // private const float ActivityPerHiveScalePerDay = 0.045f;
    // private const float ActivityPerGroupCostPerDay = 0.2f;
    // private const float HungryActivityGrowthMultiplier = 2f;
    private const float RiotActivityAftermathPercent = 0.35f;
    private const float RiotDangerRampDays = 3f;
    private const float MaxRiotMtbDays = 2f;
    private const float MinRiotMtbDays = 0.25f;
    private const int MinFullActivityTicksBeforeRiot = GenDate.TicksPerHour * 6;
    private const float WildPlantSpawnerMapFractionCheckPerTick = 0.0001f;
    private const float WildPlantSpawnerTickInterval = 10000f;
    private const float FleshBushDesiredDensity = 0.015f;
    private const float FleshBushSaturationScanRadius = 20f;
    private const int UpgradeTickInterval = 250;
    private const int SelfRepairTickInterval = GenDate.TicksPerHour;
    private const int SelfRepairHitPointsPerTick = 200;
    private const int DefaultPlanCheckIntervalTicks = GenDate.TicksPerHour * 6;
    private const float SelfRepairNutritionPerHitPoint = 0.02f;
    private const string FleshBuildingTradeTag = "FH_FleshBuilding";
    private static readonly Dictionary<Map, MapFleshHive> mapFleshHives = new Dictionary<Map, MapFleshHive>();
}
