using UnityEngine;
using Verse;

namespace FleshHive.CE;

public class Mote_BeamIncineratorCE : MoteDualAttached
{
    public Vector3 BeamStart
    {
        get
        {
            link1.UpdateDrawPos();
            return link1.LastDrawPos;
        }
    }

    public Vector3 BeamEnd
    {
        get
        {
            link2.UpdateDrawPos();
            return link2.LastDrawPos;
        }
    }

    public void ClipEnd(Vector3 position)
    {
        link2.UpdateTarget(new TargetInfo(position.ToIntVec3(), Map), position - position.ToIntVec3().ToVector3Shifted());
    }
}
