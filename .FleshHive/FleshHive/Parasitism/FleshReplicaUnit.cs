using HiveCreatureFramework;
using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive;

public class FleshReplicaUnit : Unit
{
    public static readonly Color FleshColor = new Color(0.8549f, 0.2941f, 0.3686f, 1f);

    public static bool RenderingHost;

    public Pawn? Host => host;

    public ParasitismHediff? SourceHediff => sourceHediff;

    public PawnRenderTree? DebugHostRenderTree => Drawer?.renderer?.renderTree;

    public bool Active => host is { Destroyed: false } && !Destroyed;

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        if (scavengedWeaponRolled)
        {
            return;
        }

        scavengedWeaponRolled = true;
        if (Faction?.IsPlayer != true && equipment?.Primary == null)
        {
            TryEquipScavengedWeapon();
        }
    }

    public void DebugEnsureHostRendererInitialized()
    {
        Drawer?.renderer?.renderTree?.SetDirty();
        Drawer?.renderer?.EnsureGraphicsInitialized();
    }

    public void SyncTo(Pawn newHost, ParasitismHediff hediff)
    {
        host = newHost;
        sourceHediff = hediff;
        NotifyRenderTreeChanged();
    }

    public void ClearSync()
    {
        host = null;
        sourceHediff = null;
        NotifyRenderTreeChanged();
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref host, "host");
        Scribe_References.Look(ref sourceHediff, "sourceHediff");
        Scribe_Values.Look(ref scavengedWeaponRolled, "scavengedWeaponRolled", false);
    }

    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        RemoveSourceHediff();
        base.Destroy(mode);
    }

    public override void DynamicDrawPhaseAt(DrawPhase phase, Vector3 drawLoc, bool flip = false)
    {
        if (!Active || host == null)
        {
            base.DynamicDrawPhaseAt(phase, drawLoc, flip);
            return;
        }

        if (phase == DrawPhase.EnsureInitialized)
        {
            EnsureHostRenderState();
        }

        RenderingHost = true;
        try
        {
            base.DynamicDrawPhaseAt(phase, drawLoc, flip);
        }
        finally
        {
            RenderingHost = false;
        }
    }

    private void RemoveSourceHediff()
    {
        Pawn? hediffPawn = sourceHediff?.pawn;
        if (hediffPawn == null || sourceHediff == null || hediffPawn.health?.hediffSet?.hediffs?.Contains(sourceHediff) != true)
        {
            ClearSync();
            return;
        }

        sourceHediff.flesh = null!;
        hediffPawn.health.RemoveHediff(sourceHediff);
        if (hediffPawn.health.hediffSet.GetFirstHediffOfDef(FleshHiveDefOf.FH_ParasitismSystem) is ParasitismSystem system)
        {
            system.SetDirty();
            system.AssignAngle();
        }
        ClearSync();
    }

    private void TryEquipScavengedWeapon()
    {
        FleshReplicaWeaponExtension? extension = kindDef?.GetModExtension<FleshReplicaWeaponExtension>();
        if (extension?.weapons == null || extension.weapons.Count == 0
            || !extension.weapons.TryRandomElementByWeight(
                option => option?.weapon != null ? Mathf.Max(0f, option.weight) : 0f,
                out FleshReplicaWeaponOption option)
            || ThingMaker.MakeThing(option.weapon, GenStuff.DefaultStuffFor(option.weapon)) is not ThingWithComps weapon)
        {
            return;
        }

        equipment.AddEquipment(weapon);
    }

    private void NotifyRenderTreeChanged()
    {
        Drawer?.renderer?.renderTree?.SetDirty();
        Drawer?.renderer?.EnsureGraphicsInitialized();
    }

    private void EnsureHostRenderState()
    {
        int apparelCount = host?.apparel?.WornApparelCount ?? 0;
        if (hostApparelCount == apparelCount)
        {
            return;
        }

        hostApparelCount = apparelCount;
        Drawer?.renderer?.renderTree?.SetDirty();
        Drawer?.renderer?.EnsureGraphicsInitialized();
    }

    private Pawn? host;
    private ParasitismHediff? sourceHediff;
    private int hostApparelCount = -1;
    private bool scavengedWeaponRolled;
}
