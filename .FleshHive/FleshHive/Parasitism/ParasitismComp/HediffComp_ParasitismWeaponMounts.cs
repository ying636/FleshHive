using UnityEngine;
using Verse;

namespace FleshHive;

public class HediffComp_ParasitismWeaponMounts : HediffComp_Parasitism
{
    public new HediffCompProperties_ParasitismWeaponMounts Props => (HediffCompProperties_ParasitismWeaponMounts)this.props;

    public override bool ShowAttackGizmo => false;

    public override int TentacleCount => Props.tentacles?.Count ?? 0;

    public override void GetAngle(ref int index, int count)
    {
        List<Tentacle_WeaponMount> mounts = WeaponMounts.ToList();
        for (int i = 0; i < mounts.Count; i++)
        {
            Tentacle_WeaponMount mount = mounts[i];
            bool right = i == 1;
            float x = (right ? 1f : -1f) * FixedSideOffset;
            mount.drawPosOffset = new Vector3(x, 0f, 0f);
            mount.angle = right ? 90f : -90f;
            mount.isRight = right;
            index++;
        }
    }

    public bool HasEmptyMount => WeaponMounts.Any(mount => !mount.HasMountedWeapon);

    public IEnumerable<Tentacle_WeaponMount> WeaponMounts => tentacles.OfType<Tentacle_WeaponMount>().Take(TentacleCount);

    public override void CompPostPostRemoved()
    {
        DropMountedWeapons();
        base.CompPostPostRemoved();
    }

    public bool MountWeapon(ThingWithComps weapon, int preferredIndex = -1)
    {
        if (weapon == null || !Tentacle_WeaponMount.CanMountWeapon(weapon))
        {
            return false;
        }

        List<Tentacle_WeaponMount> mounts = WeaponMounts.ToList();
        if (preferredIndex >= 0 && preferredIndex < mounts.Count && !mounts[preferredIndex].HasMountedWeapon)
        {
            return mounts[preferredIndex].MountWeapon(weapon);
        }

        Tentacle_WeaponMount mount = mounts.FirstOrDefault(slot => !slot.HasMountedWeapon);
        return mount != null && mount.MountWeapon(weapon);
    }

    public static HediffComp_ParasitismWeaponMounts GetFirstWithEmptyMount(Pawn pawn)
    {
        if (pawn?.health?.hediffSet?.hediffs == null)
        {
            return null;
        }
        foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
        {
            if (hediff is HediffWithComps withComps)
            {
                HediffComp_ParasitismWeaponMounts comp = withComps.TryGetComp<HediffComp_ParasitismWeaponMounts>();
                if (comp?.HasEmptyMount == true)
                {
                    return comp;
                }
            }
        }
        return null;
    }

    public static bool CanMountWeapon(Thing weapon)
    {
        return Tentacle_WeaponMount.CanMountWeapon(weapon);
    }

    public static HediffComp_ParasitismWeaponMounts GetFirst(Pawn pawn)
    {
        return GetAll(pawn).FirstOrDefault();
    }

    public static IEnumerable<HediffComp_ParasitismWeaponMounts> GetAll(Pawn pawn)
    {
        if (pawn?.health?.hediffSet?.hediffs == null)
        {
            yield break;
        }
        foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
        {
            if (hediff is HediffWithComps withComps)
            {
                HediffComp_ParasitismWeaponMounts comp = withComps.TryGetComp<HediffComp_ParasitismWeaponMounts>();
                if (comp != null)
                {
                    yield return comp;
                }
            }
        }
    }

    private void DropMountedWeapons()
    {
        if (Pawn?.MapHeld == null)
        {
            return;
        }

        foreach (Tentacle_WeaponMount mount in WeaponMounts)
        {
            if (mount.TryUnmountWeapon(out ThingWithComps weapon) && weapon != null && !weapon.Spawned)
            {
                GenPlace.TryPlaceThing(weapon, Pawn.PositionHeld, Pawn.MapHeld, ThingPlaceMode.Near);
            }
        }
    }

    protected override IEnumerable<Tentacle> ActiveTentacles => WeaponMounts;

    private const float FixedSideOffset = 0.42f;
}
