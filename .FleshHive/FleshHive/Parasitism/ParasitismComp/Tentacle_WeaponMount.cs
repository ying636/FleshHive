using System;
using HarmonyLib;
using RimWorld;
using System.Reflection;
using UnityEngine;
using Verse;
using Verse.AI;

namespace FleshHive;

public class TentacleProperties_WeaponMount : TentacleProperties
{
    public TentacleProperties_WeaponMount()
    {
        tentacleClass = typeof(Tentacle_WeaponMount);
    }

    public float angleOffset = -90f;
    public float weaponDrawOffset = 0.55f;
    public string slotLabel;
}

public class Tentacle_WeaponMount : Tentacle
{
    public Tentacle_WeaponMount()
    {
    }

    public Tentacle_WeaponMount(TentacleProperties prop)
    {
        this.prop = prop;
    }

    public new TentacleProperties_WeaponMount Prop => (TentacleProperties_WeaponMount)base.Prop;
    public override bool CanAutoAttack => true;
    public bool HasMountedWeapon => mountedWeapon != null;
    public ThingWithComps MountedWeapon => mountedWeapon;
    public TaggedString SlotLabel => Prop.slotLabel.NullOrEmpty() ? "FH_ParasiticWeaponSlot".Translate() : Prop.slotLabel.Translate();

    public override void Tick()
    {
        base.Tick();
        if (cooldown > 0)
        {
            cooldown--;
        }
        if (mountedWeapon == null)
        {
            return;
        }

        EnsureVerbCaster();
        CompEquippable equippable = mountedWeapon.GetComp<CompEquippable>();
        foreach (Verb verb in equippable.AllVerbs)
        {
            verb.VerbTick();
        }
        if (!AutoAttackEnabled)
        {
            ResetCurrentTarget();
            return;
        }
        TickCurrentTarget(equippable.PrimaryVerb);
    }

    public override void RareTick()
    {
        if (AutoAttackEnabled && mountedWeapon != null)
        {
            FindAttackTargetAndAttack();
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref cooldown, "cooldown");
        Scribe_Deep.Look(ref mountedWeapon, "mountedWeapon");
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            EnsureVerbCaster();
        }
    }

    public bool MountWeapon(ThingWithComps weapon)
    {
        if (mountedWeapon != null || !CanMountWeapon(weapon))
        {
            return false;
        }

        if (weapon.Spawned)
        {
            weapon.DeSpawn();
        }
        mountedWeapon = weapon;
        EnsureVerbCaster();
        NotifyVisualsChanged();
        return true;
    }

    public bool TryUnmountWeapon(out ThingWithComps weapon)
    {
        weapon = mountedWeapon;
        mountedWeapon = null;
        NotifyVisualsChanged();
        return weapon != null;
    }

    public static bool CanMountWeapon(Thing weapon)
    {
        return weapon is ThingWithComps thing
               && thing.def.IsWeapon
               && (thing.def.IsRangedWeapon || thing.def.IsMeleeWeapon)
               && thing.TryGetComp<CompEquippable>() != null;
    }

    protected override void NotifyAutoAttackDisabled()
    {
        ResetCurrentTarget();
    }

    private void FindAttackTargetAndAttack()
    {
        Pawn pawn = Comp?.Pawn;
        Verb verb = PrimaryVerb;
        if (pawn?.Spawned != true || verb == null || verb.state == VerbState.Bursting || cooldown > 0 || warmupTicksLeft > 0)
        {
            return;
        }

        Thing target = FindTarget(pawn, verb);
        if (target == null)
        {
            ResetCurrentTarget();
            return;
        }

        currentTarget = target;
        warmupTicksLeft = GetWarmupTicks(pawn, verb);
        UpdateTargetAngle(pawn, currentTarget);
    }

    private Thing FindTarget(Pawn pawn, Verb verb)
    {
        return verb.verbProps.IsMeleeAttack ? FindMeleeTarget(pawn, verb) : FindRangedTarget(pawn, verb);
    }

    private Thing FindMeleeTarget(Pawn pawn, Verb verb)
    {
        Thing bestTarget = null;
        float bestDistance = float.MaxValue;
        foreach (Thing target in GenRadial.RadialDistinctThingsAround(pawn.Position, pawn.Map, verb.EffectiveRange, true))
        {
            if (target is not IAttackTarget attackTarget
                || !target.Spawned
                || target.Map != pawn.Map
                || !target.HostileTo(pawn)
                || attackTarget.ThreatDisabled(pawn)
                || !AttackTargetFinder.IsAutoTargetable(attackTarget)
                || !verb.ValidateTarget(target, false)
                || !verb.CanHitTarget(target))
            {
                continue;
            }

            float distance = target.Position.DistanceToSquared(pawn.Position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestTarget = target;
            }
        }
        return bestTarget;
    }

    private Pawn FindRangedTarget(Pawn pawn, Verb verb)
    {
        Map map = pawn.Map;
        float range = verb.EffectiveRange;
        Pawn bestTarget = null;
        float bestDistance = float.MaxValue;
        foreach (Pawn target in map.mapPawns.AllPawnsSpawned)
        {
            if (!target.HostileTo(pawn) || target.Downed || target.Position.DistanceTo(pawn.Position) > range)
            {
                continue;
            }
            if (!verb.CanHitTarget(target))
            {
                continue;
            }

            float distance = target.Position.DistanceToSquared(pawn.Position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestTarget = target;
            }
        }
        return bestTarget;
    }

    private Verb PrimaryVerb
    {
        get
        {
            CompEquippable equippable = mountedWeapon?.GetComp<CompEquippable>();
            return equippable?.PrimaryVerb;
        }
    }

    private void TickCurrentTarget(Verb verb)
    {
        Pawn pawn = Comp?.Pawn;
        if (pawn?.Spawned != true || verb == null || !currentTarget.IsValid)
        {
            ResetCurrentTarget();
            return;
        }
        if (verb.state == VerbState.Bursting)
        {
            UpdateTargetAngle(pawn, currentTarget);
            return;
        }
        if (!verb.CanHitTarget(currentTarget))
        {
            ResetCurrentTarget();
            return;
        }

        UpdateTargetAngle(pawn, currentTarget);
        if (cooldown > 0)
        {
            return;
        }

        if (warmupTicksLeft > 0)
        {
            warmupTicksLeft--;
            if (warmupTicksLeft <= 0)
            {
                if (verb.verbProps.IsMeleeAttack)
                {
                    TryPerformMeleeAttack(verb, currentTarget);
                }
                else
                {
                    TryStartCastWithoutPawnStance(verb, currentTarget);
                }
            }
            return;
        }
        if (verb.verbProps.IsMeleeAttack)
        {
            TryPerformMeleeAttack(verb, currentTarget);
        }
        else
        {
            TryStartCastWithoutPawnStance(verb, currentTarget);
        }
    }

    private bool TryStartCastWithoutPawnStance(Verb verb, LocalTargetInfo target)
    {
        if (verb.caster?.Spawned != true || verb.state == VerbState.Bursting || !verb.CanHitTarget(target))
        {
            ResetCurrentTarget();
            return false;
        }

        surpriseAttackField.SetValue(verb, false);
        canHitNonTargetPawnsNowField.SetValue(verb, true);
        verb.preventFriendlyFire = false;
        nonInterruptingSelfCastField.SetValue(verb, true);
        currentTargetField.SetValue(verb, target);
        currentDestinationField.SetValue(verb, LocalTargetInfo.Invalid);
        verb.WarmupComplete();
        return true;
    }

    private bool TryPerformMeleeAttack(Verb verb, LocalTargetInfo target)
    {
        if (verb is not Verb_MeleeAttack meleeVerb
            || verb.Caster is not Pawn pawn
            || !target.IsValid
            || !verb.CanHitTarget(target))
        {
            ResetCurrentTarget();
            return false;
        }

        ApplyMeleeDamageDelegate(meleeVerb, target);
        pawn.Notify_UsedVerb(pawn, verb);
        pawn.health?.Notify_UsedVerb(verb, target);
        verb.EquipmentSource?.Notify_UsedWeapon(pawn);
        pawn.Drawer.Notify_MeleeAttackOn(target.Thing);
        verb.castCompleteCallback?.Invoke();
        cooldown = verb.verbProps.AdjustedCooldownTicks(verb, pawn);
        ResetCurrentTarget();
        return true;
    }

    private void UpdateTargetAngle(Pawn pawn, LocalTargetInfo target)
    {
        if (!target.IsValid)
        {
            return;
        }
        rotateTime = 1f;
        targetAngle = (target.Cell.ToVector3Shifted() - pawn.DrawPos).AngleFlat() + Prop.angleOffset;
    }

    private void ResetCurrentTarget()
    {
        currentTarget = LocalTargetInfo.Invalid;
        warmupTicksLeft = 0;
    }

    private void EnsureVerbCaster()
    {
        if (mountedWeapon == null || Comp?.Pawn == null)
        {
            return;
        }

        CompEquippable equippable = mountedWeapon.GetComp<CompEquippable>();
        if (equippable == null)
        {
            return;
        }

        foreach (Verb verb in equippable.AllVerbs)
        {
            verb.caster = Comp.Pawn;
            verb.castCompleteCallback = delegate
            {
                cooldown = verb.verbProps.AdjustedCooldownTicks(verb, Comp.Pawn);
            };
        }
    }

    private int GetWarmupTicks(Pawn pawn, Verb verb)
    {
        return (verb.WarmupTime * pawn.GetStatValue(StatDefOf.AimingDelayFactor)).SecondsToTicks();
    }

    private void NotifyVisualsChanged()
    {
        Pawn pawn = Comp?.Pawn;
        if (pawn == null)
        {
            return;
        }

        pawn.Drawer.renderer.renderTree.SetDirty();
        pawn.Drawer.renderer.EnsureGraphicsInitialized();
    }

    private ThingWithComps mountedWeapon;
    private int cooldown;
    private int warmupTicksLeft;
    private LocalTargetInfo currentTarget = LocalTargetInfo.Invalid;
    private static readonly System.Reflection.FieldInfo currentTargetField = AccessTools.Field(typeof(Verb), "currentTarget");
    private static readonly System.Reflection.FieldInfo currentDestinationField = AccessTools.Field(typeof(Verb), "currentDestination");
    private static readonly System.Reflection.FieldInfo surpriseAttackField = AccessTools.Field(typeof(Verb), "surpriseAttack");
    private static readonly System.Reflection.FieldInfo canHitNonTargetPawnsNowField = AccessTools.Field(typeof(Verb), "canHitNonTargetPawnsNow");
    private static readonly System.Reflection.FieldInfo nonInterruptingSelfCastField = AccessTools.Field(typeof(Verb), "nonInterruptingSelfCast");
    private static readonly Func<Verb_MeleeAttack, LocalTargetInfo, DamageWorker.DamageResult> ApplyMeleeDamageDelegate =
        (Func<Verb_MeleeAttack, LocalTargetInfo, DamageWorker.DamageResult>)typeof(Verb_MeleeAttack)
            .GetMethod("ApplyMeleeDamageToTarget", BindingFlags.Instance | BindingFlags.NonPublic)
            .CreateDelegate(typeof(Func<Verb_MeleeAttack, LocalTargetInfo, DamageWorker.DamageResult>));
}
