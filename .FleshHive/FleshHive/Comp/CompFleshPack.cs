using System.Linq;
using RimWorld;
using Verse;

namespace FleshHive;

public class CompProperties_FleshPack : CompProperties
{
    public CompProperties_FleshPack()
    {
        this.compClass = typeof(CompFleshPack);
    }
}

public class CompFleshPack : ThingComp
{
    public override void Notify_Equipped(Pawn pawn)
    {
        base.Notify_Equipped(pawn);
        ParasitismSystem? system = pawn.health?.hediffSet?
            .GetFirstHediffOfDef(FleshHiveDefOf.FH_ParasitismSystem) as ParasitismSystem;
        system?.SetDirty();
    }

    public override void Notify_Unequipped(Pawn pawn)
    {
        base.Notify_Unequipped(pawn);
        ParasitismSystem? system = pawn.health?.hediffSet?
            .GetFirstHediffOfDef(FleshHiveDefOf.FH_ParasitismSystem) as ParasitismSystem;
        if (system == null)
        {
            return;
        }

        system.SetDirty();
        int limit = system.Limit;
        while (system.Count > limit)
        {
            ParasitismHediff hediff = system.ParasitismHediffs.Last();
            if (hediff.flesh != null)
            {
                GenSpawn.Spawn(hediff.flesh, pawn.Position, pawn.Map);
            }

            hediff.flesh = null;
            pawn.health.RemoveHediff(hediff);
            system.ParasitismHediffs.Remove(hediff);
            system.SetDirty();
        }
    }
}
