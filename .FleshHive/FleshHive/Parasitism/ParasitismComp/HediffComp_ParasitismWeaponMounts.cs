using Verse;

namespace FleshHive;

public class HediffComp_ParasitismWeaponMounts : HediffComp_Parasitism
{
    public new HediffCompProperties_ParasitismWeaponMounts Props => (HediffCompProperties_ParasitismWeaponMounts)this.props;

    public override int TentacleCount => Props.tentacles?.Count ?? 0;

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
}
