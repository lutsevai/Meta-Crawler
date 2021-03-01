using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;


public class MetaCrawlerScript : MonoBehaviour
{
    //public static string names = "IOTSZJL";
    enum Rotations { no, one, two };
    enum ZoidType { O, I, S, Z, J, L, T }
    // enum SpeedSteps { zero, one, two, three, four, five, six, seven, eight, nine, ten, thriteen, sixteen };
    //                0            1           2             3           4      5          6          7        8           9          10        11
    enum Header { timestamp, system_ticks, event_type, episode_number, level, score, lines_cleared, evt_id, evt_data1, evt_data2, curr_zoid, next_zoid };

    //constants
    const int maxLvl = 18;
    const string logExtension = ".tsv";
    const string inDir_test = @"D:\documents\rpi\cwl\meta-two\rt\";
    const string inDir_ctwc20 = @"F:\SIWIEL\CTWC20\SpeedTetris_version-2\";
    const string outDir_standard = @"D:\documents\rpi\cwl\meta-two\rt_out\";

    string inDir;
    string outDir;

    // array to sort all reaction times, format [zoidType,SpeedSteps]
    List<string[]>[,] commulitativeRTs;

    //structure for per-subject reaction times
    Dictionary<string, List<string[]>[,]> subjectRTs;


    // Start is called before the first frame update
    void Start()
    {
        //variable initialization
        commulitativeRTs = new List<string[]>[Enum.GetNames(typeof(ZoidType)).Length, maxLvl + 1];
        subjectRTs = new Dictionary<string, List<string[]>[,]>();


        // creates structures to put the sorted RTs in
        for (int i = 0; i < commulitativeRTs.GetLength(0); i++)
        {
            for (int j = 0; j < commulitativeRTs.GetLength(1); j++)
            {
                commulitativeRTs[i, j] = new List<string[]>();
            }
        }

        Debug.Log("big stru 1 " + commulitativeRTs.GetLength(0));
        Debug.Log("big stru 2 " + commulitativeRTs.GetLength(1));

        // setting directories
        inDir = inDir_test;
        outDir = outDir_standard;

        // raw data processing
        WalkDirectoryTree(new DirectoryInfo(inDir));

        // post processing

        // OUTPUT
        
        // write RT to separate files for each lvl, and zoid type
        for (int zoid = 0; zoid < commulitativeRTs.GetLength(0); zoid++)
        {
            for (int level = 0; level < commulitativeRTs.GetLength(1); level++)
            {
                string zoidName = Enum.GetValues(typeof(ZoidType)).GetValue(zoid).ToString();
                string fileName = String.Format("lvl{0}_{1}", level, zoidName);
                WriteRTsToFile(commulitativeRTs[zoid, level], outDir + fileName + logExtension);
            }
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
                                commulitativeRTs[(int)zoid, level].Add(lineSplit_j);
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

        WriteRTsToFile(output, outfile);
    }


    /// <summary>
    /// Writes the passed list structure to a file
    /// </summary>
    /// <param name="rtData">Data that has to be written out.</param>
    /// <param name="path">File to which the data has to be written to.</param>
    void WriteRTsToFile(List<string[]> rtData, string path)
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

}