using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;


public class MetaCrawlerScript : MonoBehaviour
{

    string logExtension = ".tsv";
    static System.Collections.Specialized.StringCollection log = new System.Collections.Specialized.StringCollection();
    string testDir = @"D:\documents\rpi\cwl\meta-two\rt\";
    string ctwc20Dir = @"F:\SIWIEL\CTWC20\SpeedTetris_version-2\";
    string outDir = @"D:\documents\rpi\cwl\meta-two\rt_out\";

    string inDir;

    List<string[]> rt_16_18 = new List<string[]>();
    List<string[]> rt_16_18_noRot = new List<string[]>();
    List<string[]> rt_16_18_oneRot = new List<string[]>();
    List<string[]> rt_16_18_twoRot = new List<string[]>();

    List<string[]> rt_13_15 = new List<string[]>();
    List<string[]> rt_13_15_noRot = new List<string[]>();
    List<string[]> rt_13_15_oneRot = new List<string[]>();
    List<string[]> rt_13_15_twoRot = new List<string[]>();



    // Start is called before the first frame update
    void Start()
    {
        inDir = testDir;
        WalkDirectoryTree(new DirectoryInfo(inDir));
        WriteRTs(rt_16_18, outDir + "rt_16_18.tsv");
        WriteRTs(rt_16_18_noRot, outDir + "rt_16_18_noRot.tsv");
        WriteRTs(rt_16_18_oneRot, outDir + "rt_16_18_oneRot.tsv");
        WriteRTs(rt_16_18_twoRot, outDir + "rt_16_18_twoRot.tsv");

        WriteRTs(rt_13_15, outDir + "rt_13_15.tsv");
        WriteRTs(rt_13_15_noRot, outDir + "rt_13_15_noRot.tsv");
        WriteRTs(rt_13_15_oneRot, outDir + "rt_13_15_oneRot.tsv");
        WriteRTs(rt_13_15_twoRot, outDir + "rt_13_15_twoRot.tsv");
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
            log.Add(e.Message);
        }

        catch (System.IO.DirectoryNotFoundException e)
        {
            Console.WriteLine(e.Message);
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
                Console.WriteLine(fi.FullName);
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



    string[] split(string line)
    {
        return line.Split('\t');
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
                        float diff = float.Parse(lineSplit_j[0]) - float.Parse(lineSplit[0]);


                        lineSplit_j[0] = Math.Round((Decimal)diff, 5, MidpointRounding.AwayFromZero).ToString();


                        if ((int.Parse(lineSplit_j[4]) > 12) && (int.Parse(lineSplit_j[4]) < 16))
                        {
                            rt_13_15.Add(lineSplit_j);

                            switch (lineSplit_j[10])
                            {
                                case "O":
                                    rt_13_15_noRot.Add(lineSplit_j);
                                    break;
                                case "I":
                                    rt_13_15_oneRot.Add(lineSplit_j);
                                    break;
                                case "S":
                                    rt_13_15_oneRot.Add(lineSplit_j);
                                    break;
                                case "Z":
                                    rt_13_15_oneRot.Add(lineSplit_j);
                                    break;
                                case "T":
                                    rt_13_15_twoRot.Add(lineSplit_j);
                                    break;
                                case "J":
                                    rt_13_15_twoRot.Add(lineSplit_j);
                                    break;
                                case "L":
                                    rt_13_15_twoRot.Add(lineSplit_j);
                                    break;
                            }
                        }


                        if (int.Parse(lineSplit_j[4]) > 15)
                        {
                            rt_16_18.Add(lineSplit_j);

                            switch (lineSplit_j[10])
                            {
                                case "O":
                                    rt_16_18_noRot.Add(lineSplit_j);
                                    break;
                                case "I":
                                    rt_16_18_oneRot.Add(lineSplit_j);
                                    break;
                                case "S":
                                    rt_16_18_oneRot.Add(lineSplit_j);
                                    break;
                                case "Z":
                                    rt_16_18_oneRot.Add(lineSplit_j);
                                    break;
                                case "T":
                                    rt_16_18_twoRot.Add(lineSplit_j);
                                    break;
                                case "J":
                                    rt_16_18_twoRot.Add(lineSplit_j);
                                    break;
                                case "L":
                                    rt_16_18_twoRot.Add(lineSplit_j);
                                    break;
                            }
                        }

                        output.Add(lineSplit_j);
                        i = j;
                        break;
                    }
                    // no action found for current zoid, add empty rt entry
                    else if (containsEvent(lineSplit_j, "ZOID", "NEW"))
                    {
                        lineSplit[0] = "N/A";
                        output.Add(lineSplit);
                        i = j - 1;
                        break;
                    }
                }
            }
        }

        WriteRTs(output, outfile);
    }


    void WriteRTs(List<string[]> output, string outfile)
    {
        // convert collected values to string array
        string[] outlines = new string[output.Count];
        for (int i = 0; i < outlines.Length; i++)
        {
            outlines[i] = string.Join("\t", output[i]);
        }

        // write the result to file
        File.WriteAllLines(outfile, outlines);
    }



    bool containsEvent(string[] l, string s1, string s2)
    {
        if (l[7].Equals(s1) && l[8].Equals(s2))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

}