using System.Collections;
using System.Collections.Generic;

public static class Settings 
{

    public static string outDir_default = @"D:\documents\data\meta-two\rt\";

    //TODO: Implement the logic for these settings.
    public static bool discardIncompleteLvl = true;
    public static bool onlyLastCompletedLvl = false;
    
    // levels to analyze
    public static int minLvl = 0;
    public static int maxLvl = 29;
    
    // minimum amount of episodes in a log to be considered for analysis
    public static int minLogEpisodes = 14;

    // Level boundries of a log to be considered for analysis.
    // When not met, the logs is completely discarded .
    public static int minStartLvl_cutoff = 0;
    public static int maxEndLvl_cutoff = 29;


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
        minLogEpisodes = 0;
    }

}