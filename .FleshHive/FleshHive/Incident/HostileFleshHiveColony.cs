using HiveCreatureFramework;
using RimWorld;
using Verse;
using Verse.AI;

namespace FleshHive;

public partial class HostileFleshHiveColony : IExposable
{
    public HostileFleshHiveColony()
    {
    }

    public HostileFleshHiveColony(HiveLord lord)
    {
        this.lord = lord;
        origin = lord.hives[0].Position;
        orientation = Rand.Range(0, 4);
        slots.Add(new HostileHiveSlot
        {
            def = lord.hives[0].def, anchor = origin, position = origin,
            building = lord.hives[0], cost = 50f, minHives = 1
        });
        foreach (HostileHivePlanSlot plan in Settings.layout)
        {
            IntVec3 offset = plan.offset;
            for (int index = 0; index < orientation; index++)
                offset = new IntVec3(offset.z, 0, -offset.x);
            slots.Add(new HostileHiveSlot
            {
                def = plan.building, anchor = origin + offset, cost = plan.cost, minHives = plan.minHives
            });
        }
    }

    public HiveLord Lord => lord;
    public HiveLordDef_HostileFleshHive Settings => (HiveLordDef_HostileFleshHive)lord.def;
    public IntVec3 Origin => origin;
    public IReadOnlyList<HostileHiveSlot> Slots => slots;
    public float Nutrition => Resources.Sum(resource => resource.Amount);
    public float ExpansionCost => Resources.Select(resource => resource.GetLimit() * lord.def.expansionResourceFraction).DefaultIfEmpty(50f).Min();
    public bool Hunting => prey != null || huntGroup?.units.Count > 0;
    public Building_HostileFleshStore? Store => slots.Where(slot => slot.Occupied)
        .Select(slot => slot.building).OfType<Building_HostileFleshStore>().FirstOrDefault();
    public int HealthyCount => lord.groups.SelectMany(group => group.units).Distinct().Count(IsHealthy);
    public int HealthyDefenders => lord.groups.Where(group => group != huntGroup
        && group.WorkModeDef != HCFDefOf.HCF_GroupWorkMode_Attack).SelectMany(group => group.units).Distinct().Count(IsHealthy);
    public bool CanRaid
    {
        get
        {
            List<Pawn> ready = lord.groups.SelectMany(group => group.units).Distinct()
                .Where(pawn => pawn.Spawned && pawn.Map == lord.Map && !pawn.Dead && !pawn.Downed
                    && pawn.Faction == lord.faction && pawn.health.summaryHealth.SummaryHealthPercent >= 0.6f
                    && (pawn.GetNeed<Need_Maintenance>()?.CurLevelPercentage ?? 1f) > 0.25f)
                .OrderByDescending(pawn => pawn.health.summaryHealth.SummaryHealthPercent).ThenBy(pawn => pawn.thingIDNumber).ToList();
            int count = Math.Min((int)(ready.Count * lord.def.attackFraction), ready.Count - lord.def.minDefenders);
            return !Hunting && count >= lord.def.minAttackUnits && ready.Skip(Math.Max(0, count)).Count(IsHealthy) >= lord.def.minDefenders;
        }
    }
    public bool HasNerveCenter => slots.Any(slot => slot.Occupied && slot.def == HostileFleshHiveDefOf.FH_HostileNerveCenter);
    private IEnumerable<HiveResource> Resources => lord.hives.Where(hive => hive.Spawned && hive.Faction == lord.faction)
        .SelectMany(hive => hive.TryGetComp<CompHiveResource>().resources)
        .Where(resource => resource.def == HostileFleshHiveDefOf.FH_Resource_HostileNutrition);

    public bool Owns(Thing building)
    {
        return lord != null && (lord.hives.Contains(building) || slots.Any(slot => slot.building == building));
    }

    public void Tick()
    {
        RefreshSlots();
        LinkGroups();
        BalanceResources();
        PrepareConstructionSites();
        TickConstruction();
        TickHunting();
        TickRecovery();
    }

    public float AddNutrition(float amount)
    {
        float remaining = amount;
        foreach (HiveResource resource in Resources.OrderBy(resource => resource.comp.parent == lord.hives[0] ? 0 : 1))
        {
            float addition = Math.Min(remaining, Math.Max(0f, resource.GetLimit() - resource.Amount));
            resource.IncreaseResource(addition);
            remaining -= addition;
            if (remaining <= 0f)
                break;
        }
        return amount - remaining;
    }

    public bool TrySpend(float amount)
    {
        if (amount < 0f || Nutrition + 0.0001f < amount)
            return false;
        float remaining = amount;
        foreach (HiveResource resource in Resources.OrderByDescending(resource => resource.Amount))
        {
            float payment = Math.Min(remaining, resource.Amount);
            resource.DecreaseResource(payment);
            remaining -= payment;
            if (remaining <= 0f)
                break;
        }
        return true;
    }

    public string GetStatus()
    {
        string key = lord.Threatened ? "FH_HostileHive_Defending" : lord.Attacking ? "FH_HostileHive_Attacking"
            : Hunting ? "FH_HostileHive_Hunting" : lord.Expanding ? "FH_HostileHive_Expanding"
            : slots.Any(slot => slot.finishTick > 0) ? "FH_HostileHive_Constructing" : "FH_HostileHive_Developing";
        return key.Translate();
    }

    public void ExposeData()
    {
        Scribe_References.Look(ref lord, "lord");
        Scribe_Values.Look(ref origin, "origin");
        Scribe_Values.Look(ref orientation, "orientation");
        Scribe_Collections.Look(ref slots, "slots", LookMode.Deep);
        Scribe_References.Look(ref huntGroup, "huntGroup");
        Scribe_References.Look(ref prey, "prey");
        Scribe_References.Look(ref corpse, "corpse");
        Scribe_Values.Look(ref huntStartedTick, "huntStartedTick");
        Scribe_Values.Look(ref nextHuntTick, "nextHuntTick");
        Scribe_Values.Look(ref gathered, "gathered");
    }

    public void Disband()
    {
        foreach (HostileHiveSlot slot in slots)
            if (slot.building is { Spawned: true } building && building.Faction == lord?.faction)
                building.Destroy(DestroyMode.KillFinalize);
    }

    private bool IsHealthy(Pawn pawn)
    {
        return pawn.Spawned && pawn.Map == lord.Map && pawn.Faction == lord.faction && !pawn.Dead && !pawn.Downed
            && pawn.health.summaryHealth.SummaryHealthPercent >= 0.85f
            && (pawn.GetNeed<Need_Maintenance>()?.CurLevelPercentage ?? 1f) >= 0.65f;
    }

    private void LinkGroups()
    {
        foreach (UnitGroup group in lord.groups)
        {
            group.Resources.RemoveWhere(resource => !lord.hives.Contains(resource.parent));
            group.Containers.RemoveWhere(container => !lord.hives.Contains(container.parent));
            foreach (Thing hive in lord.hives)
            {
                if (hive.TryGetComp<CompHiveResource>() is { } resource && resource.CanAdd(group))
                    resource.AddForGroup(group);
                if (hive.TryGetComp<CompHiveContainer>() is { } container && container.CanAdd(group))
                    container.AddForGroup(group);
            }
        }
    }

    private void BalanceResources()
    {
        List<HiveResource> resources = Resources.ToList();
        if (resources.Count < 2)
            return;
        HiveResource treasury = resources[0];
        foreach (HiveResource resource in resources.Skip(1))
        {
            float transfer = Math.Min(Math.Max(0f, resource.Amount - 12f), treasury.GetLimit() - treasury.Amount);
            resource.DecreaseResource(transfer);
            treasury.IncreaseResource(transfer);
            float refill = Math.Min(Math.Max(0f, 12f - resource.Amount), Math.Max(0f, treasury.Amount - Settings.reserveNutrition));
            treasury.DecreaseResource(refill);
            resource.IncreaseResource(refill);
        }
    }

    private void TickRecovery()
    {
        if (lord.Threatened || Nutrition <= 0f)
            return;
        foreach (UnitGroup group in lord.groups.Where(group => group != huntGroup
            && group.WorkModeDef != HCFDefOf.HCF_GroupWorkMode_Attack))
        {
            group.maintenanceThresholds = new FloatRange(0.65f, 1f);
            foreach (Pawn pawn in group.units.Where(pawn => pawn.Spawned && !pawn.Dead && !pawn.Downed
                && pawn.Faction == lord.faction && pawn.CurJobDef != HCFDefOf.HCF_EnterHive))
            {
                if (!(pawn.health.summaryHealth.SummaryHealthPercent < 0.9f && pawn.health.hediffSet.HasNaturallyHealingInjury())
                    && (pawn.GetNeed<Need_Maintenance>()?.CurLevelPercentage ?? 1f) >= 0.65f)
                    continue;
                Thing? container = group.GetContainer(pawn, true);
                if (container?.Map == lord.Map && pawn.CanReach(container, PathEndMode.Touch, Danger.Deadly))
                    pawn.jobs.StartJob(JobMaker.MakeJob(HCFDefOf.HCF_EnterHive, container), JobCondition.InterruptForced);
            }
        }
    }

    private HiveLord lord = null!;
    private IntVec3 origin;
    private int orientation;
    private List<HostileHiveSlot> slots = new();
}
