using NLog;
using System;
using System.IO;
using Downloader = FileDownloader.Implementation;

namespace FileDownloaderDemoApp
{
    class Program
    {

        public static int fileCount = 0;
        public static int downloadedCount = 0;
        public static int failedCount = 0;

        private static object syncAcccessToCounters = new();

        private static Logger logger = LogManager.GetCurrentClassLogger();

        static void Main()
        {
            Console.WriteLine("To interrupt download process and exit, please press any key");

            var defaultPathToSave = "Downloaded";
            var defaultPathToListOfUrls = "download_list.txt";

            logger.Info($"Path to save files: {defaultPathToSave}");
            logger.Info($"Path to file with urls: {defaultPathToListOfUrls}");

            EnsureDirectoryCreated(defaultPathToSave);

            var fileDownloader = new Downloader.FileDownloader();
            fileDownloader.SetDegreeOfParallelism(5);
            fileDownloader.OnDownloaded += SucceessDownload;
            fileDownloader.OnFailed += FailedDownload;

            using (StreamReader sr = new(path: defaultPathToListOfUrls))
            {
                while (sr.EndOfStream != true)
                {
                    var urlFromFile = sr.ReadLine();
                    var startpos = urlFromFile.LastIndexOf('/') + 1;
                    var fileId = urlFromFile[startpos..];
                    fileCount++;
                    fileDownloader.AddFileToDownloadingQueue(fileId, urlFromFile, defaultPathToSave);
                }
            }

            Console.ReadKey();
        }

        private static void FailedDownload(string filePath, Exception exception)
        {
            Console.WriteLine($"{exception.Message}");

            logger.Error($"{exception.Message}");

            if (exception.StackTrace != null)
            {
                logger.Error($"{exception.StackTrace}");
            }

            lock (syncAcccessToCounters)
            {
                failedCount++;
                UpdateInfoInCosole();
            }
        }

        private static void SucceessDownload(string obj)
        {
            lock (syncAcccessToCounters)
            {
                downloadedCount++;

                Console.WriteLine($"{obj} downloaded");

                UpdateInfoInCosole();
            }

        }

        private static void UpdateInfoInCosole()
        {
            lock (syncAcccessToCounters)
            {
                var progress = ((downloadedCount + failedCount) * 100) / fileCount;

                Console.Title = $"Total:{fileCount} Downloaded:{downloadedCount} Failed:{failedCount} Progress: {progress}%";

                if (progress == 100)
                {
                    Console.WriteLine($"Precceded all urls. Successfuly download: {downloadedCount}. Failed: {failedCount}");
                    Console.WriteLine("Press any key to exit.");
                }

            }
        }

        private static void EnsureDirectoryCreated(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                logger.Info("Directory has been created.");
            }
        }
    }
}
