using System.Collections.Generic;
using System.Linq;
using HiveCreatureFramework;
using HiveCreatureFramework.Evolution;
using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive;

public class CompProperties_FleshSpread : CompProperties
{
    public CompProperties_FleshSpread()
    {
        compClass = typeof(CompFleshSpread);
    }

    public float intervalDays = 3f;

    public float radius = 10.5f;

    public int cellsPerErosion = 3;

    public int initialErosionCount = 10;
}

public class CompFleshSpread : ThingComp, ITransfer
{
    private CompProperties_FleshSpread Props => (CompProperties_FleshSpread)props;

    private int TicksBetweenErosions => Mathf.Max(1, Mathf.RoundToInt(Props.intervalDays * 60000f
        / MapComponent_FleshHive.GetFleshExpansionSpeedFactor(parent.Map)));

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        if (respawningAfterLoad)
        {
            return;
        }
        if (initializedFromTransfer)
        {
            cacheDirty = true;
            cacheBuildIndex = 0;
            borderCache = null;
            initializedFromTransfer = false;
            return;
        }
        infectedCells = new HashSet<IntVec3>();
        foreach (IntVec3 cell in GenAdj.OccupiedRect(parent).Cells)
        {
            if (cell.InBounds(parent.Map) && cell.DistanceTo(parent.Position) <= Props.radius)
            {
                infectedCells.Add(cell);
            }
        }
        ticksUntilErosion = TicksBetweenErosions;
        cacheDirty = true;
        DoInitialErosions();
    }

    public bool CanTransfer(ThingComp targetComp, HiveEvolutionOptionDef option, out string reason)
    {
        reason = null;
        return targetComp is CompFleshSpread;
    }

    public override void CompTick()
    {
        base.CompTick();
        if (!parent.Spawned)
        {
            return;
        }
        BuildBorderCache();
        ticksUntilErosion--;
        if (ticksUntilErosion > 0)
        {
            return;
        }
        ticksUntilErosion = TicksBetweenErosions;
        if (borderCache == null || borderCache.Count == 0)
        {
            return;
        }
        DoErosion();
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Collections.Look(ref infectedCells, "infectedCells", LookMode.Value);
        Scribe_Values.Look(ref ticksUntilErosion, "ticksUntilErosion", TicksBetweenErosions);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            if (infectedCells == null)
            {
                infectedCells = new HashSet<IntVec3>();
            }
            cacheDirty = true;
        }
    }

    public void Transfer(ThingComp targetComp, HiveEvolutionOptionDef option, HiveEvolutionProgress currentProgress)
    {
        if (targetComp is not CompFleshSpread target)
        {
            return;
        }

        target.infectedCells = infectedCells != null ? new HashSet<IntVec3>(infectedCells) : new HashSet<IntVec3>();
        target.ticksUntilErosion = ticksUntilErosion;
        target.cacheDirty = true;
        target.cacheBuildIndex = 0;
        target.borderCache = null;
        target.initializedFromTransfer = true;
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (var g in base.CompGetGizmosExtra())
        {
            yield return g;
        }
        if (!Prefs.DevMode)
        {
            yield break;
        }
        yield return new Command_Action
        {
            defaultLabel = "DEV: Flesh Spread",
            defaultDesc = "Trigger one erosion cycle.",
            action = delegate
            {
                cacheDirty = true;
                cacheBuildIndex = 0;
                borderCache = null;
                BuildBorderCacheImmediate();
                DoErosion();
            }
        };
    }

    private void BuildBorderCache()
    {
        if (infectedCells == null || infectedCells.Count == 0)
        {
            return;
        }
        if (!cacheDirty && borderCache != null && borderCache.Count > 0)
        {
            return;
        }
        if (borderCache == null)
        {
            borderCache = new List<IntVec3>();
        }
        if (cacheDirty)
        {
            borderCache.Clear();
            cacheBuildIndex = 0;
            cacheDirty = false;
        }
        int processed = 0;
        var infectedList = infectedCells.ToList();
        while (cacheBuildIndex < infectedList.Count && processed < CellsPerTick)
        {
            IntVec3 pos = infectedList[cacheBuildIndex];
            for (int i = 0; i < 8; i++)
            {
                IntVec3 neighbor = pos + GenAdj.AdjacentCells[i];
                if (IsValidTarget(neighbor))
                {
                    borderCache.Add(neighbor);
                }
            }
            cacheBuildIndex++;
            processed++;
        }
    }

    private void BuildBorderCacheImmediate()
    {
        if (infectedCells == null)
        {
            return;
        }
        borderCache = new List<IntVec3>();
        foreach (var pos in infectedCells)
        {
            for (int i = 0; i < 8; i++)
            {
                IntVec3 neighbor = pos + GenAdj.AdjacentCells[i];
                if (IsValidTarget(neighbor))
                {
                    borderCache.Add(neighbor);
                }
            }
        }
    }

    private void DoErosion()
    {
        if (borderCache == null || borderCache.Count == 0)
        {
            return;
        }
        Vector3 center = parent.Position.ToVector3Shifted();
        float radiusSq = Props.radius * Props.radius;

        var weighted = borderCache
            .Where(c => (c.ToVector3Shifted() - center).sqrMagnitude <= radiusSq)
            .Select(c =>
            {
                float dist = (c.ToVector3Shifted() - center).magnitude;
                float weight = Mathf.Max(0.1f, (Props.radius - dist) / Props.radius);
                weight *= Rand.Range(0.5f, 1.5f);
                return (cell: c, weight);
            })
            .ToList();

        if (weighted.Count == 0)
        {
            return;
        }

        float totalWeight = weighted.Sum(w => w.weight);
        int count = Mathf.Min(Props.cellsPerErosion, weighted.Count);

        for (int n = 0; n < count; n++)
        {
            float roll = Rand.Value * totalWeight;
            float accum = 0f;
            int idx = 0;
            for (int i = 0; i < weighted.Count; i++)
            {
                accum += weighted[i].weight;
                if (roll <= accum)
                {
                    idx = i;
                    break;
                }
            }
            IntVec3 cell = weighted[idx].cell;
            float removedWeight = weighted[idx].weight;
            parent.Map.terrainGrid.SetTerrain(cell, TerrainDefOf.Flesh);
            infectedCells.Add(cell);
            borderCache.Remove(cell);
            for (int i = 0; i < 8; i++)
            {
                IntVec3 neighbor = cell + GenAdj.AdjacentCells[i];
                if (IsValidTarget(neighbor) && !borderCache.Contains(neighbor))
                {
                    borderCache.Add(neighbor);
                }
            }
            weighted.RemoveAt(idx);
            totalWeight -= removedWeight;
        }
    }

    private void DoInitialErosions()
    {
        for (int i = 0; i < Props.initialErosionCount; i++)
        {
            BuildBorderCacheImmediate();
            DoErosion();
        }
        cacheDirty = true;
        cacheBuildIndex = 0;
    }

    private bool IsValidTarget(IntVec3 c)
    {
        if (!FleshTerrainUtility.CanFleshSpreadTo(parent.Map, c))
        {
            return false;
        }
        if (c.DistanceTo(parent.Position) > Props.radius)
        {
            return false;
        }
        if (infectedCells != null && infectedCells.Contains(c))
        {
            return false;
        }
        if (c.Fogged(parent.Map))
        {
            return false;
        }
        return true;
    }

    private HashSet<IntVec3> infectedCells;

    private List<IntVec3> borderCache;

    private int cacheBuildIndex;

    private int ticksUntilErosion;

    private bool cacheDirty;

    private bool initializedFromTransfer;

    private const int CellsPerTick = 8;
}
