using System.Collections.Generic;
using HiveCreatureFramework;
using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive;

public class CompSynapsePack : ThingComp
{
    public CompProperties_SynapsePack Props => (CompProperties_SynapsePack)props;

    public int CurrentTwistedFlesh => currentTwistedFlesh;

    public int MaxTwistedFlesh => Mathf.Max(0, Props.capacity);

    public static CompSynapsePack? GetWorn(Pawn? pawn)
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

    public override void Notify_Equipped(Pawn pawn)
    {
        base.Notify_Equipped(pawn);
        ParasitismSystem? system = EnsureSystem(pawn);
        if (system != null)
        {
            system.SetDirty();
            capacityDirty = false;
            TransferStoredTwistedFlesh(pawn, system);
        }
        EnsureNode(pawn);
        pawn.Map?.GetComponent<MapComponent_FleshHive>()?.RegisterTwistedFlesh(pawn);
    }

    public override void Notify_Unequipped(Pawn pawn)
    {
        base.Notify_Unequipped(pawn);
        ParasitismSystem? system = pawn.health?.hediffSet?.GetFirstHediffOfDef(FleshHiveDefOf.FH_ParasitismSystem)
            as ParasitismSystem;
        if (system != null)
        {
            StoreTwistedFlesh(pawn, system);
        }
        else
        {
            Log.Error("[FleshHive] Could not find the twisted flesh system when removing FH_SynapsePack from " + pawn + ".");
        }
        RemoveNode(pawn);
        system?.SetDirty();
        if (createdSystem && system != null && system.ParasitismHediffs.Count == 0
            && pawn.health?.hediffSet?.HasHediff(FleshHiveDefOf.FH_Hela) != true)
        {
            pawn.health?.RemoveHediff(system);
        }
        createdSystem = false;
        capacityDirty = true;
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
            ParasitismSystem? system = EnsureSystem(pawn);
            if (system != null)
            {
                if (capacityDirty)
                {
                    system.SetDirty();
                    capacityDirty = false;
                }
                TransferStoredTwistedFlesh(pawn, system);
            }
            EnsureNode(pawn);
            pawn.Map?.GetComponent<MapComponent_FleshHive>()?.RegisterTwistedFlesh(pawn);
        }
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref currentTwistedFlesh, "synapsePackTwistedFlesh");
        Scribe_Values.Look(ref createdSystem, "synapsePackCreatedSystem");
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            currentTwistedFlesh = Mathf.Clamp(currentTwistedFlesh, 0, MaxTwistedFlesh);
            capacityDirty = true;
        }
    }

    private ParasitismSystem? EnsureSystem(Pawn pawn)
    {
        if (pawn.health?.hediffSet?.GetFirstHediffOfDef(FleshHiveDefOf.FH_ParasitismSystem)
            is ParasitismSystem system)
        {
            return system;
        }

        ParasitismSystem? addedSystem = pawn.health?.AddHediff(FleshHiveDefOf.FH_ParasitismSystem)
            as ParasitismSystem;
        if (addedSystem == null)
        {
            Log.Error("[FleshHive] Could not add the twisted flesh system to " + pawn + " for FH_SynapsePack.");
            return null;
        }

        createdSystem = true;
        capacityDirty = true;
        return addedSystem;
    }

    private void TransferStoredTwistedFlesh(Pawn pawn, ParasitismSystem system)
    {
        if (currentTwistedFlesh <= 0)
        {
            return;
        }

        CompTwistedFlesh? comp = pawn.TryGetComp<CompTwistedFlesh>();
        int before = comp != null ? Mathf.FloorToInt(comp.CurrentTwistedFlesh) : system.CurrentTwistedFlesh;
        if (comp != null)
        {
            comp.FillTwistedFlesh(currentTwistedFlesh);
        }
        else
        {
            system.FillTwistedFlesh(currentTwistedFlesh);
        }

        int after = comp != null ? Mathf.FloorToInt(comp.CurrentTwistedFlesh) : system.CurrentTwistedFlesh;
        currentTwistedFlesh -= Mathf.Clamp(after - before, 0, currentTwistedFlesh);
        if (currentTwistedFlesh > 0)
        {
            Log.ErrorOnce("[FleshHive] Could not transfer all stored twisted flesh from " + parent + " to " + pawn + ".",
                parent.thingIDNumber);
        }
    }

    private void StoreTwistedFlesh(Pawn pawn, ParasitismSystem system)
    {
        int available = MaxTwistedFlesh - currentTwistedFlesh;
        if (available <= 0)
        {
            return;
        }

        CompTwistedFlesh? comp = pawn.TryGetComp<CompTwistedFlesh>();
        int storedByPawn = comp != null ? Mathf.FloorToInt(comp.CurrentTwistedFlesh) : system.CurrentTwistedFlesh;
        int capacityWithoutPack = (comp?.BaseMaxTwistedFlesh ?? 0) + system.IntrinsicTwistedFleshCapacity;
        int amount = Mathf.Min(available, Mathf.Max(0, storedByPawn - capacityWithoutPack));
        if (amount <= 0)
        {
            return;
        }

        bool consumed = comp != null ? comp.ConsumeTwistedFlesh(amount) : system.ConsumeTwistedFlesh(amount);
        if (consumed)
        {
            currentTwistedFlesh += amount;
        }
        else
        {
            Log.Error("[FleshHive] Could not store twisted flesh in " + parent + " while removing it from " + pawn + ".");
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
    private bool createdSystem;
    private bool capacityDirty = true;
}
