using RimWorld;
using Verse;

namespace FleshHive;

public class Building_HostileFleshStore : Building, IThingHolder
{
    public Building_HostileFleshStore()
    {
        corpses = new ThingOwner<Corpse>(this, false, LookMode.Deep);
    }

    public int StoredCount => corpses.Count;
    public int ProcessedCorpses => processedCorpses;
    public float HarvestedNutrition => harvestedNutrition;
    public bool CanReceive => Spawned && StoredCount < 8
        && Map.GetComponent<MapComponent_HostileFleshHive>().GetColony(this)?.Lord.Active == true;

    public bool Receive(Corpse corpse)
    {
        if (!CanReceive || corpse.Destroyed || !corpse.InnerPawn.RaceProps.Animal || corpse.InnerPawn.Faction != null
            || corpse.GetRotStage() == RotStage.Dessicated)
            return false;
        bool spawned = corpse.Spawned;
        IntVec3 position = corpse.Position;
        Map map = corpse.Map;
        if (spawned)
            corpse.DeSpawn();
        if (corpses.TryAddOrTransfer(corpse))
            return true;
        if (spawned)
            GenSpawn.Spawn(corpse, position, map);
        return false;
    }

    public ThingOwner GetDirectlyHeldThings()
    {
        return corpses;
    }

    public void GetChildHolders(List<IThingHolder> outChildren)
    {
        ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, corpses);
    }

    public override void TickRare()
    {
        base.TickRare();
        HostileFleshHiveColony? colony = Map.GetComponent<MapComponent_HostileFleshHive>().GetColony(this);
        if (colony?.Lord.Active != true)
            return;
        if (pendingNutrition > 0f)
        {
            pendingNutrition -= colony.AddNutrition(pendingNutrition);
            return;
        }
        if (corpses.Count == 0)
        {
            processTicks = 0;
            return;
        }
        processTicks += colony.HasNerveCenter ? 500 : 250;
        if (processTicks < 2500)
            return;
        processTicks = 0;
        Corpse corpse = corpses[0];
        if (corpse.GetRotStage() != RotStage.Dessicated)
        {
            float efficiency = corpse.GetRotStage() == RotStage.Rotting ? 0.5f : 1f;
            foreach (Thing product in corpse.InnerPawn.ButcherProducts(null!, efficiency))
            {
                if (product.def.IsMeat)
                    pendingNutrition += product.stackCount * product.GetStatValue(StatDefOf.Nutrition);
                product.Destroy();
            }
            harvestedNutrition += pendingNutrition;
            processedCorpses++;
        }
        corpses.Remove(corpse);
        corpse.Destroy();
        pendingNutrition -= colony.AddNutrition(pendingNutrition);
    }

    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        if (Spawned)
            corpses.TryDropAll(Position, Map, ThingPlaceMode.Near);
        base.Destroy(mode);
    }

    public override string GetInspectString()
    {
        return base.GetInspectString() + "\n" + "FH_HostileHive_StoredCorpses".Translate() + $": {StoredCount}/8"
            + "\n" + "FH_HostileHive_PendingNutrition".Translate() + $": {pendingNutrition:0.##}"
            + "\n" + "FH_HostileHive_HarvestedNutrition".Translate() + $": {harvestedNutrition:0.##}";
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Deep.Look(ref corpses, "FH_corpses", this);
        Scribe_Values.Look(ref processTicks, "FH_processTicks");
        Scribe_Values.Look(ref pendingNutrition, "FH_pendingNutrition");
        Scribe_Values.Look(ref processedCorpses, "FH_processedCorpses");
        Scribe_Values.Look(ref harvestedNutrition, "FH_harvestedNutrition");
    }

    private ThingOwner<Corpse> corpses;
    private int processTicks;
    private float pendingNutrition;
    private int processedCorpses;
    private float harvestedNutrition;
}
