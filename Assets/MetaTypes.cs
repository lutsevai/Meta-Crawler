using System;
using System.IO;



// SIMPLE TYPES
// ============

public enum Zoid { O, I, S, Z, J, L, T }
public enum Action { Left, Right, Down, Rotate, Counterrotate };
public enum Strategy { das, hypertap }



public static class MetaTypes
{
    public static readonly int[] levels = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29 };
    public static readonly int[] speedLevels = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 13, 16, 19, 29 };
    public static readonly int[] rotations = { 0, 1, 2 };

    /// <summary>
    /// Determines in which speed category the current lvl is in, and returns it
    /// </summary>
    /// <param name="lvl"> current Tetris level</param>
    /// <returns>The speed category of the passed level.</returns>
    public static int getSpeedRank(int lvl)
    {
        int cSpeed = 0;
        while ((cSpeed < (speedLevels.Length - 1)) && (speedLevels[cSpeed + 1] < (lvl + 1)))
        {
            cSpeed++;
        }

        return cSpeed;
    }


    // ======
    // ACTION
    // ======

    public static bool IsValidAction(string actionString)
    {
        foreach (Action a in Enum.GetValues(typeof(Action)))
        {
            if (a.ToString().Equals(actionString))
            {
                return true;
            }
        }
        return false;
    }

    public static Action GetAction(string actionString)
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


    // ====
    // ZOID
    // ====

    /// <summary>
    /// Matches an input string to the corresponding ZoidType, if present
    /// </summary>
    /// <param name="z">zoid name</param>
    /// <returns>zoid type matching the input</returns>
    public static Zoid GetZoidType(string z)
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
    public static Zoid GetZoidType(int z)
    {
        return (Zoid)Enum.GetValues(typeof(Zoid)).GetValue(z);
    }


    /// <summary>
    /// Returns the amount of rotations that can be performed on a zoid
    /// </summary>
    /// <param name="z">Zoid ordinal number to be analyzed for rotations</param>
    /// <returns></returns>
    public static int GetRotations(int z)
    {
        Zoid zoid = GetZoidType(z);
        return GetRotations(zoid);
    }


    /// <summary>
    /// Returns the amount of rotations that can be performed on a zoid
    /// </summary>
    /// <param name="z">Zoid to be analyzed for rotations</param>
    /// <returns></returns>
    public static int GetRotations(string z)
    {
        return GetRotations(GetZoidType(z));
    }


    /// <summary>
    /// Returns the amount of rotations that can be performed on a zoid
    /// </summary>
    /// <param name="zoid">Zoid to be analyzed for rotations</param>
    /// <returns></returns>
    public static int GetRotations(Zoid zoid)
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
            throw new InvalidDataException("Unknown Zoid Type provided for rotations: " + zoid.ToString());
        }
    }


}
