using System.IO;



public static class Settings
{

    public static string outDir_default = @"D:\documents\data\meta-two\rt\";

    public static bool discardIncompleteLvl = true;
    public static bool onlyLastLvl = true;

    // level range to analyze inside a log
    public static int minLvl = 0;
    public static int maxLvl = 29;

    // Requirements for a log file to be considered for analysis. If not met, log is discarded entirely.
    public static int log_minEpisodes = 14;
    public static int log_minStartLvl = 0;
    public static int log_maxEndLvl = 29;

    // HyperTapping detection
    public static int minTapsPerEpisode = 4;
    public static int htapSample_min = 6;

    public static float rtCutoff_min = 0f;
    public static float rtCutoff_max = 30f;


    public static void PrintTo(string path)
    {
        File.WriteAllLines(path + "Settings.txt", new string[]{
            "discardIncompleteLvl = " + discardIncompleteLvl.ToString(),
            "onlyLastLvl = " + onlyLastLvl.ToString(),
            "minLvl = " + minLvl.ToString(),
            "maxLvl = " + maxLvl.ToString(),
            "log_minEpisodes = " + log_minEpisodes.ToString(),
            "log_minStartLvl = " + log_minStartLvl.ToString(),
            "log_maxEndLvl = " + log_maxEndLvl.ToString(),
            "minTapsPerEpisode = " + minTapsPerEpisode.ToString(),
            "htapSample_min = " + htapSample_min.ToString(),
            "rtCutoff_min = " + rtCutoff_min.ToString(),
            "rtCutoff_min = " + rtCutoff_max.ToString()
        });
    }

}