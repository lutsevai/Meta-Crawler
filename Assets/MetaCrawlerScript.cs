using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;


public class MetaCrawlerScript : MonoBehaviour
{
    //public static string names = "IOTSZJL";
    enum ZoidType { O, I, S, Z, J, L, T }
    enum SpeedLevelString { zero, one, two, three, four, five, six, seven, eight, nine, ten, thriteen, sixteen, nineteen, twentynine };

    //column nr:      0            1           2             3           4      5          6          7        8           9          10        11
    enum Header { timestamp, system_ticks, event_type, episode_number, level, score, lines_cleared, evt_id, evt_data1, evt_data2, curr_zoid, next_zoid };

    //constants & read-onlys

    // total amount of possible levels in the datastructure
    const char sep = '\t';
    const int levelCount = 19;
    const string logExtension = ".tsv";
    const string inDir_test = @"D:\documents\rpi\cwl\meta-two\rt\";
    const string inDir_ctwc20 = @"F:\SIWIEL\CTWC20\SpeedTetris_version-2\";
    const string outDir_standard = @"D:\documents\rpi\cwl\meta-two\rt_out\";

    // readonly int[] levels = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29 }
    readonly int[] speedLevels = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 13, 16, 19, 29 };
    readonly int[] rotations = { 0, 1, 2 };

    string inDir;
    string outDir;

    // initial raw sort all reaction times, format [zoidType,level]
    List<string[]>[,] rtList_raw;

    // categorized reaction times by zoid [rotations, fall_speed] 
    List<string[]>[,] rtList_categorized;


    // Start is called before the first frame update
    void Start()
    {
        // VARIABLE INITIALIZATION

        rtList_raw = new List<string[]>[Enum.GetNames(typeof(ZoidType)).Length, levelCount];
        // initialize individual structures inside the big one 
        for (int i = 0; i < rtList_raw.GetLength(0); i++)
        {
            for (int j = 0; j < rtList_raw.GetLength(1); j++)
            {
                rtList_raw[i, j] = new List<string[]>();
            }
        }

        rtList_categorized = new List<string[]>[rotations.GetLength(0), speedLevels.GetLength(0)];
        // initialize individual structures inside the big one
        for (int i = 0; i < rtList_categorized.GetLength(0); i++)
        {
            for (int j = 0; j < rtList_categorized.GetLength(1); j++)
            {
                rtList_categorized[i, j] = new List<string[]>();
            }
        }

        subjectRTs = new Dictionary<string, List<string[]>[,]>();


        // SETTING DIRS
        inDir = inDir_test;
        outDir = outDir_standard;

        // RAW DATA PROCESSING
        WalkDirectoryTree(new DirectoryInfo(inDir));

        // POST PROCESSING

        // sort data by [rotationNr , speedRank]
        for (int zoidNr = 0; zoidNr < rtList_raw.GetLength(0); zoidNr++)
        {
            int cRot = GetRotations(zoidNr);

            for (int lvl = 0; lvl < rtList_raw.GetLength(1); lvl++)
            {
                // Determening the position of the level in speedArray
                int cSpeedRank = getSpeedRank(lvl);

                // Adding all values from the raw lists in the current loop to the corresponding node in the rotation/speedlvl structure.
                rtList_categorized[cRot, cSpeedRank].AddRange(rtList_raw[zoidNr, lvl]);
            }

        }

        // OUTPUT

        // clear output folder
        System.IO.DirectoryInfo di = new DirectoryInfo(outDir);
        foreach (FileInfo file in di.GetFiles())
        {
            file.Delete();
        }
        foreach (DirectoryInfo dir in di.GetDirectories())
        {
            dir.Delete(true);
        }

        // write RT to separate files for each lvl, and zoid type
        for (int zoid = 0; zoid < rtList_raw.GetLength(0); zoid++)
        {
            for (int level = 0; level < rtList_raw.GetLength(1); level++)
            {
                string zoidName = GetZoidType(zoid).ToString();
                string fileName = String.Format("lvl{0}_{1}", level, zoidName);
                WriteRTsToFile(outDir + fileName + logExtension, rtList_raw[zoid, level]);
            }
        }

        // write RT to separate files for each speedstep, and rotation type
        for (int rot = 0; rot < rtList_categorized.GetLength(0); rot++)
        {
            for (int speedRank = 0; speedRank < rtList_categorized.GetLength(1); speedRank++)
            {
                string fileName = String.Format("speed{0}_rot{1}", speedRank, rot);
                WriteRTsToFile(outDir + fileName + logExtension, rtList_categorized[rot, speedRank]);
            }
        }



        // create summary datastructure of RTs for all rot-types, all levels
        List<List<string>> allRots_allSpeedRanks = new List<List<string>>();


        //write a summary of all rts per speedstep to a single file, 3 columns
        for (int speedRank = 0; speedRank < rtList_categorized.GetLength(1); speedRank++)
        {
            List<string>[] listRots_speedRank = new List<string>[3];
            for (int i = 0; i < listRots_speedRank.Length; i++)
            {
                listRots_speedRank[i] = new List<string>();
            }

            for (int rot = 0; rot < rtList_categorized.GetLength(0); rot++)
            {
                foreach (string[] lineSlit in rtList_categorized[rot, speedRank])
                {
                    listRots_speedRank[rot].Add(lineSlit[0]);
                }
            }

            foreach(List<string> list in listRots_speedRank)
            {
                allRots_allSpeedRanks.Add(list);
            }

            List<string> rotLines = merge(listRots_speedRank, sep);

            string fileName = String.Format("speedRank{0}_allRot", speedRank);
            File.WriteAllLines(outDir + fileName + logExtension, rotLines.ToArray());
        }

        //Writing a summary file with all rts, categorized by rotation, and levels
        List<string> allRots_allRanksMerged = merge(allRots_allSpeedRanks.ToArray(), sep);
        File.WriteAllLines(outDir + "allRTs_allSpeedRanks" + logExtension, allRots_allRanksMerged.ToArray());
    }


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



    void WalkDirectoryTree(System.IO.DirectoryInfo root)
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

                    ExtractRtData(fi.FullName, outDir + Path.GetFileName(fi.FullName));
                }

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
                WalkDirectoryTree(dirInfo);
            }
        }
    }



    void ExtractRtData(string infile, string outfile)
    {

        List<string[]> output = new List<string[]>();
        string[] lines = File.ReadAllLines(infile);

        //adding header
        string[] header = split(lines[0]);
        header[0] = "RT";
        output.Add(header);

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

                    // found key down, add it
                    //todo: what about key up?!
                    if (containsEvent(lineSplit_j, "PLAYER", "KEY_DOWN"))
                    {
                        float diff = float.Parse(lineSplit_j[(int)Header.timestamp]) - float.Parse(lineSplit[0]);

                        lineSplit_j[0] = Math.Round((Decimal)diff, 5, MidpointRounding.AwayFromZero).ToString();

                        foreach (ZoidType zoid in Enum.GetValues(typeof(ZoidType)))
                        {
                            if (lineSplit_j[(int)Header.curr_zoid].Equals(zoid.ToString()))
                            {
                                int level = int.Parse(lineSplit_j[(int)Header.level]);
                                rtList_raw[(int)zoid, level].Add(lineSplit_j);
                                Debug.Log(string.Format("adding lvl {0} , zoid {1}", lineSplit_j[(int)Header.level], zoid.ToString()));
                                break;
                            }
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

        WriteRTsToFile(outfile, output);
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


    ZoidType GetZoidType(string z)
    {
        foreach (ZoidType zoid in Enum.GetValues(typeof(ZoidType)))
        {
            if (z.Equals(zoid.ToString()))
            {
                return zoid;
            }
        }

        throw new InvalidDataException("Invalid Zoid Type input provided: " + z);
    }



    ZoidType GetZoidType(int z)
    {
        return (ZoidType)Enum.GetValues(typeof(ZoidType)).GetValue(z);
    }


    /// <summary>
    /// Returns the amount of rotations that can be performed on a zoid
    /// </summary>
    /// <param name="z">Zoid ordinal number to be analyzed for rotations</param>
    /// <returns></returns>
    int GetRotations(int z)
    {
        ZoidType zoid = GetZoidType(z);
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
    int GetRotations(ZoidType zoid)
    {
        if (zoid.Equals(ZoidType.O))
        {
            return 0;
        }
        else if (zoid.Equals(ZoidType.I) || zoid.Equals(ZoidType.S) || zoid.Equals(ZoidType.Z))
        {
            return 1;
        }
        else if (zoid.Equals(ZoidType.J) || zoid.Equals(ZoidType.L) || zoid.Equals(ZoidType.T))
        {
            return 2;
        }
        else
        {
            throw new InvalidDataException("Invalid Zoid Type provided for rotations: " + zoid.ToString());
        }
    }



    int getSpeedRank(int lvl)
    {
        int cSpeed = 0;
        while ((speedLevels[cSpeed] < lvl) && (cSpeed < speedLevels.Length))
        {
            cSpeed++;
        }

        return cSpeed;
    }
}