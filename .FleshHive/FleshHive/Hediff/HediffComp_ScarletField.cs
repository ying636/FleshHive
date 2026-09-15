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

    public bool Active => active && Pawn != null && TwistedFleshUtility.GetCurrentTwistedFlesh(Pawn) >= 1;

    public bool CanActivate => Pawn != null && Pawn.Spawned && TwistedFleshUtility.GetCurrentTwistedFlesh(Pawn) >= 1;

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

    public void Activate()
    {
        SetActive(!active && CanActivate);
    }

    public override bool PreApplyDamage(ref DamageInfo dinfo)
    {
        if (!active)
        {
            return true;
        }
        if (TwistedFleshUtility.GetCurrentTwistedFlesh(Pawn) < 1)
        {
            SetActive(false);
            return true;
        }
        bool isRanged = dinfo.Def.isRanged;
        int cost = isRanged ? 1 : 20;
        if (TwistedFleshUtility.ConsumeTwistedFlesh(this.Pawn, cost))
        {
            if (TwistedFleshUtility.GetCurrentTwistedFlesh(Pawn) < 1)
            {
                SetActive(false);
            }
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
            if (TwistedFleshUtility.GetCurrentTwistedFlesh(pawn) < 1)
            {
                SetActive(false);
                return;
            }
            tickCounter++;
            if (tickCounter >= 2)
            {
                tickCounter = 0;
                InterceptProjectiles(pawn);
            }
        }
    }

    public override void CompExposeData()
    {
        base.CompExposeData();
        Scribe_Values.Look(ref active, "active");
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
        for (int i = things.Count - 1; i >= 0; i--)
        {
            if (things[i] is not Projectile proj || !proj.Spawned || proj.Destroyed)
            {
                continue;
            }
            if (proj.Map != map)
            {
                continue;
            }
            if (proj.Launcher == null)
            {
                continue;
            }
            if (pawn.Faction != null && !(proj.Launcher.Spawned
                    ? proj.Launcher.HostileTo(pawn.Faction)
                    : proj.Launcher.Faction?.HostileTo(pawn.Faction) == true))
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
            if (!IsIncomingProjectile(projectileOrigin(proj), projPos, shieldCenter))
            {
                continue;
            }

            if (!TwistedFleshUtility.ConsumeTwistedFlesh(pawn, 1))
            {
                SetActive(false);
                return;
            }
            ImpactMethod.Invoke(proj, new object[] { null, true });
            if (TwistedFleshUtility.GetCurrentTwistedFlesh(pawn) < 1)
            {
                SetActive(false);
                return;
            }
        }
    }

    private bool IsIncomingProjectile(Vector3 origin, Vector3 position, Vector3 center)
    {
        float originX = origin.x - center.x;
        float originZ = origin.z - center.z;
        return originX * originX + originZ * originZ > Props.areaShieldRadius * Props.areaShieldRadius
            && (position.x - center.x) * (position.x - origin.x)
                + (position.z - center.z) * (position.z - origin.z) < 0f;
    }

    private void SetActive(bool value)
    {
        if (active == value)
        {
            return;
        }
        Pawn pawn = this.Pawn;
        if (active && !value && pawn != null)
        {
            if (pawn.Spawned)
            {
                EffecterDefOf.Shield_Break.SpawnAttached(pawn, pawn.MapHeld, Props.areaShieldRadius);
            }
        }
        active = value;
        if (active)
        {
            tickCounter = 0;
        }
        if (pawn != null && pawn.Spawned)
        {
            pawn.Drawer?.renderer?.SetAllGraphicsDirty();
        }
    }

    private bool active = true;
    private int tickCounter;
    private static MethodInfo impactMethod;

    private static readonly AccessTools.FieldRef<Projectile, Vector3> projectileOrigin =
        AccessTools.FieldRefAccess<Projectile, Vector3>("origin");
}
