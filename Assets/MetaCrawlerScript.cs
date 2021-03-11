using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;



public struct RT
{
    public RT(float newVal, string[] newMetadata)
    {
        val = newVal;
        metaData = newMetadata;
    }

    public double val { get; }
    public string[] metaData { get; }

    public override string ToString() => $"({val.ToString()}, {metaData.ToString()})";
}



public class MetaCrawlerScript : MonoBehaviour
{
    //public static string names = "IOTSZJL";
    enum Zoid { O, I, S, Z, J, L, T }
    enum SpeedLevelString { zero, one, two, three, four, five, six, seven, eight, nine, ten, thriteen, sixteen, nineteen, twentynine };

    //column nr:      0            1           2             3           4      5          6          7        8           9          10        11
    enum Header { timestamp, system_ticks, event_type, episode_number, level, score, lines_cleared, evt_id, evt_data1, evt_data2, curr_zoid, next_zoid };
    enum Action { Left, Right, Down, Rotate, Counterrotate };
    enum PlayStyle { das, hypertap }

    //constants & read-onlys
    const char sep = '\t';
    // total amount of possible levels in the datastructure
    const int levelCount = 30;
    const string logExtension = ".tsv";

    readonly int[] levels = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29 };
    readonly int[] speedLevels = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 13, 16, 19, 29 };
    readonly int[] rotations = { 0, 1, 2 };

    readonly int ZoidCount;
    readonly int ActionCount;
    readonly int HeaderCount;

    string outDir;

    // initial raw sort all reaction times, format [zoidType,level]
    List<RT>[,] rts_raw_all;

    //structure for per-subject reaction times
    Dictionary<string, List<RT>[,]> subjectRTs;

    Dictionary<string, int[]> playstyles;

    // log of bad data, that could not be used
    Dictionary<string, List<string>> badData;


    // Start is called before the first frame update
    void Start()
    {

    }

    /// <summary>
    /// Main method of the class - recursively searches a directroy for log files, and process them to extract various aspects of RTs into an output folder.
    /// </summary>
    /// <param name="newInDir">Path containing the data needed processing.</param>
    /// <param name="newOutDir">Path to which all the processed data will be written to.</param>
    public void Crawl(string newInDir, string newOutDir)
    {
        DirectoryCrawler dirCrawler = new DirectoryCrawler();

        // INITIALIZATION
        // ==============
        subjectRTs = new Dictionary<string, List<RT>[,]>();
        rts_raw_all = NewLoA_Raw();

        playstyles = new Dictionary<string, int[]>();
        badData = new Dictionary<string, List<string>>();

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

        // HYPERTAP / DAS CATEGORIZATION
        // =============================
        dirCrawler.WalkDirectoryTree(new DirectoryInfo(newInDir), isHyperTapper);

        List<string> outputLines = new List<string>();

        outputLines.Add(string.Format("SID{0}DAS{0}H-TAP{0}", sep));
        foreach (KeyValuePair<string, int[]> s in playstyles)
        {
            outputLines.Add(s.Key + sep + String.Join(sep.ToString(), s.Value));
        }
        File.WriteAllLines(outDir + "strats.tsv", outputLines.ToArray());

        // RT PROCESSING
        // =============
        // raw data feed in
        dirCrawler.WalkDirectoryTree(new DirectoryInfo(newInDir), ExtractRtData);

        //per-subject processing
        foreach (KeyValuePair<string, List<RT>[,]> kvp in subjectRTs)
        {
            processRT(outDir + kvp.Key + @"\", kvp.Value);
        }
        //summary of all
        processRT(outDir, rts_raw_all);

        // BAD DATA
        // ========
        outputLines.Clear();
        foreach (KeyValuePair<string, List<string>> s in badData)
        {
            outputLines.Add(s.Key + sep + s.Value.Count + sep + String.Join(sep.ToString(), s.Value));
        }
        File.WriteAllLines(outDir + "bad_data.tsv", outputLines.ToArray());
    }



    void processRT(string dirPath, List<RT>[,] rts_raw)
    {
        Directory.CreateDirectory(dirPath);

        // categorized reaction times by zoid [rotations, fall_speed] 
        List<RT>[,] rts_rotlvl_all = NewLoA(rotations.GetLength(0), speedLevels.Length);

        // condense data from [zoid, lvl] data structure into [rotationNr , speedRank]
        for (int zoidNr = 0; zoidNr < rts_raw.GetLength(0); zoidNr++)
        {
            int cRot = GetRotations(zoidNr);

            for (int lvl = 0; lvl < rts_raw.GetLength(1); lvl++)
            {
                // Determening the position of the level in speedArray
                int cSpeedRank = getSpeedRank(lvl);

                // Adding all values from the raw lists in the current loop to the corresponding node in the rotation/speedlvl structure.
                rts_rotlvl_all[cRot, cSpeedRank].AddRange(rts_raw[zoidNr, lvl]);
            }
        }

        // write RT to separate files for each lvl, and zoid type
        for (int zoid = 0; zoid < rts_raw.GetLength(0); zoid++)
        {
            for (int level = 0; level < rts_raw.GetLength(1); level++)
            {
                string zoidName = GetZoidType(zoid).ToString();
                string fileName = String.Format("lvl{0}_{1}", level, zoidName);
                WriteRTsToFile(dirPath + fileName + logExtension, rts_raw[zoid, level]);
            }
        }

        // write RT to separate files for each speedstep, and rotation type
        for (int rot = 0; rot < rts_rotlvl_all.GetLength(0); rot++)
        {
            for (int speedRank = 0; speedRank < rts_rotlvl_all.GetLength(1); speedRank++)
            {
                string fileName = String.Format("speed{0}_rot{1}", speedRank, rot);
                WriteRTsToFile(dirPath + fileName + logExtension, rts_rotlvl_all[rot, speedRank]);
            }
        }

        // create an output summary datastructure of RTs for all rot-types, all levels
        List<List<RT>> allRots_allSpeedRanks = new List<List<RT>>();

        //write a summary of all rts per speedstep to a single file, 3 columns
        for (int speedRank = 0; speedRank < rts_rotlvl_all.GetLength(1); speedRank++)
        {
            List<RT>[] listRots = new List<RT>[rotations.Length];
            for (int i = 0; i < listRots.Length; i++)
            {
                listRots[i] = new List<RT>();
            }

            //extract the first column of the log containing the timestamp
            for (int rot = 0; rot < rts_rotlvl_all.GetLength(0); rot++)
            {
                foreach (RT lineSlit in rts_rotlvl_all[rot, speedRank])
                {
                    listRots[rot].Add(lineSlit);
                }
            }

            // add all extracted RTs to the general rots list
            foreach (List<RT> list in listRots)
            {
                allRots_allSpeedRanks.Add(list);
            }

            List<string> mergedRots = merge(listRots, sep);

            string fileName = String.Format("speedRank{0}_allRot", speedRank);
            File.WriteAllLines(dirPath + fileName + logExtension, mergedRots.ToArray());
        }

        //Writing a summary file with all rts, categorized by rotation, and levels
        List<string> allRots_allRanksMerged = merge(allRots_allSpeedRanks.ToArray(), sep);
        File.WriteAllLines(dirPath + "allRTs_allSpeedRanks" + logExtension, allRots_allRanksMerged.ToArray());


        // --------------
        // RT Action Type
        // --------------

        //initialize basic structure
        int[,,] rt_playerActions = new int[rotations.Length, speedLevels.Length, Enum.GetValues(typeof(Action)).Length];
        for (int i = 0; i < rt_playerActions.GetLength(0); i++)
            for (int j = 0; j < rt_playerActions.GetLength(1); j++)
                for (int k = 0; k < rt_playerActions.GetLength(2); k++)
                    rt_playerActions[i, j, k] = 0;


        // count each type of rt separateley
        for (int speedRank = 0; speedRank < rts_rotlvl_all.GetLength(1); speedRank++)
        {
            //extract the column of the log containing the player action
            for (int rot = 0; rot < rts_rotlvl_all.GetLength(0); rot++)
            {
                foreach (RT lineSlit in rts_rotlvl_all[rot, speedRank])
                {
                    if (!lineSlit.metaData[(int)Header.evt_data2].Equals("Pause"))
                    {
                        Action p = getAction(lineSlit.metaData[(int)Header.evt_data2]);
                        rt_playerActions[rot, speedRank, (int)p] += 1;
                    }
                }
            }
        }

        // RT Action type output
        //initialize output structure of lines to be written out. +1 Length for header
        string[] actionLines = new string[Enum.GetValues(typeof(Action)).Length + 1];

        // make header
        string rotLines_header = "" + sep;
        foreach (int i in speedLevels)
        {
            foreach (int r in rotations)
            {
                rotLines_header += r.ToString() + sep;
            }
        }
        actionLines[0] = rotLines_header;

        //fill output structure with data from rt_playerActions
        foreach (Action act in Enum.GetValues(typeof(Action)))
        {
            string line = act.ToString() + sep;
            for (int speedRank = 0; speedRank < speedLevels.Length; speedRank++)
            {
                for (int rot = 0; rot < rotations.Length; rot++)
                {
                    line += rt_playerActions[rot, speedRank, (int)act].ToString() + sep;
                }
            }
            actionLines[(int)act + 1] = line;
        }
        File.WriteAllLines(dirPath + "rt_actions" + logExtension, actionLines);
    }



    void ExtractRtData(string inPath)
    {
        //todo: make check for good data here, extract this logic into method: bool isGoodData(string inPath, out string[] lines)
        List<RT> output = new List<RT>();
        string[] lines;

        if (!hasGoodData(inPath, out lines))
            return;

        string sid = getSid(lines);

        List<RT>[,] rts_raw_subject;
        int startIndex = GetGameStart(lines);

        // initialize / find subject - specific structure
        if (subjectRTs.ContainsKey(sid))
        {
            subjectRTs.TryGetValue(sid, out rts_raw_subject);
        }
        else
        {
            rts_raw_subject = NewLoA_Raw();
            subjectRTs.Add(sid, rts_raw_subject);
        }

        //search for new zoid events + rt
        for (int i = startIndex; i < lines.Length; i++)
        {
            string[] lineSplit = lines[i].Split('\t');
            if (containsEvent(lineSplit, "ZOID", "NEW"))
            {
                // search for the first action after zoid appearance
                for (int j = i + 1; j < lines.Length; j++)
                {
                    string[] lineSplit_j = lines[j].Split('\t');

                    // found key down
                    // todo: what about key up?!
                    if (containsEvent(lineSplit_j, "PLAYER", "KEY_DOWN"))
                    {
                        // add it, if both times are not different
                        // todo: check frequency of presses, perhaps they coincide, because it is anticipated
                        if (!lineSplit_j[(int)Header.timestamp].Equals(lineSplit[0]))
                        {
                            float diff = float.Parse(lineSplit_j[(int)Header.timestamp]) - float.Parse(lineSplit[0]);
 
                            //TODO: careful, as sometimes the string from the metadata is being used. Make universal
                            lineSplit_j[0] = diff.ToString();
                            RT r = new RT(diff, lineSplit_j);

                            Zoid thisZoid = GetZoidType(lineSplit_j[(int)Header.curr_zoid]);
                            int level = int.Parse(lineSplit_j[(int)Header.level]);
                            rts_raw_all[(int)thisZoid, level].Add(r);
                            rts_raw_subject[(int)thisZoid, level].Add(r);
                            output.Add(r);
                            break;
                        }
                        i = j;
                        break;
                    }
                    // no action found for current zoid,
                    else if (containsEvent(lineSplit_j, "ZOID", "NEW"))
                    {
                        // jump to the that line-1, do nothing
                        i = j - 1;
                        break;
                    }
                }
            }
        }
        string outFile = Path.GetFileName(inPath);
        string outDir = this.outDir + sid + @"\";
        Directory.CreateDirectory(outDir);
        WriteRTsToFile(outDir + outFile, output);
    }


    /// <summary>
    /// Finds the subject ID in passed game log data.
    /// </summary>
    /// <param name="lines">Log data where the subject ID has to be extracted from.</param>
    /// <returns>string of subject ID found in the data. If no SID was found, returns "NOSID".</returns>
    string getSid(string[] lines)
    {
        //get SID
        foreach (string line in lines)
        {
            string[] lSplit = split(line);

            if (containsEvent(lSplit, "SID"))
                return lSplit[(int)Header.evt_data1];
            else if (containsEvent(lSplit, "GAME", "BEGIN"))
                break;
        }
        return "NOSID";
    }


    /// <summary>
    /// Finds the array cell in the passed log data where the game begins.
    /// </summary>
    /// <param name="lines">Game log data.</param>
    /// <returns>Array index in the passed data, where the game starts. If no game start has been found, returns -1.</returns>
    private int GetGameStart(string[] lines)
    {
        bool found = false;
        int startIndex = 0;

        foreach (string line in lines)
        {
            string[] lSplit = split(line);

            if (containsEvent(lSplit, "GAME", "BEGIN"))
            {
                found = true;
                break;
            }
            startIndex++;
        }

        if (!found)
            startIndex = -1;

        return startIndex;
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


    /// <summary>
    /// Merges all elements at the ith position of each list in the passed array into single string lines, separated by a separator, adds them to a list, and returns them
    /// </summary>
    /// <param name="listarray">Main data structure, whose elements have to be merged.</param>
    /// <param name="sep">A separator which will be put between each merging element.</param>
    /// <returns>List of the merged string elements.</returns>
    List<string> merge(List<string>[] listarray, char sep)
    {
        List<string> result = new List<string>();

        //determening the longest list
        int max = 0;
        foreach (List<string> list in listarray)
        {
            if (list.Count > max)
            {
                max = list.Count;
            }
        }

        //merging the elements of the list
        for (int i = 0; i < max; i++)
        {
            string line = "";
            foreach (List<string> list in listarray)
            {
                line += getElem(list, i) + sep;
            }
            //trims out the last sep-char, and adds to the results
            result.Add(line.Remove(line.Length - 1));
        }

        return result;
    }


    List<string> merge(List<RT>[] listarray, char sep)
    {
        List<string> result = new List<string>();

        //determening the longest list
        int max = 0;
        foreach (List<RT> list in listarray)
        {
            if (list.Count > max)
            {
                max = list.Count;
            }
        }

        //merging the elements of the list
        for (int i = 0; i < max; i++)
        {
            string line = "";
            foreach (List<RT> list in listarray)
            {
                line += getElem(list, i) + sep;
            }
            //trims out the last sep-char, and adds to the results
            result.Add(line.Remove(line.Length - 1));
        }

        return result;
    }






    /// <summary>
    /// Fetches an element from a string list by its ordianl position in the list, or returns an empty string, if the position exceeds the size of the list.
    /// </summary>
    /// <param name="list">String list, from which the string is to be fetched.</param>
    /// <param name="i">Position of the string in the list.</param>
    /// <returns>String at the ith position, or an empty string, if the position exceeds the size of the list.</returns>
    string getElem(List<string> list, int i)
    {
        if (i < list.Count)
        {
            return list[i];
        }
        else
        {
            return "";
        }
    }


    string getElem(List<RT> list, int i)
    {
        if (i < list.Count)
        {
            return list[i].metaData[(int)Header.timestamp];
        }
        else
        {
            return "";
        }
    }




    /// <summary>
    /// Checks whether a line from the log contains a certain event.
    /// </summary>
    /// <param name="lineSplit">The line to be checked for the event string.</param>
    /// <param name="event_id">First portion of the event string.</param>
    /// <param name="event_data1">Second part of the event string.</param>
    /// <returns>True if the line contains the event string.</returns>
    bool containsEvent(string[] lineSplit, string event_id, string event_data1)
    {
        if (lineSplit[(int)Header.evt_id].Equals(event_id) && lineSplit[(int)Header.evt_data1].Equals(event_data1))
        {
            return true;
        }
        else
        {
            return false;
        }
    }


    /// <summary>
    /// Checks whether a line from the log contains a certain event string.
    /// </summary>
    /// <param name="lineSplit">The line to be checked for the event.</param>
    /// <param name="event_id">Event string.</param>
    /// <returns>True if the line contains the event string.</returns>
    bool containsEvent(string[] lineSplit, string event_id)
    {
        if (lineSplit[(int)Header.evt_id].Equals(event_id))
        {
            return true;
        }
        else
        {
            return false;
        }
    }


    /// <summary>
    /// Splits a tab separated line into an array
    /// </summary>
    string[] split(string line)
    {
        return line.Split('\t');
    }


    /// <summary>
    /// Matches an input string to the corresponding ZoidType, if present
    /// </summary>
    /// <param name="z">zoid name</param>
    /// <returns>zoid type matching the input</returns>
    Zoid GetZoidType(string z)
    {
        foreach (Zoid zoid in Enum.GetValues(typeof(Zoid)))
        {
            if (z.Equals(zoid.ToString()))
            {
                return zoid;
            }
        }
        throw new InvalidDataException("Invalid Zoid Type input provided: " + z);
    }


    /// <summary>
    /// Matches an input ordinal position to the corresponding ZoidType, if present
    /// </summary>
    /// <param name="z">zoid position in the enum list</param>
    /// <returns>zoid type matching the input</returns>
    Zoid GetZoidType(int z)
    {
        return (Zoid)Enum.GetValues(typeof(Zoid)).GetValue(z);
    }


    /// <summary>
    /// Returns the amount of rotations that can be performed on a zoid
    /// </summary>
    /// <param name="z">Zoid ordinal number to be analyzed for rotations</param>
    /// <returns></returns>
    int GetRotations(int z)
    {
        Zoid zoid = GetZoidType(z);
        return GetRotations(zoid);
    }


    /// <summary>
    /// Returns the amount of rotations that can be performed on a zoid
    /// </summary>
    /// <param name="z">Zoid to be analyzed for rotations</param>
    /// <returns></returns>
    int GetRotations(string z)
    {
        return GetRotations(GetZoidType(z));
    }


    /// <summary>
    /// Returns the amount of rotations that can be performed on a zoid
    /// </summary>
    /// <param name="zoid">Zoid to be analyzed for rotations</param>
    /// <returns></returns>
    int GetRotations(Zoid zoid)
    {
        if (zoid.Equals(Zoid.O))
        {
            return 0;
        }
        else if (zoid.Equals(Zoid.I) || zoid.Equals(Zoid.S) || zoid.Equals(Zoid.Z))
        {
            return 1;
        }
        else if (zoid.Equals(Zoid.J) || zoid.Equals(Zoid.L) || zoid.Equals(Zoid.T))
        {
            return 2;
        }
        else
        {
            throw new InvalidDataException("Invalid Zoid Type provided for rotations: " + zoid.ToString());
        }
    }


    /// <summary>
    /// Determines in which speed category the current lvl is in, and returns it
    /// </summary>
    /// <param name="lvl"> current Tetris level</param>
    /// <returns>The speed category of the passed level.</returns>
    int getSpeedRank(int lvl)
    {
        int cSpeed = 0;
        while ((cSpeed < (speedLevels.Length - 1)) && (speedLevels[cSpeed + 1] < (lvl + 1)))
        {
            cSpeed++;
        }

        return cSpeed;
    }


    /// <summary>
    /// Wrapper method for NewLoa. Used to create a 2dimensional array of lists for the rt raw data format [zoidType,levelCount].
    /// </summary>
    /// <returns>Fully initialized 2-dimensional array of lists [zoidType,levelCount].</returns>
    List<RT>[,] NewLoA_Raw()
    {
        return NewLoA(Enum.GetNames(typeof(Zoid)).Length, levelCount);
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



    Action getAction(string actionString)
    {
        foreach (Action a in Enum.GetValues(typeof(Action)))
        {
            if (a.ToString().Equals(actionString))
            {
                return a;
            }
        }
        throw new InvalidDataException("Invalid Player Action provided for rotations: " + actionString);
    }

    /// <summary>
    /// Categorizes a data from file into play strategies: hypertapping, and das.
    /// </summary>
    /// <param name="infile">Path to the file that is to be analyzed.</param>
    void isHyperTapper(string infile)
    {
        const int htap_threshhold = 6;

        string[] lines;
        if (!hasGoodData(infile, out lines))
            return;

        string sid = getSid(lines);
        int htaps = 0;

        for (int i = 0; i < lines.Length; i++)
        {
            //found enough evidence, halt loop
            if (htaps > htap_threshhold)
                break;

            string[] lineSplit = lines[i].Split('\t');

            if (containsEvent(lineSplit, "ZOID", "NEW"))
            {
                int tapLeft = 0;
                int tapRight = 0;
                Action lastTap = Action.Down;

                // search for the first action after zoid appearance
                for (int j = i + 1; j < lines.Length; j++)
                {
                    string[] lineSplit_j = lines[j].Split('\t');

                    // found key down
                    // todo: what about key up?!
                    if (containsEvent(lineSplit_j, "PLAYER", "KEY_DOWN"))
                    {
                        //todo: implement pause
                        if (lineSplit_j[(int)Header.evt_data2].Equals("Pause"))
                            continue;

                        Action a = getAction(lineSplit_j[(int)Header.evt_data2]);

                        if ((!a.Equals(Action.Left)) && (!a.Equals(Action.Right)))
                            continue;

                        if (!a.Equals(lastTap))
                        {
                            tapLeft = 0;
                            tapRight = 0;
                        }
                        lastTap = a;

                        if (a.Equals(Action.Left))
                            tapLeft++;
                        else if (a.Equals(Action.Right))
                            tapRight++;
                    }
                    // no action found for current zoid,
                    else if (containsEvent(lineSplit_j, "ZOID", "NEW"))
                    {
                        // jump to the that line-1, do nothing
                        lastTap = Action.Down;
                        i = j - 1;
                        break;
                    }
                }

                if ((tapLeft > 4) || (tapRight > 4))
                {
                    htaps++;
                }
            }
        }

        int[] s;
        if (!playstyles.TryGetValue(sid, out s))
        {
            s = new int[Enum.GetValues(typeof(PlayStyle)).Length];
            playstyles.Add(sid, s);
        }

        if (htaps > htap_threshhold)
            s[(int)PlayStyle.hypertap]++;
        else
            s[(int)PlayStyle.das]++;
    }


    /// <summary>
    /// Checks if a game log file is worth being analyzed.
    /// </summary>
    /// <param name="infile">Path to the file to be checked.</param>
    /// <param name="lines">Out array containing the data, if it turns out good. Otherwise, returns an empty array.</param>
    /// <returns>True if data worth being analyzed, and false, if not.</returns>
    bool hasGoodData(string infile, out string[] lines)
    {    
        if ((!Path.GetExtension(infile).Equals(logExtension)) || (Path.GetFileNameWithoutExtension(infile).Contains("tobii-sync")))
        {
            lines = new string[0];
            return false;
        }

        lines = File.ReadAllLines(infile);

        //initial check, in case not even header is fully present
        if (lines.Length < 50)
            return false;

        string sid = getSid(lines);
        if (sid.ToLower().Contains("test"))
            return false;

        int start = GetGameStart(lines);

        string[] split = lines[start + 1].Split('\t');
        int startlvl = int.Parse(split[(int)Header.level]);

        int minLength = 2200;

        // approximately 15 episodes
        //todo: catch bad lvl
        switch (startlvl)
        {
            case 0:
                minLength = 2200;
                break;
            case 7:
                minLength = 2100;
                break;
        }

        if (lines.Length < minLength)
        {
            List<string> badFiles;
            if (!(badData.TryGetValue(sid, out badFiles)))
            {
                badFiles = new List<string>();
                badData.Add(sid, badFiles);
            }
            badFiles.Add(infile);

            return false;
        }
        return true;
    }
}


