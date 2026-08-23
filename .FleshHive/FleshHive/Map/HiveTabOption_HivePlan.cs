using System.Collections.Generic;
using System.Linq;
using HiveCreatureFramework;
using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive;

public class HiveTabOption_HivePlan : HiveTabOption_FleshHive
{
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

        List<CompHivePlan> plans = GetPlans(map);
        ValidateSelection(plans);

        Rect contentRect = inRect.ContractedBy(8f);
        Rect selectorRect = new Rect(contentRect.x, contentRect.y, SelectorWidth, contentRect.height);
        Rect planRect = new Rect(selectorRect.xMax + PanelGap, contentRect.y,
            contentRect.width - SelectorWidth - PanelGap, contentRect.height);

        DrawPlanSelector(selectorRect, plans);
        if (selectedPlan == null)
        {
            DrawNoPlans(planRect);
            return;
        }

        DrawSelectedPlan(planRect, selectedPlan);
    }

    private void DrawPlanSelector(Rect rect, List<CompHivePlan> plans)
    {
        Widgets.DrawMenuSection(rect);
        Rect innerRect = rect.ContractedBy(8f);
        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(innerRect.x, innerRect.y, innerRect.width, HeaderHeight),
            "FH_HivePlan_Selector".Translate());
        Text.Font = GameFont.Small;

        Rect outRect = new Rect(innerRect.x, innerRect.y + HeaderHeight + 4f,
            innerRect.width, innerRect.height - HeaderHeight - 4f);
        float viewHeight = Mathf.Max(outRect.height + 1f, plans.Count * (HiveRowHeight + HiveRowGap));
        Rect viewRect = new Rect(0f, 0f, outRect.width - ScrollbarWidth, viewHeight);
        Widgets.BeginScrollView(outRect, ref selectorScrollPosition, viewRect);
        for (int i = 0; i < plans.Count; i++)
        {
            DrawPlanRow(new Rect(0f, i * (HiveRowHeight + HiveRowGap), viewRect.width, HiveRowHeight), plans[i]);
        }

        Widgets.EndScrollView();
    }

    private void DrawPlanRow(Rect rect, CompHivePlan plan)
    {
        bool selected = selectedPlan == plan;
        Widgets.DrawBoxSolid(rect, selected ? SelectedBackgroundColor : RowBackgroundColor);

        Rect iconRect = new Rect(rect.x + 6f, rect.y + 6f, HiveIconSize, HiveIconSize);
        Widgets.ThingIcon(iconRect, plan.parent);

        Rect labelRect = new Rect(iconRect.xMax + 8f, rect.y + 7f,
            rect.width - iconRect.width - 20f, 26f);
        Widgets.Label(labelRect, plan.parent.LabelCap);

        Text.Font = GameFont.Tiny;
        GUI.color = Color.grey;
        Widgets.Label(new Rect(labelRect.x, labelRect.yMax + 3f, labelRect.width, 22f), GetPlanStatus(plan));
        GUI.color = Color.white;
        Text.Font = GameFont.Small;

        if (Widgets.ButtonInvisible(rect))
        {
            SelectPlan(plan);
        }

        TooltipHandler.TipRegion(rect, plan.parent.GetInspectString());
        if (selected)
        {
            Color color = GUI.color;
            GUI.color = SelectedBorderColor;
            Widgets.DrawBox(rect.ContractedBy(1f), 2);
            GUI.color = color;
        }
    }

    private void DrawSelectedPlan(Rect rect, CompHivePlan plan)
    {
        Rect headerRect = new Rect(rect.x, rect.y, rect.width, PlanHeaderHeight);
        Widgets.DrawBoxSolid(headerRect, SelectedHeaderBackgroundColor);

        Rect iconRect = new Rect(headerRect.x + 8f, headerRect.y + 8f, 48f, 48f);
        Widgets.ThingIcon(iconRect, plan.parent);
        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(iconRect.xMax + 10f, headerRect.y + 8f,
            headerRect.width - iconRect.width - 20f, 28f), plan.parent.LabelCap);
        Text.Font = GameFont.Small;
        Widgets.Label(new Rect(iconRect.xMax + 10f, headerRect.y + 35f,
            headerRect.width - iconRect.width - 20f, 22f), GetPlanStatus(plan));

        Rect outRect = new Rect(rect.x, headerRect.yMax + PanelGap, rect.width,
            rect.height - PlanHeaderHeight - PanelGap);
        float viewWidth = Mathf.Max(1f, outRect.width - ScrollbarWidth);
        float viewHeight = Mathf.Max(outRect.height + 1f, planContentHeight);
        Rect viewRect = new Rect(0f, 0f, viewWidth, viewHeight);
        Widgets.BeginScrollView(outRect, ref planScrollPosition, viewRect);
        CompHivePlan_FleshHive? fleshPlan = plan as CompHivePlan_FleshHive;
        MapComponent_FleshHive? mapComponent = plan.parent.Map?.GetComponent<MapComponent_FleshHive>();
        if (fleshPlan != null)
        {
            mapComponent?.PreparePlanCheckInterval(fleshPlan);
        }
        plan.Draw(new Rect(0f, 0f, viewWidth, viewHeight), out Vector2 limit);
        if (fleshPlan != null)
        {
            mapComponent?.AdoptPlanCheckInterval(fleshPlan);
        }
        planContentHeight = Mathf.Max(outRect.height + 1f, limit.y);
        Widgets.EndScrollView();
    }

    private void DrawNoPlans(Rect rect)
    {
        Text.Anchor = TextAnchor.MiddleCenter;
        GUI.color = Color.grey;
        Widgets.Label(rect, "FH_HivePlan_NoHives".Translate());
        GUI.color = Color.white;
        Text.Anchor = TextAnchor.UpperLeft;
    }

    private List<CompHivePlan> GetPlans(Map map)
    {
        return map.listerThings.AllThings
            .OfType<ThingWithComps>()
            .Where(thing => thing.Spawned && thing.Faction == Faction.OfPlayer && IsPlanTarget(thing))
            .Select(thing => thing.TryGetComp<CompHivePlan>())
            .Where(plan => plan?.CanShow == true)
            .OrderBy(plan => plan.parent.Position.z)
            .ThenBy(plan => plan.parent.Position.x)
            .ToList();
    }

    private void ValidateSelection(List<CompHivePlan> plans)
    {
        if (selectedPlan != null && plans.Contains(selectedPlan))
        {
            return;
        }

        selectedPlan = plans.FirstOrDefault();
        selectorScrollPosition = Vector2.zero;
        planScrollPosition = Vector2.zero;
        planContentHeight = 1f;
    }

    private void SelectPlan(CompHivePlan plan)
    {
        if (selectedPlan == plan)
        {
            return;
        }

        selectedPlan = plan;
        planScrollPosition = Vector2.zero;
        planContentHeight = 1f;
    }

    private string GetPlanStatus(CompHivePlan plan)
    {
        if (plan.entries.NullOrEmpty())
        {
            return "FH_HivePlan_StatusEmpty".Translate();
        }

        int activeCount = plan.entries.Count(entry => entry != null
            && entry.IsValid
            && !entry.suspended
            && (entry.mode != HivePlanMode.RepeatCount || entry.repeatCount > 0));
        return "FH_HivePlan_StatusEntries".Translate(plan.entries.Count, activeCount);
    }

    private bool IsPlanTarget(Thing thing)
    {
        return thing.def == FleshHiveDefOf.FH_FleshPrimaryNest
            || thing.def.defName == FleshHiveDefName
            || thing.def == FleshHiveDefOf.FH_FleshHopper;
    }

    private Vector2 selectorScrollPosition;
    private Vector2 planScrollPosition;
    private CompHivePlan? selectedPlan;
    private float planContentHeight = 1f;

    private const string FleshHiveDefName = "FH_FleshHive";
    private const float SelectorWidth = 240f;
    private const float PanelGap = 10f;
    private const float HeaderHeight = 34f;
    private const float HiveRowHeight = 70f;
    private const float HiveRowGap = 6f;
    private const float HiveIconSize = 58f;
    private const float PlanHeaderHeight = 64f;
    private const float ScrollbarWidth = 16f;
    private static readonly Color RowBackgroundColor = new Color(0f, 0f, 0f, 0.18f);
    private static readonly Color SelectedBackgroundColor = new Color32(70, 70, 70, 255);
    private static readonly Color SelectedHeaderBackgroundColor = new Color(0f, 0f, 0f, 0.12f);
    private static readonly Color SelectedBorderColor = new Color(0.2f, 0.75f, 0.85f, 0.95f);
}
