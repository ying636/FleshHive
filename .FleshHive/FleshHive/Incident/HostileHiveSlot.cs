using Verse;

namespace FleshHive;

public class HostileHiveSlot : IExposable
{
    public bool Occupied => building is { Spawned: true } && building.def == def;

    public void ExposeData()
    {
        Scribe_Defs.Look(ref def, "def");
        Scribe_Values.Look(ref anchor, "anchor");
        Scribe_Values.Look(ref position, "position", IntVec3.Invalid);
        Scribe_Values.Look(ref cost, "cost");
        Scribe_Values.Look(ref minHives, "minHives", 1);
        Scribe_Values.Look(ref finishTick, "finishTick");
        Scribe_References.Look(ref building, "building");
    }

    public ThingDef def = null!;
    public IntVec3 anchor;
    public IntVec3 position = IntVec3.Invalid;
    public float cost;
    public int minHives;
    public int finishTick;
    public Thing? building;
}
