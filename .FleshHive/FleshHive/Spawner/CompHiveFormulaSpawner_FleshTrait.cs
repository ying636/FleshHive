using HiveCreatureFramework;
using Verse;

namespace FleshHive;

public class CompPropertiesHiveFormulaSpawner_FleshTrait : CompPropertiesHiveFormulaSpawner
{
    public CompPropertiesHiveFormulaSpawner_FleshTrait()
    {
        compClass = typeof(CompHiveFormulaSpawner_FleshTrait);
    }
}

public class CompHiveFormulaSpawner_FleshTrait : CompHiveFormulaSpawner
{
    public override bool TryStartFormula(Formula formula)
    {
        return TryStartFormula(formula, null);
    }

    public bool TryStartFormula(Formula formula, UnitGroup? reservedGroup)
    {
        if (formula == null || Spawner == null || ProgressHolder == null
            || !formula.IsAvailable(Spawner, this) || !formula.TryConsume(Resource))
        {
            return false;
        }

        ProgressHolder.progresses.Add(new FormulaProgress_FleshTrait(formula, parent.Map, reservedGroup));
        return true;
    }
}
