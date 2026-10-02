using CombatExtended;
using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive.CE;

public class HediffComp_ParasitismWeaponsCE : HediffComp_ParasitismWeaponMounts
{
    public override IEnumerable<Gizmo> CompGetGizmos()
    {
        foreach (Gizmo gizmo in base.CompGetGizmos() ?? Enumerable.Empty<Gizmo>())
        {
            yield return gizmo;
        }
        foreach (Tentacle_WeaponMount mount in Tentacles.OfType<Tentacle_WeaponMount>())
        {
            CompAmmoUser? ammo = mount.MountedWeapon?.TryGetComp<CompAmmoUser>();
            if (ammo != null)
            {
                foreach (Gizmo gizmo in ammo.CompGetGizmosExtra())
                {
                    yield return gizmo;
                }
                if (Pawn.Faction == Faction.OfPlayer && !Pawn.IsColonistPlayerControlled && !Pawn.IsColonyMech)
                {
                    yield return new GizmoAmmoStatus { compAmmo = ammo };
                    yield return new Command_Reload
                    {
                        compAmmo = ammo,
                        action = ammo.TryStartReload,
                        defaultLabel = ammo.HasMagazine ? "CE_ReloadLabel".Translate() : "",
                        defaultDesc = "CE_ReloadDesc".Translate(),
                        icon = ammo.CurrentAmmo == null ? ContentFinder<Texture2D>.Get("UI/Buttons/Reload") : ammo.SelectedAmmo.IconTexture(),
                        tutorTag = ammo.HasMagazine ? "CE_Reload" : "CE_ReloadNoMag"
                    };
                }
            }
        }
    }
}
