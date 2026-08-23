namespace FleshHive;

public interface IFleshHiveHuntingGroup
{
    bool AllowHuntUndesignatedAnimals { get; set; }

    int MinimumHealthyHunters { get; set; }
}
