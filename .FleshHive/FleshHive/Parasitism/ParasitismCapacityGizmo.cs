using UnityEngine;
using Verse;

namespace FleshHive;

public class ParasitismCapacityGizmo(ParasitismSystem system) : Gizmo
{
    public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
    {
        Rect rect = new Rect(topLeft.x, topLeft.y, this.GetWidth(maxWidth), 75f);
        Widgets.DrawWindowBackground(rect);
        Rect rectLabel = new Rect(rect.x + 10f, rect.y, rect.width - 10f, 30f);
        Widgets.Label(rectLabel, "ParasitismCapacity".Translate(this.system.Count, this.system.Limit));
        Rect box = new Rect(topLeft.x + 10f, topLeft.y + 30f, 15f, 15f);
        for (int i = 0; i < system.Count; i++)
        {
            Widgets.DrawBoxSolid(box, Color.red);
            box.x += box.width + 5f;
            if (box.xMax > rect.xMax)
            {
                box.y += box.height + 5f;
                box.x = topLeft.x + 10f;
            }
        }

        var empty = system.Limit - system.Count;
        if (empty > 0)
        {
            for (int i = 0; i < empty; i++)
            {
                Widgets.DrawBoxSolid(box, Color.gray);
                box.x += box.width + 5f;
                if (box.xMax > rect.xMax)
                {
                    box.y += box.height + 5f;
                    box.x = topLeft.x + 10f;
                }
            }
        }

        return new GizmoResult(GizmoState.Clear);
    }

    public override float GetWidth(float maxWidth)
    {
        int count = this.system.Limit / 2;
        return Mathf.Max(count * 15f + (count - 1) * 5f, 170);
    }

    public ParasitismSystem system = system;
}
