using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive;

public class ParasitismAbilityGizmo(ParasitismSystem system) : Gizmo
{
    public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
    {
        Rect rect = new Rect(topLeft.x, topLeft.y, this.GetWidth(maxWidth), 75f);
        GUI.color = Color.red;
        Widgets.DrawWindowBackground(rect);
        GUI.color = Color.white;
        int columns = GetColumnCount();
        int index = 0;
        foreach (var hd in system.ParasitismHediffs)
        {
            if (!hd.CanDraw)
            {
                continue;
            }
            if (!HasTentacleComp(hd))
            {
                continue;
            }
            int column = index % columns;
            int row = index / columns;
            Rect box = new Rect(topLeft.x + 5f + column * 35f, topLeft.y + 5f + row * 35f, 30f, 30f);
            hd.Draw(box);
            string abilityLabel = hd.Comp?.Props.abilityLabel ?? hd.LabelCap;
            TipSignal tip = new TipSignal(() =>
                abilityLabel + "\n" +
                "ToggleTentacleAttackMode".Translate(abilityLabel) + "\n" +
                (hd.allow ? "TentacleAttackModeOn".Translate() : "TentacleAttackModeOff".Translate()),
                hd.GetHashCode());
            TooltipHandler.TipRegion(box, tip);
            index++;
        }
        return new GizmoResult(GizmoState.Clear);
    }

    public override float GetWidth(float maxWidth)
    {
        int columns = GetColumnCount();
        return Mathf.Max(10f + columns * 30f + (columns - 1) * 5f, 170f);
    }

    private int GetColumnCount()
    {
        int count = 0;
        foreach (var hd in this.system.ParasitismHediffs)
        {
            if (hd.CanDraw && HasTentacleComp(hd))
            {
                count++;
            }
        }
        return Mathf.Max((count + 1) / 2, 1);
    }

    private bool HasTentacleComp(ParasitismHediff hd)
    {
        return hd.TryGetComp<HediffComp_Parasitism>() != null;
    }

    public ParasitismSystem system = system;
}
