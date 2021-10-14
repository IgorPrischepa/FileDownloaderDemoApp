using System;

namespace FileDownloader.Contract
{
    public interface IFileDownloader
    {
        void AddFileToDownloadingQueue(string fileId, string url, string pathToSave);

        void SetDegreeOfParallelism(int degreeOfParallelism);

        event Action<string> OnDownloaded;

        event Action<string, Exception> OnFailed;
    }
}
