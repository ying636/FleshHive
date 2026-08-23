using System.Collections.Generic;
using System.Linq;
using System.Text;
using HiveCreatureFramework;
using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive;

[StaticConstructorOnStartup]
public class FormulaMaterial_Parasite : FormulaMaterial
{
    static FormulaMaterial_Parasite()
    {
        RegisterProvider(() => GetAll());
        FormulaMaterialCategory.Templates.Add(new FormulaMaterialCategory_Parasite
        {
            CategoryKey = "Parasite"
        });
    }

    public static List<FormulaMaterial> GetAll()
    {
        List<FormulaMaterial> list = new List<FormulaMaterial>();
        foreach (PawnKindDef kind in DefDatabase<PawnKindDef>.AllDefsListForReading)
        {
            if (kind.race?.GetCompProperties<ParasitismCompProperties>() is { })
            {
                list.Add(new FormulaMaterial_Parasite { parasiteKind = kind });
            }
        }
        return list;
    }

    internal ParasitismCompProperties Props
    {
        get
        {
            if (cachedProps == null && parasiteKind != null)
            {
                cachedProps = parasiteKind.race?.GetCompProperties<ParasitismCompProperties>();
            }
            return cachedProps;
        }
    }

    public override string CategoryKey => "Parasite";

    public override bool CanUseOnSpawner(CompHiveFormulaSpawner spawner)
    {
        var resource = spawner.parent.TryGetComp<CompHiveResource>();
        return resource?.Props.tags?.Contains("FleshHive") == true;
    }

    public override bool CanAddMore(UnitDef selectedUnit, List<FormulaMaterial> alreadyChosen, out string reason)
    {
        float hostBodySize = selectedUnit?.kind?.race?.race?.baseBodySize ?? 1f;
        int capacity = Mathf.Min((int)(hostBodySize + 1), 14);
        int used = 0;
        foreach (var m in alreadyChosen)
        {
            if (m is FormulaMaterial_Parasite p && p.Props != null)
            {
                used += p.Props.cost;
            }
        }
        int need = Props?.cost ?? 1;
        if (need > capacity - used)
        {
            reason = "HCF_Parasite_NoSpace".Translate(capacity - used, need);
            return false;
        }
        reason = null;
        return true;
    }

    public override bool CanShowForKind(PawnKindDef kindDef)
    {
        return true;
    }

    public override List<string> GetAdjective()
    {
        return new List<string> { parasiteKind?.label ?? "" };
    }

    public override List<string> GetNoun()
    {
        return new List<string> { "HCF_Parasite_Noun".Translate() };
    }

    private UnitDef CachedUnitDef => sourceUnitDef;

    public override List<ResourceCount> GetCosts()
    {
        if (CachedUnitDef != null)
        {
            List<ResourceCount> costs = new List<ResourceCount>();
            foreach (ResourceCount c in CachedUnitDef.costs)
            {
                costs.Add(new ResourceCount(c));
            }
            return costs;
        }
        return new List<ResourceCount>();
    }

    public override List<ThingDefCountClass> GetRequirements()
    {
        if (CachedUnitDef != null)
        {
            List<ThingDefCountClass> reqs = new List<ThingDefCountClass>();
            foreach (ThingDefCountClass r in CachedUnitDef.requirements)
            {
                reqs.Add(new ThingDefCountClass(r.thingDef, r.count));
            }
            return reqs;
        }
        return new List<ThingDefCountClass>();
    }

    public override void Do(CompProgressHolder comp, Pawn unit)
    {
        if (parasiteKind == null) return;
        ParasitismSystem.EnsureAbilityTracker(unit);
        ParasitismSystem system = unit.health.hediffSet.GetFirstHediffOfDef(FleshHiveDefOf.FH_ParasitismSystem) as ParasitismSystem;
        if (system == null)
        {
            system = (ParasitismSystem)unit.health.AddHediff(FleshHiveDefOf.FH_ParasitismSystem);
        }
        Pawn parasitePawn = PawnGenerator.GeneratePawn(parasiteKind, comp.parent.Faction);
        if (system.Parasite(parasitePawn) && parasitePawn.TryGetComp<ParasitismComp>()?.Props.synchronizeHost == true)
        {
            system.EnsureSynchronizedReplicaSpawned(parasitePawn);
        }
    }

    public override void Draw(Rect rect, bool selected, PawnKindDef selectedKind)
    {
        string text = parasiteKind?.LabelCap ?? "Unknown";
        Widgets.Label(new Rect(rect.x + 5f, rect.y, rect.width - 10f, rect.height), text);
        StringBuilder tip = new StringBuilder();
        tip.AppendLine(parasiteKind?.LabelCap ?? "");
        if (Props != null)
        {
            tip.AppendLine("ParasitismCapacityCost".Translate() + Props.cost);
            if (!Props.effect.NullOrEmpty())
            {
                tip.AppendLine(Props.effect);
            }
        }
        TooltipHandler.TipRegion(rect, tip.ToString().Trim());
    }

    public override bool IsSameMaterial(FormulaMaterial other)
    {
        return other is FormulaMaterial_Parasite p && p.parasiteKind == parasiteKind;
    }

    public override void ExposeData()
    {
        Scribe_Defs.Look(ref parasiteKind, "parasiteKind");
        Scribe_Defs.Look(ref sourceUnitDef, "sourceUnitDef");
    }

    public PawnKindDef parasiteKind;
    public UnitDef sourceUnitDef;
    private ParasitismCompProperties cachedProps;
}

public class FormulaMaterialCategory_Parasite : FormulaMaterialCategory
{
    public List<FormulaMaterial> ChosenMaterials = new List<FormulaMaterial>();
    private Formula editingFormula;
    private List<FormulaMaterialCategory> allCategories;
    private Thing hive;
    private Vector2 chosenScrollPos;
    private static bool windowOpen;

    public override FormulaMaterialCategory Clone()
    {
        return new FormulaMaterialCategory_Parasite
        {
            CategoryKey = CategoryKey,
            Collapsed = Collapsed,
            ChosenMaterials = new List<FormulaMaterial>()
        };
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

    public override bool CanUseOn(UnitDef selectedUnit, Thing hive)
    {
        this.hive = hive;
        var resource = hive.TryGetComp<CompHiveResource>();
        return resource?.Props.tags?.Contains("FleshHive") == true;
    }

    public override void Draw(float entryX, ref float entryY, float entryWidth, UnitDef selectedUnit)
    {
        base.Draw(entryX, ref entryY, entryWidth, selectedUnit);
        if (Collapsed) return;

        float listHeight = ChosenMaterials.Count * 30f;
        if (listHeight > 120f) listHeight = 120f;

        if (ChosenMaterials.Any())
        {
            Widgets.BeginScrollView(
                new Rect(entryX + 5f, entryY, entryWidth - 10f, listHeight),
                ref chosenScrollPos,
                new Rect(0f, 0f, entryWidth - 30f, ChosenMaterials.Count * 30f));

            float matY = 0f;
            foreach (FormulaMaterial mat in ChosenMaterials.ToList())
            {
                Rect row = new Rect(0f, matY, entryWidth - 30f, 28f);
                Widgets.DrawBox(row);
                mat.Draw(new Rect(row.x + 2f, row.y, row.width - 28f, row.height), true, selectedUnit?.kind);

                Rect removeBtn = new Rect(row.xMax - 25f, row.y + 4f, 20f, 20f);
                if (Widgets.ButtonImage(removeBtn, TexButton.Delete))
                {
                    int idx = ChosenMaterials.FindIndex(m => m == mat);
                    if (idx >= 0) ChosenMaterials.RemoveAt(idx);
                    SyncToFormula();
                }
                matY += 30f;
            }
            Widgets.EndScrollView();
            entryY += listHeight + 5f;
        }

        Rect addRect = new Rect(entryX + entryWidth / 2f - 50f, entryY, 100f, 30f);
        if (Widgets.ButtonText(addRect, "HCF_AddParasite".Translate(), true, true, ColorLibrary.SkyBlue, true))
        {
            if (selectedUnit != null && hive != null && !windowOpen)
            {
                windowOpen = true;
                Find.WindowStack.Add(new Window_SelectParasite(hive, this, selectedUnit));
            }
        }
        entryY += 35f;
    }

    public static void NotifyWindowClosed()
    {
        windowOpen = false;
    }

    public void SyncToFormula()
    {
        if (editingFormula == null || allCategories == null) return;
        editingFormula.materials.Clear();
        foreach (FormulaMaterialCategory cat in allCategories)
        {
            editingFormula.materials.AddRange(cat.GetChosenMaterials());
        }
        if (!editingFormula.nameIsCustom)
        {
            editingFormula.Recalculate();
        }
    }
}
