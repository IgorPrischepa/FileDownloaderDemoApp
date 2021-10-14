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
                //Think about it
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

        private async Task DownloadFileAndSaveToDiskAsync(QueueItem queueItem)
        {
            string pathname = Path.Combine(queueItem.PathToSave, queueItem.FileId);

            if (File.Exists(queueItem.PathToSave))
            {
                var exception = new InvalidOperationException($"File {pathname} already exists.");

                OnFailed?.Invoke($"File {pathname} already exists.", exception);

                throw exception;
            }

            using (HttpClient httpClient = new())
            {
                var result = await httpClient.GetByteArrayAsync(queueItem.Url);

                using (FileStream fw = new FileStream(pathname, FileMode.CreateNew, FileAccess.Write))
                {
                    await fw.WriteAsync(result);
                }

                OnDownloaded?.Invoke(queueItem.FileId);
            }
        }
    }
}
