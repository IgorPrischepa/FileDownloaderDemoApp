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

        static void Main()
        {
            var defaultPathToSave = "Downloaded";
            var defaultPathToListOfUrls = "download_list.txt";

            EnsureDirectoryCreated(defaultPathToSave);

            var fileDonwloader = new Downloader.FileDownloader();

            fileDonwloader.OnDownloaded += SucceessDownload;
            fileDonwloader.OnFailed += FailedDownload;

            using (StreamReader sr = new(path: defaultPathToListOfUrls))
            {
                while (sr.EndOfStream != true)
                {
                    var urlFromFile = sr.ReadLine();
                    var startpos = urlFromFile.LastIndexOf('/') + 1;
                    var fileId = urlFromFile[startpos..];
                    fileCount++;
                    fileDonwloader.AddFileToDownloadingQueue(fileId, urlFromFile, defaultPathToSave);
                }
            }

            Console.WriteLine("To interrupt download process and exit, please press any key");
            Console.ReadKey();
        }

        private static void FailedDownload(string filePath, Exception exception)
        {
            failedCount++;

            Console.WriteLine($"{exception.Message}");

            UpdateInfoInCosole();
        }

        private static void SucceessDownload(string obj)
        {
            downloadedCount++;

            Console.WriteLine($"{obj} downloaded");

            UpdateInfoInCosole();
        }

        private static void UpdateInfoInCosole()
        {
            var progress = ((downloadedCount + failedCount) * 100) / fileCount;

            Console.Title = $"Total:{fileCount} Downloaded:{downloadedCount} Failed:{failedCount} Progress: {progress}%";

            if (progress == 100)
            {
                Console.WriteLine($"Precceded all urls. Successfuly download: {downloadedCount}. Failed: {failedCount}");
                Console.WriteLine("Press any key to exit.");
            }
        }

        private static void EnsureDirectoryCreated(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }
}
