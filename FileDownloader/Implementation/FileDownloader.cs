using FileDownloader.Contract;
using System;
using System.Collections.Concurrent;
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
        private event Action OnAddToQueue;

        private static int ParallelismValue = 4;
        static Semaphore sem = new Semaphore(ParallelismValue, ParallelismValue);

        Thread DownloadThread;

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
                        DownloadThread = new Thread(new ParameterizedThreadStart(DownloadFileAndSaveToDiskAsync));
                        DownloadThread.Start(queueItem);
                    }
                }
            }

        }

        private async void DownloadFileAndSaveToDiskAsync(object item)
        {

            var queueItem = (QueueItem)item;

            string pathname = Path.Combine(queueItem.PathToSave, queueItem.FileId);

            if (File.Exists(pathname))
            {
                var exception = new InvalidOperationException($"File {pathname} already exists.");
                OnFailed?.Invoke(pathname, exception);
            }
            else
            {
                sem.WaitOne();
                using (HttpClient httpClient = new())
                {
                    var result = await httpClient.GetByteArrayAsync(queueItem.Url);

                    using (FileStream fw = new FileStream(pathname, FileMode.CreateNew, FileAccess.Write))
                    {
                        await fw.WriteAsync(result);
                    }
                }

                OnDownloaded?.Invoke(queueItem.FileId);
                sem.Release();
            }
        }

        public void SetDegreeOfParallelism(int degreeOfParallelism)
        {
            ParallelismValue = degreeOfParallelism;
        }
    }
}
