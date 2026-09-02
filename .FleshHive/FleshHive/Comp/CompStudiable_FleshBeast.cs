using RimWorld;
using Verse;

namespace FleshHive;

public class CompProperties_Studiable_FleshBeast : CompProperties_Studiable
{
    public CompProperties_Studiable_FleshBeast()
    {
        this.compClass = typeof(CompStudiable_FleshBeast);
    }
}

public class CompStudiable_FleshBeast : CompStudiable
{
    public override float AnomalyKnowledge
    {
        get
        {
            if (!ModsConfig.AnomalyActive || this.Pawn == null)
            {
                return 0f;
            }

            return FleshBeastKindUtility.SizeOf(this.Pawn.kindDef) switch
            {
                FleshBeastSize.Small => 1f,
                FleshBeastSize.Medium => 2f,
                FleshBeastSize.Large => 2.5f,
                FleshBeastSize.Giant => 4f,
                _ => base.AnomalyKnowledge
            };
        }
    }

    public override KnowledgeCategoryDef KnowledgeCategory
    {
        get
        {
            if (this.Pawn != null)
            {
                FleshBeastSize? size = FleshBeastKindUtility.SizeOf(this.Pawn.kindDef);
                if (size == FleshBeastSize.Large || size == FleshBeastSize.Giant)
                {
                    return DefDatabase<KnowledgeCategoryDef>.GetNamed("Advanced");
                }
            }

            return base.KnowledgeCategory;
        }
    }
}
