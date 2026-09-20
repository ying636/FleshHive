using UnityEngine;
using Verse;

namespace FleshHive;

public class Gizmo_FleshHiveDailySummary : Gizmo
{
    public Gizmo_FleshHiveDailySummary(MapComponent_FleshHive mapComp)
    {
        this.mapComp = mapComp;
    }

    public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
    {
        Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), GizmoHeight);
        Widgets.DrawWindowBackground(rect);

        mapComp.GetDailyHiveSummary(out float nutritionUpkeep, out float activityIncrease);
        GameFont oldFont = Text.Font;
        TextAnchor oldAnchor = Text.Anchor;
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleLeft;

        Rect contentRect = rect.ContractedBy(ContentPadding);
        contentRect.height /= 2f;
        Widgets.Label(contentRect, "FH_HiveDailyUpkeep".Translate(nutritionUpkeep.ToString("0.##")));
        contentRect.y += contentRect.height;
        Widgets.Label(contentRect, "FH_HiveDailyActivityIncrease".Translate(activityIncrease.ToString("0.###")));

        Text.Font = oldFont;
        Text.Anchor = oldAnchor;
        return new GizmoResult(GizmoState.Clear);
    }

    public override float GetWidth(float maxWidth)
    {
        return Width;
    }

    public override bool GroupsWith(Gizmo other)
    {
        return other is Gizmo_FleshHiveDailySummary summary && summary.mapComp == mapComp;
    }

    private const float GizmoHeight = 75f;
    private const float Width = 230f;
    private const float ContentPadding = 8f;

    private readonly MapComponent_FleshHive mapComp;
}
