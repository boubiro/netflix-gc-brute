using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Netflix_GC_Brute.Helpers
{
    public class FileHelper
    {

        private static readonly object WriteLock = new object();
        private static string _date;

        public static void BeginCheck()
        {
            _date = $"{DateTime.Today:dd-MM-yyyy} {DateTime.Now:HH-mm-ss}";
        }

        public static void Write(string file, object content)
        {
            var folder = $"Results\\{_date}";

            Directory.CreateDirectory("Results");
            Directory.CreateDirectory(folder);

            Append($"{folder}\\{file}", content);
        }

        public static void Create(string file)
        {

            lock (WriteLock)
                File.Create(file).Close();

        }

        public static void Append(string file, object content)
        {
            try
            {
                lock (WriteLock)
                    using (var fStream = File.Open(file, FileMode.Append))
                    using (var sWriter = new StreamWriter(fStream))
                        sWriter.WriteLine(content);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public static bool Exists(string file)
        {
            return File.Exists(file);
        }

        public static void DeleteAll(params string[] file)
        {
            foreach (var s in file)
                File.Delete(s);

        }

        public static void Move(string file, string newFile)
        {
            File.Move(file, newFile);
        }

        public static List<string> ReadAsList(string file)
        {
            return File.ReadAllLines(file).ToList();
        }

        public static string ReadAsText(string file)
        {
            return File.ReadAllText(file);
        }

    }
}