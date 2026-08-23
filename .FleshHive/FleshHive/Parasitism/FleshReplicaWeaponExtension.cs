using System.Collections.Generic;
using Verse;

namespace FleshHive;

public class FleshReplicaWeaponExtension : DefModExtension
{
    public List<FleshReplicaWeaponOption> weapons = new List<FleshReplicaWeaponOption>();
}

public class FleshReplicaWeaponOption
{
    public ThingDef weapon = null!;
    public float weight = 1f;
}
