using System.Collections.Generic;
using System.Linq;
using HiveCreatureFramework;
using RimWorld;
using Verse;

namespace FleshHive;

public class FissionmeldUnit : Unit
{
    public override void Kill(DamageInfo? dinfo, Hediff exactCulprit = null)
    {
        CompHiveGroup groupComp = this.TryGetComp<CompHiveGroup>();
        CompFissionmeldState state = this.TryGetComp<CompFissionmeldState>();
        if (this.Spawned && groupComp != null && state != null && groupComp.groups.Count > 0)
        {
            state.PreservedGroups.Clear();
            state.PreservedGroups.AddRange(groupComp.groups.Where(group => group != null));
            foreach (UnitGroup group in state.PreservedGroups)
            {
                if (group.units.Contains(this))
                {
                    group.RemoveUnit(this);
                }
            }
            groupComp.groups.Clear();
        }

        base.Kill(dinfo, exactCulprit);
    }
}
