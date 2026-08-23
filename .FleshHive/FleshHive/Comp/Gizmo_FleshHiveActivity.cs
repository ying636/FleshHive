using UnityEngine;
using Verse;

namespace FleshHive;

public class Gizmo_FleshHiveActivity : Gizmo
{
    public Gizmo_FleshHiveActivity(MapComponent_FleshHive mapComp)
    {
        this.mapComp = mapComp;
    }

    private Texture2D ActivityBarTex => activityBarTex ??= SolidColorMaterials.NewSolidColorTexture(ActivityBarColor);

    private Texture2D ActivityDangerBarTex => activityDangerBarTex ??= SolidColorMaterials.NewSolidColorTexture(ActivityDangerBarColor);

    private Texture2D EmptyBarTex => emptyBarTex ??= SolidColorMaterials.NewSolidColorTexture(EmptyBarColor);

    private Texture2D ActivityThresholdBarTex => activityThresholdBarTex ??= SolidColorMaterials.NewSolidColorTexture(ActivityThresholdBarColor);

    private Texture2D SuppressionToggleTex => suppressionToggleTex ??= ContentFinder<Texture2D>.Get("UI/Icons/SuppressionToggle");

    public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
    {
        Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), GizmoHeight);
        Widgets.DrawWindowBackground(rect);

        Rect toggleRect = new Rect(rect.xMax - 28f, rect.y + 6f, 22f, 22f);
        Widgets.Label(new Rect(rect.x + 8f, rect.y + 6f, rect.width - 42f, 24f), "FH_FleshManagement_Activity".Translate());
        DrawAutoSuppressToggle(toggleRect);

        Rect barRect = new Rect(rect.x + 12f, rect.y + 38f, rect.width - 24f, 24f);
        Texture2D fillTex = mapComp.ActivityPercent >= 1f ? ActivityDangerBarTex : ActivityBarTex;
        float threshold = mapComp.AutoSuppressActivityThreshold;
        Widgets.DraggableBar(barRect, fillTex, fillTex, EmptyBarTex, ActivityThresholdBarTex,
            ref draggingBar, mapComp.ActivityPercent, ref threshold, null, 100, 0f, 1f);
        mapComp.AutoSuppressActivityThreshold = threshold;

        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(barRect, mapComp.ActivityPercent.ToStringPercent("0"));
        Text.Anchor = TextAnchor.UpperLeft;

        TooltipHandler.TipRegion(rect, "FH_FleshManagement_ActivityTooltip".Translate());
        TooltipHandler.TipRegion(barRect,
            "FH_FleshManagement_ActivityThresholdTooltip".Translate(mapComp.AutoSuppressActivityThreshold.ToStringPercent("0")));
        return new GizmoResult(GizmoState.Clear);
    }

    public override float GetWidth(float maxWidth)
    {
        return Width;
    }

    private void DrawAutoSuppressToggle(Rect rect)
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

    private const float GizmoHeight = 75f;
    private const float Width = 170f;

    private static readonly Color ActivityBarColor = new Color(0.42f, 0.55f, 0.55f);
    private static readonly Color ActivityDangerBarColor = new Color(0.75f, 0.08f, 0.08f);
    private static readonly Color ActivityThresholdBarColor = new Color(0.74f, 0.97f, 0.8f);
    private static readonly Color EmptyBarColor = new Color(0f, 0f, 0f, 0.65f);

    private readonly MapComponent_FleshHive mapComp;
    private Texture2D? activityBarTex;
    private Texture2D? activityDangerBarTex;
    private Texture2D? emptyBarTex;
    private Texture2D? activityThresholdBarTex;
    private Texture2D? suppressionToggleTex;
    private static bool draggingBar;
}
