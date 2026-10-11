using HiveCreatureFramework;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace FleshHive;

public partial class HostileFleshHiveColony
{
    public Pawn? Prey => prey;
    public Corpse? Corpse => corpse;
    public bool Gathered => gathered;
    public UnitGroup? HuntGroup => huntGroup;

    public void FinishHunt()
    {
        if (huntGroup != null)
        {
            foreach (Pawn pawn in huntGroup.units.ToArray())
            {
                UnitGroup? home = lord.groups.FirstOrDefault(group => group != huntGroup && group.CanAccept(pawn).Accepted);
                if (home == null)
                    throw new InvalidOperationException($"[FleshHive] No defending group can receive returning hunter {pawn}.");
                pawn.jobs?.StopAll();
                pawn.mindState.enemyTarget = null;
                home.AcceptUnit(pawn);
                home.SetTarget(new TargetInfo(home.hive), false);
                home.SetMode(HCFDefOf.HCF_GroupWorkMode_Defend, false);
            }
            if (lord.hives.FirstOrDefault(hive => hive.Spawned) is { } homeHive)
            {
                huntGroup.SetTarget(new TargetInfo(homeHive), false);
                huntGroup.SetMode(HCFDefOf.HCF_GroupWorkMode_Defend, false);
            }
        }
        prey = null;
        corpse = null;
        gathered = false;
        nextHuntTick = Find.TickManager.TicksGame + Settings.huntCooldownTicks;
    }

    private void TickHunting()
    {
        int now = Find.TickManager.TicksGame;
        if (Hunting)
        {
            if (prey?.Dead == true && corpse == null)
                corpse = prey.Corpse;
            if (corpse?.holdingOwner == Store?.GetDirectlyHeldThings() && corpse != null)
            {
                lord.RecordDebugDecision("Recall", "RecallRequested", prey, executed: true);
                FinishHunt();
                return;
            }
            if (lord.Threatened || lord.Attacking || Store is not { CanReceive: true }
                || now - huntStartedTick > Settings.huntTimeoutTicks
                || huntGroup == null || huntGroup.units.Count(IsHuntFit) < Settings.minHunters
                || HealthyDefenders < lord.def.minDefenders
                || prey == null || prey.Destroyed && corpse == null
                || !prey.Dead && (!prey.Spawned || prey.Map != lord.Map || prey.Faction != null)
                || prey.Dead && (corpse == null || corpse.Destroyed || corpse.GetRotStage() == RotStage.Dessicated))
            {
                lord.RecordDebugDecision("Recall", "ShouldRecall", prey, executed: true,
                    details: $"{huntGroup?.units.Count(IsHuntFit)}/{Settings.minHunters} | {HealthyDefenders} | {now - huntStartedTick} | {lord.Threatened} | {corpse}");
                FinishHunt();
                return;
            }
            if (huntGroup.WorkModeDef != HostileFleshHiveDefOf.FH_GroupWorkMode_HostileHunt)
                huntGroup.SetMode(HostileFleshHiveDefOf.FH_GroupWorkMode_HostileHunt, false);
            if (!gathered && huntGroup.units.Where(IsHealthy).All(pawn => pawn.Position.InHorDistOf(prey.Position, 9f)))
            {
                gathered = true;
                foreach (Pawn pawn in huntGroup.units.Where(IsHealthy))
                    pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
            }
            return;
        }
        if (now < nextHuntTick || lord.Threatened || lord.Attacking || Nutrition >= Settings.huntStopNutrition
            || Store is not { CanReceive: true } || Store.StoredCount >= 3)
            return;
        nextHuntTick = now + Settings.huntCooldownTicks;
        List<Pawn> ready = lord.groups.Where(group => group != huntGroup)
            .SelectMany(group => group.units).Distinct().Where(IsHealthy)
            .OrderByDescending(pawn => pawn.health.summaryHealth.SummaryHealthPercent).ThenBy(pawn => pawn.thingIDNumber).ToList();
        int count = Math.Min(Settings.maxHunters, ready.Count - lord.def.minDefenders);
        if (count < Settings.minHunters)
            return;
        Pawn leader = ready[0];
        Corpse? salvage = lord.Map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse).OfType<Corpse>()
            .Where(corpse => corpse.InnerPawn.RaceProps.Animal && corpse.InnerPawn.Faction == null
                && corpse.GetRotStage() != RotStage.Dessicated && corpse.InnerPawn.RaceProps.meatDef != null
                && corpse.Position.DistanceToSquared(origin) <= Settings.huntRadius * Settings.huntRadius
                && !corpse.Position.Fogged(lord.Map))
            .OrderBy(corpse => corpse.Position.DistanceToSquared(origin)).Take(12)
            .FirstOrDefault(corpse => leader.CanReserveAndReach(corpse, PathEndMode.Touch, Danger.Deadly));
        Pawn? candidate = lord.Map.mapPawns.AllPawnsSpawned.Where(pawn => pawn.Faction == null && pawn.RaceProps.Animal
                && !pawn.Dead && !pawn.Downed && !pawn.IsPsychologicallyInvisible() && !pawn.Position.Fogged(lord.Map)
                && pawn.RaceProps.meatDef != null && pawn.GetStatValue(StatDefOf.MeatAmount) > 0f
                && pawn.kindDef.combatPower <= count * 25f
                && pawn.Position.DistanceToSquared(origin) <= Settings.huntRadius * Settings.huntRadius)
            .OrderByDescending(pawn => pawn.GetStatValue(StatDefOf.MeatAmount) * pawn.RaceProps.meatDef.GetStatValueAbstract(StatDefOf.Nutrition)
                / (10f + pawn.Position.DistanceTo(origin) + pawn.kindDef.combatPower * 0.3f))
            .Take(12).FirstOrDefault(pawn => ready.Take(count).All(hunter => hunter.CanReach(pawn, PathEndMode.Touch, Danger.Deadly))
                && leader.CanReach(Store, PathEndMode.Touch, Danger.Deadly));
        candidate = salvage?.InnerPawn ?? candidate;
        if (candidate == null)
        {
            lord.RecordDebugDecision("Attack", "NoAttackTarget", details: "FH_HostileHive_NoPrey".Translate());
            return;
        }
        if (huntGroup == null || !lord.groups.Contains(huntGroup))
        {
            huntGroup = (HostileFleshHiveHuntGroup)GameComponent_UnitGroup.Instance.MakeNewGroup(typeof(HostileFleshHiveHuntGroup));
            huntGroup.hive = lord.hives[0];
            huntGroup.map = lord.Map;
            huntGroup.unitLimit = lord.def.groupCapacity;
            huntGroup.tags.Add("Flesh");
            huntGroup.Make();
            huntGroup.SetTarget(new TargetInfo(lord.hives[0]), false);
            huntGroup.SetMode(HostileFleshHiveDefOf.FH_GroupWorkMode_HostileHunt, false);
            huntGroup.lord = LordMaker.MakeNewLord(lord.faction, new LordJob_HiveGroup(huntGroup), lord.Map);
            huntGroup.Spawn(lord.Map);
            lord.groups.Add(huntGroup);
        }
        prey = candidate;
        corpse = salvage;
        gathered = salvage != null;
        huntStartedTick = now;
        foreach (Pawn pawn in ready.Take(count))
        {
            pawn.jobs.StopAll();
            huntGroup.AcceptUnit(pawn);
        }
        huntGroup.SetTarget(new TargetInfo((Thing?)corpse ?? prey), false);
        huntGroup.SetMode(HostileFleshHiveDefOf.FH_GroupWorkMode_HostileHunt, false);
        huntGroup.lord.CurLordToil.UpdateAllDuties();
        lord.RecordDebugDecision("Attack", "AttackStarted", prey, executed: true,
            details: "FH_HostileHive_Hunting".Translate());
    }

    private bool IsHuntFit(Pawn pawn)
    {
        return pawn.Spawned && pawn.Map == lord.Map && pawn.Faction == lord.faction && !pawn.Dead && !pawn.Downed
            && pawn.health.summaryHealth.SummaryHealthPercent >= 0.6f
            && (pawn.GetNeed<Need_Maintenance>()?.CurLevelPercentage ?? 1f) >= 0.35f;
    }

    private HostileFleshHiveHuntGroup? huntGroup;
    private Pawn? prey;
    private Corpse? corpse;
    private int huntStartedTick;
    private int nextHuntTick;
    private bool gathered;
}
