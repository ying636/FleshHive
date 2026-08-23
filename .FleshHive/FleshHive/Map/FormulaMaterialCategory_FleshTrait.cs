using System.Collections.Generic;
using System.Linq;
using HiveCreatureFramework;
using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive;

public class FormulaMaterialCategory_FleshTrait : FormulaMaterialCategory
{
    public List<FormulaMaterial> ChosenMaterials = new List<FormulaMaterial>();

    public override FormulaMaterialCategory Clone()
    {
        return new FormulaMaterialCategory_FleshTrait
        {
            CategoryKey = CategoryKey,
            Collapsed = Collapsed,
            ChosenMaterials = new List<FormulaMaterial>()
        };
    }

    public override bool CanUseOn(UnitDef selectedUnit, Thing hive)
    {
        return hive?.TryGetComp<CompHiveFormulaSpawner>()?.Spawner is CompHiveSpawner_FleshTrait;
    }

    public override void SetFormulaContext(Formula formula, List<FormulaMaterialCategory> allCats)
    {
        editingFormula = formula;
        allCategories = allCats;
    }

    public override void ClearChosen()
    {
        ChosenMaterials.Clear();
    }

    public override List<FormulaMaterial> GetChosenMaterials()
    {
        return ChosenMaterials;
    }

    public override void Draw(float entryX, ref float entryY, float entryWidth, UnitDef selectedUnit)
    {
        base.Draw(entryX, ref entryY, entryWidth, selectedUnit);
        if (Collapsed)
        {
            return;
        }

        bool drewTrait = false;
        foreach (FormulaMaterial material in GetItems())
        {
            if (!material.CanShowForKind(selectedUnit?.kind))
            {
                continue;
            }

            drewTrait = true;

            bool isChosen = ChosenMaterials.Any(chosen => chosen.IsSameMaterial(material));
            string reason = null;
            bool canAdd = material.CanAddMore(selectedUnit, ChosenMaterials, out reason);
            bool disabled = !canAdd && !isChosen;
            Rect itemRect = new Rect(entryX + 10f, entryY, entryWidth - 30f, 30f);

            if (disabled)
            {
                GUI.color = Color.grey;
            }

            material.Draw(itemRect, isChosen, selectedUnit?.kind);
            GUI.color = Color.white;

            if (Widgets.ButtonInvisible(itemRect) && !disabled)
            {
                if (isChosen)
                {
                    ChosenMaterials.RemoveAll(chosen => chosen.IsSameMaterial(material));
                }
                else
                {
                    ChosenMaterials.Clear();
                    ChosenMaterials.Add(material);
                }

                SyncToFormula();
            }

            if (disabled && !reason.NullOrEmpty())
            {
                TooltipHandler.TipRegion(itemRect, reason);
            }

            entryY += 32f;
        }

        if (!drewTrait)
        {
            GUI.color = Color.grey;
            Widgets.Label(new Rect(entryX + 10f, entryY, entryWidth - 30f, 30f), "FH_FormulaTrait_NoneAvailable".Translate());
            GUI.color = Color.white;
            entryY += 32f;
        }
    }

    public void SyncToFormula()
    {
        if (editingFormula == null || allCategories == null)
        {
            return;
        }

        editingFormula.materials.Clear();
        foreach (FormulaMaterialCategory category in allCategories)
        {
            editingFormula.materials.AddRange(category.GetChosenMaterials());
        }

        if (!editingFormula.nameIsCustom)
        {
            editingFormula.Recalculate();
        }
    }

    private Formula editingFormula;
    private List<FormulaMaterialCategory> allCategories;
}
