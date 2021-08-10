using System.Collections;
using System.Collections.Generic;

public static class Settings 
{

    public static string outDir_default = @"D:\documents\data\meta-two\rt\";

    public static bool discardIncompleteLvl = true;
    public static bool onlyLastLvl = false;
    
    // level range to analyze inside a log
    public static int minLvl = 3;
    public static int maxLvl = 6;

    // Requirements for a log files to be considered for analysis. If not met, log is discarded entirely.
    public static int log_minEpisodes = 14;
    public static int log_minStartLvl = 0;
    public static int log_maxEndLvl = 29;

    // HyperTapping detection
    public static int minTapsPerEpisode = 4;
    public static int htapSample_min = 6;

    public static float rtCutoff_min = 0f;
    public static float rtCutoff_max = 30f;

}