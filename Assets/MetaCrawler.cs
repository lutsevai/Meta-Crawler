using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;



public class MetaCrawler
{

    // FILE VARIABLES
    // ==============
    const char sep = '\t';
    string outDir;


    // GENERAL STRUCTURES
    // ==================
    List<string>[,] sample_meanRt_speedrot;


    //PER-SUBJECT STRUCTURES
    // =================

    // reaction times [sid > Zoid,level]
    Dictionary<string, List<RT>[,]> subj_rt_zoidlvl;

    // one rt (mean) per subject
    Dictionary<string, double> subj_meanRt;
    Dictionary<string, double> subj_meanRt_o;

    // count of games classified as das/hypertap games [sid > PlayStyle]
    Dictionary<string, int[]> subj_strats;

    // discarded data [sid > filenames of bad logs]
    Dictionary<string, List<string>> subj_badLogs;

    Dictionary<string, RTStats> subj_rtStats;

    int[,,] rt_sampleActions;


    /// <summary>
    /// Main method of the class - recursively searches a directroy for log files, and process them to extract various aspects of RTs into an output folder.
    /// </summary>
    /// <param name="newInDir">Path containing the data needed processing.</param>
    /// <param name="newOutDir">Path to which all the processed data will be written to.</param>
    public void Crawl(string newInDir, string newOutDir)
    {
        // INITIALIZATION
        // ==============
        sample_meanRt_speedrot = NewLoA_string(MetaTypes.rotations.Length, MetaTypes.speedLevels.Length);
        subj_rt_zoidlvl = new Dictionary<string, List<RT>[,]>();
        subj_meanRt = new Dictionary<string, double>();
        subj_meanRt_o = new Dictionary<string, double>();
        subj_strats = new Dictionary<string, int[]>();
        subj_badLogs = new Dictionary<string, List<string>>();
        subj_rtStats = new Dictionary<string, RTStats>();
        DirectoryCrawler dirCrawler = new DirectoryCrawler();
        rt_sampleActions = NewRTTypeCounter();

        // setting directories
        outDir = newOutDir + Path.GetFileName(newInDir.TrimEnd(Path.DirectorySeparatorChar)) + Path.DirectorySeparatorChar;
        Directory.CreateDirectory(outDir);

        //clear output directory
        System.IO.DirectoryInfo di = new DirectoryInfo(outDir);
        foreach (FileInfo file in di.GetFiles())
        {
            file.Delete();
        }
        foreach (DirectoryInfo dir in di.GetDirectories())
        {
            dir.Delete(true);
        }

        Settings.PrintTo(outDir);

        List<string> outputLines = new List<string>();


        // RT
        // ==
        dirCrawler.WalkDirectoryTree(new DirectoryInfo(newInDir), ExtractRt);

        //per-subject
        foreach (KeyValuePair<string, List<RT>[,]> kvp in subj_rt_zoidlvl)
        {
            ProcessRT(outDir + kvp.Key + @"\", kvp.Value, kvp.Key);
        }


        // ADDITIONAL OUTPUT
        // =================

        // combined averages
        List<string>[] out_means = new List<string>[MetaTypes.rotations.Length * MetaTypes.speedLevels.Length];
        // calculate averages, add them to the according structure
        for (int speedLvl = 0; speedLvl < sample_meanRt_speedrot.GetLength(1); speedLvl++)
        {
            for (int rot = 0; rot < sample_meanRt_speedrot.GetLength(0); rot++)
            {
                out_means[speedLvl * MetaTypes.rotations.Length + rot] = sample_meanRt_speedrot[rot, speedLvl];
            }
        }
        File.WriteAllLines(outDir + "sample_rt_mean_speedrot" + MetaLog.logExtension, Data.Merge(out_means, sep));


        //rotations averaged over all lvls
        List<string>[] sample_mean_rt = new List<string>[MetaTypes.rotations.Length];
        for (int i = 0; i < sample_mean_rt.Length; i++)
        {
            sample_mean_rt[i] = new List<string>();
        }

        for (int speedLvl = 0; speedLvl < sample_meanRt_speedrot.GetLength(1); speedLvl++)
        {
            for (int rot = 0; rot < sample_meanRt_speedrot.GetLength(0); rot++)
            {
                sample_mean_rt[rot].AddRange(sample_meanRt_speedrot[rot, speedLvl]);
            }
        }
        File.WriteAllLines(outDir + "sample_rt_mean" + MetaLog.logExtension, Data.Merge(sample_mean_rt, sep));


        WritePlayerActionTypes(outDir, rt_sampleActions);

        // per-subject mean rt for all zoids
        outputLines.Clear();
        foreach (KeyValuePair<string, double> s in subj_meanRt)
        {
            outputLines.Add(s.Key + sep + s.Value);
        }
        File.WriteAllLines(outDir + "subjects_rt_mean.tsv", outputLines.ToArray());

        // per-subject mean rt for just the O zoid
        outputLines.Clear();
        foreach (KeyValuePair<string, double> s in subj_meanRt_o)
        {
            outputLines.Add(s.Key + sep + s.Value);
        }
        File.WriteAllLines(outDir + "subjects_rt_mean_o.tsv", outputLines.ToArray());

        // bad logs
        outputLines.Clear();
        foreach (KeyValuePair<string, List<string>> s in subj_badLogs)
        {
            outputLines.Add(s.Key + sep + s.Value.Count + sep + String.Join(sep.ToString(), s.Value));
        }
        File.WriteAllLines(outDir + "subjects_badData.tsv", outputLines.ToArray());


        // bad rt data, good rt data
        outputLines.Clear();
        outputLines.Add("SID" + sep + "good_count" + sep + "belowMin_count" + sep + "belowMin_percent" + sep + "aboveMax_count" + sep + "aboveMax_percent");
        foreach (KeyValuePair<string, RTStats> s in subj_rtStats)
        {
            float belowPercent = (float)s.Value.belowMin_count / (float)(s.Value.getTotalRT()) * 100;
            float abovePercent = (float)s.Value.aboveMax_count / (float)(s.Value.getTotalRT()) * 100;
            outputLines.Add(s.Key + sep + s.Value.good_count.ToString() + sep + s.Value.belowMin_count.ToString() + sep + string.Format("{0:N2}", belowPercent) + sep + s.Value.aboveMax_count.ToString() + sep + string.Format("{0:N2}", abovePercent));
        }
        File.WriteAllLines(outDir + "subjects_rtStats.tsv", outputLines.ToArray());



        // HYPERTAP / DAS CATEGORIZATION
        // =============================
        dirCrawler.WalkDirectoryTree(new DirectoryInfo(newInDir), ProcessStrategy);

        outputLines.Clear();
        outputLines.Add(string.Format("SID{0}DAS{0}H-TAP{0}", sep));
        foreach (KeyValuePair<string, int[]> s in subj_strats)
        {
            outputLines.Add(s.Key + sep + String.Join(sep.ToString(), s.Value));
        }
        File.WriteAllLines(outDir + "subj_strats.tsv", outputLines.ToArray());
    }



    List<RT>[,] CondenseToSpeedRotArray(List<RT>[,] rts_raw)
    {
        List<RT>[,] current_rt_rotlvl = NewLoA(MetaTypes.rotations.Length, MetaTypes.speedLevels.Length);

        // condense data from [zoid, lvl] data structure into [rotationNr , speedRank]
        for (int zoidNr = 0; zoidNr < rts_raw.GetLength(0); zoidNr++)
        {
            int cRot = MetaTypes.GetRotations(zoidNr);

            for (int lvl = 0; lvl < rts_raw.GetLength(1); lvl++)
            {
                // Determening the position of the level in speedArray
                int cSpeedRank = MetaTypes.getSpeedRank(lvl);

                // Adding all values from the raw lists in the current loop to the corresponding node in the rotation/speedlvl structure.
                current_rt_rotlvl[cRot, cSpeedRank].AddRange(rts_raw[zoidNr, lvl]);
            }
        }
        return current_rt_rotlvl;
    }



    void ProcessRT(string dirPath, List<RT>[,] rts_raw, string cSid)
    {
        Directory.CreateDirectory(dirPath);

        int belowMinRt = 0;
        int goodRt = 0;
        int aboveMaxRt = 0;
        List<RT> badList = new List<RT>();

        for (int zoid = 0; zoid < rts_raw.GetLength(0); zoid++)
        {
            for (int level = 0; level < rts_raw.GetLength(1); level++)
            {
                badList.Clear();
                foreach (RT rt in rts_raw[zoid, level])
                {
                    if (rt.val < Settings.rtCutoff_min)
                    {
                        belowMinRt++;
                        badList.Add(rt);
                    }
                    else if (rt.val > Settings.rtCutoff_max)
                    {

                        Debug.Log(cSid + " " + rt.val);
                        aboveMaxRt++;
                        badList.Add(rt);
                    }
                    else
                    {
                        goodRt++;
                    }
                }

                foreach (RT rt in badList)
                {
                    rts_raw[zoid, level].Remove(rt);
                }

            }
        }

        subj_rtStats.Add(cSid, new RTStats(goodRt, belowMinRt, aboveMaxRt));

        string[] outlier_lines = new string[] { "goodRT" + sep + goodRt.ToString(), "belowMinRT" + sep + belowMinRt.ToString(), "aboveMaxRT" + sep + aboveMaxRt.ToString() };
        File.WriteAllLines(dirPath + "rt_outliers" + MetaLog.logExtension, outlier_lines);


        // write RT to separate files for each lvl, and zoid type
        for (int zoid = 0; zoid < rts_raw.GetLength(0); zoid++)
        {
            for (int level = 0; level < rts_raw.GetLength(1); level++)
            {
                string zoidName = MetaTypes.GetZoidType(zoid).ToString();
                string fileName = String.Format("lvl{0}_{1}", level, zoidName);
                WriteRTsToFile(dirPath + fileName + MetaLog.logExtension, rts_raw[zoid, level]);
            }
        }

        // categorized reaction times by zoid [rotations, fall_speed] 
        List<RT>[,] current_rt_rotlvl = CondenseToSpeedRotArray(rts_raw);


        // write RT to separate files for each speedstep, and rotation type
        for (int rot = 0; rot < current_rt_rotlvl.GetLength(0); rot++)
        {
            for (int speedRank = 0; speedRank < current_rt_rotlvl.GetLength(1); speedRank++)
            {
                string fileName = String.Format("speed{0}_rot{1}", speedRank, rot);
                WriteRTsToFile(dirPath + fileName + MetaLog.logExtension, current_rt_rotlvl[rot, speedRank]);
            }
        }

        // create an output summary datastructure of RTs for all rot-types, all levels
        List<List<RT>> allRots_allSpeedRanks = new List<List<RT>>();

        //write a summary of all rts per speedstep to a single file, 3 columns
        for (int speedRank = 0; speedRank < current_rt_rotlvl.GetLength(1); speedRank++)
        {
            List<RT>[] listRots = new List<RT>[MetaTypes.rotations.Length];
            for (int i = 0; i < listRots.Length; i++)
            {
                listRots[i] = new List<RT>();
            }

            for (int rot = 0; rot < current_rt_rotlvl.GetLength(0); rot++)
            {
                listRots[rot].AddRange(current_rt_rotlvl[rot, speedRank]);
            }

            // add all extracted RTs to the general rots list
            foreach (List<RT> list in listRots)
            {
                allRots_allSpeedRanks.Add(list);
            }

            string fileName = String.Format("speedRank{0}_allRot", speedRank);
            File.WriteAllLines(dirPath + fileName + MetaLog.logExtension, Data.Merge(listRots, sep));
        }

        //Writing a summary file with all rts, categorized by rotation, and levels
        File.WriteAllLines(dirPath + "allRTs_allSpeedRanks" + MetaLog.logExtension, Data.Merge(allRots_allSpeedRanks.ToArray(), sep));



        // AVERAGES
        // --------
        // calculate averages, add them to the according structure
        double total_all = 0;
        long total_count = 0;

        double total_all_o = 0;
        long total_count_o = 0;
        for (int speedLvl = 0; speedLvl < current_rt_rotlvl.GetLength(1); speedLvl++)
        {
            for (int rot = 0; rot < current_rt_rotlvl.GetLength(0); rot++)
            {
                if (current_rt_rotlvl[rot, speedLvl].Count > 0)
                {
                    double total = 0;
                    foreach (RT r in current_rt_rotlvl[rot, speedLvl])
                    {
                        total += r.val;
                        ;
                    }
                    total_all += total;
                    total_count += current_rt_rotlvl[rot, speedLvl].Count;

                    if (rot == 0)
                    {
                        total_all_o += total;
                        total_count_o += current_rt_rotlvl[rot, speedLvl].Count;
                    }

                    double avg = total / current_rt_rotlvl[rot, speedLvl].Count;
                    sample_meanRt_speedrot[rot, speedLvl].Add(avg.ToString());
                }
            }
        }

        subj_meanRt.Add(Path.GetFileName(cSid), total_all / total_count);
        subj_meanRt_o.Add(Path.GetFileName(cSid), total_all_o / total_count_o);

        // --------------
        // RT Action Type
        // --------------

        //initialize basic structure
        int[,,] rt_playerActions = NewRTTypeCounter();

        CountActionTypes(current_rt_rotlvl, rt_playerActions);

        WritePlayerActionTypes(dirPath, rt_playerActions);
    }


    private static void WritePlayerActionTypes(string dirPath, int[,,] rt_playerActions)
    {  
        // RT Action type per level output
        // -------------------------------
        //initialize output structure of lines to be written out. +1 Length for header
        string[] actionLines = new string[Enum.GetValues(typeof(Action)).Length + 1];
        // make header
        string rotLines_header = "" + sep;
        foreach (int i in MetaTypes.speedLevels)
        {
            foreach (int r in MetaTypes.rotations)
            {
                rotLines_header += r.ToString() + sep;
            }
            rotLines_header += sep;
        }
        actionLines[0] = rotLines_header;

        //fill output structure with data from rt_playerActions
        foreach (Action act in Enum.GetValues(typeof(Action)))
        {
            string line = act.ToString() + sep;
            for (int speedRank = 0; speedRank < MetaTypes.speedLevels.Length; speedRank++)
            {
                for (int rot = 0; rot < MetaTypes.rotations.Length; rot++)
                {
                    line += rt_playerActions[rot, speedRank, (int)act].ToString() + sep;
                }
                // leave space inbetween levels
                line += sep;
            }
            actionLines[(int)act + 1] = line;
        }
        File.WriteAllLines(dirPath + "rt_actions_rot-speedlvl" + MetaLog.logExtension, actionLines);


        // RT Action type aggregated output
        // --------------------------------
        
        // initialize summary structure
        int[,] aggregratedRTTypes = new int[MetaTypes.rotations.Length, Enum.GetValues(typeof(Action)).Length];

        for (int i = 0; i < rt_playerActions.GetLength(0); i++)
            for (int j = 0; j < rt_playerActions.GetLength(1); j++)
                for (int k = 0; k < rt_playerActions.GetLength(2); k++)
                    aggregratedRTTypes[i, k] += rt_playerActions[i, j, k];

        //initialize output structure of lines to be written out. +1 Length for header
        string[] actionLines_mean = new string[Enum.GetValues(typeof(Action)).Length + 1];

        //make header
        string rotLines_header_mean = "" + sep;
        foreach (int r in MetaTypes.rotations)
        {
            rotLines_header += r.ToString() + sep;
        }
        rotLines_header += sep;
        actionLines_mean[0] = rotLines_header;


        foreach (Action act in Enum.GetValues(typeof(Action)))
        {
            string line = act.ToString() + sep;
            for (int rot = 0; rot < MetaTypes.rotations.Length; rot++)
                line += aggregratedRTTypes[rot, (int)act].ToString() + sep;
            actionLines_mean[(int)act + 1] = line;
        }
        File.WriteAllLines(dirPath + "rt_actions_rot" + MetaLog.logExtension, actionLines_mean);
    }


    private void CountActionTypes(List<RT>[,] current_rt_rotlvl, int[,,] rt_playerActions)
    {
        // count each type of rt separateley
        for (int speedRank = 0; speedRank < current_rt_rotlvl.GetLength(1); speedRank++)
        {
            //extract the column of the log containing the player action
            for (int rot = 0; rot < current_rt_rotlvl.GetLength(0); rot++)
            {
                foreach (RT lineSplit in current_rt_rotlvl[rot, speedRank])
                {
                    if (MetaTypes.IsValidAction(lineSplit.metaData[(int)Header.evt_data2]))
                    {
                        Action p = MetaTypes.GetAction(lineSplit.metaData[(int)Header.evt_data2]);
                        rt_playerActions[rot, speedRank, (int)p] += 1;
                        rt_sampleActions[rot, speedRank, (int)p] += 1;
                    }
                }
            }
        }
    }


    void ProcessStrategy(string infile)
    {
        string[] lines;
        if (!MetaLog.HasGoodData(infile, out lines))
            return;

        Strategy strat = MetaLog.DetectStrategyy(lines);
        string sid = MetaLog.getSid(lines);

        int[] s;
        if (!subj_strats.TryGetValue(sid, out s))
        {
            s = new int[Enum.GetValues(typeof(Strategy)).Length];
            subj_strats.Add(sid, s);
        }

        s[(int)strat]++;
    }



    //todo: extract method to MetaLog
    void ExtractRt(string inPath)
    {
        List<RT> output = new List<RT>();
        string[] lines;

        if (!MetaLog.IsLog(inPath))
            return;

        // if file is not worth analyzing, adds it to the bad files list, and exits this methods
        if (!MetaLog.HasGoodData(inPath, out lines))
        {
            string badSid = MetaLog.getSid(lines);
            List<string> badFiles;
            if (!(subj_badLogs.TryGetValue(badSid, out badFiles)))
            {
                badFiles = new List<string>();
                subj_badLogs.Add(badSid, badFiles);
            }
            badFiles.Add(inPath);
            return;
        }

        string sid = MetaLog.getSid(lines);

        List<RT>[,] rts_raw_subject;
        int startIndex = MetaLog.GetGameStart(lines);

        // initialize / find subject - specific structure
        if (subj_rt_zoidlvl.ContainsKey(sid))
            subj_rt_zoidlvl.TryGetValue(sid, out rts_raw_subject);
        else
            rts_raw_subject = NewLoA_Raw();            


        string[] finalLine = lines[lines.Length - 1].Split('\t');
        int finalLevel = int.Parse(finalLine[(int)Header.level]);

        int rtCount = 0;
        //search for new zoid events + rt
        for (int i = startIndex; i < lines.Length; i++)
        {
            string[] lineSplit = lines[i].Split('\t');
            if (MetaLog.ContainsEvent(lineSplit, "ZOID", "NEW"))
            {
                int level = int.Parse(lineSplit[(int)Header.level]);

                if (((level == finalLevel) && Settings.discardIncompleteLvl)
                    || level > Settings.maxLvl)
                    break;

                if ((level < Settings.minLvl) ||
                    (level != (finalLevel - 1) && Settings.onlyLastLvl ) )
                    continue;

                // search for the first action after zoid appearance
                for (int j = i + 1; j < lines.Length; j++)
                {
                    string[] lineSplit_j = lines[j].Split('\t');

                    // found key down
                    // todo: what about key up?!
                    if (MetaLog.ContainsEvent(lineSplit_j, "PLAYER", "KEY_DOWN"))
                    {
                        // add it, if both times are not different
                        // todo: check frequency of presses, perhaps they coincide, because it is anticipated
                        if (!lineSplit_j[(int)Header.timestamp].Equals(lineSplit[0]))
                        {
                            rtCount++;
                            float diff = float.Parse(lineSplit_j[(int)Header.timestamp]) - float.Parse(lineSplit[0]);

                            //TODO: careful, as sometimes the string from the metadata is being used. Make universal
                            lineSplit_j[0] = diff.ToString();
                            RT r = new RT(diff, lineSplit_j);

                            Zoid thisZoid = MetaTypes.GetZoidType(lineSplit_j[(int)Header.curr_zoid]);
                            rts_raw_subject[(int)thisZoid, level].Add(r);
                            output.Add(r);
                            break;
                        }
                        i = j;
                        break;
                    }
                    // no action found for current zoid,
                    else if (MetaLog.ContainsEvent(lineSplit_j, "ZOID", "NEW"))
                    {
                        // jump to the that line-1, do nothing
                        i = j - 1;
                        break;
                    }
                }
            }
        }

        if (rtCount > 0)
        {
            if (!subj_rt_zoidlvl.ContainsKey(sid))
                subj_rt_zoidlvl.Add(sid, rts_raw_subject);
            string outFile = Path.GetFileName(inPath);
            string outDir = this.outDir + sid + @"\";
            Directory.CreateDirectory(outDir);
            WriteRTsToFile(outDir + outFile + "_RT", output);
        }
    }


    /// <summary>
    /// Writes the passed list structure to a file
    /// </summary>
    /// <param name="path">File to which the data has to be written to.</param>
    /// <param name="rtData">Data that has to be written out.</param>
    void WriteRTsToFile(string path, List<RT> rtData)
    {
        // convert collected values to string array
        string[] outlines = new string[rtData.Count];
        for (int i = 0; i < outlines.Length; i++)
        {
            outlines[i] = string.Join("\t", rtData[i].metaData);
        }
        // write the result to file
        File.WriteAllLines(path, outlines);
    }


    // ===================
    // CONSTRUCTOR HELPERS
    // ===================

    /// <summary>
    /// Wrapper method for NewLoa. Used to create a 2dimensional array of lists for the rt raw data format [zoidType,levelCount].
    /// </summary>
    /// <returns>Fully initialized 2-dimensional array of lists [zoidType,levelCount].</returns>
    List<RT>[,] NewLoA_Raw()
    {
        return NewLoA(Enum.GetNames(typeof(Zoid)).Length, MetaTypes.levels.Length);
    }


    /// <summary>
    /// Initializes a 2-dimensional array of lists, and initiates each node with an empty list.
    /// </summary>
    /// <param name="x_length">Size of the 1st dimension of the array to be created.</param>
    /// <param name="y_length">Size of the 2nd dimension of the array to be created.</param>
    /// <returns>Fully initialized 2-dimensional array of lists.</returns>
    List<RT>[,] NewLoA(int x_length, int y_length)
    {
        List<RT>[,] newStruct = new List<RT>[x_length, y_length];
        // initialize individual structures inside the big one 
        for (int i = 0; i < newStruct.GetLength(0); i++)
        {
            for (int j = 0; j < newStruct.GetLength(1); j++)
            {
                newStruct[i, j] = new List<RT>();
            }
        }
        return newStruct;
    }



    List<double>[,] NewLoA_double(int x_length, int y_length)
    {
        List<double>[,] newStruct = new List<double>[x_length, y_length];
        // initialize individual structures inside the big one 
        for (int i = 0; i < newStruct.GetLength(0); i++)
        {
            for (int j = 0; j < newStruct.GetLength(1); j++)
            {
                newStruct[i, j] = new List<double>();
            }
        }
        return newStruct;
    }



    List<string>[,] NewLoA_string(int x_length, int y_length)
    {
        List<string>[,] newStruct = new List<string>[x_length, y_length];
        // initialize individual structures inside the big one 
        for (int i = 0; i < newStruct.GetLength(0); i++)
        {
            for (int j = 0; j < newStruct.GetLength(1); j++)
            {
                newStruct[i, j] = new List<string>();
            }
        }
        return newStruct;
    }



    public static int[,,] NewRTTypeCounter()
    {
        int[,,] rt_playerActions = new int[MetaTypes.rotations.Length, MetaTypes.speedLevels.Length, Enum.GetValues(typeof(Action)).Length];
        for (int i = 0; i < rt_playerActions.GetLength(0); i++)
            for (int j = 0; j < rt_playerActions.GetLength(1); j++)
                for (int k = 0; k < rt_playerActions.GetLength(2); k++)
                    rt_playerActions[i, j, k] = 0;
        return rt_playerActions;
    }

}


