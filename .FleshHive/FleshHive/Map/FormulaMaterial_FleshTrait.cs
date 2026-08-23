using System.Collections.Generic;
using System.Linq;
using HiveCreatureFramework;
using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive;

[StaticConstructorOnStartup]
public class FormulaMaterial_FleshTrait : FormulaMaterial
{
    static FormulaMaterial_FleshTrait()
    {
        RegisterProvider(GetAll);
        FormulaMaterialCategory.Templates.Add(new FormulaMaterialCategory_FleshTrait
        {
            CategoryKey = "FleshTrait"
        });
    }

    public static List<FormulaMaterial> GetAll()
    {
        List<FormulaMaterial> materials = new List<FormulaMaterial>();
        HashSet<string> seen = new HashSet<string>();
        foreach (ThingDef thingDef in DefDatabase<ThingDef>.AllDefsListForReading)
        {
            CompPropertiesHiveSpawner_FleshTrait properties = thingDef.GetCompProperties<CompPropertiesHiveSpawner_FleshTrait>();
            if (properties?.traitOptions.NullOrEmpty() != false)
            {
                continue;
            }

            foreach (FleshTraitSpawnOption option in properties.traitOptions)
            {
                if (option.unit == null || option.hediff == null || option.prerequisiteFusion == null)
                {
                    continue;
                }

                string key = option.unit.defName + ":" + option.hediff.defName + ":" + option.prerequisiteFusion.defName;
                if (seen.Add(key) && CompHiveSpawner_FleshTrait.IsTraitDiscovered(option))
                {
                    materials.Add(new FormulaMaterial_FleshTrait
                    {
                        unit = option.unit,
                        trait = option.hediff,
                        prerequisiteFusion = option.prerequisiteFusion
                    });
                }
            }
        }

        return materials;
    }

    public override string CategoryKey => "FleshTrait";

    public override bool CanUseOnSpawner(CompHiveFormulaSpawner spawner)
    {
        return spawner?.Spawner is CompHiveSpawner_FleshTrait fleshSpawner
            && fleshSpawner.GetUnlockedTraitSelections(unit).Any(selection => selection.hediff == trait)
            && CompHiveSpawner_FleshTrait.IsTraitDiscovered(new FleshTraitSpawnOption
            {
                unit = unit,
                hediff = trait,
                prerequisiteFusion = prerequisiteFusion
            });
    }

    public override bool CanAddMore(UnitDef selectedUnit, List<FormulaMaterial> alreadyChosen, out string reason)
    {
        if (alreadyChosen.Any(material => material is FormulaMaterial_FleshTrait && !IsSameMaterial(material)))
        {
            reason = "FH_FormulaTrait_OnlyOne".Translate();
            return false;
        }

        reason = null;
        return true;
    }

    public override bool CanShowForKind(PawnKindDef kindDef)
    {
        return unit?.kind == kindDef
            && CompHiveSpawner_FleshTrait.IsTraitDiscovered(new FleshTraitSpawnOption
            {
                unit = unit,
                hediff = trait,
                prerequisiteFusion = prerequisiteFusion
            });
    }

    public override List<string> GetAdjective()
    {
        return new List<string> { trait?.label ?? "" };
    }

    public override List<string> GetNoun()
    {
        return new List<string> { "FH_FormulaTrait_Noun".Translate() };
    }

    public override List<ResourceCount> GetCosts()
    {
        // 特性种使用基底 UnitDef 的消耗，这里不再追加营养，避免重复扣除。
        return new List<ResourceCount>();
    }

    public override List<ThingDefCountClass> GetRequirements()
    {
        return new List<ThingDefCountClass>();
    }

    public override void Do(CompProgressHolder comp, Pawn pawn)
    {
        if (trait != null && pawn?.health != null && !pawn.health.hediffSet.HasHediff(trait))
        {
            HealthUtility.AdjustSeverity(pawn, trait, 1f);
        }
    }

    public override void Draw(Rect rect, bool selected, PawnKindDef selectedKind)
    {
        if (selected)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.2f, 0.4f, 0.6f, 0.3f));
        }

        Widgets.Label(new Rect(rect.x + 5f, rect.y, rect.width - 10f, rect.height), trait?.label ?? "Unknown");
        if (trait != null && !trait.description.NullOrEmpty())
        {
            TooltipHandler.TipRegion(rect, trait.description);
        }
    }

    public override bool IsSameMaterial(FormulaMaterial other)
    {
        return other is FormulaMaterial_FleshTrait fleshTrait
            && fleshTrait.unit == unit
            && fleshTrait.trait == trait;
    }

    public override void ExposeData()
    {
        Scribe_Defs.Look(ref unit, "unit");
        Scribe_Defs.Look(ref trait, "trait");
        Scribe_Defs.Look(ref prerequisiteFusion, "prerequisiteFusion");
    }

    public UnitDef unit;
    public HediffDef trait;
    public FusionDef prerequisiteFusion;
}
