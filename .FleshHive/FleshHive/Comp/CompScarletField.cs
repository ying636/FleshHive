using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive;

public class CompProperties_ScarletField : CompProperties
{
    public CompProperties_ScarletField()
    {
        this.compClass = typeof(CompScarletField);
    }

    public float areaShieldRadius = 4.5f;

    public int durationTicks = 1500;

}

public class CompScarletField : ThingComp
{
    private CompProperties_ScarletField Props => (CompProperties_ScarletField)this.props;

    private Pawn PawnOwner => this.parent as Pawn;

    public bool Active => active;

    public float AreaShieldRadius => Props.areaShieldRadius;

    public bool CanActivate => PawnOwner != null && PawnOwner.Spawned;

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

    private static Material GetShieldMat(float alpha)
    {
        float key = Mathf.Round(alpha * 100f);
        if (!shieldMatCache.TryGetValue(key, out Material mat))
        {
            mat = new Material(MaterialPool.MatFrom("Other/FleshShieldBubble", ShaderDatabase.Transparent));
            mat.color = new Color(1f, 0.15f, 0.15f, alpha);
            shieldMatCache[key] = mat;
        }
        return mat;
    }

    public void Activate()
    {
        SetActive(!active);
    }

    public override void CompTick()
    {
        base.CompTick();
        if (!active || PawnOwner == null || !PawnOwner.Spawned)
        {
            return;
        }

        tickCounter++;
        if (tickCounter < 2)
        {
            return;
        }
        tickCounter = 0;

        AreaShieldInterceptTick();
    }

    public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
    {
        absorbed = false;
        if (!active)
        {
            return;
        }

        Pawn pawn = PawnOwner;
        if (pawn == null || !pawn.Spawned)
        {
            return;
        }

        bool isRanged = dinfo.Def.isRanged;
        int cost = isRanged ? 1 : 20;

        if (TwistedFleshUtility.ConsumeTwistedFlesh(pawn, cost))
        {
            absorbed = true;
        }
    }

    public override void PostDraw()
    {
        base.PostDraw();
        if (!active || PawnOwner == null || !PawnOwner.Spawned)
        {
            return;
        }

        Vector3 pos = PawnOwner.DrawPos;

        float smallRadius = 0.5f + PawnOwner.BodySize * 0.3f;
        Matrix4x4 smallMat = default(Matrix4x4);
        smallMat.SetTRS(pos, Quaternion.identity, new Vector3(smallRadius * 2f, 1f, smallRadius * 2f));
        Graphics.DrawMesh(MeshPool.plane10, smallMat, GetShieldMat(0.6f), 0);

        pos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
        float bigRadius = Props.areaShieldRadius;
        Matrix4x4 bigMat = default(Matrix4x4);
        bigMat.SetTRS(pos, Quaternion.identity, new Vector3(bigRadius * 2f, 1f, bigRadius * 2f));
        Graphics.DrawMesh(MeshPool.plane10, bigMat, GetShieldMat(0.55f), 0);
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref active, "active");
    }

    private void AreaShieldInterceptTick()
    {
        Pawn pawn = PawnOwner;
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
    }

    private bool active = true;

    private int tickCounter;

    private static MethodInfo impactMethod;

    private static Dictionary<float, Material> shieldMatCache = new Dictionary<float, Material>();
}
