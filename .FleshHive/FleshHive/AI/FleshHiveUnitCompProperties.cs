using HiveCreatureFramework;

namespace FleshHive;

public class FleshHiveUnitCompProperties : UnitCompProperties
{
    public FleshHiveUnitCompProperties()
    {
        compClass = typeof(FleshHiveUnitComp);
        GizmoClass = typeof(Gizmo_Group_FleshHive);
    }
}
