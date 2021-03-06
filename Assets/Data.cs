using System.Collections.Generic;



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


public struct RTStats
{
    public RTStats(int newGood, int newBelow, int newAbove)
    {
        good_count = newGood;
        belowMin_count = newBelow;
        aboveMax_count = newAbove;

    }

    public int good_count { get; }
    public int belowMin_count { get; }
    public int aboveMax_count { get; }

    public int getTotalRT()
    {
        return good_count + belowMin_count + aboveMax_count;
    }

    public override string ToString() => $"({good_count.ToString()}, {belowMin_count.ToString()}, {aboveMax_count.ToString()})";
}




public static class Data 
{
     /// <summary>
     /// Fetches an element from a string list by its ordianl position in the list, or returns an empty string, if the position exceeds the size of the list.
     /// </summary>
     /// <param name="list">String list, from which the string is to be fetched.</param>
     /// <param name="i">Position of the string in the list.</param>
     /// <returns>String at the ith position, or an empty string, if the position exceeds the size of the list.</returns>
    public static string getElem(List<string> list, int i)
    {
        if (i < list.Count)
            return list[i];
        else
            return "";
    }



    public static string GetElem(List<RT> list, int i)
    {
        if (i < list.Count)
            return list[i].metaData[(int)Header.timestamp];
        else
            return "";
    }



    public static string[] Merge(List<string>[] listarray, char sep)
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

        return result.ToArray();
    }



    // ===================
    // STRING DATA HELPERS
    // ===================

    /// <summary>
    /// Merges all elements at the ith position of each list in the passed array into single string lines, separated by a separator, adds them to a list, and returns them
    /// </summary>
    /// <param name="listarray">Main data structure, whose elements have to be merged.</param>
    /// <param name="sep">A separator which will be put between each merging element.</param>
    /// <returns>List of the merged string elements.</returns>
    public static string[] Merge(List<RT>[] listarray, char sep)
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
                line += GetElem(list, i) + sep;
            }
            //trims out the last sep-char, and adds to the results
            result.Add(line.Remove(line.Length - 1));
        }
        return result.ToArray();
    }

}
