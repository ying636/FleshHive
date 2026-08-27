using System.Collections.Generic;
using System.Linq;
using System.Text;
using HiveCreatureFramework;
using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive;

public class HiveTabOption_FleshManagement : HiveTabOption_FleshHive
{
    private Texture2D NutritionBarTex => nutritionBarTex ??= SolidColorMaterials.NewSolidColorTexture(NutritionBarColor);
    private Texture2D ActivityBarTex => activityBarTex ??= SolidColorMaterials.NewSolidColorTexture(ActivityBarColor);
    private Texture2D ActivityDangerBarTex => activityDangerBarTex ??= SolidColorMaterials.NewSolidColorTexture(ActivityDangerBarColor);
    private Texture2D EmptyBarTex => emptyBarTex ??= SolidColorMaterials.NewSolidColorTexture(new Color(0f, 0f, 0f, 0.65f));
    private Texture2D ActivityThresholdBarTex => activityThresholdBarTex ??= SolidColorMaterials.NewSolidColorTexture(ActivityThresholdBarColor);
    private Texture2D ProgressBarTex => progressBarTex ??= SolidColorMaterials.NewSolidColorTexture(ProgressBarColor);
    private Texture2D TemporaryGroupIconTex => temporaryGroupIconTex ??= ContentFinder<Texture2D>.Get("UI/Ability/FH_BulbfreakReleaseFleshbeasts");
    private Texture2D SuppressionToggleTex => suppressionToggleTex ??= ContentFinder<Texture2D>.Get("UI/Icons/SuppressionToggle");
    private CachedTexture MenuIcon => menuIcon ??= new CachedTexture("UI/Buttons/MainButtons/Menu");
    private Texture2D MaintenanceSettingsIconTex => maintenanceSettingsIconTex ??= ContentFinder<Texture2D>.Get("UI/Group/WorkMode/MechRechargeSettings");
    private Texture2D MiscSettingsIconTex => miscSettingsIconTex ??= ContentFinder<Texture2D>.Get("UI/Icon_Edit");

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

        MapComponent_FleshHive mapComp = map.GetComponent<MapComponent_FleshHive>();
        if (mapComp == null)
        {
            return;
        }

        int newDragAndDropGroup = DragAndDropWidget.NewGroup(null);
        if (newDragAndDropGroup >= 0)
        {
            dragAndDropGroup = newDragAndDropGroup;
        }
        draggedUnit = null;

        float summaryHeight = GetSummaryHeight(mapComp, inRect.width);
        DrawSummary(mapComp, inRect, summaryHeight);

        Rect outRect = new Rect(inRect.x, inRect.y + summaryHeight, inRect.width - 10f, inRect.height - summaryHeight);
        Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, contentHeight);
        Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);

        float curY = 0f;
        foreach (FleshHiveGroupEntry entry in GetEntries(mapComp, map))
        {
            Rect rowRect = new Rect(0f, curY, viewRect.width, RowHeight);
            DrawHiveRow(rowRect, entry, mapComp);
            curY += RowHeight + RowGap;
        }

        contentHeight = Mathf.Max(curY, outRect.height + 1f);

        Widgets.EndScrollView();
        DrawDraggedUnit();
    }

    private float GetSummaryHeight(MapComponent_FleshHive mapComp, float width)
    {
        float scaleWidth = width * 0.56f;
        float scaleBlocksHeight = GetScaleBlocksHeight(scaleWidth, mapComp.HiveGroupCostLimit);
        return Mathf.Max(SummaryBaseHeight, 10f + 24f + 8f + scaleBlocksHeight + 12f);
    }

    private void DrawSummary(MapComponent_FleshHive mapComp, Rect inRect, float summaryHeight)
    {
        Rect scaleRect = new Rect(inRect.x + 10f, inRect.y + 10f, inRect.width * 0.56f, 24f);
        Widgets.Label(scaleRect, "FH_FleshManagement_HiveScale".Translate(mapComp.HiveScale));
        TooltipHandler.TipRegion(scaleRect, BuildHiveScaleTooltip(mapComp));
        float scaleBlocksHeight = GetScaleBlocksHeight(scaleRect.width, mapComp.HiveGroupCostLimit);
        Rect scaleBlocksRect = new Rect(scaleRect.x, scaleRect.yMax + 8f, scaleRect.width, scaleBlocksHeight);
        DrawScaleBlocks(scaleBlocksRect, mapComp.CurrentHiveGroupCost, mapComp.HiveGroupCostLimit);
        TooltipHandler.TipRegion(scaleBlocksRect, "FH_FleshManagement_HiveCapacityTooltip".Translate(mapComp.CurrentHiveGroupCost, mapComp.HiveGroupCostLimit));

        Rect nutritionRect = new Rect(inRect.xMax - 390f, inRect.y + 8f, 170f, 75f);
        DrawInfoBox(nutritionRect, "FH_FleshManagement_Nutrition".Translate(), mapComp.MapFleshHive.nutrition, mapComp.NutritionLimit);

        Rect activityRect = new Rect(inRect.xMax - 205f, inRect.y + 8f, 170f, 75f);
        DrawActivityBox(activityRect, mapComp);

        Rect fleshCountRect = new Rect(nutritionRect.x + 8f, nutritionRect.yMax + 8f, nutritionRect.width - 16f, 24f);
        Widgets.Label(fleshCountRect, "FH_FleshManagement_FleshCount".Translate(mapComp.MapFleshHive.fleshTerrainCount));

        DrawLineHorizontal(inRect.x + 10f, inRect.y + summaryHeight - 12f, inRect.width - 20f);
    }

    private void DrawScaleBlocks(Rect rect, int scale)
    {
        DrawScaleBlocks(rect, scale, scale);
    }

    private void DrawScaleBlocks(Rect rect, int used, int limit)
    {
        int blockCount = Mathf.Max(0, limit);
        int filledCount = Mathf.Clamp(used, 0, blockCount);
        int columns = GetScaleBlockColumns(rect.width);
        for (int i = 0; i < blockCount; i++)
        {
            int row = i / columns;
            int column = i % columns;
            Rect blockRect = new Rect(
                rect.x + column * ScaleBlockStride,
                rect.y + row * ScaleBlockStride,
                ScaleBlockSize,
                ScaleBlockSize);
            Widgets.DrawBoxSolid(blockRect, i < filledCount ? PopulationFilledColor : PopulationEmptyColor);
        }
    }

    private float GetScaleBlocksHeight(float width, int limit)
    {
        int blockCount = Mathf.Max(0, limit);
        if (blockCount == 0)
        {
            return 0f;
        }

        int columns = GetScaleBlockColumns(width);
        int rows = Mathf.CeilToInt(blockCount / (float)columns);
        return rows * ScaleBlockStride;
    }

    private int GetScaleBlockColumns(float width)
    {
        return Mathf.Max(1, Mathf.FloorToInt((width + ScaleBlockGap) / ScaleBlockStride));
    }

    private void DrawInfoBox(Rect rect, TaggedString label, float value, float limit)
    {
        DrawBox(rect);
        Widgets.Label(new Rect(rect.x + 8f, rect.y + 6f, rect.width - 16f, 24f), label);

        Rect barRect = new Rect(rect.x + 12f, rect.y + 38f, rect.width - 24f, 24f);
        float fillPercent = limit > 0f ? Mathf.Clamp01(value / limit) : 0f;
        Widgets.FillableBar(barRect, fillPercent, NutritionBarTex, EmptyBarTex, true);

        string text = limit > 1f ? $"{Mathf.RoundToInt(value)} / {Mathf.RoundToInt(limit)}" : fillPercent.ToStringPercent();
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(barRect, text);
        Text.Anchor = TextAnchor.UpperLeft;
    }

    private void DrawPlaceholderInfoBox(Rect rect, TaggedString label)
    {
        DrawBox(rect);
        Widgets.Label(new Rect(rect.x + 8f, rect.y + 6f, rect.width - 16f, 24f), label);

        Rect textRect = new Rect(rect.x + 12f, rect.y + 38f, rect.width - 24f, 24f);
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(textRect, "--");
        Text.Anchor = TextAnchor.UpperLeft;
    }

    private void DrawActivityBox(Rect rect, MapComponent_FleshHive mapComp)
    {
        DrawBox(rect);
        Rect toggleRect = new Rect(rect.xMax - 28f, rect.y + 6f, 22f, 22f);
        Widgets.Label(new Rect(rect.x + 8f, rect.y + 6f, rect.width - 42f, 24f), "FH_FleshManagement_Activity".Translate());
        DrawAutoSuppressToggle(toggleRect, mapComp);

        Rect barRect = new Rect(rect.x + 12f, rect.y + 38f, rect.width - 24f, 24f);
        Texture2D fillTex = mapComp.ActivityPercent >= 1f ? ActivityDangerBarTex : ActivityBarTex;
        bool draggingThreshold = draggingActivityThreshold;
        float threshold = mapComp.AutoSuppressActivityThreshold;
        Widgets.DraggableBar(barRect, fillTex, fillTex, EmptyBarTex, ActivityThresholdBarTex,
            ref draggingThreshold, mapComp.ActivityPercent, ref threshold, null, 100, 0f, 1f);
        draggingActivityThreshold = draggingThreshold;
        mapComp.AutoSuppressActivityThreshold = threshold;

        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(barRect, mapComp.ActivityPercent.ToStringPercent("0"));
        Text.Anchor = TextAnchor.UpperLeft;

        if (Mouse.IsOver(rect))
        {
            TooltipHandler.TipRegion(rect, "FH_FleshManagement_ActivityTooltip".Translate());
            TooltipHandler.TipRegion(barRect,
                "FH_FleshManagement_ActivityThresholdTooltip".Translate(mapComp.AutoSuppressActivityThreshold.ToStringPercent("0")));
        }
    }

    private void DrawAutoSuppressToggle(Rect rect, MapComponent_FleshHive mapComp)
    {
        if (mapComp.AutoSuppressActivity)
        {
            Widgets.DrawHighlightSelected(rect);
        }

        if (Widgets.ButtonImage(rect, SuppressionToggleTex, true, "FH_FleshManagement_AutoSuppressTooltip".Translate()))
        {
            mapComp.AutoSuppressActivity = !mapComp.AutoSuppressActivity;
        }

        Rect checkRect = new Rect(rect.xMax - 10f, rect.yMax - 10f, 10f, 10f);
        GUI.DrawTexture(checkRect, mapComp.AutoSuppressActivity ? Widgets.CheckboxOnTex : Widgets.CheckboxOffTex, ScaleMode.ScaleToFit);
    }

    private void DrawHiveRow(Rect rect, FleshHiveGroupEntry entry, MapComponent_FleshHive mapComp)
    {
        if (!entry.IsUngrouped)
        {
            DragAndDropWidget.DropArea(dragAndDropGroup, rect, context =>
            {
                if (context is Pawn unit)
                {
                    TryMoveUnit(unit, entry);
                }
            }, entry);
        }

        Widgets.DrawBoxSolid(rect, RowBackgroundColor);
        if (!entry.IsUngrouped
            && DragAndDropWidget.Dragging
            && ReferenceEquals(DragAndDropWidget.HoveringDropArea(dragAndDropGroup), entry))
        {
            Widgets.DrawHighlightSelected(rect);
        }
        DrawLineHorizontal(rect.x, rect.yMax, rect.width);

        Rect iconRect = new Rect(rect.x + 10f, rect.y + 15f, 90f, 90f);
        if (entry.hive != null)
        {
            Widgets.ThingIcon(iconRect, entry.hive);
        }
        else
        {
            DrawBox(iconRect);
            GUI.DrawTexture(iconRect.ContractedBy(8f), TemporaryGroupIconTex, ScaleMode.ScaleToFit);
        }

        Rect textRect = new Rect(iconRect.xMax + 18f, rect.y + 18f, 300f, 70f);
        Text.Font = GameFont.Medium;
        Widgets.Label(textRect, entry.Label);
        Text.Font = GameFont.Small;

        int unitCount = GetEntryUnitCount(entry);
        Widgets.Label(new Rect(textRect.x, textRect.y + 38f, textRect.width, 24f),
            "FH_FleshManagement_HiveScaleShort".Translate(unitCount));

        if (entry.IsUngrouped)
        {
            Color color = GUI.color;
            GUI.color = Color.gray;
            Widgets.Label(new Rect(textRect.x, rect.y + 86f, textRect.width, 48f),
                "FH_FleshManagement_UngroupedHint".Translate());
            GUI.color = color;
        }

        UnitGroup group = entry.groups.FirstOrDefault(group => group != null);
        if (group != null)
        {
            DrawGroupControls(new Rect(iconRect.x, rect.yMax - GroupControlSize - 10f,
                GroupControlSize * GroupControlCount + GroupControlGap * (GroupControlCount - 1), GroupControlSize),
                group, mapComp, entry.IsTemporary);
        }

        if (!entry.IsTemporary && !entry.IsUngrouped)
        {
            float progressX = iconRect.x + GroupControlSize * GroupControlCount
                + GroupControlGap * (GroupControlCount - 1) + 12f;
            DrawProgressSummary(new Rect(progressX, rect.y + 84f, 170f, 58f), entry.Progresses);
        }

        Rect unitsRect = new Rect(textRect.xMax + 20f, rect.y + 10f, rect.width - textRect.xMax - 35f, rect.height - 20f);
        DrawUnitGrid(unitsRect, entry.Units);
    }

    private void DrawGroupControls(Rect rect, UnitGroup group, MapComponent_FleshHive mapComp, bool isTemporary)
    {
        Rect controlRect = new Rect(rect.x, rect.y, GroupControlSize, GroupControlSize);
        DrawGroupColorControl(controlRect, group);

        controlRect.x += GroupControlSize + GroupControlGap;
        DrawGroupModeControl(controlRect, group);

        controlRect.x += GroupControlSize + GroupControlGap;
        DrawGroupButton(controlRect, MaintenanceSettingsIconTex, "HCF_GroupMaintenanceSettingsTip".Translate(),
            () => Find.WindowStack.Add(new Window_GroupMaintenanceSettings(group)));

        controlRect.x += GroupControlSize + GroupControlGap;
        DrawGroupButton(controlRect, MiscSettingsIconTex, "FH_FleshManagement_GroupSettings".Translate(),
            () => Find.WindowStack.Add(new Window_GroupMisc(group)));

        controlRect.x += GroupControlSize + GroupControlGap;
        DrawGroupButton(controlRect, TexButton.Rename, "Rename".Translate(),
            () => Find.WindowStack.Add(new Dialog_RenameGroup(group)));

        controlRect.x += GroupControlSize + GroupControlGap;
        DrawGroupButton(controlRect, MenuIcon.Texture, "WorkSetting".Translate(), () =>
        {
            group.OpenWorkSettings();
        });

        if (isTemporary)
        {
            controlRect.x += GroupControlSize + GroupControlGap;
            DrawGroupButton(controlRect, TexButton.Reload, "FH_FleshManagement_RecallTemporary".Translate(),
                () => RecallTemporaryUnits(group, mapComp));
        }
    }

    private void DrawGroupColorControl(Rect rect, UnitGroup group)
    {
        Widgets.DrawBoxSolid(rect, group.color);
        DrawBox(rect);
        TooltipHandler.TipRegion(rect, "FH_FleshManagement_GroupColor".Translate());
        if (!Widgets.ButtonInvisible(rect))
        {
            return;
        }

        List<FloatMenuOption> options =
        [
            new FloatMenuOption("Colorbase".Translate(), () =>
                Find.WindowStack.Add(new Dialog_ChooseColor("Select".Translate(), group.color,
                    DefDatabase<ColorDef>.AllDefsListForReading.Select(colorDef => colorDef.color).ToList(),
                    color => group.color = color))),
            new FloatMenuOption("Hex".Translate(), () =>
                Find.WindowStack.Add(new Dialog_RGB(group.color, color => group.color = color)))
        ];
        Find.WindowStack.Add(new FloatMenu(options));
    }

    private void DrawGroupModeControl(Rect rect, UnitGroup group)
    {
        DrawGroupButton(rect, group.WorkModeDef.Icon,
            "HCF_WorkMode".Translate(group.WorkModeDef.LabelCap), () =>
            {
                List<FloatMenuOption> options = group.GetModeDefs()
                    .Select(modeDef =>
                    {
                        GroupWorkModeDef selectedMode = modeDef;
                        return new FloatMenuOption(selectedMode.LabelCap,
                            () => group.SetMode(selectedMode), selectedMode.Icon, Color.white);
                    })
                    .ToList();
                Find.WindowStack.Add(new FloatMenu(options));
            });
    }

    private void DrawGroupButton(Rect rect, Texture2D icon, TipSignal tooltip, System.Action action)
    {
        Widgets.DrawTextureFitted(rect, icon, 1f);
        TooltipHandler.TipRegion(rect, tooltip);
        if (Mouse.IsOver(rect))
        {
            Widgets.DrawHighlight(rect);
        }

        if (Widgets.ButtonInvisible(rect))
        {
            action();
        }
    }

    private void DrawUnitGrid(Rect rect, IEnumerable<Pawn> units)
    {
        float x = rect.x;
        float y = rect.y;
        foreach (Pawn unit in units.Where(unit => unit != null))
        {
            Rect unitRect = new Rect(x, y, UnitIconSize, UnitIconSize);
            DrawBox(unitRect);
            if (DragAndDropWidget.Draggable(dragAndDropGroup, unitRect, unit))
            {
                draggedUnit = unit;
            }
            else
            {
                Widgets.ThingIcon(unitRect.ContractedBy(4f), unit);
            }
            DrawUnitCost(new Rect(unitRect.x + 4f, unitRect.yMax - 11f, unitRect.width - 8f, 8f), unit.TryGetComp<UnitComp>()?.Props.groupCost ?? 0);

            if (Mouse.IsOver(unitRect))
            {
                Widgets.DrawHighlight(unitRect);
                TooltipHandler.TipRegion(unitRect, unit.LabelCap);
            }

            x += UnitIconSize + 10f;
            if (x + UnitIconSize > rect.xMax)
            {
                x = rect.x;
                y += UnitIconSize + 22f;
                if (y + UnitIconSize > rect.yMax)
                {
                    break;
                }
            }
        }
    }

    private void DrawDraggedUnit()
    {
        if (draggedUnit == null)
        {
            return;
        }

        Rect draggedRect = new Rect(Event.current.mousePosition, new Vector2(UnitIconSize, UnitIconSize));
        Widgets.ThingIcon(draggedRect, draggedUnit);
    }

    private void TryMoveUnit(Pawn unit, FleshHiveGroupEntry targetEntry)
    {
        UnitGroup? currentGroup = unit.TryGetComp<UnitComp>()?.group;
        if (currentGroup != null && targetEntry.groups.Contains(currentGroup))
        {
            return;
        }

        string? rejectReason = null;
        foreach (UnitGroup targetGroup in targetEntry.groups.Where(group => group != null))
        {
            AcceptReason acceptReason = targetGroup.CanAccept(unit);
            if (acceptReason.Accepted)
            {
                targetGroup.AcceptUnit(unit);
                return;
            }

            rejectReason ??= acceptReason.Reason;
        }

        Messages.Message(rejectReason ?? "TargetGroupDontAccept".Translate(), MessageTypeDefOf.RejectInput, false);
    }

    private void RecallTemporaryUnits(UnitGroup temporaryGroup, MapComponent_FleshHive mapComp)
    {
        List<Pawn> units = temporaryGroup.units
            .Where(unit => unit != null)
            .OrderByDescending(unit => unit.TryGetComp<UnitComp>()?.Props.groupCost ?? 0)
            .ThenBy(unit => unit.thingIDNumber)
            .ToList();
        if (units.Count == 0)
        {
            Messages.Message("FH_FleshManagement_RecallTemporaryEmpty".Translate(), MessageTypeDefOf.NeutralEvent, false);
            return;
        }

        List<UnitGroup> targetGroups = GetNormalGroups(mapComp.map)
            .Where(group => group != temporaryGroup)
            .ToList();
        if (targetGroups.Count == 0)
        {
            Messages.Message("FH_FleshManagement_RecallTemporaryNoHive".Translate(), MessageTypeDefOf.RejectInput, false);
            return;
        }

        int recalledCount = 0;
        string? rejectReason = null;
        foreach (Pawn unit in units)
        {
            foreach (UnitGroup targetGroup in targetGroups)
            {
                AcceptReason acceptReason = targetGroup.CanAccept(unit);
                if (acceptReason.Accepted)
                {
                    targetGroup.AcceptUnit(unit);
                    recalledCount++;
                    break;
                }

                rejectReason ??= acceptReason.Reason;
            }
        }

        int remainingCount = temporaryGroup.units.Count(unit => unit != null);
        TaggedString message = remainingCount > 0
            ? "FH_FleshManagement_RecallTemporaryPartial".Translate(
                recalledCount,
                remainingCount,
                rejectReason ?? "TargetGroupDontAccept".Translate())
            : "FH_FleshManagement_RecallTemporaryComplete".Translate(recalledCount);
        Messages.Message(message, remainingCount > 0 ? MessageTypeDefOf.RejectInput : MessageTypeDefOf.PositiveEvent, false);
    }

    private void DrawUnitCost(Rect rect, int cost)
    {
        float x = rect.x;
        for (int i = 0; i < cost; i++)
        {
            Widgets.DrawBoxSolid(new Rect(x, rect.y, 7f, 7f), PopulationFilledColor);
            x += 9f;
            if (x + 7f > rect.xMax)
            {
                break;
            }
        }
    }

    private void DrawProgressSummary(Rect rect, IEnumerable<Progress> progresses)
    {
        Progress progress = progresses.FirstOrDefault(progress => progress != null);
        if (progress == null)
        {
            return;
        }

        Rect iconRect = new Rect(rect.x, rect.y, UnitIconSize, UnitIconSize);
        DrawBox(iconRect);
        TryDrawProgressIcon(iconRect.ContractedBy(4f), progress);

        Rect labelRect = new Rect(iconRect.xMax + 8f, rect.y + 4f, rect.width - iconRect.width - 8f, 20f);
        Rect barRect = new Rect(labelRect.x, labelRect.yMax + 2f, labelRect.width, 20f);
        DrawProgressBar(barRect, progress);

        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(labelRect, "FH_FleshManagement_Breeding".Translate());
        Text.Anchor = TextAnchor.UpperLeft;

        if (Mouse.IsOver(rect))
        {
            Widgets.DrawHighlight(rect);
            TooltipHandler.TipRegion(rect, progress.Label + progress.Tooltip);
        }
    }

    private void DrawProgressBar(Rect rect, Progress progress)
    {
        DrawBox(rect);
        float fillPercent = progress.totalTime > 0f ? Mathf.Clamp01(1f - progress.time / progress.totalTime) : 0f;
        Widgets.FillableBar(rect.ContractedBy(3f), fillPercent, ProgressBarTex, EmptyBarTex, true);

        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(rect, fillPercent.ToStringPercent("0"));
        Text.Anchor = TextAnchor.UpperLeft;
    }

    private bool TryDrawProgressIcon(Rect rect, Progress progress)
    {
        if (progress is UnitSpawnData_FleshTrait unitProgress && unitProgress.Def?.kind != null)
        {
            Widgets.DefIcon(rect, unitProgress.Def.kind);
            return true;
        }

        if (progress is ItemSpawnData itemProgress && itemProgress.item?.thing != null)
        {
            Widgets.DefIcon(rect, itemProgress.item.thing);
            return true;
        }

        if (progress is FusionProgress fusionProgress && GetFusionProgressResultKind(fusionProgress) is { } kind)
        {
            Widgets.DefIcon(rect, kind);
            return true;
        }

        return false;
    }

    private PawnKindDef? GetFusionProgressResultKind(FusionProgress progress)
    {
        return progress.fusionDef?.results.FirstOrDefault()?.result is FusionResult_PawnKind pawnKindResult ? pawnKindResult.kind : null;
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

    private string BuildHiveScaleTooltip(MapComponent_FleshHive mapComp)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("FH_FleshManagement_HiveScaleTooltipTitle".Translate());
        builder.AppendLine("FH_FleshManagement_HiveScaleTooltipTerrain".Translate(mapComp.MapFleshHive.fleshTerrainCount, mapComp.FleshTerrainHiveScale));
        AppendHiveScaleBuildings(builder, mapComp);
        builder.AppendLine("FH_FleshManagement_HiveScaleTooltipTotal".Translate(mapComp.HiveScale));
        builder.AppendLine();
        builder.Append("FH_FleshManagement_HiveScaleTooltipRule".Translate());
        return builder.ToString();
    }

    private void AppendHiveScaleBuildings(StringBuilder builder, MapComponent_FleshHive mapComp)
    {
        List<IGrouping<string, CompHiveScaleProvider>> groups = mapComp.map.listerThings.AllThings
            .OfType<ThingWithComps>()
            .Select(thing => thing.TryGetComp<CompHiveScaleProvider>())
            .Where(comp => comp?.parent?.Spawned == true)
            .GroupBy(comp => comp.parent.LabelCap)
            .OrderBy(group => group.Key)
            .ToList();

        if (!groups.Any())
        {
            builder.AppendLine("FH_FleshManagement_HiveScaleTooltipBuildingsNone".Translate());
            return;
        }

        foreach (IGrouping<string, CompHiveScaleProvider> group in groups)
        {
            builder.AppendLine("FH_FleshManagement_HiveScaleTooltipBuilding".Translate(group.Key, group.Count(), group.Sum(comp => comp.Scale)));
        }
    }

    private int GetEntryUnitCount(FleshHiveGroupEntry entry)
    {
        return entry.Units.Count(unit => unit != null);
    }

    private IEnumerable<FleshHiveGroupEntry> GetEntries(MapComponent_FleshHive mapComp, Map map)
    {
        foreach (IGrouping<Thing, UnitGroup> groupSet in GetNormalGroups(map)
            .Where(group => group != mapComp.group)
            .GroupBy(group => group.hive))
        {
            yield return new FleshHiveGroupEntry(groupSet.Key, groupSet.ToList(), false);
        }

        if (mapComp.group != null)
        {
            yield return new FleshHiveGroupEntry(null, new List<UnitGroup> { mapComp.group }, true);
        }

        List<Pawn> ungroupedUnits = mapComp.CachedFleshBeasts
            .Where(unit => unit != null
                && unit.Spawned
                && !unit.Destroyed
                && !unit.Dead
                && unit.Map == map
                && unit.Faction == Faction.OfPlayer
                && unit.TryGetComp<CompFleshBeastCache>() != null
                && unit.TryGetComp<UnitComp>()?.group == null)
            .OrderBy(unit => unit.kindDef?.label)
            .ThenBy(unit => unit.thingIDNumber)
            .ToList();
        if (ungroupedUnits.Count > 0)
        {
            yield return new FleshHiveGroupEntry(null, new List<UnitGroup>(), false, true, ungroupedUnits);
        }
    }

    private IEnumerable<UnitGroup> GetNormalGroups(Map map)
    {
        if (GameComponent_UnitGroup.Instance == null)
        {
            yield break;
        }

        foreach (UnitGroup group in GameComponent_UnitGroup.Instance.groups)
        {
            if (group?.Map == map
                && group.Show
                && group.tags?.Contains(FleshHiveTags.Flesh) == true)
            {
                yield return group;
            }
        }
    }

    private Vector2 scrollPosition;
    private float contentHeight = 1f;
    private int dragAndDropGroup = -1;
    private Pawn? draggedUnit;
    private bool draggingActivityThreshold;
    private Texture2D? nutritionBarTex;
    private Texture2D? activityBarTex;
    private Texture2D? activityDangerBarTex;
    private Texture2D? activityThresholdBarTex;
    private Texture2D? emptyBarTex;
    private Texture2D? progressBarTex;
    private Texture2D? temporaryGroupIconTex;
    private Texture2D? suppressionToggleTex;
    private CachedTexture? menuIcon;
    private Texture2D? maintenanceSettingsIconTex;
    private Texture2D? miscSettingsIconTex;

    private const float SummaryBaseHeight = 125f;
    private const float RowHeight = 166f;
    private const float RowGap = 8f;
    private const float UnitIconSize = 58f;
    private const float ScaleBlockSize = 12f;
    private const float ScaleBlockGap = 3f;
    private const float ScaleBlockStride = ScaleBlockSize + ScaleBlockGap;
    private const float GroupControlSize = 26f;
    private const float GroupControlGap = 4f;
    private const int GroupControlCount = 6;
    private static readonly Color NutritionBarColor = new Color(0.55f, 0.28f, 0.25f);
    private static readonly Color ActivityBarColor = new Color(0.42f, 0.55f, 0.55f);
    private static readonly Color ActivityDangerBarColor = new Color(0.75f, 0.08f, 0.08f);
    private static readonly Color ActivityThresholdBarColor = new Color(0.74f, 0.97f, 0.8f);
    private static readonly Color ProgressBarColor = new Color(0.42f, 0.17f, 0.16f);
    private static readonly Color PopulationFilledColor = Color.red;
    private static readonly Color PopulationEmptyColor = new Color(0.12f, 0.12f, 0.12f, 0.8f);
    private static readonly Color RowBackgroundColor = new Color(0f, 0f, 0f, 0.15f);
    private static readonly Color LineColor = new Color(0.42f, 0.47f, 0.5f, 0.55f);

    private class FleshHiveGroupEntry
    {
        public FleshHiveGroupEntry(Thing? hive, List<UnitGroup> groups, bool isTemporary,
            bool isUngrouped = false, List<Pawn>? ungroupedUnits = null)
        {
            this.hive = hive;
            this.groups = groups;
            this.IsTemporary = isTemporary;
            this.IsUngrouped = isUngrouped;
            this.ungroupedUnits = ungroupedUnits ?? new List<Pawn>();
        }

        public TaggedString Label
        {
            get
            {
                if (IsUngrouped)
                {
                    return "FH_FleshManagement_Ungrouped".Translate();
                }
                if (IsTemporary)
                {
                    return "FH_FleshManagement_TemporaryGroup".Translate();
                }

                return hive?.LabelCap ?? "FH_FleshManagement_NoHive".Translate();
            }
        }

        public IEnumerable<Pawn> Units => IsUngrouped
            ? ungroupedUnits
            : groups.Where(group => group != null).SelectMany(group => group.units);

        public IEnumerable<CompProgressHolder> ProgressHolders
        {
            get
            {
                if (hive is ThingWithComps thingWithComps)
                {
                    CompProgressHolder progressHolder = thingWithComps.TryGetComp<CompProgressHolder>();
                    if (progressHolder != null)
                    {
                        yield return progressHolder;
                    }
                }

                foreach (CompProgressHolder progressHolder in groups
                    .Where(group => group != null)
                    .SelectMany(group => group.progressHolders)
                    .Where(progressHolder => progressHolder != null)
                    .Distinct())
                {
                    yield return progressHolder;
                }
            }
        }

        public IEnumerable<Progress> Progresses => ProgressHolders
            .Where(holder => holder?.progresses != null)
            .SelectMany(holder => holder.progresses)
            .Where(progress => progress != null);

        public readonly Thing? hive;
        public readonly List<UnitGroup> groups;
        public readonly bool IsTemporary;
        public readonly bool IsUngrouped;
        private readonly List<Pawn> ungroupedUnits;
    }
}
