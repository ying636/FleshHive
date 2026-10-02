using Verse;

namespace FleshHive.CE;

public class TentacleProperties_CE : TentacleProperties
{
    public TentacleProperties_CE()
    {
        tentacleClass = typeof(Tentacle_Attackable_CE);
    }

    public ThingDef meleeSource = null!;
    public int sourceToolIndex;
}
