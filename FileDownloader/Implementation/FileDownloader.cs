using FileDownloader.Contract;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace FileDownloader.Implementation
{
    public class FileDownloader : IFileDownloader
    {
        public event Action<string> OnDownloaded;
        public event Action<string, Exception> OnFailed;
        private event Action OnAddToQueue;
        private ConcurrentQueue<QueueItem> DownloadQueue = new();

        public FileDownloader()
        {
            OnAddToQueue += TryToGetItemForDownload;
        }

        public void AddFileToDownloadingQueue(string fileId, string url, string pathToSave)
        {
            Task.Run(() =>
            {
                DownloadQueue.Enqueue(new QueueItem() { FileId = fileId, Url = url, PathToSave = pathToSave });
                OnAddToQueue();
            });
        }

        private void TryToGetItemForDownload()
        {
            foreach (var item in DownloadQueue)
            {
                bool isSuccessful = DownloadQueue.TryDequeue(out QueueItem queueItem);
                if (isSuccessful)
                {
                    Task.Run(() => DownloadFileAndSaveToDiskAsync(queueItem));
                }
            }
        }

        private void DownloadFileAndSaveToDiskAsync(QueueItem queueItem)
        {
            string pathname = Path.GetFullPath($"{queueItem.PathToSave}/{queueItem.FileId}");
            if (File.Exists(queueItem.PathToSave))
            {
                var exception = new InvalidOperationException($"File {pathname} already exists.");

                OnFailed($"File {pathname} already exists.", exception);

                throw exception;
            }

            using (HttpClient httpClient = new())
            {
                var result = httpClient.GetAsync(queueItem.Url);
                using (StreamWriter sw = new(path: queueItem.PathToSave))
                {
                    sw.Write(result.Result.Content);
                }
                OnDownloaded(queueItem.FileId);
            }
        }
    }
}
