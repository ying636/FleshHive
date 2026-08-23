using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive;

public class TentacleProperties_WeaponMount : TentacleProperties
{
    public TentacleProperties_WeaponMount()
    {
        tentacleClass = typeof(Tentacle_WeaponMount);
    }

    public float angleOffset = -90f;
    public string slotLabel;
}

public class Tentacle_WeaponMount : Tentacle
{
    public new TentacleProperties_WeaponMount Prop => (TentacleProperties_WeaponMount)base.Prop;

    public Tentacle_WeaponMount()
    {
    }

    public Tentacle_WeaponMount(TentacleProperties prop)
    {
        this.prop = prop;
    }

    public bool HasMountedWeapon => mountedWeapon != null;

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
        TickCurrentTarget(equippable.PrimaryVerb);
    }

    public override void RareTick(bool allow)
    {
        if (allow && mountedWeapon != null)
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
        return weapon is ThingWithComps { def.IsRangedWeapon: true } thing && thing.TryGetComp<CompEquippable>() != null;
    }

    public ThingWithComps MountedWeapon => mountedWeapon;

    public TaggedString SlotLabel => Prop.slotLabel.NullOrEmpty() ? "FH_ParasiticWeaponSlot".Translate() : Prop.slotLabel.Translate();

    private void FindAttackTargetAndAttack()
    {
        Pawn pawn = Comp?.Pawn;
        Verb verb = PrimaryVerb;
        if (pawn?.Spawned != true || verb == null || verb.state == VerbState.Bursting || cooldown > 0 || warmupTicksLeft > 0)
        {
            return;
        }

        Pawn target = FindTarget(pawn, verb);
        if (target == null)
        {
            ResetCurrentTarget();
            return;
        }

        currentTarget = target;
        warmupTicksLeft = GetWarmupTicks(pawn, verb);
        UpdateTargetAngle(pawn, currentTarget);
    }

    private Pawn FindTarget(Pawn pawn, Verb verb)
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
                TryStartCastWithoutPawnStance(verb, currentTarget);
            }
            return;
        }
        TryStartCastWithoutPawnStance(verb, currentTarget);
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
        verb.preventFriendlyFire = true;
        nonInterruptingSelfCastField.SetValue(verb, true);
        currentTargetField.SetValue(verb, target);
        currentDestinationField.SetValue(verb, LocalTargetInfo.Invalid);
        verb.WarmupComplete();
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
}
