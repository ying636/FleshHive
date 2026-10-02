using System.Collections.Generic;
using HiveCreatureFramework;
using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive;

public class FormulaProgress_FleshTrait : FormulaProgress
{
    public FormulaProgress_FleshTrait()
    {
    }

    public FormulaProgress_FleshTrait(Formula formula, Map map, UnitGroup? reservedGroup = null)
    {
        this.formula = formula;
        this.reservedGroup = reservedGroup;
        float productionSpeedFactor = MapComponent_FleshHive.GetCellDivisionSpeedFactor(map);
        time = Mathf.Max(1, Mathf.RoundToInt(formula.unit.spawningDay.RandomInRange * GenDate.TicksPerDay / productionSpeedFactor));
        totalTime = time;
    }

    public UnitGroup? ReservedGroup
    {
        get => reservedGroup;
        set => reservedGroup = value;
    }

    public override void Finish(CompProgressHolder comp)
    {
        comp.Notify_ProgressFinished(this);
        if (formula?.unit == null)
        {
            return;
        }

        Pawn unit = HCFGameUtility.SpawnUnit(
            comp.parent,
            FleshHiveFleshbeastSpawnUtility.GeneratePawn(formula.unit.kind, comp.parent.Faction),
            ReservedGroup);
        if (FleshBeastKindUtility.IsGiant(unit.kindDef)
            && comp.parent.TryGetComp<CompHiveContainer>() is { } container
            && container.units.Contains(unit))
        {
            if (!container.units.TryDrop(unit, comp.parent.Position, comp.parent.Map, ThingPlaceMode.Near, out _))
            {
                Log.Error($"[FleshHive] Failed to release cultivated mother fleshbeast {unit.def.defName} from {comp.parent.def.defName}.");
            }
        }

        if (!formula.name.NullOrEmpty())
        {
            unit.Name = new NameSingle(formula.name);
        }

        foreach (FormulaMaterial material in formula.materials)
        {
            material.Do(comp, unit);
        }
        comp.parent.Map?.GetComponent<MapComponent_FleshHive>()?.GrantFleshBeastUpgradeHediffs(unit);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref time, "time");
        Scribe_Values.Look(ref totalTime, "totalTime");
        Scribe_References.Look(ref reservedGroup, "reservedGroup");
    }

    private UnitGroup? reservedGroup;
}
