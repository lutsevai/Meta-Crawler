using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;


//column nr:      0            1           2             3           4      5          6          7        8           9          10        11
public enum Header { timestamp, system_ticks, event_type, episode_number, level, score, lines_cleared, evt_id, evt_data1, evt_data2, curr_zoid, next_zoid };

public static class MetaLog
{
    public const string logExtension = ".tsv";
    const char sep = '\t';

    /// <summary>
    /// Finds the subject ID in passed game log data.
    /// </summary>
    /// <param name="lines">Log data where the subject ID has to be extracted from.</param>
    /// <returns>string of subject ID found in the data. If no SID was found, returns "NOSID".</returns>
    public static string getSid(string[] lines)
    {
        //get SID
        foreach (string line in lines)
        {
            string[] lSplit = MetaLog.split(line);

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
    public static int GetGameStart(string[] lines)
    {
        bool found = false;
        int startIndex = 0;

        foreach (string line in lines)
        {
            string[] lSplit = MetaLog.split(line);

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



    public static bool IsLog(string path)
    {
        if ((!Path.GetExtension(path).Equals(logExtension)) || (Path.GetFileNameWithoutExtension(path).Contains("tobii-sync")))
            return false;

        return true;
    }



    public static bool IsGoodData(string[] lines)
    {
        //initial check, in case not even header is fully present
        if (lines.Length < 50)
            return false;

        string sid = getSid(lines);
        if (sid.ToLower().Contains("test"))
            return false;

        int start = MetaLog.GetGameStart(lines);

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
            return false;

        return true;

    }


    /// <summary>
    /// Checks if a game log file is worth being analyzed.
    /// </summary>
    /// <param name="infile">Path to the file to be checked.</param>
    /// <param name="lines">Out array containing the data, if it turns out good. Otherwise, returns an empty array.</param>
    /// <returns>True if data worth being analyzed, and false, if not.</returns>
    public static bool hasGoodData(string infile, out string[] lines)
    {
        if (!MetaLog.IsLog(infile))
        {
            lines = new string[0];
            return false;
        }

        lines = File.ReadAllLines(infile);
        return IsGoodData(lines);
    }


    /// <summary>
    /// Categorizes a data from file into play strategies: hypertapping, and das.
    /// </summary>
    /// <param name="infile">Path to the file that is to be analyzed.</param>
    public static Strategy detectStrategyy(string[] lines)
    {
        const int htap_threshhold = 6;

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
                        if (!MetaTypes.isValidAction(lineSplit_j[(int)Header.evt_data2]))
                            continue;

                        Action a = MetaTypes.getAction(lineSplit_j[(int)Header.evt_data2]);

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

        if (htaps > htap_threshhold)
            return Strategy.hypertap;
        else
            return Strategy.das;

    }


    /// <summary>
    /// Checks whether a line from the log contains a certain event string.
    /// </summary>
    /// <param name="lineSplit">The line to be checked for the event.</param>
    /// <param name="event_id">Event string.</param>
    /// <returns>True if the line contains the event string.</returns>
    public static bool containsEvent(string[] lineSplit, string event_id)
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
    /// Checks whether a line from the log contains a certain event.
    /// </summary>
    /// <param name="lineSplit">The line to be checked for the event string.</param>
    /// <param name="event_id">First portion of the event string.</param>
    /// <param name="event_data1">Second part of the event string.</param>
    /// <returns>True if the line contains the event string.</returns>
    public static bool containsEvent(string[] lineSplit, string event_id, string event_data1)
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
    /// Splits a tab separated line into an array
    /// </summary>
    public static string[] split(string line)
    {
        return line.Split(sep);
    }


}
