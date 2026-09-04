using System.Collections.Generic;
using HiveCreatureFramework;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace FleshHive;

public class CompProperties_TwistedFlesh : CompProperties
{
    public CompProperties_TwistedFlesh()
    {
        this.compClass = typeof(CompTwistedFlesh);
    }

    public int capacity;
}

public class CompTwistedFlesh : CompPawnResourceContainer
{
    private CompProperties_TwistedFlesh Props => (CompProperties_TwistedFlesh)this.props;

    public float CurrentTwistedFlesh
    {
        get => currentTwistedFlesh;
        private set => currentTwistedFlesh = value;
    }

    public int MaxTwistedFlesh
    {
        get
        {
            int maxTwistedFlesh = this.BaseMaxTwistedFlesh;
            ParasitismSystem system = (this.parent as Pawn)?.health?.hediffSet?.GetFirstHediffOfDef(FleshHiveDefOf.FH_ParasitismSystem) as ParasitismSystem;
            if (system != null)
            {
                maxTwistedFlesh += system.AdditionalTwistedFleshCapacity;
            }
            return maxTwistedFlesh;
        }
    }

    public int BaseMaxTwistedFlesh
    {
        get
        {
            int growthCapacity = 0;
            if (this.parent is Pawn pawn
                && pawn.health?.hediffSet?.GetFirstHediffOfDef(FleshHiveDefOf.FH_MeldGrowth)
                is Hediff_MeldGrowth growth)
            {
                growthCapacity = growth.Level * 100;
            }
            return Props.capacity + growthCapacity;
        }
    }

    public float TwistedFleshTargetValue
    {
        get => twistedFleshTargetValue;
        set => twistedFleshTargetValue = Mathf.Clamp01(value);
    }

    public bool AllowAutoRefillTwistedFlesh
    {
        get => allowAutoRefillTwistedFlesh;
        set => allowAutoRefillTwistedFlesh = value;
    }

    public override IEnumerable<HiveResourceDef> ResourceDefs
    {
        get
        {
            yield return FleshHiveDefOf.FH_Resource_TwistedFlesh;
        }
    }

    public override HiveResourceDef PrimaryResourceDef => FleshHiveDefOf.FH_Resource_TwistedFlesh;

    public override bool HasResource(HiveResourceDef resourceDef)
    {
        return resourceDef != null && resourceDef == FleshHiveDefOf.FH_Resource_TwistedFlesh;
    }

    public override float GetAmount(HiveResourceDef resourceDef)
    {
        return HasResource(resourceDef) ? CurrentTwistedFlesh : 0f;
    }

    public override void SetAmount(HiveResourceDef resourceDef, float amount)
    {
        if (HasResource(resourceDef))
        {
            CurrentTwistedFlesh = Mathf.Clamp(amount, 0f, MaxTwistedFlesh);
        }
    }

    public override float GetLimit(HiveResourceDef resourceDef)
    {
        return HasResource(resourceDef) ? MaxTwistedFlesh : 0f;
    }

    public bool CanConsumeTwistedFlesh(int amount)
    {
        return CurrentTwistedFlesh >= amount;
    }

    public bool ConsumeTwistedFlesh(int amount)
    {
        if (!CanConsumeTwistedFlesh(amount))
        {
            return false;
        }
        CurrentTwistedFlesh -= amount;
        return true;
    }

    public void FillTwistedFlesh(int amount)
    {
        CurrentTwistedFlesh += amount;
        if (CurrentTwistedFlesh > MaxTwistedFlesh)
        {
            CurrentTwistedFlesh = MaxTwistedFlesh;
        }
    }

    public int NeededAmount
    {
        get
        {
            int n = Mathf.CeilToInt(Mathf.RoundToInt(MaxTwistedFlesh * TwistedFleshTargetValue)
                                   - CurrentTwistedFlesh);
            return n > 0 ? n : 0;
        }
    }

    public override void PostPostMake()
    {
        base.PostPostMake();
        if (MaxTwistedFlesh > 0)
        {
            CurrentTwistedFlesh = MaxTwistedFlesh;
        }
    }

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        if (MaxTwistedFlesh > 0 && this.parent is Pawn pawn)
        {
            MapComponent_FleshHive comp = pawn.Map?.GetComponent<MapComponent_FleshHive>();
            comp?.RegisterTwistedFlesh(pawn);
        }
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        base.PostDeSpawn(map, mode);
        if (this.parent is Pawn pawn)
        {
            MapComponent_FleshHive comp = map?.GetComponent<MapComponent_FleshHive>();
            comp?.UnregisterTwistedFlesh(pawn);
        }
    }

    public override void PostDestroy(DestroyMode mode, Map previousMap)
    {
        base.PostDestroy(mode, previousMap);
        if (this.parent is Pawn pawn)
        {
            MapComponent_FleshHive comp = previousMap?.GetComponent<MapComponent_FleshHive>();
            comp?.UnregisterTwistedFlesh(pawn);
        }
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref currentTwistedFlesh, "currentTwistedFlesh");
        Scribe_Values.Look(ref twistedFleshTargetValue, "twistedFleshTargetValue", 1f);
        Scribe_Values.Look(ref allowAutoRefillTwistedFlesh, "allowAutoRefillTwistedFlesh", true);
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        if (MaxTwistedFlesh > 0 && this.parent.Faction == Faction.OfPlayer)
        {
            yield return new Gizmo_TwistedFleshStatus(this);
        }
        if (DebugSettings.ShowDevGizmos && MaxTwistedFlesh > 0)
        {
            yield return new Command_Action
            {
                defaultLabel = "DEV: +10% twisted flesh",
                action = delegate
                {
                    FillTwistedFlesh(Mathf.Max(1, MaxTwistedFlesh / 10));
                }
            };
            yield return new Command_Action
            {
                defaultLabel = "DEV: -10% twisted flesh",
                action = delegate
                {
                    ConsumeTwistedFlesh(Mathf.Max(1, MaxTwistedFlesh / 10));
                }
            };
        }
    }

    private float currentTwistedFlesh;
    private float twistedFleshTargetValue = 1f;
    private bool allowAutoRefillTwistedFlesh = true;
}

public class Gizmo_TwistedFleshStatus : Gizmo_Slider
{
    public Gizmo_TwistedFleshStatus(CompTwistedFlesh comp)
    {
        this.comp = comp;
    }

    protected override float Target
    {
        get => this.comp.TwistedFleshTargetValue;
        set => this.comp.TwistedFleshTargetValue = value;
    }

    protected override bool DraggingBar
    {
        get => draggingBar;
        set => draggingBar = value;
    }

    protected override Color BarColor => FleshHiveDefOf.FH_Resource_TwistedFlesh.color;
    protected override bool IsDraggable => true;
    protected override float ValuePercent => this.comp.MaxTwistedFlesh > 0
        ? (float)this.comp.CurrentTwistedFlesh / this.comp.MaxTwistedFlesh
        : 0f;
    protected override string Title => FleshHiveDefOf.FH_Resource_TwistedFlesh.label;
    protected override string BarLabel => this.comp.CurrentTwistedFlesh.ToString("F2")
                                          + "/" + this.comp.MaxTwistedFlesh;

    protected override void DrawHeader(Rect headerRect, ref bool mouseOverElement)
    {
        mouseOverElement = false;
        headerRect.xMax -= 24f;
        Rect checkRect = new Rect(headerRect.xMax, headerRect.y, 24f, 24f);
        GUI.DrawTexture(checkRect, this.comp.AllowAutoRefillTwistedFlesh
            ? Widgets.CheckboxOnTex
            : Widgets.CheckboxOffTex);
        if (Widgets.ButtonInvisible(checkRect, true))
        {
            this.comp.AllowAutoRefillTwistedFlesh = !this.comp.AllowAutoRefillTwistedFlesh;
            (this.comp.AllowAutoRefillTwistedFlesh ? SoundDefOf.Tick_High : SoundDefOf.Tick_Low)
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
    private readonly CompTwistedFlesh comp;
}
