using HiveCreatureFramework;

namespace FleshHive;

public class FleshHiveNodeUnitCompProperties : NodeUnitCompProperties
{
    public FleshHiveNodeUnitCompProperties()
    {
        compClass = typeof(FleshHiveNodeUnitComp);
        GizmoClass = typeof(Gizmo_Group_FleshHive);
    }
}
