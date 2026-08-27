using System.Collections.Generic;
using System.Linq;
using HiveCreatureFramework;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace FleshHive;

public class LordToil_GroupHunt : LordToil
{
    public LordToil_GroupHunt(UnitGroup group)
    {
        this.group = group;
    }

    public override void Init()
    {
        base.Init();
        nextPreySearchTick = Find.TickManager.TicksGame;
        TryStartHuntCycle();
        UpdateAllDuties();
    }

    public override void UpdateAllDuties()
    {
        foreach (Pawn pawn in lord.ownedPawns)
        {
            if (!IsActiveMember(pawn))
            {
                continue;
            }

            if (!IsPreyStillPresent(currentPrey))
            {
                SetWaitingDuty(pawn);
                continue;
            }

            if (currentPrey.Downed)
            {
                pawn.mindState.duty = new PawnDuty(FleshHiveDefOf.FH_GroupHuntExecute, currentPrey);
                continue;
            }

            if (!huntStarted)
            {
                pawn.mindState.duty = new PawnDuty(FleshHiveDefOf.FH_GroupHuntGather, currentPrey);
                continue;
            }

            UnitComp comp = pawn.TryGetComp<UnitComp>();
            pawn.mindState.duty = comp?.Props.overrideDuty_Attack != null
                ? new PawnDuty(comp.Props.overrideDuty_Attack, currentPrey)
                : new PawnDuty(HCFDefOf.HCF_HuntEnemies, currentPrey);
        }
    }

    public override void LordToilTick()
    {
        if (Find.TickManager.TicksGame % CheckInterval != 0)
        {
            return;
        }

        int currentTick = Find.TickManager.TicksGame;
        if (currentPrey == null)
        {
            if (waitingForRecovery && !HasEnoughHealthyMembers())
            {
                return;
            }

            waitingForRecovery = false;
            if (currentTick >= nextPreySearchTick && TryStartHuntCycle())
            {
                UpdateAllDuties();
                InterruptActiveJobs();
            }
            return;
        }

        if (!IsPreyStillPresent(currentPrey))
        {
            EndHuntCycle();
            UpdateAllDuties();
            return;
        }

        if (currentPrey.Downed)
        {
            UpdateAllDuties();
            return;
        }

        if (!huntStarted && AllActiveMembersGathered())
        {
            huntStarted = true;
            UpdateAllDuties();
            InterruptActiveJobs();
        }
    }

    private bool AllActiveMembersGathered()
    {
        Pawn? prey = currentPrey;
        if (prey == null)
        {
            return false;
        }

        List<Pawn> activeMembers = lord.ownedPawns.Where(IsActiveMember).ToList();
        return activeMembers.Count > 0 && activeMembers.All(pawn => IsHunterReady(pawn, prey));
    }

    private bool IsHunterReady(Pawn pawn, Pawn prey)
    {
        DutyDef? attackDuty = pawn.TryGetComp<UnitComp>()?.Props.overrideDuty_Attack;
        if (JobGiver_GroupRangedAttackTarget.IsRangedAttackDuty(attackDuty))
        {
            ModExtension_RangedDuty? rangedSettings = attackDuty?.GetModExtension<ModExtension_RangedDuty>();
            Ability? ability =
                JobGiver_GroupRangedAttackTarget.FindRangedAbility(pawn, prey, rangedSettings != null);
            return (ability != null
                    ? JobGiver_GroupRangedAttackTarget.TryFindCastPosition(pawn, prey, ability.verb,
                        out IntVec3 shootingPosition, rangedSettings)
                    : JobGiver_GroupRangedAttackTarget.TryFindSupportPosition(pawn, prey, out shootingPosition,
                        rangedSettings))
                && shootingPosition == pawn.Position;
        }

        return pawn.Position.InHorDistOf(prey.Position, GatherRadius);
    }

    private bool HasEnoughHealthyMembers()
    {
        return CountHealthyMembers() >= GetMinimumHealthyHunters();
    }

    private int CountHealthyMembers()
    {
        return lord.ownedPawns.Count(IsFullyRecoveredMember);
    }

    private Pawn? FindPrey()
    {
        IntVec3 origin = GetSearchOrigin();
        IEnumerable<Pawn> markedPrey = Map.designationManager
            .SpawnedDesignationsOfDef(DesignationDefOf.Hunt)
            .Select(designation => designation.target.Thing as Pawn)
            .Where(IsValidPrey);
        Pawn? prey = markedPrey.OrderBy(candidate => candidate.Position.DistanceToSquared(origin)).FirstOrDefault();
        if (prey != null)
        {
            return prey;
        }

        if (group is not UnitGroup_FleshHive { AllowHuntUndesignatedAnimals: true })
        {
            return null;
        }

        return Map.mapPawns.AllPawnsSpawned
            .Where(IsValidUndesignatedPrey)
            .OrderBy(candidate => candidate.Position.DistanceToSquared(origin))
            .FirstOrDefault();
    }

    private IntVec3 GetSearchOrigin()
    {
        if (group?.hive is { Spawned: true } hive && hive.Map == Map)
        {
            return hive.Position;
        }

        Pawn member = lord.ownedPawns.FirstOrDefault(IsActiveMember);
        return member?.Position ?? Map.Center;
    }

    private void SetWaitingDuty(Pawn pawn)
    {
        IntVec3 waitingPoint = group.Position;
        if (!waitingPoint.IsValid || !waitingPoint.InBounds(Map))
        {
            waitingPoint = group.hive is { Spawned: true } hive && hive.Map == Map
                ? hive.Position
                : pawn.Position;
        }

        UnitComp comp = pawn.TryGetComp<UnitComp>();
        if (comp?.Props.overrideDuty_Defend != null)
        {
            pawn.mindState.duty = new PawnDuty(comp.Props.overrideDuty_Defend, waitingPoint, -1f);
            return;
        }

        pawn.mindState.duty = new PawnDuty(HCFDefOf.HCF_Defend, waitingPoint, -1f)
        {
            focusSecond = waitingPoint,
            radius = pawn.kindDef.defendPointRadius >= 0f
                ? pawn.kindDef.defendPointRadius
                : WaitingDefendRadius,
            wanderRadius = WaitingWanderRadius
        };
    }

    private void InterruptActiveJobs()
    {
        foreach (Pawn pawn in lord.ownedPawns.Where(IsActiveMember))
        {
            pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
        }
    }

    private bool IsActiveMember(Pawn pawn)
    {
        return pawn != null
            && pawn.Spawned
            && pawn.Map == Map
            && !pawn.Dead
            && !pawn.Downed
            && !pawn.InMentalState;
    }

    private bool IsFullyRecoveredMember(Pawn pawn)
    {
        return IsActiveMember(pawn)
            && !pawn.health.hediffSet.HasNaturallyHealingInjury()
            && pawn.health.hediffSet.BleedRateTotal <= 0.001f;
    }

    private bool IsValidPrey(Pawn? prey)
    {
        List<Pawn> activeMembers = lord.ownedPawns.Where(IsActiveMember).ToList();
        return prey != null
            && prey.Spawned
            && prey.Map == Map
            && !prey.Dead
            && !prey.Downed
            && prey.AnimalOrWildMan()
            && !prey.IsPrisonerInPrisonCell()
            && (prey.Faction == null || !prey.Faction.def.humanlikeFaction)
            && activeMembers.Count > 0
            && activeMembers.All(member => member.CanReach(prey, PathEndMode.Touch, Danger.Deadly));
    }

    private bool IsPreyStillPresent(Pawn? prey)
    {
        return prey != null
            && prey.Spawned
            && prey.Map == Map
            && !prey.Dead
            && prey.AnimalOrWildMan();
    }

    private bool IsValidUndesignatedPrey(Pawn? prey)
    {
        return IsValidPrey(prey)
            && prey.RaceProps.Animal
            && prey.Faction == null
            && !prey.Position.Fogged(Map)
            && Map.designationManager.DesignationOn(prey, DesignationDefOf.Hunt) == null;
    }

    private bool TryStartHuntCycle()
    {
        int currentTick = Find.TickManager.TicksGame;
        if (currentPrey != null || waitingForRecovery || currentTick < nextPreySearchTick)
        {
            return false;
        }

        if (CountHealthyMembers() < GetMinimumHealthyHunters())
        {
            nextPreySearchTick = currentTick + PreySearchInterval;
            return false;
        }

        currentPrey = FindPrey();
        if (currentPrey == null)
        {
            nextPreySearchTick = currentTick + PreySearchInterval;
            return false;
        }

        huntStarted = false;
        group.SetTarget(new TargetInfo(currentPrey));
        return true;
    }

    private int GetMinimumHealthyHunters()
    {
        return group is UnitGroup_FleshHive huntingGroup
            ? Mathf.Max(1, huntingGroup.MinimumHealthyHunters)
            : 1;
    }

    private void EndHuntCycle()
    {
        Corpse corpse = currentPrey?.Corpse;
        if (corpse != null && !corpse.Destroyed)
        {
            corpse.SetForbidden(false);
        }

        currentPrey = null;
        huntStarted = false;
        waitingForRecovery = true;
        nextPreySearchTick = Find.TickManager.TicksGame + CheckInterval;
        InterruptActiveJobs();
    }

    public const float GatherRadius = 9f;

    private const int CheckInterval = 60;

    private const int PreySearchInterval = 250;

    private const float WaitingDefendRadius = 24f;

    private const float WaitingWanderRadius = 8f;

    private readonly UnitGroup group;

    private Pawn? currentPrey;

    private bool huntStarted;

    private int nextPreySearchTick;

    private bool waitingForRecovery;
}
