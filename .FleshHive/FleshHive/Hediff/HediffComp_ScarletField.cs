using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using HiveCreatureFramework;
using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive;

public class HediffCompProperties_ScarletField : HediffCompProperties
{
    public HediffCompProperties_ScarletField()
    {
        this.compClass = typeof(HediffComp_ScarletField);
    }

    public float areaShieldRadius = 4.5f;

}

public class HediffComp_ScarletField : HCFHediffComp
{
    public new HediffCompProperties_ScarletField Props => (HediffCompProperties_ScarletField)this.props;

    public bool Active => active;

    private static MethodInfo ImpactMethod
    {
        get
        {
            if (impactMethod == null)
            {
                impactMethod = AccessTools.Method(typeof(Projectile), "Impact", new Type[] { typeof(Thing), typeof(bool) });
            }
            return impactMethod;
        }
    }

    public void Activate()
    {
        SetActive(!active);
    }

    public override bool PreApplyDamage(ref DamageInfo dinfo)
    {
        if (!active)
        {
            return true;
        }
        bool isRanged = dinfo.Def.isRanged;
        int cost = isRanged ? 1 : 20;
        if (TwistedFleshUtility.ConsumeTwistedFlesh(this.Pawn, cost))
        {
            return false;
        }
        return true;
    }

    public override void CompPostTick(ref float severityAdjustment)
    {
        base.CompPostTick(ref severityAdjustment);
        Pawn pawn = this.Pawn;
        if (pawn == null || !pawn.Spawned)
        {
            return;
        }
        if (active)
        {
            tickCounter++;
            if (tickCounter >= 2)
            {
                tickCounter = 0;
                InterceptProjectiles(pawn);
            }
        }
    }

    private void InterceptProjectiles(Pawn pawn)
    {
        Map map = pawn.MapHeld;
        if (map == null)
        {
            return;
        }
        Vector3 shieldCenter = pawn.Position.ToVector3Shifted();
        float radiusSq = (Props.areaShieldRadius + 1f) * (Props.areaShieldRadius + 1f);
        List<Thing> things = map.listerThings.ThingsInGroup(ThingRequestGroup.Projectile);
        for (int i = 0; i < things.Count; i++)
        {
            if (things[i] is not Projectile proj || !proj.Spawned || proj.Destroyed)
            {
                continue;
            }
            if (proj.Map != map)
            {
                continue;
            }
            bool hostile = true;
            if (proj.Launcher != null && pawn.Faction != null)
            {
                hostile = proj.Launcher.Spawned
                    ? proj.Launcher.HostileTo(pawn.Faction)
                    : proj.Launcher.Faction?.HostileTo(pawn.Faction) ?? true;
            }
            if (!hostile)
            {
                continue;
            }
            Vector3 projPos = proj.ExactPosition;
            float dx = projPos.x - shieldCenter.x;
            float dz = projPos.z - shieldCenter.z;
            if (dx * dx + dz * dz > radiusSq)
            {
                continue;
            }
            if (!TwistedFleshUtility.ConsumeTwistedFlesh(pawn, 1))
            {
                continue;
            }
            ImpactMethod.Invoke(proj, new object[] { null, true });
        }
    }

    private void SetActive(bool value)
    {
        if (active == value)
        {
            return;
        }
        active = value;
        if (active)
        {
            tickCounter = 0;
        }
        Pawn pawn = this.Pawn;
        if (pawn != null && pawn.Spawned)
        {
            pawn.Drawer?.renderer?.SetAllGraphicsDirty();
        }
    }

    public override void CompExposeData()
    {
        base.CompExposeData();
        Scribe_Values.Look(ref active, "active");
    }

    private bool active = true;
    private int tickCounter;
    private static MethodInfo impactMethod;

    public static HediffComp_ScarletField FindOnPawn(Pawn pawn)
    {
        foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
        {
            HediffComp_ScarletField comp = hediff.TryGetComp<HediffComp_ScarletField>();
            if (comp != null)
            {
                return comp;
            }
        }
        return null;
    }
}
