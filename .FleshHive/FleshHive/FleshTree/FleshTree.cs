using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive;

public class FleshTree : Plant
{
    private int BloomLevel => Mathf.Max(Mathf.FloorToInt(Growth / 0.25f - 0.001f), 0);

    private IEnumerable<IntVec3> RadialCells => GenRadial.RadialCellsAround(Position, ConsumeRadius, true);

    public float ConsumeRadius => 7.9f;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref rootTargets, "rootTargets", LookMode.Reference);
        Scribe_Values.Look(ref terraformNutrition, "terraformNutrition");
        Scribe_Values.Look(ref terraformProgress, "terraformProgress");
        Scribe_Values.Look(ref terraformRadius, "terraformRadius", 1f);
        Scribe_Values.Look(ref consumeTicks, "consumeTicks");
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            rootTargets?.RemoveAll(thing => thing == null || thing.Destroyed);
        }
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        if (terraformRadius <= 0f)
        {
            terraformRadius = 1f;
        }
        LongEventHandler.ExecuteWhenFinished(UpdateRoots);
        UpdateBloomingStage();
    }

    protected override void Tick()
    {
        base.Tick();
        if (this.IsHashIntervalTick(250))
        {
            TickConsumeAndTerraform();
        }
    }

    public override void TickRare()
    {
        base.TickRare();
        TickConsumeAndTerraform();
    }

    public override void TickLong()
    {
        base.TickLong();
        if (!Spawned || Map == null)
        {
            return;
        }

        UpdateRoots();
        UpdateBloomingStage();
    }

    public override string GetInspectString()
    {
        string inspect = base.GetInspectString();
        string extra = "FH_FleshTree_TerraformNutrition".Translate(terraformNutrition.ToString("F1"), Mathf.Clamp01(terraformRadius / ConsumeRadius).ToStringPercent());
        return inspect.NullOrEmpty() ? extra : inspect + "\n" + extra;
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (Gizmo gizmo in base.GetGizmos())
        {
            yield return gizmo;
        }

        if (!Spawned || Map == null)
        {
            yield break;
        }

        yield return new Command_Action
        {
            defaultLabel = "CreateCorpseStockpile".Translate(),
            defaultDesc = "CreateCorpseStockpileDesc".Translate(),
            icon = ContentFinder<Texture2D>.Get("UI/Icons/CorpseStockpileZone"),
            action = CreateCorpseStockpile
        };
    }

    public override void DrawExtraSelectionOverlays()
    {
        base.DrawExtraSelectionOverlays();
        GenDraw.DrawRadiusRing(Position, ConsumeRadius);
    }

    private void TickConsumeAndTerraform()
    {
        if (!Spawned || Map == null)
        {
            return;
        }

        UpdateRoots();
        UpdateBloomingStage();
        consumeTicks += 250;
        if (consumeTicks >= ConsumeInterval)
        {
            consumeTicks -= ConsumeInterval;
            TryConsumeNearbyNutrition();
        }

        TryTerraform();
    }

    private void CreateCorpseStockpile()
    {
        List<FleshTree> selectedTrees = Find.Selector.SelectedObjects.OfType<FleshTree>().ToList();
        if (selectedTrees.Count == 0)
        {
            selectedTrees.Add(this);
        }

        Zone_Stockpile existingStockpile = Map.zoneManager.ZoneAt(Position) as Zone_Stockpile;
        if (existingStockpile != null)
        {
            FloodFillSelectedTreeRadius(selectedTrees, existingStockpile);
            return;
        }

        Zone_Stockpile stockpile = new Zone_Stockpile(StorageSettingsPreset.CorpseStockpile, Map.zoneManager);
        stockpile.settings.filter.SetAllow(ThingCategoryDefOf.CorpsesMechanoid, false);
        stockpile.settings.filter.SetAllow(SpecialThingFilterDefOf.AllowCorpsesUnnatural, false);
        Map.zoneManager.RegisterZone(stockpile);

        Zone_Stockpile foundStockpile = null;
        Map.floodFiller.FloodFill(
            Position,
            cell =>
            {
                Zone_Stockpile zone = Map.zoneManager.ZoneAt(cell) as Zone_Stockpile;
                if (zone != null)
                {
                    foundStockpile = zone;
                }

                return selectedTrees.Any(tree => tree.RadialCells.Contains(cell))
                    && Map.zoneManager.ZoneAt(cell) == null
                    && Designator_ZoneAdd.IsZoneableCell(cell, Map);
            },
            cell => stockpile.AddCell(cell),
            int.MaxValue,
            false,
            null);

        if (foundStockpile == null)
        {
            return;
        }

        List<IntVec3> cells = stockpile.Cells.ToList();
        stockpile.Delete();
        foreach (IntVec3 cell in cells)
        {
            foundStockpile.AddCell(cell);
        }
    }

    private void FloodFillSelectedTreeRadius(List<FleshTree> selectedTrees, Zone_Stockpile existingStockpile)
    {
        Map.floodFiller.FloodFill(
            Position,
            cell =>
            {
                Zone zone = Map.zoneManager.ZoneAt(cell);
                return selectedTrees.Any(tree => tree.RadialCells.Contains(cell))
                    && (zone == null || zone == existingStockpile)
                    && Designator_ZoneAdd.IsZoneableCell(cell, Map);
            },
            cell =>
            {
                if (!existingStockpile.ContainsCell(cell))
                {
                    existingStockpile.AddCell(cell);
                }
            },
            int.MaxValue,
            false,
            null);
    }

    private void TryConsumeNearbyNutrition()
    {
        if (Map == null)
        {
            return;
        }

        EnsureRootCollections();
        List<ThingWithComps> candidates = rootTargets.Where(CanConsume).ToList();
        foreach (ThingWithComps target in candidates.InRandomOrder())
        {
            if (!TryConsumeTarget(target, out float consumedNutrition))
            {
                continue;
            }

            float absorptionFactor = target is Corpse
                ? MapComponent_FleshHive.GetNutritionAbsorptionFactor(Map)
                : 1f;
            float nutrition = consumedNutrition * absorptionFactor;
            float terraformShare = nutrition * 0.5f;
            MapComponent_FleshHive.AddNutrition(Map, nutrition - terraformShare);
            terraformNutrition += terraformShare;
            EffecterDefOf.HarbingerTreeConsume.Spawn(target.Position, Map, 1f);
        }
    }

    private void UpdateBloomingStage()
    {
        overrideGraphicIndex = BloomLevel;
        if (Map != null)
        {
            DirtyMapMesh(Map);
        }
    }

    private void UpdateRoots()
    {
        if (Map == null)
        {
            return;
        }

        EnsureRootCollections();
        tmpRadialCells.Clear();
        tmpRadialCells.AddRange(RadialCells);

        foreach (IntVec3 cell in tmpRadialCells)
        {
            if (!cell.InBounds(Map))
            {
                continue;
            }

            tmpThings.Clear();
            tmpThings.AddRange(cell.GetThingList(Map));
            foreach (Thing thing in tmpThings)
            {
                if (thing is ThingWithComps thingWithComps && CanConsume(thingWithComps))
                {
                    TryMakeRoot(thingWithComps);
                }
            }
        }

        foreach (ThingWithComps rootTarget in rootTargets)
        {
            if (rootTarget.Destroyed || !tmpRadialCells.Contains(rootTarget.PositionHeld) || !CanConsume(rootTarget))
            {
                deferredDestroy.Enqueue(rootTarget);
            }
        }

        while (deferredDestroy.Count > 0)
        {
            DestroyRoot(deferredDestroy.Dequeue());
        }
    }

    private void EnsureRootCollections()
    {
        roots ??= new Dictionary<ThingWithComps, Mote>();
        rootTargets ??= new List<ThingWithComps>();
    }

    private void TryTerraform()
    {
        if (Map == null)
        {
            return;
        }

        while (true)
        {
            IntVec3 terraformCell = IntVec3.Invalid;
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(Position, terraformRadius, true).InRandomOrder())
            {
                if (CanTerraform(cell))
                {
                    terraformCell = cell;
                    break;
                }
            }

            if (terraformCell.IsValid)
            {
                if (terraformNutrition < TerraformNutritionPerTile)
                {
                    return;
                }

                Map.terrainGrid.SetTerrain(terraformCell, TerrainDefOf.Flesh);
                terraformNutrition -= TerraformNutritionPerTile;
                continue;
            }

            if (terraformRadius >= ConsumeRadius)
            {
                MapComponent_FleshHive.AddNutrition(Map, terraformNutrition);
                terraformNutrition = 0f;
                terraformProgress = 0f;
                return;
            }

            terraformRadius = Mathf.Min(terraformRadius + 1f, ConsumeRadius);
        }
    }

    private bool CanTerraform(IntVec3 cell)
    {
        if (Map == null)
        {
            return false;
        }
        if (!cell.InBounds(Map) || cell.Fogged(Map))
        {
            return false;
        }
        if (FleshTerrainUtility.IsFleshTerrain(Map, cell))
        {
            return false;
        }
        if (cell.Impassable(Map))
        {
            return false;
        }
        return true;
    }

    private bool CanConsume(ThingWithComps thing)
    {
        if (thing == null || thing.Destroyed || thing == this)
        {
            return false;
        }

        if (thing is Corpse corpse)
        {
            return CanBeConsumed(corpse) && GetNutritionFromCorpse(corpse, false) > 0f;
        }

        if (!CanBeConsumed(thing))
        {
            return false;
        }

        CompHarbingerTreeConsumable comp = thing.TryGetComp<CompHarbingerTreeConsumable>();
        if (comp != null && comp.CanBeConsumed && comp.AvailableNutrition(false) > 0f)
        {
            return true;
        }

        return CanConsumeItem(thing) && GetNutritionFromItemStack(thing, false) > 0f;
    }

    private bool TryConsumeTarget(ThingWithComps thing, out float nutrition)
    {
        nutrition = 0f;
        if (thing == null || thing.Destroyed)
        {
            return false;
        }

        CompHarbingerTreeConsumable comp = thing.TryGetComp<CompHarbingerTreeConsumable>();
        if (comp != null && comp.CanBeConsumed)
        {
            nutrition = comp.AvailableNutrition(true);
            return nutrition > 0f;
        }

        if (thing is Corpse corpse)
        {
            nutrition = GetNutritionFromCorpse(corpse, true);
            return nutrition > 0f;
        }

        if (CanConsumeItem(thing))
        {
            nutrition = GetNutritionFromItemStack(thing, true);
            return nutrition > 0f;
        }

        return false;
    }

    private bool CanBeConsumed(ThingWithComps thing)
    {
        if (!thing.Spawned)
        {
            return false;
        }

        SlotGroup slotGroup = thing.GetSlotGroup();
        return slotGroup == null || slotGroup.parent.Isnt<Building>();
    }

    private bool CanConsumeItem(ThingWithComps thing)
    {
        return thing.def.category == ThingCategory.Item &&
               thing.GetStatValue(StatDefOf.Nutrition) > 0f &&
               (thing.def.IsMeat || thing.def.ingestible?.foodType.HasFlag(FoodTypeFlags.Meat) == true);
    }

    private float GetNutritionFromCorpse(Corpse corpse, bool applyDigestion)
    {
        if (corpse.InnerPawn == null || !corpse.InnerPawn.RaceProps.IsFlesh)
        {
            return 0f;
        }

        float nutrition = corpse.InnerPawn.BodySize * NutritionPerBodySize;
        if (corpse.GetRotStage() == RotStage.Dessicated)
        {
            nutrition *= DessicatedNutritionFactor;
        }

        if (applyDigestion)
        {
            corpse.Destroy(DestroyMode.Vanish);
        }

        return nutrition;
    }

    private float GetNutritionFromItemStack(ThingWithComps item, bool applyDigestion)
    {
        int count = Mathf.Min(item.stackCount, ItemConsumeCountRange.RandomInRange);
        if (!applyDigestion)
        {
            return item.GetStatValue(StatDefOf.Nutrition) * ((float)count / item.stackCount);
        }

        Thing splitItem = item.SplitOff(count);
        float nutrition = splitItem.GetStatValue(StatDefOf.Nutrition) * count;
        splitItem.Destroy(DestroyMode.Vanish);
        return nutrition;
    }

    private void TryMakeRoot(ThingWithComps thing)
    {
        if (thing == null || thing.Destroyed)
        {
            return;
        }

        if (roots.TryGetValue(thing, out Mote existingRoot))
        {
            if (existingRoot != null && !existingRoot.Destroyed && existingRoot.PositionHeld == thing.PositionHeld)
            {
                if (!rootTargets.Contains(thing))
                {
                    rootTargets.Add(thing);
                }

                return;
            }

            existingRoot?.Destroy(DestroyMode.Vanish);
            roots.Remove(thing);
        }

        if (!roots.ContainsKey(thing))
        {
            float exactRot = 0f;
            if (thing is Corpse corpse)
            {
                exactRot = corpse.InnerPawn.Drawer.renderer.BodyAngle(PawnRenderFlags.None);
            }
            roots[thing] = MoteMaker.MakeStaticMote(thing.Position.ToVector3Shifted(), Map, ThingDefOf.Mote_HarbingerTreeRoots, 1f, false, exactRot);
        }

        if (!rootTargets.Contains(thing))
        {
            rootTargets.Add(thing);
        }
    }

    private void DestroyRoot(ThingWithComps thing)
    {
        if (thing == null)
        {
            return;
        }

        if (roots.TryGetValue(thing, out Mote mote) && mote != null)
        {
            mote.Destroy(DestroyMode.Vanish);
        }
        roots.Remove(thing);
        rootTargets.Remove(thing);
    }

    private Dictionary<ThingWithComps, Mote> roots;

    private List<ThingWithComps> rootTargets;

    private float terraformNutrition;

    private float terraformProgress;

    private float terraformRadius = 1f;

    private int consumeTicks;
    private readonly Queue<ThingWithComps> deferredDestroy = new();

    private readonly List<Thing> tmpThings = new();

    private readonly List<IntVec3> tmpRadialCells = new();

    private const int ConsumeInterval = 500;

    private const float NutritionPerBodySize = 5f;

    private const float DessicatedNutritionFactor = 0.5f;

    private const float TerraformNutritionPerTile = 1f;

    private static readonly IntRange ItemConsumeCountRange = new(4, 12);
}
