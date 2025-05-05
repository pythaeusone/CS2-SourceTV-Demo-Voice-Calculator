using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace FaceitDemoVoiceCalc
{
    /// <summary>
    /// Provides extension methods for various common operations.
    /// </summary>
    internal static class Extensions
    {
        /// <summary>
        /// Splits a string into two halves.
        /// </summary>
        /// <param name="s">The input string to split.</param>
        /// <returns>An array of two substrings: the first half and the second half.</returns>
        public static string[] CutString(this string s)
        {
            // Calculate midpoint
            int midpoint = s.Length / 2;
            // Return first half and second half
            return new[]
            {
                s.Substring(0, midpoint),
                s.Substring(midpoint)
            };
        }


        /// <summary>
        /// Compares two integers, returning 1 if the first is greater, -1 if less, or 0 if equal.
        /// </summary>
        /// <param name="x">The first integer to compare.</param>
        /// <param name="y">The second integer to compare.</param>
        /// <returns>An integer that indicates the relative values of x and y.</returns>
        public static int Compare(this int x, int y) => x.CompareTo(y);

        /// <summary>
        /// Determines whether a string is null or consists only of whitespace.
        /// </summary>
        /// <param name="s">The string to check.</param>
        /// <returns>True if the string is null or whitespace; otherwise, false.</returns>
        public static bool IsNullOrEmptyOrWhiteSpace(this string s) => string.IsNullOrWhiteSpace(s);


        /// <summary>
        /// Converts a string to its hexadecimal representation using UTF8 encoding.
        /// </summary>
        /// <param name="s">The input string to convert.</param>
        /// <returns>A hex-encoded representation of the string.</returns>
        public static string ConvertToHex(this string s) => Convert.ToHexString(Encoding.UTF8.GetBytes(s));


        /// <summary>
        /// Lists all file and directory paths under the given directory.
        /// </summary>
        /// <param name="dirInfo">The directory to enumerate.</param>
        /// <param name="excludeCurrentDir">If true, excludes the root directory path.</param>
        /// <returns>An enumerable of file and directory full paths.</returns>
        public static IEnumerable<string> ListDirectory(this DirectoryInfo dirInfo, bool excludeCurrentDir = false)
        {
            // Optionally include the current directory
            if (!excludeCurrentDir)
            {
                yield return dirInfo.FullName;
            }

            // Recursively yield each subdirectory and its contents
            foreach (var subDir in dirInfo.GetDirectories())
            {
                foreach (var path in subDir.ListDirectory())
                {
                    yield return path;
                }
            }

            // Yield each file in the current directory
            foreach (var file in dirInfo.GetFiles())
            {
                yield return file.FullName;
            }
        }


        /// <summary>
        /// Calculates the total size of a directory by summing all file lengths recursively.
        /// </summary>
        /// <param name="dir">The directory for which to calculate size.</param>
        /// <returns>The total size in bytes.</returns>
        public static long GetFolderSize(this DirectoryInfo dir)
        {
            long size = 0;

            // Sum sizes of files in this directory
            foreach (var file in dir.GetFiles())
            {
                size += file.Length;
            }

            // Recursively include sizes of subdirectories
            foreach (var subDir in dir.GetDirectories())
            {
                size += subDir.GetFolderSize();
            }

            return size;
        }


        /// <summary>
        /// Counts all files under the specified directory path, including subdirectories.
        /// </summary>
        /// <param name="path">The directory path to search.</param>
        /// <returns>The total file count, or zero if the path is invalid.</returns>
        public static int CountFiles(this string path)
        {
            // Return zero if path is null, empty, or does not exist
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            {
                return 0;
            }

            // Count files recursively
            return Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories).Count();
        }


        /// <summary>
        /// Determines whether a string array is null or contains no elements.
        /// </summary>
        /// <param name="array">The array to check.</param>
        /// <returns>True if the array is null or empty; otherwise, false.</returns>
        public static bool IsNullOrEmpty(this string[] array) => array == null || array.Length == 0;


        /// <summary>
        /// Computes the MD5 checksum of the specified file.
        /// </summary>
        /// <param name="filePath">The full path to the file.</param>
        /// <returns>The lowercase hex-encoded MD5 checksum.</returns>
        public static string GetMD5Checksum(this string filePath)
        {
            // Create MD5 instance
            using var md5 = MD5.Create();
            // Open file stream for reading
            using var stream = File.OpenRead(filePath);
            // Compute hash bytes
            byte[] hashBytes = md5.ComputeHash(stream);
            // Convert to hex string
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }


        /// <summary>
        /// Checks whether the specified path points to an existing file.
        /// </summary>
        /// <param name="path">The file path to check.</param>
        /// <returns>True if the file exists at the given path; otherwise, false.</returns>
        public static bool DoesFileExist(this string path)
        {
            return File.Exists(path);
        }


        /// <summary>
        /// Moves the file from its current path into the specified directory,
        /// prompting the user via an InputBox for a new filename. If the chosen
        /// name already exists at the destination, the user is prompted again.
        /// Returns true if the move-and-rename operation succeeded; otherwise, false.
        /// </summary>
        /// <param name="sourcePath">The full path of the file to move and rename.</param>
        /// <param name="destinationDirectory">The directory to which the file will be moved.</param>
        /// <returns>
        /// True if the file was successfully moved and renamed; false if canceled,
        /// if an error occurred, or if the chosen name was left empty.
        /// </returns>
        public static bool MoveAndRenameFile(this string sourcePath, string destinationDirectory)
        {
            // Verify that the source file exists
            if (!File.Exists(sourcePath))
            {
                MessageBox.Show(
                    $"Source file not found:\n{sourcePath}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                return false;
            }

            // Ensure the destination directory exists (create if necessary)
            if (!Directory.Exists(destinationDirectory))
            {
                try
                {
                    Directory.CreateDirectory(destinationDirectory);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Unable to create destination directory:\n{ex.Message}",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    return false;
                }
            }

            // Use the original filename as the default suggestion
            string defaultName = Path.GetFileName(sourcePath);

            while (true)
            {
                // Prompt the user for a new filename (including extension)
                // Correct positional call: Prompt, Title, DefaultResponse
                string newFileName = Interaction.InputBox(
                    "Please enter the new filename (including extension):",
                    "New Filename",
                    defaultName
                );

                // If the user cancels or submits an empty name, abort
                if (string.IsNullOrWhiteSpace(newFileName))
                    return false;

                // Compose the full destination path
                string destinationPath = Path.Combine(destinationDirectory, newFileName);

                // If a file with that name already exists, warn and retry
                if (File.Exists(destinationPath))
                {
                    MessageBox.Show(
                        "A file with this name already exists. Please choose a different name.",
                        "Name Already Taken",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                    continue;  // prompt again
                }

                // Attempt to move (and rename) the file
                try
                {
                    File.Move(sourcePath, destinationPath);
                    return true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Error while moving/renaming the file:\n{ex.Message}",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    return false;
                }
            }
        }
    }
}
