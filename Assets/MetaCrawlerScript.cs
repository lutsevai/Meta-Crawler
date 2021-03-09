using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;




public class MetaCrawlerScript : MonoBehaviour
{

    delegate void fileTask(string inPath, string outPath);

    //public static string names = "IOTSZJL";
    enum Zoid { O, I, S, Z, J, L, T }
    enum SpeedLevelString { zero, one, two, three, four, five, six, seven, eight, nine, ten, thriteen, sixteen, nineteen, twentynine };

    //column nr:      0            1           2             3           4      5          6          7        8           9          10        11
    enum Header { timestamp, system_ticks, event_type, episode_number, level, score, lines_cleared, evt_id, evt_data1, evt_data2, curr_zoid, next_zoid };

    enum Action { Left, Right, Down, Rotate, Counterrotate };

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
    List<string[]>[,] rts_raw_all;


    List<string[]>[,] rts_raw_hypertap;
    List<string[]>[,] rts_raw_das;

    Dictionary<string, int[]> strats;


    //structure for per-subject reaction times
    Dictionary<string, List<string[]>[,]> subjectRTs;



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
        // VARIABLE INITIALIZATION
        subjectRTs = new Dictionary<string, List<string[]>[,]>();
        rts_raw_all = NewLoA_Raw();

        rts_raw_hypertap = NewLoA_Raw();
        rts_raw_das = NewLoA_Raw();
        strats = new Dictionary<string, int[]>();


        // SETTING DIRS
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
        WalkDirectoryTree(new DirectoryInfo(newInDir), isHyperTapper);
        
        List<string> vallls = new List<string>();
        vallls.Add(string.Format("SID{0}DAS{0}H-TAP{0}", sep));
        foreach (KeyValuePair<string, int[]> s in strats)
        {
            vallls.Add(s.Key + sep + String.Join(sep.ToString(), s.Value));

        }
        File.WriteAllLines(outDir + "strats.tsv", vallls.ToArray());



        // RAW DATA PROCESSING
        WalkDirectoryTree(new DirectoryInfo(newInDir), ExtractRtData);

        // DATA POSTPROCESSING & OUTPUT
        //per-subject
        foreach (KeyValuePair<string, List<string[]>[,]> kvp in subjectRTs)
        {
            processRT(outDir + kvp.Key + @"\", kvp.Value);
        }
        //summary of all
        processRT(outDir, rts_raw_all);
    }



    void processRT(string dirPath, List<string[]>[,] rts_raw)
    {
        Directory.CreateDirectory(dirPath);

        // categorized reaction times by zoid [rotations, fall_speed] 
        List<string[]>[,] rts_rotlvl_all = NewLoA(rotations.GetLength(0), speedLevels.Length);

        int[,] jakag = new int[Enum.GetValues(typeof(Action)).Length, speedLevels.Length];


        //condense data from [zoid, lvl] data structure into [rotationNr , speedRank]
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

        // create summary datastructure of RTs for all rot-types, all levels
        List<List<string>> allRots_allSpeedRanks = new List<List<string>>();

        //write a summary of all rts per speedstep to a single file, 3 columns
        for (int speedRank = 0; speedRank < rts_rotlvl_all.GetLength(1); speedRank++)
        {
            List<string>[] listRots = new List<string>[rotations.Length];
            for (int i = 0; i < listRots.Length; i++)
            {
                listRots[i] = new List<string>();
            }

            //extract the first column of the log containing the timestamp
            for (int rot = 0; rot < rts_rotlvl_all.GetLength(0); rot++)
            {
                foreach (string[] lineSlit in rts_rotlvl_all[rot, speedRank])
                {
                    listRots[rot].Add(lineSlit[(int)Header.timestamp]);
                }
            }

            // add all extracted RTs to the general rots list
            foreach (List<string> list in listRots)
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





        // --------------------
        // RT Action Type
        // ---------------------

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
                foreach (string[] lineSlit in rts_rotlvl_all[rot, speedRank])
                {
                    if (!lineSlit[(int)Header.evt_data2].Equals("Pause"))
                    {
                        Action p = getAction(lineSlit[(int)Header.evt_data2]);
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



    void WalkDirectoryTree(System.IO.DirectoryInfo root, fileTask processFile)
    {
        System.IO.FileInfo[] files = null;
        System.IO.DirectoryInfo[] subDirs = null;

        // First, process all the files directly under this folder
        try
        {
            files = root.GetFiles("*.*");
        }
        // This is thrown if even one of the files requires permissions greater
        // than the application provides.
        catch (UnauthorizedAccessException e)
        {
            // This code just writes out the message and continues to recurse.
            // You may decide to do something different here. For example, you
            // can try to elevate your privileges and access the file again.
            Debug.Log(e.Message);
        }

        catch (System.IO.DirectoryNotFoundException e)
        {
            Debug.Log(e.Message);
        }

        if (files != null)
        {
            foreach (System.IO.FileInfo fi in files)
            {
                if (fi.Extension.Equals(logExtension))
                {
                    if (!fi.FullName.Contains("tobii-sync"))
                        processFile(fi.FullName, Path.GetFileName(fi.FullName));
                }

                //TODO:
                // In this example, we only access the existing FileInfo object. If we
                // want to open, delete or modify the file, then
                // a try-catch block is required here to handle the case
                // where the file has been deleted since the call to TraverseTree().
                //Debug.Log(fi.FullName);
            }

            // Now find all the subdirectories under this directory.
            subDirs = root.GetDirectories();

            foreach (System.IO.DirectoryInfo dirInfo in subDirs)
            {
                // Resursive call for each subdirectory.
                WalkDirectoryTree(dirInfo, processFile);
            }
        }
    }






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



    void ExtractRtData(string infile, string outfile)
    {
        List<string[]> output = new List<string[]>();
        string[] lines = File.ReadAllLines(infile);

        //adding header
        string[] header = split(lines[0]);
        header[0] = "RT";
        output.Add(header);

        string sid = getSid(lines);
        List<string[]>[,] rts_raw_subject;


        //skip meta-data
        int startIndex = 0;
        foreach (string line in lines)
        {
            string[] lSplit = split(line);

            if (containsEvent(lSplit, "GAME", "BEGIN"))
            {
                break;
            }
            startIndex++;
        }

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
                            lineSplit_j[0] = Math.Round((Decimal)diff, 5, MidpointRounding.AwayFromZero).ToString();

                            Zoid thisZoid = GetZoidType(lineSplit_j[(int)Header.curr_zoid]);
                            int level = int.Parse(lineSplit_j[(int)Header.level]);
                            rts_raw_all[(int)thisZoid, level].Add(lineSplit_j);
                            rts_raw_subject[(int)thisZoid, level].Add(lineSplit_j);
                            output.Add(lineSplit_j);
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

        string outfolder = outDir + sid + @"\";
        Directory.CreateDirectory(outfolder);
        WriteRTsToFile(outfolder + outfile, output);
    }


    /// <summary>
    /// Writes the passed list structure to a file
    /// </summary>
    /// <param name="path">File to which the data has to be written to.</param>
    /// <param name="rtData">Data that has to be written out.</param>
    void WriteRTsToFile(string path, List<string[]> rtData)
    {
        // convert collected values to string array
        string[] outlines = new string[rtData.Count];
        for (int i = 0; i < outlines.Length; i++)
        {
            outlines[i] = string.Join("\t", rtData[i]);
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
    List<string[]>[,] NewLoA_Raw()
    {
        return NewLoA(Enum.GetNames(typeof(Zoid)).Length, levelCount);
    }


    /// <summary>
    /// Initializes a 2-dimensional array of lists, and initiates each node with an empty list.
    /// </summary>
    /// <param name="x_length">Size of the 1st dimension of the array to be created.</param>
    /// <param name="y_length">Size of the 2nd dimension of the array to be created.</param>
    /// <returns>Fully initialized 2-dimensional array of lists.</returns>
    List<string[]>[,] NewLoA(int x_length, int y_length)
    {
        List<string[]>[,] newStruct = new List<string[]>[x_length, y_length];
        // initialize individual structures inside the big one 
        for (int i = 0; i < newStruct.GetLength(0); i++)
        {
            for (int j = 0; j < newStruct.GetLength(1); j++)
            {
                newStruct[i, j] = new List<string[]>();
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


    void isHyperTapper(string infile, string outfile)
    {
        string[] lines = File.ReadAllLines(infile);
        string sid = getSid(lines);
        int htaps = 0;

        for (int i = 0; i < lines.Length; i++)
        {
            //found enough evidence, halt loop
            if (htaps > 4)
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

                if ((tapLeft > 5) || (tapRight > 5))
                {
                    htaps++;
                }
            }
        }

        int[] s;
        if (!strats.TryGetValue(sid, out s))
        {
            s = new int[2];
            strats.Add(sid, s);
        }

        if (htaps > 4)
            s[1]++;
        else
            s[0]++;
    }

}


