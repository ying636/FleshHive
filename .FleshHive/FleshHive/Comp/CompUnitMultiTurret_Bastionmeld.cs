using HiveCreatureFramework;
using Verse;

namespace FleshHive;

public class CompUnitMultiTurret_Bastionmeld : CompUnitMultiTurret
{
    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        foreach (UnitTurret turret in Turrets)
        {
            turret.MakeGun();
        }
    }
}
