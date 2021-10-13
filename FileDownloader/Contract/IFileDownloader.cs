using System;

namespace FileDownloader.Contract
{
    public interface IFileDownloader
    {
        void AddFileToDownloadingQueue(string fileId, string url, string pathToSave);

        event Action<string> OnDownloaded;

        event Action<string, Exception> OnFailed;
    }
}
