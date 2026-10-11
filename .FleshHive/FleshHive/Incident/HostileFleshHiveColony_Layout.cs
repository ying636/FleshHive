using HiveCreatureFramework;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace FleshHive;

public partial class HostileFleshHiveColony
{
    public bool CanExpand => Store != null && HealthyCount >= Settings.minHunters + lord.def.minDefenders
        && Nutrition >= Settings.reserveNutrition + (lord.Expanding ? 0f : ExpansionCost)
        && !slots.Any(slot => slot.finishTick > 0);

    public float ScoreHiveCell(ThingDef building, IntVec3 cell)
    {
        HostileHiveSlot? slot = slots.FirstOrDefault(slot => slot.def == building && !slot.Occupied);
        if (slot == null || cell.DistanceToSquared(slot.anchor) > 9f || !FitsLayout(slot, cell))
            return float.NegativeInfinity;
        return -cell.DistanceToSquared(slot.anchor);
    }

    private void RefreshSlots()
    {
        foreach (HostileHiveSlot slot in slots)
        {
            if (slot.building != null && (!slot.Occupied || slot.building.Faction != lord.faction))
                slot.building = null;
            if (slot.def == HostileFleshHiveDefOf.FH_HostileFleshHive && !slot.Occupied)
            {
                slot.building = lord.hives.FirstOrDefault(hive => hive.Position.DistanceToSquared(slot.anchor) <= 9f);
                if (slot.building != null)
                    slot.position = slot.building.Position;
            }
        }
    }

    private void TickConstruction()
    {
        if (lord.Threatened || lord.Expanding)
        {
            foreach (HostileHiveSlot slot in slots.Where(slot => slot.finishTick > 0))
                slot.finishTick += 250;
            return;
        }
        HostileHiveSlot? pending = slots.FirstOrDefault(slot => slot.finishTick > 0);
        if (pending != null)
        {
            if (Find.TickManager.TicksGame < pending.finishTick)
                return;
            pending.finishTick = 0;
            if (!CanPlaceSupport(pending, pending.position) || Nutrition < Settings.reserveNutrition + pending.cost)
            {
                lord.RecordDebugDecision("Expansion", "ShouldExpand", target: pending.def,
                    details: "FH_HostileHive_BuildBlocked".Translate());
                return;
            }
            if (!TrySpend(pending.cost))
                throw new InvalidOperationException("[FleshHive] Hostile support construction budget changed during completion.");
            Thing support = ThingMaker.MakeThing(pending.def);
            support.SetFaction(lord.faction);
            pending.building = GenSpawn.Spawn(support, pending.position, lord.Map, pending.def.defaultPlacingRot);
            foreach (PlaceWorker worker in pending.def.PlaceWorkers ?? Enumerable.Empty<PlaceWorker>())
                worker.PostPlace(lord.Map, pending.def, pending.position, pending.def.defaultPlacingRot);
            lord.RecordDebugDecision("Expansion", "ExpansionCompleted", support, pending.def, true);
            return;
        }
        foreach (HostileHiveSlot slot in slots.Where(slot => slot.def != HostileFleshHiveDefOf.FH_HostileFleshHive
            && !slot.Occupied && slot.minHives <= lord.hives.Count))
        {
            if (Nutrition < Settings.reserveNutrition + slot.cost)
                return;
            IEnumerable<IntVec3> cells = slot.position.IsValid ? new[] { slot.position }
                : GenRadial.RadialCellsAround(slot.anchor, slot.def == HostileFleshHiveDefOf.FH_HostileFleshBarricade ? 0f : 2f, true)
                    .OrderBy(cell => cell.DistanceToSquared(slot.anchor));
            foreach (IntVec3 cell in cells)
            {
                if (!CanPlaceSupport(slot, cell))
                    continue;
                slot.position = cell;
                slot.finishTick = Find.TickManager.TicksGame + Settings.constructionTicks;
                lord.RecordDebugDecision("Expansion", "ExpansionStarted", target: slot.def, executed: true,
                    details: cell.ToString());
                return;
            }
            lord.RecordDebugDecision("Expansion", "NoExpansionCandidate", target: slot.def,
                details: "FH_HostileHive_BuildBlocked".Translate() + $": {slot.anchor}");
        }
    }

    private void PrepareConstructionSites()
    {
        if (lord.Threatened)
            return;
        HostileHiveSlot? nextHive = slots.FirstOrDefault(slot => slot.def == HostileFleshHiveDefOf.FH_HostileFleshHive && !slot.Occupied);
        foreach (HostileHiveSlot slot in slots.Where(slot => !slot.Occupied && slot.minHives <= lord.hives.Count
            && (slot.def != HostileFleshHiveDefOf.FH_HostileFleshHive || slot == nextHive)
            && Nutrition >= Settings.reserveNutrition + slot.cost))
        {
            float radius = slot.def == HostileFleshHiveDefOf.FH_HostileFleshHive ? 3f
                : slot.def == HostileFleshHiveDefOf.FH_HostileFleshBarricade ? 0f : 2f;
            IEnumerable<IntVec3> centers = slot.position.IsValid ? new[] { slot.position }
                : GenRadial.RadialCellsAround(slot.anchor, radius, true);
            foreach (IntVec3 cell in centers.SelectMany(center => GenAdj.OccupiedRect(center, slot.def.defaultPlacingRot, slot.def.size)).Distinct())
            {
                if (!cell.InBounds(lord.Map) || cell.Fogged(lord.Map))
                    continue;
                foreach (Thing vegetation in cell.GetThingList(lord.Map)
                    .Where(thing => (thing is Filth || thing is Plant) && GenSpawn.SpawningWipes(slot.def, thing.def)).ToArray())
                    vegetation.Destroy();
            }
        }
    }

    private bool CanPlaceSupport(HostileHiveSlot slot, IntVec3 cell)
    {
        return cell.IsValid && FitsLayout(slot, cell)
            && lord.CanPlaceExpansion(slot.def, null, cell, slot.def.defaultPlacingRot)
            && GenAdj.OccupiedRect(cell, slot.def.defaultPlacingRot, slot.def.size).Cells
                .All(occupied => !occupied.Fogged(lord.Map) && lord.Map.terrainGrid.FoundationAt(occupied)?.IsSubstructure != true)
            && lord.Map.reachability.CanReach(lord.hives[0].Position, cell,
                PathEndMode.Touch, TraverseMode.PassDoors, Danger.Deadly);
    }

    private bool FitsLayout(HostileHiveSlot candidate, IntVec3 cell)
    {
        CellRect rect = GenAdj.OccupiedRect(cell, candidate.def.defaultPlacingRot, candidate.def.size);
        foreach (IntVec3 occupied in rect)
        {
            if (!occupied.InBounds(lord.Map) || occupied.InNoBuildEdgeArea(lord.Map) || occupied.Fogged(lord.Map)
                || lord.Map.terrainGrid.FoundationAt(occupied)?.IsSubstructure == true)
                return false;
            foreach (HostileHiveSlot other in slots)
            {
                if (other == candidate)
                    continue;
                IntVec3 center = other.position.IsValid ? other.position : other.anchor;
                if (GenAdj.OccupiedRect(center, other.def.defaultPlacingRot, other.def.size).Contains(occupied))
                    return false;
            }
            foreach (HostileHiveSlot hive in slots.Where(slot => slot.def == HostileFleshHiveDefOf.FH_HostileFleshHive))
            {
                if (hive == candidate || occupied.DistanceToSquared(origin) < 16f)
                    continue;
                Vector2 direction = new(hive.anchor.x - origin.x, hive.anchor.z - origin.z);
                if (direction.sqrMagnitude == 0f)
                    continue;
                Vector2 point = new(occupied.x - origin.x, occupied.z - origin.z);
                float fraction = Mathf.Clamp01(Vector2.Dot(point, direction) / direction.sqrMagnitude);
                if ((point - direction * fraction).sqrMagnitude < 2.25f)
                    return false;
            }
        }
        return true;
    }
}
