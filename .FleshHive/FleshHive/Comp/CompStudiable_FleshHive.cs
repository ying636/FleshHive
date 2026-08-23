using RimWorld;
using Verse;

namespace FleshHive;

public class CompStudiable_FleshHive : CompStudiable
{
    public override float AnomalyKnowledge
    {
        get
        {
            if (!ModsConfig.AnomalyActive || KnowledgeCategory == null)
            {
                return 0f;
            }

            MapComponent_FleshHive? mapComp = parent.Map?.GetComponent<MapComponent_FleshHive>();
            return mapComp?.HiveScale / 24f ?? 0f;
        }
    }
}
