using System.Collections;
using System.Collections.Generic;

public static class Settings 
{
    //TODO: Implement the logic for these settings.
    public static bool discardIncompleteLvl = true;
    public static bool onlyLastCompletedLvl = false;
    public static int minLvl = 0;
    public static int maxLvl = 29;
    public static int minLogLength = 0;

    public static int minTapsPerEpisode = 4;
    public static int htapSample_min = 6;

    public static float rtCutoff_min = 0f;
    public static float rtCutoff_max = 30f;


    public static void Reset()
    {
        discardIncompleteLvl = true;
        onlyLastCompletedLvl = false;
        minLvl = 0;
        maxLvl = 29;
        minLogLength = 0;
    }

}