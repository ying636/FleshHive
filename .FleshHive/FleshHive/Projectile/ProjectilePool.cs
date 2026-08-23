using System.Collections.Generic;
using Verse;

namespace FleshHive;

public static class ProjectilePool
{
    public static HashSet<Projectile> ActiveProjectiles
    {
        get
        {
            if (activeProjectiles == null)
            {
                activeProjectiles = new HashSet<Projectile>();
            }
            return activeProjectiles;
        }
    }

    public static void Register(Projectile p)
    {
        ActiveProjectiles.Add(p);
    }

    public static void Unregister(Projectile p)
    {
        if (activeProjectiles == null)
        {
            return;
        }
        activeProjectiles.Remove(p);
    }

    public static int Count => activeProjectiles?.Count ?? 0;

    private static HashSet<Projectile> activeProjectiles;
}
