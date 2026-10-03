using CombatExtended;
using HarmonyLib;
using RimWorld;
using Verse;

namespace FleshHive.CE;

public class Tentacle_WeaponMountCE : Tentacle_WeaponMount
{
    public Tentacle_WeaponMountCE()
    {
    }

    public Tentacle_WeaponMountCE(TentacleProperties props) : base(props)
    {
    }

    public override void Tick()
    {
        ThingDef? weaponDef = MountedWeapon?.def;
        if (weaponDef == null || MountedWeapon?.TryGetComp<CompAmmoUser>() == null
            || weaponDef.weaponTags?.Contains("NoSwitch") == true)
        {
            base.Tick();
            return;
        }
        List<string>? originalTags = weaponDef.weaponTags;
        List<string> tags = originalTags ?? new List<string>();
        tags.Add("NoSwitch");
        weaponDef.weaponTags = tags;
        try
        {
            base.Tick();
        }
        finally
        {
            tags.Remove("NoSwitch");
            weaponDef.weaponTags = originalTags;
        }
    }

    public void TryStartReload()
    {
        Pawn? pawn = Comp?.Pawn;
        ThingWithComps? weapon = MountedWeapon;
        CompAmmoUser? ammo = weapon?.TryGetComp<CompAmmoUser>();
        if (pawn == null || weapon == null || ammo == null || !base.CanUseMountedWeapon(pawn)
            || pawn.CurJobDef == CE_JobDefOf.ReloadWeapon || !ammo.HasMagazine
            || ammo.UseAmmo && !ammo.HasAmmo)
        {
            return;
        }
        ThingDef weaponDef = weapon.def;
        List<string>? originalTags = weaponDef.weaponTags;
        List<string> tags = originalTags ?? new List<string>();
        bool added = !tags.Contains("NoSwitch");
        if (added)
        {
            tags.Add("NoSwitch");
        }
        weaponDef.weaponTags = tags;
        try
        {
            ammo.TryStartReload();
        }
        finally
        {
            if (added)
            {
                tags.Remove("NoSwitch");
            }
            weaponDef.weaponTags = originalTags;
        }
    }

    protected override bool PerformMeleeAttack(Verb_MeleeAttack verb, LocalTargetInfo target)
    {
        if (verb is Verb_MeleeAttackCE ceVerb)
        {
            ceVerb.TryCastShot();
            return true;
        }
        return base.PerformMeleeAttack(verb, target);
    }

    protected override bool CanUseMountedWeapon(Pawn pawn)
    {
        if (!base.CanUseMountedWeapon(pawn) || pawn.CurJobDef == CE_JobDefOf.ReloadWeapon)
        {
            return false;
        }
        CompAmmoUser? ammo = MountedWeapon?.TryGetComp<CompAmmoUser>();
        if (ammo == null || ammo.CanBeFiredNow)
        {
            return true;
        }
        if (AutoAttackEnabled)
        {
            TryStartReload();
        }
        return false;
    }

    protected override void CompleteMountedWarmup(Verb verb)
    {
        if (verb is not Verb_ShootCE ceVerb)
        {
            base.CompleteMountedWarmup(verb);
            return;
        }
        bool wasAiming = isAiming(ceVerb);
        isAiming(ceVerb) = true;
        try
        {
            base.CompleteMountedWarmup(verb);
        }
        finally
        {
            isAiming(ceVerb) = wasAiming;
        }
    }

    private static readonly AccessTools.FieldRef<Verb_ShootCE, bool> isAiming =
        AccessTools.FieldRefAccess<Verb_ShootCE, bool>("_isAiming");
}
