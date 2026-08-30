using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive;

public class ParasitismAbilityGizmo(ParasitismSystem system) : Gizmo
{
    public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
    {
        List<Tentacle> attackTentacles = GetAttackTentacles();
        int columns = GetColumnCount(attackTentacles.Count);
        Rect rect = new Rect(topLeft.x, topLeft.y, this.GetWidth(maxWidth), 75f);
        GUI.color = Color.red;
        Widgets.DrawWindowBackground(rect);
        GUI.color = Color.white;
        int index = 0;
        foreach (Tentacle tentacle in attackTentacles)
        {
            int column = index % columns;
            int row = index / columns;
            Rect box = new Rect(topLeft.x + 5f + column * 35f, topLeft.y + 5f + row * 35f, 30f, 30f);
            Texture2D icon = GetTentacleIcon(tentacle);
            if (Widgets.ButtonImage(box, icon))
            {
                tentacle.AutoAttackEnabled = !tentacle.AutoAttackEnabled;
            }
            Widgets.DrawTextureFitted(new Rect(box.xMax - 12f, box.yMax - 12f, 12f, 12f), tentacle.AutoAttackEnabled ?
                Widgets.CheckboxOnTex : Widgets.CheckboxOffTex, 1);
            string abilityLabel = GetTentacleLabel(tentacle);
            TipSignal tip = new TipSignal(() =>
                abilityLabel + "\n" +
                "ToggleTentacleAttackMode".Translate(abilityLabel) + "\n" +
                (tentacle.AutoAttackEnabled ? "TentacleAttackModeOn".Translate() : "TentacleAttackModeOff".Translate()),
                tentacle.GetHashCode());
            TooltipHandler.TipRegion(box, tip);
            index++;
        }
        return new GizmoResult(GizmoState.Clear);
    }

    public override float GetWidth(float maxWidth)
    {
        int columns = GetColumnCount(GetAttackTentacles().Count);
        return Mathf.Max(10f + columns * 30f + (columns - 1) * 5f, 170f);
    }

    private List<Tentacle> GetAttackTentacles()
    {
        List<Tentacle> result = new List<Tentacle>();
        foreach (ParasitismHediff hd in this.system.ParasitismHediffs)
        {
            HediffComp_Parasitism parasitismComp = hd.TryGetComp<HediffComp_Parasitism>();
            if (parasitismComp != null && parasitismComp.ShowAttackGizmo)
            {
                result.AddRange(parasitismComp.AttackTentacles);
            }
        }
        return result;
    }

    private int GetColumnCount(int count)
    {
        return Mathf.Max((count + 1) / 2, 1);
    }

    private Texture2D GetTentacleIcon(Tentacle tentacle)
    {
        return tentacle.Icon;
    }

    private string GetTentacleLabel(Tentacle tentacle)
    {
        string label = tentacle.Comp?.Hediff.Comp?.Props.abilityLabel ?? tentacle.Comp?.Hediff.LabelCap ?? "FH_ParasiticTentacle".Translate();
        List<Tentacle> siblingTentacles = tentacle.Comp?.AttackTentacles.ToList();
        if (siblingTentacles == null || siblingTentacles.Count <= 1)
        {
            return label;
        }
        return "FH_ParasiticTentacleNumbered".Translate(label, siblingTentacles.IndexOf(tentacle) + 1);
    }

    public ParasitismSystem system = system;
}
