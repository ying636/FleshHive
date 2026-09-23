using Verse;

namespace FleshHive;

public interface IShardExpandableParasiteCapacity
{
    int ParasiteCapacity { get; }

    int MaximumParasiteCapacity { get; }

    bool CanIncreaseParasiteCapacity { get; }

    HediffDef ShardComaDef { get; }

    bool TryIncreaseParasiteCapacity();
}
