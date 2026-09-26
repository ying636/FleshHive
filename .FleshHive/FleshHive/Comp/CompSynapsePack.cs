using System.Collections.Generic;
using HiveCreatureFramework;
using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive;

public class CompSynapsePack : CompPawnResourceContainer
{
    public CompProperties_SynapsePack Props => (CompProperties_SynapsePack)props;

    public int CurrentTwistedFlesh => currentTwistedFlesh;

    public int MaxTwistedFlesh => Mathf.Max(0, Props.capacity);

    public int NeededAmount => Mathf.Max(0, Mathf.RoundToInt(MaxTwistedFlesh * targetValue) - currentTwistedFlesh);

    public bool AllowAutoRefillTwistedFlesh => allowAutoRefillTwistedFlesh;

    public override IEnumerable<HiveResourceDef> ResourceDefs
    {
        get
        {
            yield return FleshHiveDefOf.FH_Resource_TwistedFlesh;
        }
    }

    public override HiveResourceDef PrimaryResourceDef => FleshHiveDefOf.FH_Resource_TwistedFlesh;

    public static CompSynapsePack? GetWorn(Pawn pawn)
    {
        if (pawn?.apparel == null)
        {
            return null;
        }

        foreach (Apparel apparel in pawn.apparel.WornApparel)
        {
            if (apparel.TryGetComp<CompSynapsePack>() is { } pack)
            {
                return pack;
            }
        }

        return null;
    }

    public override bool HasResource(HiveResourceDef resourceDef)
    {
        return resourceDef != null && resourceDef == FleshHiveDefOf.FH_Resource_TwistedFlesh;
    }

    public override float GetAmount(HiveResourceDef resourceDef)
    {
        return HasResource(resourceDef) ? currentTwistedFlesh : 0f;
    }

    public override void SetAmount(HiveResourceDef resourceDef, float amount)
    {
        if (HasResource(resourceDef))
        {
            currentTwistedFlesh = Mathf.Clamp(Mathf.RoundToInt(amount), 0, MaxTwistedFlesh);
        }
    }

    public override float GetLimit(HiveResourceDef resourceDef)
    {
        return HasResource(resourceDef) ? MaxTwistedFlesh : 0f;
    }

    public override float GetTargetValue(HiveResourceDef resourceDef)
    {
        return HasResource(resourceDef) ? targetValue : 0f;
    }

    public override void SetTargetValue(HiveResourceDef resourceDef, float value)
    {
        if (HasResource(resourceDef))
        {
            targetValue = Mathf.Clamp01(value);
        }
    }

    public override bool GetAllowedToFill(HiveResourceDef resourceDef)
    {
        return HasResource(resourceDef) && allowAutoRefillTwistedFlesh;
    }

    public override void SetAllowedToFill(HiveResourceDef resourceDef, bool allowedToFill)
    {
        if (HasResource(resourceDef))
        {
            allowAutoRefillTwistedFlesh = allowedToFill;
        }
    }

    public bool ConsumeTwistedFlesh(int amount)
    {
        if (amount < 0 || currentTwistedFlesh < amount)
        {
            return false;
        }

        currentTwistedFlesh -= amount;
        return true;
    }

    public int FillTwistedFlesh(int amount)
    {
        int accepted = Mathf.Clamp(amount, 0, MaxTwistedFlesh - currentTwistedFlesh);
        currentTwistedFlesh += accepted;
        return accepted;
    }

    public override void Notify_Equipped(Pawn pawn)
    {
        base.Notify_Equipped(pawn);
        EnsureNode(pawn);
        pawn.Map?.GetComponent<MapComponent_FleshHive>()?.RegisterTwistedFlesh(pawn);
    }

    public override void Notify_Unequipped(Pawn pawn)
    {
        base.Notify_Unequipped(pawn);
        RemoveNode(pawn);
        MapComponent_FleshHive? mapComponent = pawn.Map?.GetComponent<MapComponent_FleshHive>();
        mapComponent?.UnregisterTwistedFlesh(pawn);
        if (pawn.TryGetComp<CompTwistedFlesh>()?.MaxTwistedFlesh > 0
            || pawn.health?.hediffSet?.GetFirstHediffOfDef(FleshHiveDefOf.FH_ParasitismSystem)
                is ParasitismSystem { MaxTwistedFlesh: > 0 })
        {
            mapComponent?.RegisterTwistedFlesh(pawn);
        }
    }

    public override void CompTick()
    {
        base.CompTick();
        Pawn? pawn = (parent as Apparel)?.Wearer;
        if (pawn != null && !pawn.Dead && pawn.IsHashIntervalTick(250))
        {
            EnsureNode(pawn);
            pawn.Map?.GetComponent<MapComponent_FleshHive>()?.RegisterTwistedFlesh(pawn);
        }
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref currentTwistedFlesh, "synapsePackTwistedFlesh");
        Scribe_Values.Look(ref targetValue, "synapsePackTargetValue", 1f);
        Scribe_Values.Look(ref allowAutoRefillTwistedFlesh, "synapsePackAutoRefill", true);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            currentTwistedFlesh = Mathf.Clamp(currentTwistedFlesh, 0, MaxTwistedFlesh);
            targetValue = Mathf.Clamp01(targetValue);
        }
    }

    public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
    {
        if ((parent as Apparel)?.Wearer?.Faction?.IsPlayer == true)
        {
            yield return new Gizmo_ResourceForUnitSlider(this, FleshHiveDefOf.FH_Resource_TwistedFlesh);
        }
    }

    private void EnsureNode(Pawn pawn)
    {
        if (pawn.Dead)
        {
            return;
        }

        HediffDef? nodeHediff = Props.nodeHediff;
        if (nodeHediff == null)
        {
            Log.ErrorOnce("[FleshHive] FH_SynapsePack has no node HediffDef.", 18736420);
            return;
        }

        if (pawn.health?.hediffSet?.HasHediff(nodeHediff) == true)
        {
            return;
        }

        if (pawn.health?.AddHediff(nodeHediff) == null)
        {
            Log.Error("[FleshHive] Could not add " + nodeHediff.defName + " to " + pawn + ".");
        }
    }

    private void RemoveNode(Pawn pawn)
    {
        HediffDef? nodeHediff = Props.nodeHediff;
        Hediff? node = nodeHediff == null ? null : pawn.health?.hediffSet?.GetFirstHediffOfDef(nodeHediff);
        if (node != null)
        {
            HediffComp_HelaNode? nodeComp = (node as HediffWithComps)?.TryGetComp<HediffComp_HelaNode>();
            if (nodeComp != null)
            {
                foreach (UnitGroup group in nodeComp.Groups)
                {
                    if (group == null)
                    {
                        continue;
                    }

                    foreach (Pawn unit in new List<Pawn>(group.units))
                    {
                        group.RemoveUnit(unit);
                    }
                }
            }

            pawn.health?.RemoveHediff(node);
        }
    }

    private int currentTwistedFlesh;

    private float targetValue = 1f;

    private bool allowAutoRefillTwistedFlesh = true;
}
