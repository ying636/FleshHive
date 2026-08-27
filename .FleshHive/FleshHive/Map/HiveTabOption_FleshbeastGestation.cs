using System.Collections.Generic;
using System.Linq;
using HiveCreatureFramework;
using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive;

public class HiveTabOption_FleshbeastGestation : HiveTabOption_FleshHive
{
    private Texture2D ProgressBarTex => progressBarTex ??= SolidColorMaterials.NewSolidColorTexture(ProgressBarColor);

    private Texture2D EmptyBarTex => emptyBarTex ??= SolidColorMaterials.NewSolidColorTexture(EmptyBarColor);

    public override void Draw(List<Pawn> pawns, HiveRaceCategoryDef def, Rect inRect)
    {
        if (DrawHungryIfNeeded(inRect))
        {
            return;
        }

        Map map = Find.CurrentMap;
        MapComponent_FleshHive mapComp = map?.GetComponent<MapComponent_FleshHive>();
        if (mapComp == null)
        {
            return;
        }

        List<CompHiveSpawner_FleshTrait> spawners = mapComp.GetFleshbeastSpawners()
            .OrderBy(spawner => spawner.parent.Position.z)
            .ThenBy(spawner => spawner.parent.Position.x)
            .ToList();
        ValidateSelection(spawners);

        float quotaPanelWidth = GetQuotaPanelWidth(inRect.width);
        Rect quotaRect = new Rect(inRect.xMax - quotaPanelWidth, inRect.y, quotaPanelWidth, inRect.height);
        Rect mainRect = new Rect(inRect.x, inRect.y, inRect.width - quotaPanelWidth - PanelGap, inRect.height);
        DrawQuotaPanel(quotaRect, mapComp);

        Rect formulaToolbarRect = new Rect(mainRect.x, mainRect.y, mainRect.width, FormulaToolbarHeight);
        DrawFormulaToolbar(formulaToolbarRect, spawners);
        Rect productionRect = new Rect(mainRect.x, formulaToolbarRect.yMax, mainRect.width, mainRect.height - FormulaToolbarHeight);

        float headerHeight = selectedSpawner != null && selectedUnit != null ? SelectedUnitHeight : 0f;
        if (headerHeight > 0f)
        {
            DrawSelectedUnit(new Rect(productionRect.x, productionRect.y, productionRect.width, SelectedUnitHeight), selectedSpawner, selectedUnit, selectedFormula);
        }

        Rect outRect = new Rect(productionRect.x, productionRect.y + headerHeight, productionRect.width, productionRect.height - headerHeight);
        if (spawners.Count == 0)
        {
            DrawNoSpawners(outRect);
            return;
        }

        float viewWidth = outRect.width - ScrollbarWidth;
        float requiredHeight = spawners.Sum(spawner => GetSpawnerRowHeight(spawner, viewWidth) + RowGap);
        contentHeight = Mathf.Max(requiredHeight, outRect.height + 1f);
        FocusSelectedSpawner(spawners, viewWidth, outRect.height);
        Rect viewRect = new Rect(0f, 0f, viewWidth, contentHeight);
        Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);

        float curY = 0f;
        foreach (CompHiveSpawner_FleshTrait spawner in spawners)
        {
            float rowHeight = GetSpawnerRowHeight(spawner, viewRect.width);
            DrawSpawnerRow(new Rect(0f, curY, viewRect.width, rowHeight), spawner);
            curY += rowHeight + RowGap;
        }

        Widgets.EndScrollView();
    }

    public void SelectSpawner(CompHiveSpawner_FleshTrait spawner)
    {
        selectedSpawner = spawner;
        selectedUnit = null;
        selectedFormula = null;
        selectedReservedGroup = null;
        focusSelectedSpawner = true;
        SetRepeatCount(1);
    }

    private void ValidateSelection(List<CompHiveSpawner_FleshTrait> spawners)
    {
        if (selectedSpawner == null || !spawners.Contains(selectedSpawner))
        {
            ClearSelection();
            return;
        }

        if (selectedFormula != null)
        {
            if (selectedUnit != selectedFormula.unit || !GetAvailableFormulas(selectedSpawner).Contains(selectedFormula))
            {
                ClearSelection();
                return;
            }
        }
        else if (selectedUnit != null && !GetAvailableUnits(selectedSpawner).Contains(selectedUnit))
        {
            ClearSelection();
            return;
        }

        if (selectedReservedGroup != null && !GetAvailableReservedGroups(selectedSpawner).Contains(selectedReservedGroup))
        {
            selectedReservedGroup = null;
        }
    }

    private void FocusSelectedSpawner(List<CompHiveSpawner_FleshTrait> spawners, float rowWidth, float outRectHeight)
    {
        if (!focusSelectedSpawner || selectedSpawner == null)
        {
            return;
        }

        float selectedY = 0f;
        foreach (CompHiveSpawner_FleshTrait spawner in spawners)
        {
            if (spawner == selectedSpawner)
            {
                scrollPosition.y = Mathf.Clamp(selectedY, 0f, Mathf.Max(0f, contentHeight - outRectHeight));
                focusSelectedSpawner = false;
                return;
            }

            selectedY += GetSpawnerRowHeight(spawner, rowWidth) + RowGap;
        }

        focusSelectedSpawner = false;
    }

    private void DrawSelectedUnit(Rect rect, CompHiveSpawner_FleshTrait spawner, UnitDef unit, Formula? formula)
    {
        Widgets.DrawBoxSolid(rect, SelectedUnitBackgroundColor);

        Rect iconRect = new Rect(rect.x + 10f, rect.y + 10f, 112f, 112f);
        DrawBox(iconRect);
        Widgets.DefIcon(iconRect.ContractedBy(8f), unit.kind);

        Rect actionRect = new Rect(rect.xMax - ActionPanelWidth - 10f, rect.y + 10f, ActionPanelWidth, rect.height - 20f);
        Rect detailsRect = new Rect(iconRect.xMax + 14f, rect.y + 8f, actionRect.x - iconRect.xMax - 24f, rect.height - 16f);

        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(detailsRect.x, detailsRect.y, detailsRect.width, 32f),
            formula?.name.NullOrEmpty() == false ? formula.name : unit.LabelCap.ToString());
        Text.Font = GameFont.Small;

        string cost = formula == null ? BuildCostLine(unit) : BuildCostLine(formula);
        Rect costRect = new Rect(detailsRect.x, detailsRect.y + 36f, detailsRect.width, 40f);
        Widgets.Label(costRect, "FH_Gestation_Cost".Translate(cost));
        TooltipHandler.TipRegion(costRect, cost);

        // FloatRange actualTime = new FloatRange(
        //     unit.spawningDay.min / CompHiveSpawner_FleshTrait.FleshbeastProductionSpeedFactor,
        //     unit.spawningDay.max / CompHiveSpawner_FleshTrait.FleshbeastProductionSpeedFactor);
        FloatRange actualTime = new FloatRange(
            unit.spawningDay.min,
            unit.spawningDay.max);
        Widgets.Label(new Rect(detailsRect.x, detailsRect.y + 78f, detailsRect.width, 24f),
            "FH_Gestation_Time".Translate(FormatFloatRange(unit.spawningDay), FormatFloatRange(actualTime)));
        int groupCost = unit.kind.race.GetCompProperties<UnitCompProperties>()?.groupCost ?? 0;
        Widgets.Label(new Rect(detailsRect.x, detailsRect.y + 102f, detailsRect.width * 0.25f, 24f),
            "FH_Gestation_GroupCost".Translate(groupCost));

        float marketValue = unit.kind.race.GetStatValueAbstract(StatDefOf.MarketValue);
        Widgets.Label(new Rect(detailsRect.x + detailsRect.width * 0.25f, detailsRect.y + 102f, detailsRect.width * 0.75f, 24f),
            "FH_Gestation_Value".Translate(marketValue.ToStringMoney()));

        DrawQuantityControls(actionRect, spawner, unit, formula);
        DrawLineHorizontal(rect.x, rect.yMax - 1f, rect.width);
    }

    private void DrawQuantityControls(Rect rect, CompHiveSpawner_FleshTrait spawner, UnitDef unit, Formula? formula)
    {
        Widgets.Label(new Rect(rect.x, rect.y, rect.width, 24f), "FH_Gestation_Repeat".Translate());

        float curX = rect.x;
        Rect minusTenRect = new Rect(curX, rect.y + 28f, 43f, 30f);
        curX = minusTenRect.xMax + 3f;
        Rect minusOneRect = new Rect(curX, minusTenRect.y, 39f, 30f);
        curX = minusOneRect.xMax + 3f;
        Rect inputRect = new Rect(curX, minusTenRect.y, 68f, 30f);
        curX = inputRect.xMax + 3f;
        Rect plusOneRect = new Rect(curX, minusTenRect.y, 39f, 30f);
        curX = plusOneRect.xMax + 3f;
        Rect plusTenRect = new Rect(curX, minusTenRect.y, 43f, 30f);

        if (Widgets.ButtonText(minusTenRect, "-10"))
        {
            SetRepeatCount(repeatCount - 10);
        }
        if (Widgets.ButtonText(minusOneRect, "-1"))
        {
            SetRepeatCount(repeatCount - 1);
        }

        string newBuffer = Widgets.TextField(inputRect, repeatBuffer);
        if (newBuffer != repeatBuffer)
        {
            repeatBuffer = newBuffer;
            if (int.TryParse(repeatBuffer, out int parsed))
            {
                repeatCount = Mathf.Clamp(parsed, MinRepeatCount, MaxRepeatCount);
            }
        }

        if (Widgets.ButtonText(plusOneRect, "+1"))
        {
            SetRepeatCount(repeatCount + 1);
        }
        if (Widgets.ButtonText(plusTenRect, "+10"))
        {
            SetRepeatCount(repeatCount + 10);
        }

        DrawReservedGroupSelector(new Rect(rect.x, rect.y + 64f, rect.width, 32f), spawner);

        AcceptanceReport report = formula == null ? unit.CanProduce(spawner) : CanProduceFormula(spawner, formula);
        bool active = report.Accepted && repeatCount >= MinRepeatCount;
        Rect buttonRect = new Rect(rect.x, rect.yMax - 34f, rect.width, 34f);
        if (!active && !report.Reason.NullOrEmpty())
        {
            TooltipHandler.TipRegion(buttonRect, report.Reason);
        }

        if (Widgets.ButtonText(buttonRect, "FH_Gestation_Start".Translate(), true, true,
                active ? ColorLibrary.SkyBlue : Color.grey))
        {
            if (!active)
            {
                string reason = report.Reason.NullOrEmpty()
                    ? "FH_Gestation_UnknownReason".Translate().ToString()
                    : report.Reason;
                Messages.Message(reason, MessageTypeDefOf.RejectInput, false);
                return;
            }

            if (formula == null)
            {
                AddGestationTasks(spawner, unit, repeatCount, selectedReservedGroup);
            }
            else
            {
                AddFormulaTasks(spawner, formula, repeatCount, selectedReservedGroup);
            }
        }
    }

    private void DrawReservedGroupSelector(Rect rect, CompHiveSpawner_FleshTrait spawner)
    {
        string groupLabel = selectedReservedGroup?.RenamableLabel ?? "FH_Gestation_ReservedGroupNone".Translate().ToString();
        if (Widgets.ButtonText(rect, "FH_Gestation_ReservedGroup".Translate(groupLabel), false, true, Color.white))
        {
            ShowReservedGroupMenu(spawner);
        }
        TooltipHandler.TipRegion(rect, "FH_Gestation_ReservedGroupTip".Translate());
    }

    private void ShowReservedGroupMenu(CompHiveSpawner_FleshTrait spawner)
    {
        List<FloatMenuOption> options = new List<FloatMenuOption>
        {
            new FloatMenuOption("FH_Gestation_ReservedGroupNone".Translate(), () => selectedReservedGroup = null)
        };

        foreach (UnitGroup group in GetAvailableReservedGroups(spawner))
        {
            UnitGroup selectedGroup = group;
            options.Add(new FloatMenuOption(selectedGroup.RenamableLabel, () => selectedReservedGroup = selectedGroup));
        }

        if (options.Count == 1)
        {
            options.Add(new FloatMenuOption("FH_Gestation_ReservedGroupUnavailable".Translate(), null));
        }

        Find.WindowStack.Add(new FloatMenu(options));
    }

    private List<UnitGroup> GetAvailableReservedGroups(CompHiveSpawner_FleshTrait spawner)
    {
        Map map = spawner?.parent?.Map;
        return GameComponent_UnitGroup.Instance?.groups?
            .Where(group => group != null && group.Show && HCFGameUtility.GroupOnMap(group, map))
            .OrderBy(group => group.RenamableLabel)
            .ToList() ?? new List<UnitGroup>();
    }

    private void DrawNoSpawners(Rect rect)
    {
        Text.Anchor = TextAnchor.MiddleCenter;
        GUI.color = Color.grey;
        Widgets.Label(rect, "FH_Gestation_NoSpawners".Translate());
        GUI.color = Color.white;
        Text.Anchor = TextAnchor.UpperLeft;
    }

    private void DrawSpawnerRow(Rect rect, CompHiveSpawner_FleshTrait spawner)
    {
        bool selected = selectedSpawner == spawner;
        Widgets.DrawBoxSolid(rect, SpawnerRowBackgroundColor);
        if (selected)
        {
            Widgets.DrawHighlightSelected(rect);
        }

        Rect iconRect = new Rect(rect.x + 10f, rect.y + 12f, SpawnerIconSize, SpawnerIconSize);
        DrawBox(iconRect);
        Widgets.ThingIcon(iconRect.ContractedBy(7f), spawner.parent);

        float detailsWidth = GetSpawnerDetailsWidth(rect.width);
        Rect labelRect = new Rect(iconRect.xMax + 14f, rect.y + 14f, detailsWidth - SpawnerIconSize - 34f, 30f);
        Text.Font = GameFont.Medium;
        Widgets.Label(labelRect, spawner.parent.LabelCap);
        Text.Font = GameFont.Small;

        CompHiveGroup groupComp = spawner.parent.TryGetComp<CompHiveGroup>();
        int groupCost = groupComp?.groups?.Sum(group => group?.unitCost ?? 0) ?? 0;
        int groupLimit = groupComp?.groups?.Sum(group => group?.UnitLimit ?? 0) ?? 0;
        Widgets.Label(new Rect(labelRect.x, labelRect.yMax + 2f, labelRect.width, 24f),
            "FH_Gestation_NodeCapacity".Translate(groupCost, groupLimit));

        Rect addBillRect = new Rect(labelRect.x, labelRect.yMax + 30f, 150f, 32f);
        if (Widgets.ButtonText(addBillRect, "FH_Gestation_AddBill".Translate()))
        {
            ShowUnitMenu(spawner);
        }

        Rect tasksRect = new Rect(rect.x + detailsWidth, rect.y + 12f, rect.width - detailsWidth - 10f, rect.height - 24f);
        DrawTasks(tasksRect, spawner.ProgressHolder);
        DrawLineHorizontal(rect.x, rect.yMax - 1f, rect.width);
        if (selected)
        {
            Color color = GUI.color;
            GUI.color = SelectedProducerBorderColor;
            Widgets.DrawBox(rect.ContractedBy(1f), 3);
            GUI.color = color;
        }
    }

    private void DrawTasks(Rect rect, CompProgressHolder holder)
    {
        if (holder?.progresses == null)
        {
            return;
        }

        List<Progress> progresses = holder.progresses
            .Where(progress => GetProgressUnitDef(progress)?.kind != null)
            .ToList();
        int columns = GetTaskColumns(rect.width);
        Progress? progressToCancel = null;

        for (int i = 0; i < progresses.Count; i++)
        {
            int column = i % columns;
            int row = i / columns;
            Rect taskRect = new Rect(
                rect.x + column * (TaskWidth + TaskGap),
                rect.y + row * (TaskHeight + TaskGap),
                TaskWidth,
                TaskHeight);
            if (DrawTask(taskRect, progresses[i]))
            {
                progressToCancel = progresses[i];
            }
        }

        if (progressToCancel != null)
        {
            progressToCancel.Cancel(holder);
            holder.progresses.Remove(progressToCancel);
        }
    }

    private void DrawFormulaToolbar(Rect rect, List<CompHiveSpawner_FleshTrait> spawners)
    {
        CompHiveSpawner_FleshTrait? formulaTarget = GetFormulaTarget(spawners);
        Rect buttonRect = new Rect(rect.x + (rect.width - 170f) / 2f, rect.y + 3f, 170f, 32f);
        bool active = formulaTarget != null;
        if (Widgets.ButtonText(buttonRect, "FH_Gestation_CustomFormula".Translate(), false, true,
                active ? ColorLibrary.SkyBlue : Color.grey, active) && formulaTarget != null)
        {
            selectedSpawner = formulaTarget;
            selectedUnit = null;
            selectedFormula = null;
            Find.WindowStack.Add(new Window_FleshHiveFormula(formulaTarget.parent));
        }

        DrawLineHorizontal(rect.x, rect.yMax - 1f, rect.width);
    }

    private CompHiveSpawner_FleshTrait? GetFormulaTarget(List<CompHiveSpawner_FleshTrait> spawners)
    {
        if (selectedSpawner != null && spawners.Contains(selectedSpawner)
            && selectedSpawner.parent.TryGetComp<CompHiveFormulaSpawner>() != null)
        {
            return selectedSpawner;
        }

        return spawners
            .Where(spawner => spawner.parent.TryGetComp<CompHiveFormulaSpawner>() != null)
            .OrderByDescending(spawner => spawner.parent.def == FleshHiveDefOf.FH_FleshPrimaryNest)
            .ThenByDescending(spawner => GetAvailableUnits(spawner).Count)
            .FirstOrDefault();
    }

    private bool DrawTask(Rect rect, Progress progress)
    {
        UnitDef unit = GetProgressUnitDef(progress)!;
        DrawBox(rect);
        Rect iconRect = new Rect(rect.x + 5f, rect.y + 5f, rect.width - 10f, 58f);
        Widgets.DefIcon(iconRect, unit.kind);

        Rect closeRect = new Rect(rect.xMax - 19f, rect.y + 3f, 16f, 16f);
        bool cancel = Widgets.ButtonImage(closeRect, TexButton.CloseXSmall, true, "FH_Gestation_CancelTask".Translate());

        int groupCost = unit.kind.race.GetCompProperties<UnitCompProperties>()?.groupCost ?? 0;
        DrawGroupCost(new Rect(rect.x + 5f, rect.y + 58f, rect.width - 10f, 7f), groupCost);

        Rect barRect = new Rect(rect.x + 4f, rect.yMax - 20f, rect.width - 8f, 16f);
        float fillPercent = progress.totalTime > 0f ? Mathf.Clamp01(1f - progress.time / progress.totalTime) : 0f;
        Widgets.FillableBar(barRect, fillPercent, ProgressBarTex, EmptyBarTex, true);
        Text.Font = GameFont.Tiny;
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(barRect, fillPercent.ToStringPercent("0"));
        Text.Anchor = TextAnchor.UpperLeft;
        Text.Font = GameFont.Small;

        TooltipHandler.TipRegion(rect, progress.Label + "\n" + progress.Tooltip);
        return cancel;
    }

    private UnitDef? GetProgressUnitDef(Progress? progress)
    {
        if (progress is UnitSpawnData_FleshTrait unitProgress)
        {
            return unitProgress.Def;
        }
        return progress is FormulaProgress formulaProgress ? formulaProgress.formula?.unit : null;
    }

    private void DrawGroupCost(Rect rect, int cost)
    {
        float x = rect.x;
        for (int i = 0; i < cost; i++)
        {
            Widgets.DrawBoxSolid(new Rect(x, rect.y, 7f, 7f), GroupCostColor);
            x += 9f;
            if (x + 7f > rect.xMax)
            {
                break;
            }
        }
    }

    private void DrawQuotaPanel(Rect rect, MapComponent_FleshHive mapComp)
    {
        Widgets.DrawMenuSection(rect);
        Rect innerRect = rect.ContractedBy(10f);
        Widgets.DrawBoxSolid(new Rect(innerRect.x, innerRect.y, innerRect.width, QuotaHeaderHeight), QuotaHeaderColor);

        float unitWidth = innerRect.width - CurrentColumnWidth - MaintainColumnWidth - MaximumColumnWidth;
        Rect unitHeaderRect = new Rect(innerRect.x, innerRect.y, unitWidth, QuotaHeaderHeight);
        Rect currentHeaderRect = new Rect(innerRect.x + unitWidth, innerRect.y, CurrentColumnWidth, QuotaHeaderHeight);
        Rect maintainHeaderRect = new Rect(innerRect.x + unitWidth + CurrentColumnWidth, innerRect.y, MaintainColumnWidth, QuotaHeaderHeight);
        Rect maximumHeaderRect = new Rect(innerRect.x + unitWidth + CurrentColumnWidth + MaintainColumnWidth, innerRect.y, MaximumColumnWidth, QuotaHeaderHeight);
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(unitHeaderRect, "FH_Gestation_QuotaUnit".Translate());
        Widgets.Label(currentHeaderRect, "FH_Gestation_QuotaCurrent".Translate());
        Widgets.Label(maintainHeaderRect, "FH_Gestation_QuotaMaintain".Translate());
        Widgets.Label(maximumHeaderRect, "FH_Gestation_QuotaMaximum".Translate());
        Text.Anchor = TextAnchor.UpperLeft;
        TooltipHandler.TipRegion(unitHeaderRect, "FH_Gestation_QuotaUnitTip".Translate());
        TooltipHandler.TipRegion(currentHeaderRect, "FH_Gestation_QuotaCurrentTip".Translate());
        TooltipHandler.TipRegion(maintainHeaderRect, "FH_Gestation_QuotaMaintainTip".Translate());
        TooltipHandler.TipRegion(maximumHeaderRect, "FH_Gestation_QuotaMaximumTip".Translate());

        List<UnitDef> units = mapComp.GetAvailableFleshbeastUnits();
        Rect outRect = new Rect(innerRect.x, innerRect.y + QuotaHeaderHeight + 4f, innerRect.width, innerRect.height - QuotaHeaderHeight - 8f);
        float viewHeight = Mathf.Max(outRect.height + 1f, units.Count * QuotaRowHeight);
        Rect viewRect = new Rect(0f, 0f, outRect.width - ScrollbarWidth, viewHeight);
        Widgets.BeginScrollView(outRect, ref quotaScrollPosition, viewRect);

        for (int i = 0; i < units.Count; i++)
        {
            DrawQuotaRow(new Rect(0f, i * QuotaRowHeight, viewRect.width, QuotaRowHeight), mapComp, units[i]);
        }

        Widgets.EndScrollView();
    }

    private void DrawQuotaRow(Rect rect, MapComponent_FleshHive mapComp, UnitDef unit)
    {
        if ((Mathf.FloorToInt(rect.y / QuotaRowHeight) & 1) == 1)
        {
            Widgets.DrawAltRect(rect);
        }

        float unitWidth = rect.width - CurrentColumnWidth - MaintainColumnWidth - MaximumColumnWidth;
        Rect iconRect = new Rect(rect.x + 3f, rect.y + 3f, QuotaRowHeight - 6f, QuotaRowHeight - 6f);
        Widgets.DefIcon(iconRect, unit.kind);
        Rect labelRect = new Rect(iconRect.xMax + 3f, rect.y, unitWidth - iconRect.width - 6f, rect.height);
        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(labelRect, unit.LabelCap);
        Text.Anchor = TextAnchor.MiddleCenter;

        Rect currentRect = new Rect(rect.x + unitWidth, rect.y, CurrentColumnWidth, rect.height);
        Widgets.Label(currentRect, mapComp.GetCurrentUnitCount(unit).ToString());

        int maintain = mapComp.GetUnitMaintainTarget(unit);
        string maintainBuffer = GetMaintainBuffer(unit, maintain);
        Rect maintainRect = new Rect(currentRect.xMax + 4f, rect.y + 4f, MaintainColumnWidth - 8f, rect.height - 8f);
        Widgets.TextFieldNumeric(maintainRect, ref maintain, ref maintainBuffer, 0, MaxQuotaValue);
        maintainBuffers[unit] = maintainBuffer;
        if (maintain != mapComp.GetUnitMaintainTarget(unit))
        {
            mapComp.SetUnitMaintainTarget(unit, maintain);
            int storedMaintain = mapComp.GetUnitMaintainTarget(unit);
            if (storedMaintain != maintain)
            {
                maintainBuffers[unit] = storedMaintain.ToString();
            }
        }

        int maximum = mapComp.GetUnitMaximumTarget(unit);
        Rect maximumRect = new Rect(maintainRect.xMax + 4f, rect.y + 4f, MaximumColumnWidth - 8f, rect.height - 8f);
        if (maximum < 0)
        {
            if (Widgets.ButtonText(maximumRect, "\u221E"))
            {
                mapComp.SetUnitMaximumTarget(unit, Mathf.Max(mapComp.GetCurrentUnitCount(unit), maintain));
            }
            TooltipHandler.TipRegion(maximumRect, "FH_Gestation_SetFiniteMaximum".Translate());
        }
        else
        {
            Rect maximumInputRect = new Rect(maximumRect.x, maximumRect.y, maximumRect.width - 27f, maximumRect.height);
            Rect infiniteRect = new Rect(maximumInputRect.xMax + 3f, maximumRect.y, 24f, maximumRect.height);
            string maximumBuffer = GetMaximumBuffer(unit, maximum);
            Widgets.TextFieldNumeric(maximumInputRect, ref maximum, ref maximumBuffer, 0, MaxQuotaValue);
            maximumBuffers[unit] = maximumBuffer;
            if (maximum != mapComp.GetUnitMaximumTarget(unit))
            {
                mapComp.SetUnitMaximumTarget(unit, maximum);
                int storedMaintain = mapComp.GetUnitMaintainTarget(unit);
                maintainBuffers[unit] = storedMaintain.ToString();
            }
            if (Widgets.ButtonText(infiniteRect, "\u221E"))
            {
                mapComp.SetUnitMaximumTarget(unit, -1);
                maximumBuffers.Remove(unit);
            }
            TooltipHandler.TipRegion(infiniteRect, "FH_Gestation_SetInfiniteMaximum".Translate());
        }

        Text.Anchor = TextAnchor.UpperLeft;
        TooltipHandler.TipRegion(rect, unit.description.NullOrEmpty() ? unit.kind.race.description : unit.description);
    }

    private void ShowUnitMenu(CompHiveSpawner_FleshTrait spawner)
    {
        List<FloatMenuOption> options = GetAvailableUnits(spawner)
            .Select(unit =>
            {
                FloatMenuOption option = new FloatMenuOption(
                    unit.LabelCap.ToString(),
                    () => SelectUnit(spawner, unit),
                    unit.kind.race,
                    extraPartWidth: 29f,
                    extraPartOnGUI: rect => Widgets.InfoCardButton(
                        rect.x + 5f,
                        rect.y + (rect.height - 24f) / 2f,
                        unit.kind.race));
                option.extraPartRightJustified = true;
                return option;
            })
            .ToList();
        options.AddRange(GetAvailableFormulas(spawner)
            .Select(formula =>
            {
                FloatMenuOption option = new FloatMenuOption(
                    "FH_Gestation_CustomFormulaMenuEntry".Translate(formula.name.NullOrEmpty() ? formula.unit.LabelCap : formula.name),
                    () => SelectFormula(spawner, formula),
                    formula.unit.kind.race,
                    extraPartWidth: 29f,
                    extraPartOnGUI: rect => Widgets.InfoCardButton(
                        rect.x + 5f,
                        rect.y + (rect.height - 24f) / 2f,
                        formula.unit.kind.race));
                option.extraPartRightJustified = true;
                return option;
            }));
        if (options.Count == 0)
        {
            options.Add(new FloatMenuOption("FH_Gestation_NoAvailableUnits".Translate(), null));
        }

        Find.WindowStack.Add(new FloatMenu(options));
    }

    private void SelectUnit(CompHiveSpawner_FleshTrait spawner, UnitDef unit)
    {
        selectedSpawner = spawner;
        selectedUnit = unit;
        selectedFormula = null;
        selectedReservedGroup = null;
        SetRepeatCount(1);
    }

    private void SelectFormula(CompHiveSpawner_FleshTrait spawner, Formula formula)
    {
        selectedSpawner = spawner;
        selectedUnit = formula.unit;
        selectedFormula = formula;
        selectedReservedGroup = null;
        SetRepeatCount(1);
    }

    private void ClearSelection()
    {
        selectedSpawner = null;
        selectedUnit = null;
        selectedFormula = null;
        selectedReservedGroup = null;
        SetRepeatCount(1);
    }

    private void SetRepeatCount(int value)
    {
        repeatCount = Mathf.Clamp(value, MinRepeatCount, MaxRepeatCount);
        repeatBuffer = repeatCount.ToString();
    }

    private void AddGestationTasks(CompHiveSpawner_FleshTrait spawner, UnitDef unit, int requestedCount, UnitGroup reservedGroup)
    {
        int addedCount = 0;
        AcceptanceReport lastReport = true;
        while (addedCount < requestedCount)
        {
            lastReport = spawner.TryStartUnitProduction(unit, null, true, reservedGroup);
            if (!lastReport.Accepted)
            {
                break;
            }

            addedCount++;
        }

        if (addedCount < requestedCount)
        {
            string reason = lastReport.Reason.NullOrEmpty()
                ? "FH_Gestation_UnknownReason".Translate().ToString()
                : lastReport.Reason;
            Messages.Message("FH_Gestation_PartialQueue".Translate(addedCount, requestedCount, reason), MessageTypeDefOf.RejectInput, false);
        }
    }

    private void AddFormulaTasks(CompHiveSpawner_FleshTrait spawner, Formula formula, int requestedCount, UnitGroup reservedGroup)
    {
        int addedCount = 0;
        AcceptanceReport lastReport = true;
        while (addedCount < requestedCount)
        {
            lastReport = TryStartFormulaProduction(spawner, formula, reservedGroup);
            if (!lastReport.Accepted)
            {
                break;
            }

            addedCount++;
        }

        if (addedCount < requestedCount)
        {
            string reason = lastReport.Reason.NullOrEmpty()
                ? "FH_Gestation_UnknownReason".Translate().ToString()
                : lastReport.Reason;
            Messages.Message("FH_Gestation_PartialQueue".Translate(addedCount, requestedCount, reason), MessageTypeDefOf.RejectInput, false);
        }
    }

    private AcceptanceReport TryStartFormulaProduction(CompHiveSpawner_FleshTrait spawner, Formula formula, UnitGroup reservedGroup)
    {
        AcceptanceReport report = CanProduceFormula(spawner, formula);
        if (!report.Accepted)
        {
            return report;
        }

        CompHiveFormulaSpawner formulaSpawner = spawner.parent.TryGetComp<CompHiveFormulaSpawner>();
        foreach (ResourceCount cost in formula.cacheCosts)
        {
            formulaSpawner.Resource.ConsumeResource(cost);
        }
        formulaSpawner.Resource.ConsumeRequiredItems(formula.cacheRequirements);

        FormulaProgress_FleshTrait progress = new FormulaProgress_FleshTrait
        {
            time = formula.unit.spawningDay.RandomInRange * GenDate.TicksPerDay,
            formula = formula,
            ReservedGroup = reservedGroup
        };
        progress.totalTime = progress.time;
        formulaSpawner.ProgressHolder.progresses.Add(progress);
        spawner.SendProgressAddedMessage(progress);
        return true;
    }

    private AcceptanceReport CanProduceFormula(CompHiveSpawner_FleshTrait spawner, Formula formula)
    {
        CompHiveFormulaSpawner? formulaSpawner = spawner.parent.TryGetComp<CompHiveFormulaSpawner>();
        if (formulaSpawner == null || !CanAcceptFormula(spawner, formulaSpawner, formula))
        {
            return "FH_Gestation_CustomFormulaUnavailable".Translate();
        }
        if (!formula.IsUnlocked(formulaSpawner.Resource))
        {
            return "FH_Gestation_CustomFormulaInsufficientResources".Translate();
        }
        return true;
    }

    private bool CanAcceptFormula(CompHiveSpawner_FleshTrait spawner, CompHiveFormulaSpawner formulaSpawner, Formula formula)
    {
        return formula?.unit?.kind != null
            && GetAvailableUnits(spawner).Contains(formula.unit)
            && formula.IsAvailable(spawner, formulaSpawner);
    }

    private List<UnitDef> GetAvailableUnits(CompHiveSpawner_FleshTrait spawner)
    {
        if (spawner == null)
        {
            return [];
        }

        return spawner.GetUnitCategories()
            .Where(category => category != null && spawner.CanShowUnitCategory(category))
            .SelectMany(category => category.GetUnits(spawner))
            .Where(unit => unit?.kind != null)
            .Distinct()
            .OrderBy(unit => unit.LabelCap.ToString())
            .ToList();
    }

    private List<Formula> GetAvailableFormulas(CompHiveSpawner_FleshTrait spawner)
    {
        CompHiveFormulaSpawner? formulaSpawner = spawner?.parent.TryGetComp<CompHiveFormulaSpawner>();
        if (formulaSpawner == null)
        {
            return [];
        }

        return GameComponent_UnitGroup.Instance.formulas
            .Where(formula => CanAcceptFormula(spawner, formulaSpawner, formula))
            .OrderBy(formula => formula.name.NullOrEmpty() ? formula.unit.LabelCap.ToString() : formula.name)
            .ToList();
    }

    private float GetSpawnerRowHeight(CompHiveSpawner_FleshTrait spawner, float rowWidth)
    {
        int taskCount = spawner.ProgressHolder.progresses.Count(progress => GetProgressUnitDef(progress)?.kind != null);
        float detailsWidth = GetSpawnerDetailsWidth(rowWidth);
        float taskAreaWidth = Mathf.Max(TaskWidth, rowWidth - detailsWidth - 10f);
        int rows = Mathf.Max(1, Mathf.CeilToInt(taskCount / (float)GetTaskColumns(taskAreaWidth)));
        return Mathf.Max(MinRowHeight, 24f + rows * TaskHeight + (rows - 1) * TaskGap);
    }

    private int GetTaskColumns(float width)
    {
        return Mathf.Max(1, Mathf.FloorToInt((width + TaskGap) / (TaskWidth + TaskGap)));
    }

    private float GetQuotaPanelWidth(float totalWidth)
    {
        return Mathf.Clamp(totalWidth * QuotaPanelWidthFactor, QuotaPanelWidthMin, QuotaPanelWidthMax);
    }

    private float GetSpawnerDetailsWidth(float rowWidth)
    {
        return Mathf.Clamp(rowWidth * SpawnerDetailsWidthFactor, SpawnerDetailsWidthMin, SpawnerDetailsWidthMax);
    }

    private string GetMaintainBuffer(UnitDef unit, int value)
    {
        if (!maintainBuffers.TryGetValue(unit, out string buffer))
        {
            buffer = value.ToString();
            maintainBuffers[unit] = buffer;
        }
        return buffer;
    }

    private string GetMaximumBuffer(UnitDef unit, int value)
    {
        if (!maximumBuffers.TryGetValue(unit, out string buffer))
        {
            buffer = value.ToString();
            maximumBuffers[unit] = buffer;
        }
        return buffer;
    }

    private string BuildCostLine(UnitDef unit)
    {
        List<string> parts = new();
        if (!unit.costs.NullOrEmpty())
        {
            parts.AddRange(unit.costs.Select(cost => cost.resource.LabelCap + " x" + cost.amount.ToString("0.##"))
                .Select(label => label.ToString()));
        }
        if (!unit.requirements.NullOrEmpty())
        {
            parts.AddRange(unit.requirements.Select(requirement => requirement.Label.ToString()));
        }
        if (!unit.specialResources.NullOrEmpty())
        {
            parts.AddRange(unit.specialResources.Select(special => special.Label.ToString()));
        }
        return parts.Count > 0 ? string.Join(" | ", parts) : "FH_Gestation_NoCost".Translate().ToString();
    }

    private string BuildCostLine(Formula formula)
    {
        List<string> parts = new();
        if (!formula.cacheCosts.NullOrEmpty())
        {
            parts.AddRange(formula.cacheCosts.Select(cost => cost.resource.LabelCap + " x" + cost.amount.ToString("0.##"))
                .Select(label => label.ToString()));
        }
        if (!formula.cacheRequirements.NullOrEmpty())
        {
            parts.AddRange(formula.cacheRequirements.Select(requirement => requirement.Label.ToString()));
        }
        return parts.Count > 0 ? string.Join(" | ", parts) : "FH_Gestation_NoCost".Translate().ToString();
    }

    private string FormatFloatRange(FloatRange range)
    {
        string min = range.min.ToString("0.##");
        string max = range.max.ToString("0.##");
        return range.min == range.max ? min : "FH_Gestation_Range".Translate(min, max).ToString();
    }

    private void DrawBox(Rect rect)
    {
        Color color = GUI.color;
        GUI.color = LineColor;
        Widgets.DrawBox(rect);
        GUI.color = color;
    }

    private void DrawLineHorizontal(float x, float y, float length)
    {
        Widgets.DrawBoxSolid(new Rect(x, y, length, 1f), LineColor);
    }

    private Vector2 scrollPosition;
    private Vector2 quotaScrollPosition;
    private float contentHeight = 1f;
    private CompHiveSpawner_FleshTrait? selectedSpawner;
    private UnitDef? selectedUnit;
    private Formula? selectedFormula;
    private UnitGroup? selectedReservedGroup;
    private bool focusSelectedSpawner;
    private int repeatCount = 1;
    private string repeatBuffer = "1";
    private readonly Dictionary<UnitDef, string> maintainBuffers = new();
    private readonly Dictionary<UnitDef, string> maximumBuffers = new();
    private Texture2D? progressBarTex;
    private Texture2D? emptyBarTex;

    private const int MinRepeatCount = 1;
    private const int MaxRepeatCount = 999;
    private const int MaxQuotaValue = 999;
    private const float QuotaPanelWidthFactor = 0.28f;
    private const float QuotaPanelWidthMin = 300f;
    private const float QuotaPanelWidthMax = 330f;
    private const float PanelGap = 10f;
    private const float FormulaToolbarHeight = 40f;
    private const float SelectedUnitHeight = 180f;
    private const float ActionPanelWidth = 245f;
    private const float SpawnerDetailsWidthFactor = 0.38f;
    private const float SpawnerDetailsWidthMin = 280f;
    private const float SpawnerDetailsWidthMax = 320f;
    private const float SpawnerIconSize = 96f;
    private const float MinRowHeight = 132f;
    private const float RowGap = 8f;
    private const float TaskWidth = 78f;
    private const float TaskHeight = 88f;
    private const float TaskGap = 8f;
    private const float ScrollbarWidth = 16f;
    private const float QuotaHeaderHeight = 36f;
    private const float QuotaRowHeight = 36f;
    private const float CurrentColumnWidth = 45f;
    private const float MaintainColumnWidth = 67f;
    private const float MaximumColumnWidth = 82f;
    private static readonly Color SelectedUnitBackgroundColor = new Color(0f, 0f, 0f, 0.12f);
    private static readonly Color SpawnerRowBackgroundColor = new Color(0f, 0f, 0f, 0.15f);
    private static readonly Color QuotaHeaderColor = new Color(0.1f, 0.12f, 0.14f, 0.85f);
    private static readonly Color ProgressBarColor = new Color(0.55f, 0.28f, 0.25f);
    private static readonly Color EmptyBarColor = new Color(0f, 0f, 0f, 0.65f);
    private static readonly Color GroupCostColor = Color.red;
    private static readonly Color SelectedProducerBorderColor = new Color(0.2f, 0.75f, 0.85f, 0.95f);
    private static readonly Color LineColor = new Color(0.42f, 0.47f, 0.5f, 0.55f);
}
