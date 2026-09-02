using System.Collections.Generic;
using HiveCreatureFramework;
using RimWorld;
using Verse;

namespace FleshHive;

public class FormulaProgress_FleshTrait : FormulaProgress
{
    public override void Finish(CompProgressHolder comp)
    {
        base.Finish(comp);
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
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref reservedGroup, "reservedGroup");
    }

    public UnitGroup ReservedGroup
    {
        get => reservedGroup;
        set => reservedGroup = value;
    }

    private UnitGroup reservedGroup;
}
