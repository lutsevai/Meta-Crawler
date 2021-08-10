using System;
using System.IO;
using UnityEngine;



public delegate void fileTask(string inPath);



public class DirectoryCrawler
{

    public void WalkDirectoryTree(System.IO.DirectoryInfo root, fileTask processFile)
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
            //TODO: create log file
            Debug.Log(e.Message);
        }

        catch (System.IO.DirectoryNotFoundException e)
        {
            //TODO: create log file
            Debug.Log(e.Message);
        }

        if (files != null)
        {
            foreach (System.IO.FileInfo fi in files)
            {
                try
                {
                    processFile(fi.FullName);
                }
                // In case the file was moved / deleted since the call to TraverseTree() / WalkDirectoryTree().
                catch (FileNotFoundException e)
                {
                    Debug.Log(e.Message);
                    // TODO: create log file                
                }
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


}
