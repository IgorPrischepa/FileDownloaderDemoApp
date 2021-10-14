using FileDownloader.Contract;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FileDownloader.Implementation
{
    public class FileDownloader : IFileDownloader
    {
        public event Action<string> OnDownloaded;
        public event Action<string, Exception> OnFailed;
        static object queueLocker = new object();

        private event Action OnAddToQueue;
        private ConcurrentQueue<QueueItem> downloadQueue = new();

        public FileDownloader()
        {
            OnAddToQueue += TryToGetItemForDownload;
        }

        public void AddFileToDownloadingQueue(string fileId, string url, string pathToSave)
        {

            Task.Run(() =>
            {
                downloadQueue.Enqueue(new QueueItem() { FileId = fileId, Url = url, PathToSave = pathToSave });
                OnAddToQueue();
            });
        }

        private void TryToGetItemForDownload()
        {
            if (!downloadQueue.IsEmpty)
            {
                foreach (var item in downloadQueue)
                {
                    bool isSuccessful = downloadQueue.TryDequeue(out QueueItem queueItem);
                    if (isSuccessful)
                    {
                        Task.Run(() => DownloadFileAndSaveToDiskAsync(queueItem));
                    }
                }
            }

        }

        private async Task DownloadFileAndSaveToDiskAsync(QueueItem queueItem)
        {
            Debug.WriteLine($"Enter to download and save method...");
            string pathname = Path.Combine(queueItem.PathToSave, queueItem.FileId);

            if (File.Exists(pathname))
            {
                var exception = new InvalidOperationException($"File {pathname} already exists.");
                OnFailed?.Invoke(pathname, exception);
                return;
            }

            using (HttpClient httpClient = new())
            {
                Debug.WriteLine($"{queueItem.Url} Starting download...");
                var result = await httpClient.GetByteArrayAsync(queueItem.Url);

                Debug.WriteLine($"{queueItem.Url} End dowload...");
                using (FileStream fw = new FileStream(pathname, FileMode.CreateNew, FileAccess.Write))
                {
                    Debug.WriteLine($"{queueItem.Url} Start writing to disk...");
                    await fw.WriteAsync(result);
                    Debug.WriteLine($"{queueItem.Url} End writing to disk...");
                }
            }

            Debug.WriteLine($"{queueItem.Url} Call event OnDownload");
            OnDownloaded?.Invoke(queueItem.FileId);
        }
    }
}
