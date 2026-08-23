using RimWorld;
using Verse;

namespace FleshHive;

public class Hediff_TitanDevastatingStrike : Hediff
{
    public override bool ShouldRemove => consumed || base.ShouldRemove;

    public override void Tick()
    {
        base.Tick();
        if (!pawn.Spawned)
        {
            CleanupLightningEffecter();
            return;
        }

        lightningEffecter ??= FleshHiveDefOf.FH_Effect_TitanDevastatingStrikeLightning.Spawn();
        lightningEffecter.EffectTick(pawn, pawn);
    }

    public override void Notify_PawnDamagedThing(Thing thing, DamageInfo dinfo, DamageWorker.DamageResult result)
    {
        base.Notify_PawnDamagedThing(thing, dinfo, result);

        if (!consumed && dinfo.Tool != null && result.totalDamageDealt > 0f)
        {
            Map map = thing.MapHeld ?? pawn.MapHeld;
            IntVec3 position = thing.PositionHeld;
            if (map != null && position.IsValid)
            {
                FleckMaker.Static(position.ToVector3Shifted(), map, PsychicDistortionFleck,
                    ImpactDistortionScale);
            }

            consumed = true;
        }
    }

    public override void PostRemoved()
    {
        base.PostRemoved();
        CleanupLightningEffecter();
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref consumed, "consumed");
    }

    private void CleanupLightningEffecter()
    {
        lightningEffecter?.Cleanup();
        lightningEffecter = null;
    }

    private const float ImpactDistortionScale = 1.2f;

    private static readonly FleckDef PsychicDistortionFleck =
        DefDatabase<FleckDef>.GetNamed("PsychicDistortionRingContractingQuick");

    private Effecter? lightningEffecter;

    private bool consumed;
}
