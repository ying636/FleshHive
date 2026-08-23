using System.Collections.Generic;
using System.Linq;
using HiveCreatureFramework;
using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive;

public class HiveTabOption_ItemProduction : HiveTabOption_FleshHive
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
        if (map == null)
        {
            return;
        }

        List<Building_FleshHopper> hoppers = FleshHopperUtility.GetCachedHoppers(map)
            .OrderBy(hopper => hopper.Position.z)
            .ThenBy(hopper => hopper.Position.x)
            .ToList();
        ValidateSelection(hoppers);

        float headerHeight = selectedItem != null && selectedHopper != null ? SelectedItemHeight : 0f;
        if (selectedItem != null && selectedHopper != null)
        {
            DrawSelectedItem(new Rect(inRect.x, inRect.y, inRect.width - 10f, SelectedItemHeight), selectedHopper, selectedItem);
        }

        Rect outRect = new Rect(inRect.x, inRect.y + headerHeight, inRect.width - 10f, inRect.height - headerHeight);
        if (hoppers.Count == 0)
        {
            DrawNoHoppers(outRect);
            return;
        }

        float viewWidth = outRect.width - ScrollbarWidth;
        float requiredHeight = hoppers.Sum(hopper => GetHopperRowHeight(hopper, viewWidth) + RowGap);
        contentHeight = Mathf.Max(requiredHeight, outRect.height + 1f);
        FocusSelectedProducer(hoppers, viewWidth, outRect.height);
        Rect viewRect = new Rect(0f, 0f, viewWidth, contentHeight);
        Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);

        float curY = 0f;
        foreach (Building_FleshHopper hopper in hoppers)
        {
            float rowHeight = GetHopperRowHeight(hopper, viewRect.width);
            DrawHopperRow(new Rect(0f, curY, viewRect.width, rowHeight), hopper);
            curY += rowHeight + RowGap;
        }

        Widgets.EndScrollView();
    }

    public void SelectProducer(Building_FleshHopper hopper)
    {
        selectedHopper = hopper;
        selectedItem = null;
        focusSelectedHopper = true;
        SetRepeatCount(1);
    }

    private void ValidateSelection(List<Building_FleshHopper> hoppers)
    {
        if (selectedHopper == null || !hoppers.Contains(selectedHopper))
        {
            ClearSelection();
            return;
        }

        if (selectedItem != null && !GetAvailableItems(selectedHopper).Contains(selectedItem))
        {
            selectedItem = null;
            SetRepeatCount(1);
        }
    }

    private void FocusSelectedProducer(List<Building_FleshHopper> hoppers, float rowWidth, float outRectHeight)
    {
        if (!focusSelectedHopper || selectedHopper == null)
        {
            return;
        }

        float selectedY = 0f;
        foreach (Building_FleshHopper hopper in hoppers)
        {
            if (hopper == selectedHopper)
            {
                scrollPosition.y = Mathf.Clamp(selectedY, 0f, Mathf.Max(0f, contentHeight - outRectHeight));
                focusSelectedHopper = false;
                return;
            }

            selectedY += GetHopperRowHeight(hopper, rowWidth) + RowGap;
        }

        focusSelectedHopper = false;
    }

    private void DrawSelectedItem(Rect rect, Building_FleshHopper hopper, ItemDef item)
    {
        Widgets.DrawBoxSolid(rect, SelectedItemBackgroundColor);

        Rect iconRect = new Rect(rect.x + 10f, rect.y + 10f, 112f, 112f);
        DrawBox(iconRect);
        Widgets.DefIcon(iconRect.ContractedBy(8f), item.thing);

        Rect actionRect = new Rect(rect.xMax - ActionPanelWidth - 10f, rect.y + 10f, ActionPanelWidth, rect.height - 20f);
        Rect detailsRect = new Rect(iconRect.xMax + 14f, rect.y + 8f, actionRect.x - iconRect.xMax - 24f, rect.height - 16f);

        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(detailsRect.x, detailsRect.y, detailsRect.width, 32f), item.LabelCap);
        Text.Font = GameFont.Small;

        string cost = BuildCostLine(item);
        Rect costRect = new Rect(detailsRect.x, detailsRect.y + 36f, detailsRect.width, 42f);
        Widgets.Label(costRect, "FH_ItemProduction_Cost".Translate(cost));
        TooltipHandler.TipRegion(costRect, cost);

        Widgets.Label(new Rect(detailsRect.x, detailsRect.y + 80f, detailsRect.width * 0.52f, 24f),
            "FH_ItemProduction_Time".Translate(FormatFloatRange(item.spawningDay)));
        Widgets.Label(new Rect(detailsRect.x, detailsRect.y + 104f, detailsRect.width * 0.52f, 24f),
            "FH_ItemProduction_Output".Translate(FormatIntRange(item.stackCountRange)));

        float marketValue = item.thing.GetStatValueAbstract(StatDefOf.MarketValue) * item.stackCountRange.Average;
        Widgets.Label(new Rect(detailsRect.x + detailsRect.width * 0.52f, detailsRect.y + 104f, detailsRect.width * 0.48f, 24f),
            "FH_ItemProduction_Value".Translate(marketValue.ToStringMoney()));

        DrawQuantityControls(actionRect, hopper, item);
        DrawLineHorizontal(rect.x, rect.yMax - 1f, rect.width);
    }

    private void DrawQuantityControls(Rect rect, Building_FleshHopper hopper, ItemDef item)
    {
        Widgets.Label(new Rect(rect.x, rect.y, rect.width, 24f), "FH_ItemProduction_Repeat".Translate());

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

        CompHiveSpawner spawner = hopper.TryGetComp<CompHiveSpawner>();
        AcceptanceReport report = spawner != null ? item.worker.CanProduce(spawner) : false;
        bool active = spawner != null && report.Accepted && repeatCount >= MinRepeatCount;
        Rect buttonRect = new Rect(rect.x, rect.yMax - 34f, rect.width, 34f);
        if (!active && !report.Reason.NullOrEmpty())
        {
            TooltipHandler.TipRegion(buttonRect, report.Reason);
        }

        if (Widgets.ButtonText(buttonRect, "FH_ItemProduction_Produce".Translate(), true, true,
                active ? ColorLibrary.SkyBlue : Color.grey))
        {
            if (!active)
            {
                string reason = report.Reason.NullOrEmpty()
                    ? "FH_ItemProduction_UnknownReason".Translate().ToString()
                    : report.Reason;
                Messages.Message(reason, MessageTypeDefOf.RejectInput, false);
                return;
            }

            AddProductionTasks(spawner, item, repeatCount);
        }
    }

    private void DrawNoHoppers(Rect rect)
    {
        Text.Anchor = TextAnchor.MiddleCenter;
        GUI.color = Color.grey;
        Widgets.Label(rect, "FH_ItemProduction_NoHoppers".Translate());
        GUI.color = Color.white;
        Text.Anchor = TextAnchor.UpperLeft;
    }

    private void DrawHopperRow(Rect rect, Building_FleshHopper hopper)
    {
        bool selected = selectedHopper == hopper;
        Widgets.DrawBoxSolid(rect, HopperRowBackgroundColor);
        if (selected)
        {
            Widgets.DrawHighlightSelected(rect);
        }

        Rect iconRect = new Rect(rect.x + 10f, rect.y + 12f, HopperIconSize, HopperIconSize);
        DrawBox(iconRect);
        Widgets.ThingIcon(iconRect.ContractedBy(7f), hopper);

        Rect labelRect = new Rect(iconRect.xMax + 14f, rect.y + 14f, HopperDetailsWidth - 24f, 30f);
        Text.Font = GameFont.Medium;
        Widgets.Label(labelRect, hopper.LabelCap);
        Text.Font = GameFont.Small;

        Rect addBillRect = new Rect(labelRect.x, labelRect.yMax + 12f, 150f, 32f);
        if (Widgets.ButtonText(addBillRect, "FH_ItemProduction_AddBill".Translate()))
        {
            ShowItemMenu(hopper);
        }

        Rect tasksRect = new Rect(rect.x + HopperDetailsWidth, rect.y + 12f, rect.width - HopperDetailsWidth - 10f, rect.height - 24f);
        DrawTasks(tasksRect, hopper);
        DrawLineHorizontal(rect.x, rect.yMax - 1f, rect.width);
        if (selected)
        {
            Color color = GUI.color;
            GUI.color = SelectedProducerBorderColor;
            Widgets.DrawBox(rect.ContractedBy(1f), 3);
            GUI.color = color;
        }
    }

    private void DrawTasks(Rect rect, Building_FleshHopper hopper)
    {
        CompProgressHolder holder = hopper.TryGetComp<CompProgressHolder>();
        if (holder?.progresses == null)
        {
            return;
        }

        List<ItemSpawnData> progresses = holder.progresses
            .OfType<ItemSpawnData>()
            .Where(progress => progress?.item?.thing != null)
            .ToList();
        int columns = GetTaskColumns(rect.width);
        ItemSpawnData? progressToCancel = null;

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

    private bool DrawTask(Rect rect, ItemSpawnData progress)
    {
        DrawBox(rect);

        Rect iconRect = new Rect(rect.x + 5f, rect.y + 5f, rect.width - 10f, 58f);
        Widgets.DefIcon(iconRect, progress.item.thing);

        Rect closeRect = new Rect(rect.xMax - 19f, rect.y + 3f, 16f, 16f);
        bool cancel = Widgets.ButtonImage(closeRect, TexButton.CloseXSmall, true, "FH_ItemProduction_CancelTask".Translate());

        Text.Anchor = TextAnchor.LowerRight;
        Widgets.Label(new Rect(rect.x + 4f, rect.y + 44f, rect.width - 8f, 20f), "x" + FormatIntRange(progress.item.stackCountRange));
        Text.Anchor = TextAnchor.UpperLeft;

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

    private void ShowItemMenu(Building_FleshHopper hopper)
    {
        List<FloatMenuOption> options = GetAvailableItems(hopper)
            .Select(item =>
            {
                FloatMenuOption option = new FloatMenuOption(
                    item.LabelCap.ToString(),
                    () => SelectItem(hopper, item),
                    item.thing,
                    extraPartWidth: 29f,
                    extraPartOnGUI: rect => Widgets.InfoCardButton(
                        rect.x + 5f,
                        rect.y + (rect.height - 24f) / 2f,
                        item.thing));
                option.extraPartRightJustified = true;
                return option;
            })
            .ToList();

        if (options.Count == 0)
        {
            options.Add(new FloatMenuOption("FH_ItemProduction_NoAvailableItems".Translate(), null));
        }

        Find.WindowStack.Add(new FloatMenu(options));
    }

    private void SelectItem(Building_FleshHopper hopper, ItemDef item)
    {
        selectedHopper = hopper;
        selectedItem = item;
        SetRepeatCount(1);
    }

    private void ClearSelection()
    {
        selectedHopper = null;
        selectedItem = null;
        SetRepeatCount(1);
    }

    private void SetRepeatCount(int value)
    {
        repeatCount = Mathf.Clamp(value, MinRepeatCount, MaxRepeatCount);
        repeatBuffer = repeatCount.ToString();
    }

    private void AddProductionTasks(CompHiveSpawner spawner, ItemDef item, int requestedCount)
    {
        int addedCount = 0;
        AcceptanceReport lastReport = true;
        while (addedCount < requestedCount)
        {
            lastReport = item.worker.CanProduce(spawner);
            if (!lastReport.Accepted)
            {
                break;
            }

            item.worker.AddProgress(spawner);
            addedCount++;
        }

        if (addedCount < requestedCount)
        {
            string reason = lastReport.Reason.NullOrEmpty()
                ? "FH_ItemProduction_UnknownReason".Translate().ToString()
                : lastReport.Reason;
            Messages.Message("FH_ItemProduction_PartialQueue".Translate(addedCount, requestedCount, reason), MessageTypeDefOf.RejectInput, false);
        }
    }

    private List<ItemDef> GetAvailableItems(Building_FleshHopper hopper)
    {
        CompHiveSpawner? spawner = hopper.TryGetComp<CompHiveSpawner>();
        if (spawner == null)
        {
            return [];
        }

        return spawner.GetUnitCategories()
            .Where(category => category != null && spawner.CanShowUnitCategory(category))
            .SelectMany(category => category.GetItems(spawner))
            .Where(item => item?.worker != null && item.thing != null)
            .Distinct()
            .OrderBy(item => item.LabelCap.ToString())
            .ToList();
    }

    private float GetHopperRowHeight(Building_FleshHopper hopper, float rowWidth)
    {
        CompProgressHolder holder = hopper.TryGetComp<CompProgressHolder>();
        int taskCount = holder?.progresses?.OfType<ItemSpawnData>().Count(progress => progress?.item?.thing != null) ?? 0;
        float taskAreaWidth = Mathf.Max(TaskWidth, rowWidth - HopperDetailsWidth - 10f);
        int rows = Mathf.Max(1, Mathf.CeilToInt(taskCount / (float)GetTaskColumns(taskAreaWidth)));
        return Mathf.Max(MinRowHeight, 24f + rows * TaskHeight + (rows - 1) * TaskGap);
    }

    private int GetTaskColumns(float width)
    {
        return Mathf.Max(1, Mathf.FloorToInt((width + TaskGap) / (TaskWidth + TaskGap)));
    }

    private string BuildCostLine(ItemDef item)
    {
        List<string> parts = new();
        if (!item.costs.NullOrEmpty())
        {
            parts.AddRange(item.costs.Select(cost => cost.resource.LabelCap + " x" + cost.amount.ToString("0.##"))
                .Select(label => label.ToString()));
        }
        if (!item.requirements.NullOrEmpty())
        {
            parts.AddRange(item.requirements.Select(requirement => requirement.Label.ToString()));
        }
        if (!item.specialResources.NullOrEmpty())
        {
            parts.AddRange(item.specialResources.Select(special => special.Label.ToString()));
        }
        return parts.Count > 0 ? string.Join(" | ", parts) : "FH_ItemProduction_NoCost".Translate().ToString();
    }

    private string FormatIntRange(IntRange range)
    {
        return range.min == range.max
            ? range.min.ToString()
            : "FH_ItemProduction_Range".Translate(range.min, range.max).ToString();
    }

    private string FormatFloatRange(FloatRange range)
    {
        string min = range.min.ToString("0.##");
        string max = range.max.ToString("0.##");
        return range.min == range.max ? min : "FH_ItemProduction_Range".Translate(min, max).ToString();
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
    private float contentHeight = 1f;
    private Building_FleshHopper? selectedHopper;
    private ItemDef? selectedItem;
    private bool focusSelectedHopper;
    private int repeatCount = 1;
    private string repeatBuffer = "1";
    private Texture2D? progressBarTex;
    private Texture2D? emptyBarTex;

    private const int MinRepeatCount = 1;
    private const int MaxRepeatCount = 999;
    private const float SelectedItemHeight = 145f;
    private const float ActionPanelWidth = 245f;
    private const float HopperDetailsWidth = 310f;
    private const float HopperIconSize = 96f;
    private const float MinRowHeight = 132f;
    private const float RowGap = 8f;
    private const float TaskWidth = 78f;
    private const float TaskHeight = 88f;
    private const float TaskGap = 8f;
    private const float ScrollbarWidth = 16f;
    private static readonly Color SelectedItemBackgroundColor = new Color(0f, 0f, 0f, 0.12f);
    private static readonly Color HopperRowBackgroundColor = new Color(0f, 0f, 0f, 0.15f);
    private static readonly Color ProgressBarColor = new Color(0.55f, 0.28f, 0.25f);
    private static readonly Color EmptyBarColor = new Color(0f, 0f, 0f, 0.65f);
    private static readonly Color SelectedProducerBorderColor = new Color(0.2f, 0.75f, 0.85f, 0.95f);
    private static readonly Color LineColor = new Color(0.42f, 0.47f, 0.5f, 0.55f);
}
