using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace FleshHive;

public class FloatMenuOptionProvider_MountParasiticWeapon : FloatMenuOptionProvider
{
    protected override bool Drafted => true;

    protected override bool Undrafted => true;

    protected override bool Multiselect => false;

    protected override bool RequiresManipulation => true;

    protected override bool AppliesInt(FloatMenuContext context)
    {
        return HediffComp_ParasitismWeaponMounts.GetFirstWithEmptyMount(context.FirstSelectedPawn) != null;
    }

    protected override FloatMenuOption GetSingleOptionFor(Thing clickedThing, FloatMenuContext context)
    {
        Pawn pawn = context.FirstSelectedPawn;
        HediffComp_ParasitismWeaponMounts comp = HediffComp_ParasitismWeaponMounts.GetFirstWithEmptyMount(pawn);
        if (comp == null || !HediffComp_ParasitismWeaponMounts.CanMountWeapon(clickedThing))
        {
            return null;
        }

        string labelShort = clickedThing.LabelShort;
        if (pawn.WorkTagIsDisabled(WorkTags.Shooting))
        {
            return new FloatMenuOption("FH_CannotMountParasiticWeapon".Translate(labelShort) + ": " + "IsIncapableOfShootingLower".Translate(pawn), null);
        }
        if (!pawn.CanReach(clickedThing, PathEndMode.ClosestTouch, Danger.Deadly))
        {
            return new FloatMenuOption("FH_CannotMountParasiticWeapon".Translate(labelShort) + ": " + "NoPath".Translate().CapitalizeFirst(), null);
        }
        if (clickedThing.IsBurning())
        {
            return new FloatMenuOption("FH_CannotMountParasiticWeapon".Translate(labelShort) + ": " + "BurningLower".Translate(), null);
        }
        if (EquipmentUtility.AlreadyBondedToWeapon(clickedThing, pawn))
        {
            return new FloatMenuOption("FH_CannotMountParasiticWeapon".Translate(labelShort) + ": " + "BladelinkAlreadyBonded".Translate(), null);
        }

        return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("FH_MountParasiticWeapon".Translate(labelShort), delegate
        {
            clickedThing.SetForbidden(false);
            Job job = JobMaker.MakeJob(FleshHiveDefOf.FH_Job_MountParasiticWeapon, clickedThing);
            pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            if (FleckDefOf.FeedbackEquip != null && clickedThing.MapHeld != null)
            {
                FleckMaker.Static(clickedThing.DrawPos, clickedThing.MapHeld, FleckDefOf.FeedbackEquip);
            }
        }, MenuOptionPriority.High), pawn, clickedThing);
    }
}
