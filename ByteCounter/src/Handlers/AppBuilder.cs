using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using ConsoleTools;

namespace ByteCounter.Handlers
{
    /// <summary>
    /// App builder.
    /// </summary>
    public static class AppBuilder
    {
        /// <summary>
        /// Maximum number of participating threads.
        /// </summary>
        public const int MAX_THREADS = 20;

        /// <summary>
        /// Result to send to XML file.
        /// </summary>
        public static readonly List<Result> Results =
            new List<Result>();


        private static string _rootFolder = "";
        private static List<string> _files = new List<string>();
        private static List<string> _folders = new List<string>();


        /// <summary>
        /// Creating an application menu.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        public static void CreateMenu(string[] args)
        {
            var menu = new ConsoleMenu(args, 0)
                .Add("Get root folder...", () => GetRootAction("Get the root folder " +
                                                               "for the counting of bytes in the files " +
                                                               "of all subfolders."))
                .Add("Start the process.", () => ProcessAction("Start process..."))
                .Add("Close", ConsoleMenu.Close)
                .Add("Exit", () => Environment.Exit(0))
                .Configure(config => { config.Selector = "--> "; });

            menu.Show();
        }

        private static string SetFolder()
        {
            var folder = "";
            Console.WriteLine("Enter the absolute path to the root folder " +
                              "in which you are interested in starting the process:");
            Console.Write(" >>");
            var dir = Console.ReadLine();
            if (Directory.Exists(dir)) folder = Path.GetFullPath(dir);
            else Message("Warning: the path to the root folder is not found.");

            return folder;
        }

        private static void Message(string text)
        {
            Console.WriteLine(text);
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        private static void PathList()
        {
            _folders.Clear();
            GetAllFolders(_rootFolder);
            Console.WriteLine($" Total included {_folders.Count} folders.");
        }

        private static void GetAllFiles(string startDirectory, ref List<string> files)
        {
            var searchDirectory = Directory.GetDirectories(startDirectory);
            if (searchDirectory.Length > 0)
                foreach (var file in searchDirectory)
                    GetAllFiles(file + @"\", ref files);

            var allFiles = Directory.GetFiles(startDirectory);
            files.AddRange(allFiles);
        }

        private static void GetAllFolders(string sourceFolder)
        {
            var directoryInfo = new DirectoryInfo(sourceFolder);
            var list = (
                from subDirectoryInfo in
                    directoryInfo.GetDirectories("*.*", SearchOption.AllDirectories)
                where IsDirectoryContainFiles(subDirectoryInfo.FullName)
                select subDirectoryInfo.FullName).ToList();

            _folders = list;
            _folders.Add(directoryInfo.FullName);
        }

        private static bool IsDirectoryContainFiles(string path)
        {
            if (!Directory.Exists(path)) return false;
            return Directory.EnumerateFiles(path, "*", SearchOption.TopDirectoryOnly)
                .Any();
        }

        private static void GetRootAction(string text)
        {
            Console.WriteLine(text);
            Results.Clear();
            _folders.Clear();
            _rootFolder = SetFolder();
            if (_rootFolder.Length == 0) return;
            Console.WriteLine("Wait for the folder list to be initialized...");
            PathList();
            Message("Done.");
        }

        private static void ProcessAction(string text)
        {
            Console.WriteLine(text);
            if (_rootFolder.Length == 0)
            {
                Message("Warning: the path to the root folder is empty.");
                return;
            }

            foreach (var folder in _folders)
            {
                var handler = new ThreadsHandler(MAX_THREADS, folder);
                handler.LaunchWaitingThreads();
                while (!handler.Done) Thread.Sleep(150);
            }

            PutToXml();

            Message("");
        }


        /// <summary>
        /// Creating a file with the results of the analysis.
        /// </summary>
        private static void PutToXml()
        {
            var outputFile = @_rootFolder + "results.xml";
            var xd = File.Exists(outputFile)
                ? XDocument.Load(outputFile)
                : new XDocument(new XElement("root"));

            foreach (var res in Results)
            {
                var file = res.File;
                var total = res.TotalBytes;

                xd.Root.Add(new XElement("RESULT",
                    new XElement("File", file),
                    new XElement("Total", total)));
                xd.Save(outputFile);
            }
        }
    }
}