using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace FleshHive;

public class Gizmo_TwistedFleshForParasitism : Gizmo_Slider
{
    public Gizmo_TwistedFleshForParasitism(ParasitismSystem system)
    {
        this.system = system;
    }

    protected override float Target
    {
        get => this.system.TwistedFleshTargetValue;
        set => this.system.TwistedFleshTargetValue = value;
    }

    protected override bool DraggingBar
    {
        get => draggingBar;
        set => draggingBar = value;
    }

    protected override Color BarColor => FleshHiveDefOf.FH_Resource_TwistedFlesh.color;
    protected override bool IsDraggable => true;
    protected override float ValuePercent => this.system.MaxTwistedFlesh > 0
        ? (float)this.system.CurrentTwistedFlesh / this.system.MaxTwistedFlesh
        : 0f;
    protected override string Title => FleshHiveDefOf.FH_Resource_TwistedFlesh.label;
    protected override string BarLabel => this.system.CurrentTwistedFlesh.ToString("F2")
                                          + "/" + this.system.MaxTwistedFlesh;

    protected override void DrawHeader(Rect headerRect, ref bool mouseOverElement)
    {
        mouseOverElement = false;
        headerRect.xMax -= 24f;
        Rect checkRect = new Rect(headerRect.xMax, headerRect.y, 24f, 24f);
        GUI.DrawTexture(checkRect, this.system.AllowAutoRefillTwistedFlesh
            ? Widgets.CheckboxOnTex
            : Widgets.CheckboxOffTex);
        if (Widgets.ButtonInvisible(checkRect, true))
        {
            this.system.AllowAutoRefillTwistedFlesh = !this.system.AllowAutoRefillTwistedFlesh;
            (this.system.AllowAutoRefillTwistedFlesh ? SoundDefOf.Tick_High : SoundDefOf.Tick_Low)
                .PlayOneShotOnCamera(null);
        }

        if (Mouse.IsOver(checkRect))
        {
            Widgets.DrawHighlight(checkRect);
            mouseOverElement = true;
        }
        TooltipHandler.TipRegion(checkRect, () => "HCF_AutoFill".Translate(), 828267373);
        base.DrawHeader(headerRect, ref mouseOverElement);
    }

    protected override string GetTooltip()
    {
        return FleshHiveDefOf.FH_Resource_TwistedFlesh.description;
    }

    private static bool draggingBar;
    private readonly ParasitismSystem system;
}
