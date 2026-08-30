using UnityEngine;
using Verse;

namespace FleshHive;

public class PawnNodeRenderWorker_Tentacle : PawnRenderNodeWorker
{
    public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
    {
        if (node is PawnNodeRender_Tentacle n)
        {
            bool left = !n.tentacle.isRight;
            if (!left)
            {
                if (parms.facing == Rot4.East)
                {
                    return false;
                }
            }
            else if (parms.facing == Rot4.West)
            {
                return false;
            } 
        }
        return base.CanDrawNow(node, parms);
    }

    public override float LayerFor(PawnRenderNode node, PawnDrawParms parms)
    {
        float layer = base.LayerFor(node, parms);
        if (node is PawnNodeRender_Tentacle { tentacle: Tentacle_WeaponMount } && parms.facing == Rot4.South)
        {
            return Mathf.Max(layer, 100f);
        }
        return layer;
    }

    public override Vector3 OffsetFor(PawnRenderNode node, PawnDrawParms parms, out Vector3 pivot)
    {
        var result =  base.OffsetFor(node, parms, out pivot);
        if (node is PawnNodeRender_Tentacle n)
        {
            result = n.tentacle.drawPosOffset;
            if (n.tentacle is Tentacle_WeaponMount { MountedWeapon: not null } weaponMount
                && result.sqrMagnitude > 0f)
            {
                result += result.normalized * weaponMount.Prop.weaponDrawOffset;
            }
            if (parms.facing == Rot4.North)
            {
                result.x *= -1f;
            }
        }

        return result;
    }
    public override Quaternion RotationFor(PawnRenderNode node, PawnDrawParms parms)
    {
        if (node is PawnNodeRender_Tentacle n)
        {
            if (n.tentacle is Tentacle_WeaponMount { MountedWeapon: not null } && n.tentacle.targetAngle >= 0f)
            {
                return base.RotationFor(node, parms) * Quaternion.AngleAxis(n.tentacle.targetAngle, Vector3.up);
            }

            float baseAngle = (n.tentacle.rotateTime > 0f
                ? n.tentacle.targetAngle
                : n.tentacle.angle); 
            float angle = baseAngle + n.tentacle.extraAngle;
            if (n.tentacle.isRight && n.tentacle is Tentacle_WeaponMount { MountedWeapon: not null })
            {
                angle += 180f;
            }
            if (parms.facing == Rot4.North)
            {
                angle *= -1f;
            }
            return Quaternion.AngleAxis(angle, Vector3.up);
        }
        return base.RotationFor(node, parms);
    }
}

public class PawnNodeRender_Tentacle : PawnRenderNode
{
    public PawnNodeRender_Tentacle(Tentacle tentacle,Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree) : base(pawn, props, tree)
    {
        this.tentacle = tentacle;
    }

    public override bool FlipGraphic(PawnDrawParms parms)
    {
        if (this.tentacle is Tentacle_WeaponMount { MountedWeapon: not null })
        {
            return base.FlipGraphic(parms);
        }

        if (this.tentacle.isRight && parms.facing.AsVector2.x == 0)
        {
            return true;
        }
        return base.FlipGraphic(parms);
    }

    public override Graphic GraphicFor(Pawn pawn)
    {
        if (tentacle is Tentacle_WeaponMount { MountedWeapon: not null } weaponMount)
        {
            Graphic graphic = weaponMount.MountedWeapon.Graphic;
            if (graphic != null)
            {
                return graphic;
            }
        }
        return base.GraphicFor(pawn);
    }

    public Tentacle tentacle;
}
