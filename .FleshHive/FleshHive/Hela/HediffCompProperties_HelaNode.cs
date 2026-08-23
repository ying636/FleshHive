using HiveCreatureFramework;

namespace FleshHive;

public class HediffCompProperties_HelaNode : HediffCompProperties_NodeUnit
{
    public HediffCompProperties_HelaNode()
    {
        compClass = typeof(HediffComp_HelaNode);
    }

    public int maintenanceIntervalTicks = 2500;
    public float maintenancePerInterval = 0.1f;
    public float maintenancePerTwistedFlesh = 1f;
}
