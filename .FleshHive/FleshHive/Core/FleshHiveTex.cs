using UnityEngine;
using Verse;

namespace FleshHive;

[StaticConstructorOnStartup]
public static class FleshHiveTex
{
    public static readonly Texture2D SetTargetFuelLevelCommand = ContentFinder<Texture2D>.Get("UI/Commands/SetTargetFuelLevel");

    public static readonly Material FleshBoxOverlay = MaterialPool.MatFrom("FH_FleshBox/FH_FleshBox2", ShaderDatabase.Transparent);

    public static readonly Material FleshBoxBottom = MaterialPool.MatFrom("FH_FleshBox/FH_FleshBox2", ShaderDatabase.Transparent);

    public static readonly Material FleshBoxTop = MaterialPool.MatFrom("FH_FleshBox/FH_FleshBox", ShaderDatabase.Transparent);
}
