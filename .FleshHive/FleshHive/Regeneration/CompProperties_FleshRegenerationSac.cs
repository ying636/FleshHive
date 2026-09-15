using Verse;

namespace FleshHive;

public class CompProperties_FleshRegenerationSac : CompProperties_FleshHiveContainer
{
    public CompProperties_FleshRegenerationSac()
    {
        compClass = typeof(CompFleshRegenerationSac);
        canRecycleUnit = false;
    }

    public GraphicData topGraphicData = new();
}
