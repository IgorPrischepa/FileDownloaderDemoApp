using System.IO;
using System.Threading.Tasks;
using Downloader = FileDownloader.Implementation;

namespace FileDownloaderDemoApp
{
    class Program
    {
        static void Main()
        {
            var fileDonwloader = new Downloader.FileDownloader();
            var defaultPathToSave = "Downloaded";
            var defaultPathToListOfUrls = "download_list.txt";

            using (StreamReader sr = new(path: defaultPathToListOfUrls))
            {
                while (sr.EndOfStream != true)
                {
                    var urlFromFile = sr.ReadLine();
                    var startpos = urlFromFile.LastIndexOf('/') + 1;
                    var fileId = urlFromFile[startpos..];
                    fileDonwloader.AddFileToDownloadingQueue(fileId, urlFromFile, defaultPathToSave);
                }
            }

        }
    }
}
