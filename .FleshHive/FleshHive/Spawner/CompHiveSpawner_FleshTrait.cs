using System.Text;
using HiveCreatureFramework;
using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive;

public class CompPropertiesHiveSpawner_FleshTrait : CompPropertiesHiveSpawner
{
    public CompPropertiesHiveSpawner_FleshTrait()
    {
        compClass = typeof(CompHiveSpawner_FleshTrait);
    }

    public List<FleshTraitSpawnOption> traitOptions = new();
}

public class CompHiveSpawner_FleshTrait : CompHiveSpawner
{
    public new CompPropertiesHiveSpawner_FleshTrait Props => (CompPropertiesHiveSpawner_FleshTrait)props;

    // public static float FleshbeastProductionSpeedFactor => FleshHiveDefOf.FH_Research_EfficientCellDivision.IsFinished ? 1.5f : 1f;

    public override bool CanShowUnitCategory(SpawnCategoryDef category)
    {
        return category.CanShow(this);
    }

    public override void Draw(Rect inRect, out Vector2 limit)
    {
        Rect resourceRect = new Rect(inRect.x, inRect.y, inRect.width, inRect.height);
        foreach (SpawnCategoryDef category in GetUnitCategories())
        {
            if (!CanShowUnitCategory(category))
            {
                continue;
            }

            DrawCategory(ref resourceRect, category);
            if (IsCategoryCollapsed(category))
            {
                continue;
            }

            foreach (UnitDef unit in category.GetUnits(this))
            {
                Rect cardRect = new Rect(resourceRect.x + 5f, resourceRect.y + 5f, resourceRect.width - 10f, GetUnitCardHeight(unit, resourceRect.width - 10f));
                DrawUnitCard(cardRect, unit);
                resourceRect.y = cardRect.yMax + 5f;
            }

            foreach (ItemDef item in category.GetItems(this))
            {
                Rect cardRect = new Rect(resourceRect.x + 5f, resourceRect.y + 5f, resourceRect.width - 10f, GetItemCardHeight(item, resourceRect.width - 10f));
                DrawItemCard(cardRect, item);
                resourceRect.y = cardRect.yMax + 5f;
            }
        }

        limit = inRect.position + new Vector2(inRect.width, resourceRect.y);
    }

    public override bool TryStartSpawnUnit(UnitDef unit, bool sendMessage = true)
    {
        return TryStartUnitProduction(unit, null, sendMessage).Accepted;
    }

    public AcceptanceReport TryStartUnitProduction(UnitDef unit, HediffDef traitDef = null, bool sendMessage = true, UnitGroup reservedGroup = null)
    {
        if (unit == null)
        {
            return false;
        }

        AcceptanceReport report = unit.CanProduce(this);
        if (!report.Accepted)
        {
            return report;
        }

        ConsumeUnitCost(unit);
        float productionSpeedFactor = MapComponent_FleshHive.GetCellDivisionSpeedFactor(parent.Map);
        UnitSpawnData_FleshTrait progress = new UnitSpawnData_FleshTrait(unit, traitDef, productionSpeedFactor, reservedGroup);
        ProgressHolder.progresses.Add(progress);
        if (sendMessage)
        {
            SendProgressAddedMessage(progress);
        }
        return true;
    }

    private void DrawUnitCard(Rect cardRect, UnitDef unit)
    {
        DrawBorder(cardRect);
        Rect iconRect = new Rect(cardRect.x + 8f, cardRect.y + 8f, 84f, 84f);
        DrawBorder(iconRect);
        Widgets.DefIcon(iconRect.ContractedBy(8f), unit.kind);

        Rect actionRect = new Rect(cardRect.xMax - ActionWidth - 10f, cardRect.y + 8f, ActionWidth, cardRect.height - 16f);
        Rect labelRect = new Rect(iconRect.xMax + 12f, cardRect.y + 8f, actionRect.x - iconRect.xMax - 24f, 28f);
        Text.Font = GameFont.Medium;
        Widgets.Label(labelRect, unit.label);
        Text.Font = GameFont.Small;

        Rect descRect = new Rect(labelRect.x, labelRect.yMax + 4f, labelRect.width, GetDescriptionHeight(GetUnitDescription(unit), labelRect.width));
        Widgets.Label(descRect, GetUnitDescription(unit));

        Rect costRect = new Rect(labelRect.x, cardRect.yMax - CostLineHeight - 5f, labelRect.width, CostLineHeight);
        DrawCostLine(costRect, unit.costs, unit.requirements, unit.specialResources);

        Rect traitLabelRect = new Rect(actionRect.x, actionRect.y + 6f, actionRect.width, 24f);
        Text.Anchor = TextAnchor.MiddleCenter;
        Text.Font = GameFont.Small;
        Widgets.Label(traitLabelRect, "FH_Spawner_Trait".Translate());
        Text.Anchor = TextAnchor.UpperLeft;

        Rect traitButtonRect = new Rect(actionRect.x, traitLabelRect.yMax + 4f, actionRect.width, 32f);
        DrawTraitSelector(traitButtonRect, unit);

        Rect buttonRect = new Rect(actionRect.x, actionRect.yMax - 28f, actionRect.width, 28f);
        DrawUnitButton(buttonRect, unit);
        TooltipHandler.TipRegion(new Rect(cardRect.x, cardRect.y, actionRect.x - cardRect.x - 4f, cardRect.height), BuildUnitTip(unit));
    }

    private void DrawItemCard(Rect cardRect, ItemDef item)
    {
        DrawBorder(cardRect);
        Rect iconRect = new Rect(cardRect.x + 8f, cardRect.y + 8f, 84f, 84f);
        DrawBorder(iconRect);
        Widgets.DefIcon(iconRect.ContractedBy(8f), item.thing);

        Rect labelRect = new Rect(iconRect.xMax + 12f, cardRect.y + 8f, cardRect.width - iconRect.width - ButtonWidth - 40f, 28f);
        Text.Font = GameFont.Medium;
        Widgets.Label(labelRect, item.label);
        Text.Font = GameFont.Small;

        Rect descRect = new Rect(labelRect.x, labelRect.yMax + 4f, labelRect.width, GetDescriptionHeight(item.description, labelRect.width));
        Widgets.Label(descRect, item.description);

        Rect costRect = new Rect(labelRect.x, cardRect.yMax - CostLineHeight - 5f, labelRect.width, CostLineHeight);
        DrawCostLine(costRect, item.costs, item.requirements, item.specialResources);

        Rect buttonRect = new Rect(cardRect.xMax - ButtonWidth - 10f, cardRect.yMax - 38f, ButtonWidth, 28f);
        DrawItemButton(buttonRect, item);
        TooltipHandler.TipRegion(cardRect, BuildItemTip(item));
    }

    private void DrawTraitSelector(Rect rect, UnitDef unit)
    {
        List<FleshTraitSelection> selections = GetUnlockedTraitSelections(unit).ToList();
        HediffDef selectedTrait = GetSelectedTrait(unit);
        if (!selections.Any(selection => selection.hediff == selectedTrait))
        {
            selectedTrait = null;
            selectedTraits.Remove(unit);
        }

        string label = selectedTrait?.label ?? "FH_Spawner_TraitNone".Translate();
        DrawBorder(rect);
        Rect arrowRect = new Rect(rect.x + 6f, rect.y + (rect.height - 18f) / 2f, 18f, 18f);
        Widgets.DrawTextureFitted(arrowRect, HCFUtility.Down, 1f);
        Rect labelRect = new Rect(rect.x + 24f, rect.y, rect.width - 30f, rect.height);
        if (selectedTrait != null && !selectedTrait.description.NullOrEmpty())
        {
            TooltipHandler.TipRegion(rect, selectedTrait.description);
        }
        bool clicked = Widgets.ButtonText(labelRect, label, false, false, Color.white, true, TextAnchor.MiddleCenter);
        if (!clicked)
        {
            clicked = Widgets.ButtonInvisible(rect);
        }
        if (clicked)
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>
            {
                new("FH_Spawner_TraitNone".Translate(), () => selectedTraits.Remove(unit))
            };
            foreach (FleshTraitSelection selection in selections)
            {
                options.Add(new FloatMenuOption(selection.hediff.label, () => selectedTraits[unit] = selection.hediff));
            }
            Find.WindowStack.Add(new FloatMenu(options));
        }
    }

    private void DrawUnitButton(Rect buttonRect, UnitDef unit)
    {
        string spawnText = "SpawnUnit".Translate();
        AcceptanceReport report = unit.CanProduce(this);
        bool active = report.Accepted;
        if (!active && !report.Reason.NullOrEmpty())
        {
            TooltipHandler.TipRegion(buttonRect, report.Reason);
        }

        if (Widgets.ButtonText(buttonRect, spawnText, true, true, active ? ColorLibrary.SkyBlue : Color.grey, active))
        {
            TryStartUnitProduction(unit, GetSelectedTrait(unit));
        }
    }

    private void DrawItemButton(Rect buttonRect, ItemDef item)
    {
        string spawnText = "SpawnUnit".Translate();
        AcceptanceReport report = item.worker.CanProduce(this);
        bool active = report.Accepted;
        if (!active && !report.Reason.NullOrEmpty())
        {
            TooltipHandler.TipRegion(buttonRect, report.Reason);
        }

        if (Widgets.ButtonText(buttonRect, spawnText, true, true, active ? ColorLibrary.SkyBlue : Color.grey, active))
        {
            item.worker.AddProgress(this);
        }
    }

    private void ConsumeUnitCost(UnitDef unit)
    {
        if (!unit.costs.NullOrEmpty())
        {
            foreach (ResourceCount cost in unit.costs)
            {
                Resource.ConsumeResource(cost);
            }
        }

        Resource.ConsumeRequiredItems(unit.requirements);
        if (!unit.specialResources.NullOrEmpty())
        {
            foreach (SpecialResourceConsume special in unit.specialResources)
            {
                special.Consume(parent);
            }
        }
    }

    private HediffDef GetSelectedTrait(UnitDef unit)
    {
        return selectedTraits.TryGetValue(unit, out HediffDef trait) ? trait : null;
    }

    public IEnumerable<FleshTraitSelection> GetUnlockedTraitSelections(UnitDef unit)
    {
        foreach (FleshTraitSpawnOption option in Props.traitOptions)
        {
            if (option.unit != unit || option.hediff == null)
            {
                continue;
            }
            if (!IsTraitDiscovered(option))
            {
                continue;
            }
            yield return new FleshTraitSelection(option.hediff);
        }
    }

    public static bool IsTraitDiscovered(FleshTraitSpawnOption option)
    {
        if (option?.hediff == null || option.prerequisiteFusion == null)
        {
            return false;
        }

        return GameComponent_UnitGroup.Instance?.fusionDatas?.Any(data =>
            data?.unlocked == true
            && (data.def == option.prerequisiteFusion
                || data.def?.defName == option.prerequisiteFusion.defName)
            && data.def.results.Any(result =>
                result?.result is FusionResult_PawnKindWithHediffs traitResult
                && traitResult.kind == option.unit?.kind
                && traitResult.hediffs.Contains(option.hediff))) == true;
    }

    private string BuildUnitTip(UnitDef unit)
    {
        StringBuilder tip = new StringBuilder();
        tip.AppendLine(unit.label);
        tip.AppendLine(GetUnitDescription(unit));
        AppendCosts(tip, unit.costs, unit.requirements, unit.specialResources);
        AppendUnitCompText(tip, unit);
        return tip.ToString().Trim();
    }

    private string BuildItemTip(ItemDef item)
    {
        StringBuilder tip = new StringBuilder();
        tip.AppendLine(item.label);
        tip.AppendLine(item.description);
        AppendCosts(tip, item.costs, item.requirements, item.specialResources);
        return tip.ToString().Trim();
    }

    private void AppendCosts(StringBuilder builder, List<ResourceCount> costs, List<ThingDefCountClass> requirements, List<SpecialResourceConsume> specialResources)
    {
        if (costs.NullOrEmpty() && requirements.NullOrEmpty() && specialResources.NullOrEmpty())
        {
            return;
        }

        builder.AppendLine("HCF_DetailCostLabel".Translate());
        foreach (string part in GetCostParts(costs, requirements, specialResources))
        {
            builder.AppendLine(part);
        }
    }

    private void AppendUnitCompText(StringBuilder builder, UnitDef unit)
    {
        if (unit.kind?.race?.GetCompProperties<UnitCompProperties>() is not { } properties)
        {
            return;
        }

        if (!properties.tags.NullOrEmpty())
        {
            builder.AppendLine("HCF_Tags".Translate());
            foreach (string tag in properties.tags)
            {
                builder.AppendLine(("HCF_Tag_" + tag).Translate());
            }
        }
        if (!properties.works.NullOrEmpty())
        {
            builder.AppendLine("HCF_UnitWork".Translate());
            foreach (UnitWorkDef work in properties.works)
            {
                builder.AppendLine(work.label);
            }
        }
    }

    private string GetUnitDescription(UnitDef unit)
    {
        return unit.description.NullOrEmpty() ? unit.kind.race.description : unit.description;
    }

    private float GetDescriptionHeight(string description, float width)
    {
        return Mathf.Max(24f, Text.CalcHeight(description ?? string.Empty, width));
    }

    private float GetUnitCardHeight(UnitDef unit, float width)
    {
        float labelWidth = Mathf.Max(80f, width - ActionWidth - 126f);
        float height = 8f + 28f + 4f + GetDescriptionHeight(GetUnitDescription(unit), labelWidth) + 8f;
        if (!BuildCostLine(unit.costs, unit.requirements, unit.specialResources).NullOrEmpty())
        {
            height += CostLineHeight + 5f;
        }
        return Mathf.Max(CardMinHeight, height);
    }

    private float GetItemCardHeight(ItemDef item, float width)
    {
        float labelWidth = Mathf.Max(80f, width - 244f);
        float height = 8f + 28f + 4f + GetDescriptionHeight(item.description, labelWidth) + 8f;
        if (!BuildCostLine(item.costs, item.requirements, item.specialResources).NullOrEmpty())
        {
            height += CostLineHeight + 5f;
        }
        return Mathf.Max(CardMinHeight, height);
    }

    private void DrawCostLine(Rect rect, List<ResourceCount> costs, List<ThingDefCountClass> requirements, List<SpecialResourceConsume> specialResources)
    {
        string text = BuildCostLine(costs, requirements, specialResources);
        if (text.NullOrEmpty())
        {
            return;
        }

        GameFont oldFont = Text.Font;
        Text.Font = GameFont.Tiny;
        Widgets.Label(rect, text.Truncate(rect.width));
        TooltipHandler.TipRegion(rect, BuildFullCostLine(costs, requirements, specialResources));
        Text.Font = oldFont;
    }

    private string BuildCostLine(List<ResourceCount> costs, List<ThingDefCountClass> requirements, List<SpecialResourceConsume> specialResources)
    {
        List<string> parts = GetCostParts(costs, requirements, specialResources).ToList();
        return parts.Any() ? "HCF_DetailCostLabel".Translate() + string.Join(" | ", parts) : null;
    }

    private string BuildFullCostLine(List<ResourceCount> costs, List<ThingDefCountClass> requirements, List<SpecialResourceConsume> specialResources)
    {
        return string.Join("\n", GetCostParts(costs, requirements, specialResources));
    }

    private IEnumerable<string> GetCostParts(List<ResourceCount> costs, List<ThingDefCountClass> requirements, List<SpecialResourceConsume> specialResources)
    {
        if (!costs.NullOrEmpty())
        {
            foreach (ResourceCount cost in costs)
            {
                yield return cost.resource.label + "*" + cost.amount;
            }
        }

        if (!requirements.NullOrEmpty())
        {
            foreach (ThingDefCountClass requirement in requirements)
            {
                yield return requirement.Label;
            }
        }

        if (!specialResources.NullOrEmpty())
        {
            foreach (SpecialResourceConsume special in specialResources)
            {
                yield return special.Label;
            }
        }
    }

    private void DrawBorder(Rect rect)
    {
        Color previousColor = GUI.color;
        GUI.color = BorderColor;
        Widgets.DrawBox(rect);
        GUI.color = previousColor;
    }

    private readonly Dictionary<UnitDef, HediffDef> selectedTraits = new();

    private static readonly Color BorderColor = new Color32(0x42, 0x43, 0x44, 0xFF);

    private const float CardMinHeight = 124f;

    private const float ButtonWidth = 120f;

    private const float ActionWidth = 140f;

    private const float CostLineHeight = 18f;
}

public class UnitSpawnData_FleshTrait : UnitSpawnData
{
    public UnitDef Def => def;

    public UnitGroup ReservedGroup => reservedGroup;

    public UnitSpawnData_FleshTrait()
    {
    }

    public UnitSpawnData_FleshTrait(UnitDef def, HediffDef traitDef) : base(def)
    {
        this.traitDef = traitDef;
    }

    public UnitSpawnData_FleshTrait(UnitDef def, HediffDef traitDef, float productionSpeedFactor, UnitGroup reservedGroup = null) : base(def)
    {
        this.traitDef = traitDef;
        this.reservedGroup = reservedGroup;
        if (productionSpeedFactor > 0f && productionSpeedFactor != 1f)
        {
            time = Mathf.Max(1, Mathf.RoundToInt(time / productionSpeedFactor));
            totalTime = Mathf.Max(1, Mathf.RoundToInt(totalTime / productionSpeedFactor));
        }
    }

    public override List<Pawn> SpawnUnit(CompProgressHolder comp)
    {
        PawnKindDef kind = def?.kind ?? pawn;
        if (kind == null)
        {
            return new List<Pawn>();
        }

        Pawn generatedPawn = PawnGenerator.GeneratePawn(kind, comp.parent.Faction);
        List<Pawn> pawns = new List<Pawn>
        {
            HCFGameUtility.SpawnUnit(comp.parent, generatedPawn, reservedGroup)
        };
        foreach (Pawn pawn in pawns)
        {
            if (FleshBeastKindUtility.IsGiant(pawn.kindDef)
                && comp.parent.TryGetComp<CompHiveContainer>() is { } container
                && container.units.Contains(pawn))
            {
                if (!container.units.TryDrop(pawn, comp.parent.Position, comp.parent.Map, ThingPlaceMode.Near, out _))
                {
                    Log.Error($"[FleshHive] Failed to release cultivated mother fleshbeast {pawn.def.defName} from {comp.parent.def.defName}.");
                }
            }

            if (traitDef != null)
            {
                HealthUtility.AdjustSeverity(pawn, traitDef, 1f);
            }
            comp.parent.Map?.GetComponent<MapComponent_FleshHive>()?.GrantFleshBeastUpgradeHediffs(pawn);
        }
        return pawns;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Defs.Look(ref traitDef, "traitDef");
        Scribe_References.Look(ref reservedGroup, "reservedGroup");
    }

    private HediffDef traitDef;
    private UnitGroup reservedGroup;
}

public class FleshTraitSpawnOption
{
    public UnitDef unit;

    public HediffDef hediff;

    public FusionDef prerequisiteFusion;
}

public readonly struct FleshTraitSelection
{
    public FleshTraitSelection(HediffDef hediff)
    {
        this.hediff = hediff;
    }

    public readonly HediffDef hediff;
}
