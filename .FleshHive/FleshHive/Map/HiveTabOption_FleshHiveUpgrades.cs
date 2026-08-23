using System.Collections.Generic;
using System.Linq;
using HiveCreatureFramework;
using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive;

public class HiveTabOption_FleshHiveUpgrades : HiveTabOption_FleshHive
{
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

        if (FleshHiveDefOf.FH_Research_ComplexFleshHive?.IsFinished != true)
        {
            Widgets.DrawMenuSection(inRect);
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(inRect.ContractedBy(24f), "FH_Upgrade_RequiresResearch".Translate(
                FleshHiveDefOf.FH_Research_ComplexFleshHive?.LabelCap ?? "FH_Upgrade_PageTitle".Translate()));
            Text.Anchor = TextAnchor.UpperLeft;
            return;
        }

        List<FleshHiveUpgradeDef> upgrades = DefDatabase<FleshHiveUpgradeDef>.AllDefsListForReading
            .OrderBy(upgrade => upgrade.requiresPrimaryNest)
            .ThenBy(upgrade => upgrade.index)
            .ToList();
        float headerHeight = mapComp.HasAutoRepairUpgrade ? 92f : 62f;
        Rect headerRect = new Rect(inRect.x, inRect.y, inRect.width, headerHeight);
        DrawHeader(headerRect, mapComp);

        Rect outRect = new Rect(inRect.x, headerRect.yMax + 8f, inRect.width - 16f,
            inRect.height - headerHeight - 8f);
        float rowHeight = 116f;
        float viewHeight = Mathf.Max(outRect.height + 1f, upgrades.Count * (rowHeight + 8f));
        Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, viewHeight);
        Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
        for (int i = 0; i < upgrades.Count; i++)
        {
            DrawUpgradeRow(new Rect(0f, i * (rowHeight + 8f), viewRect.width, rowHeight), upgrades[i], mapComp);
        }

        Widgets.EndScrollView();
    }

    private void DrawHeader(Rect rect, MapComponent_FleshHive mapComp)
    {
        Widgets.DrawMenuSection(rect);
        Text.Font = GameFont.Medium;
        float titleWidth = Prefs.DevMode ? rect.width - 180f : rect.width - 20f;
        Widgets.Label(new Rect(rect.x + 10f, rect.y + 8f, titleWidth, 28f),
            "FH_Upgrade_PageTitle".Translate());
        Text.Font = GameFont.Small;

        if (Prefs.DevMode)
        {
            Rect debugButtonRect = new Rect(rect.xMax - 160f, rect.y + 8f, 150f, 26f);
            if (Widgets.ButtonText(debugButtonRect, "FH_Upgrade_DebugUnlock".Translate()))
            {
                mapComp.DebugUnlockAllUpgrades();
            }
        }

        string status = mapComp.ActiveUpgrade == null
            ? "FH_Upgrade_NoActive".Translate()
            : "FH_Upgrade_Active".Translate(mapComp.ActiveUpgrade.label,
                mapComp.ActiveUpgradeProgressPercent.ToStringPercent("0"),
                Mathf.RoundToInt(mapComp.ActiveUpgradeRemainingTicks).ToStringTicksToPeriod());
        Widgets.Label(new Rect(rect.x + 10f, rect.y + 36f, rect.width - 20f, 22f), status);

        if (mapComp.HasAutoRepairUpgrade)
        {
            Rect toggleRect = new Rect(rect.x + 10f, rect.y + 62f, rect.width - 20f, 24f);
            bool autoRepair = mapComp.AutoRepairFleshBuildings;
            Widgets.CheckboxLabeled(toggleRect, "FH_AutoRepair_Label".Translate(), ref autoRepair);
            mapComp.AutoRepairFleshBuildings = autoRepair;
            TooltipHandler.TipRegion(toggleRect, "FH_AutoRepair_Description".Translate());
        }
    }

    private void DrawUpgradeRow(Rect rect, FleshHiveUpgradeDef upgrade, MapComponent_FleshHive mapComp)
    {
        bool completed = mapComp.IsUpgradeCompleted(upgrade);
        bool processing = mapComp.IsUpgradeProcessing(upgrade);
        bool prerequisitesMet = upgrade.prerequisites.NullOrEmpty() || upgrade.prerequisites.All(mapComp.IsUpgradeCompleted);
        bool primaryNestPresent = !upgrade.requiresPrimaryNest || mapComp.HasPrimaryNest;
        bool enoughNutrition = mapComp.MapFleshHive.nutrition >= upgrade.nutritionCost;
        bool enoughNerveFlesh = mapComp.AvailableNerveFlesh >= upgrade.nerveFleshCost;
        bool canStart = !completed && !processing && mapComp.ActiveUpgrade == null && prerequisitesMet
            && primaryNestPresent && enoughNutrition && enoughNerveFlesh;

        Widgets.DrawBoxSolid(rect, completed ? CompletedBackgroundColor : RowBackgroundColor);
        Color previousColor = GUI.color;
        GUI.color = GetBorderColor(upgrade, primaryNestPresent);
        Widgets.DrawBox(rect);
        GUI.color = previousColor;

        Rect labelRect = new Rect(rect.x + 12f, rect.y + 10f, rect.width - 160f, 26f);
        Text.Font = GameFont.Medium;
        Color textColor = !primaryNestPresent ? Color.gray : completed ? Color.cyan : Color.white;
        Widgets.Label(labelRect, upgrade.label.Colorize(textColor));
        Text.Font = GameFont.Small;

        Rect descriptionRect = new Rect(labelRect.x, labelRect.yMax + 4f, labelRect.width, 42f);
        Widgets.Label(descriptionRect, upgrade.description);

        string costText = "FH_Upgrade_Cost".Translate(upgrade.nutritionCost.ToString("0.##"),
            upgrade.nerveFleshCost, upgrade.processingTicks.ToStringTicksToPeriod());
        Widgets.Label(new Rect(labelRect.x, rect.yMax - 28f, labelRect.width, 22f), costText);

        Rect buttonRect = new Rect(rect.xMax - 132f, rect.y + 12f, 120f, 32f);
        if (completed)
        {
            Widgets.Label(buttonRect, "FH_Upgrade_Completed".Translate());
        }
        else if (processing)
        {
            Widgets.Label(buttonRect, "FH_Upgrade_Processing".Translate());
        }
        else
        {
            string buttonLabel = prerequisitesMet && primaryNestPresent && enoughNutrition && enoughNerveFlesh
                ? "FH_Upgrade_Start".Translate()
                : "FH_Upgrade_Locked".Translate();
            if (Widgets.ButtonText(buttonRect, buttonLabel, true, false, canStart))
            {
                mapComp.TryStartUpgrade(upgrade);
            }
        }

        if (processing)
        {
            Rect progressRect = new Rect(rect.xMax - 132f, buttonRect.yMax + 10f, 120f, 24f);
            Widgets.FillableBar(progressRect, mapComp.ActiveUpgradeProgressPercent);
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(progressRect, mapComp.ActiveUpgradeProgressPercent.ToStringPercent("0"));
            Text.Anchor = TextAnchor.UpperLeft;
        }

        if (!completed && !primaryNestPresent)
        {
            TooltipHandler.TipRegion(rect, "FH_Upgrade_PrimaryNestTooltip".Translate());
        }
        else if (!completed && !prerequisitesMet)
        {
            TooltipHandler.TipRegion(rect, "FH_Upgrade_PrerequisiteTooltip".Translate(
                upgrade.prerequisites.Select(prerequisite => prerequisite.label).ToCommaList()));
        }
        else if (!completed && !enoughNutrition)
        {
            TooltipHandler.TipRegion(rect, "FH_Upgrade_NutritionTooltip".Translate(upgrade.nutritionCost));
        }
        else if (!completed && !enoughNerveFlesh)
        {
            TooltipHandler.TipRegion(rect, "FH_Upgrade_NerveFleshTooltip".Translate(upgrade.nerveFleshCost));
        }
    }

    private Vector2 scrollPosition;

    private static Color GetBorderColor(FleshHiveUpgradeDef upgrade, bool primaryNestPresent)
    {
        if (!upgrade.requiresPrimaryNest)
        {
            return Color.white;
        }

        return primaryNestPresent ? AdvancedBorderColor : Color.gray;
    }

    private static readonly Color RowBackgroundColor = new Color(0f, 0f, 0f, 0.18f);
    private static readonly Color CompletedBackgroundColor = new Color(0.05f, 0.2f, 0.14f, 0.45f);
    private static readonly Color AdvancedBorderColor = new Color(0.95f, 0.72f, 0.2f);
}
