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

        private static void FailedDownload(string arg1, Exception arg2)
        {
            failedCount++;
            UpdateInfoInCosole();
        }

        private static void SucceessDownload(string obj)
        {
            downloadedCount++;
            UpdateInfoInCosole();
        }



        private static void UpdateInfoInCosole()
        {
            Console.Title = $"{(downloadedCount * 100) / fileCount} Total:{fileCount} Downloaded:{downloadedCount} Failed:{failedCount}";
        }
    }
}
