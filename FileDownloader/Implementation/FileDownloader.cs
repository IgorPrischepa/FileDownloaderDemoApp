using FileDownloader.Contract;
using FileDownloader.Models;
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

        private ConcurrentQueue<QueueItem> downloadQueue = new();

        Thread downloadThread;

        private static object sync = new();

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
                        lock (sync)
                        {
                            downloadThread = new Thread(new ParameterizedThreadStart(DownloadFileAndSaveToDiskAsync));
                            downloadThread.Name = queueItem.FileId;
                            downloadThread.Start(queueItem);

                        }
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
                try
                {

                    using (HttpClient httpClient = new())
                    {
                        var result = await httpClient.GetByteArrayAsync(queueItem.Url);

                        using (FileStream fw = new FileStream(pathname, FileMode.CreateNew, FileAccess.Write))
                        {
                            await fw.WriteAsync(result);
                        }
                    }

                    OnDownloaded?.Invoke(queueItem.FileId);
                }
                catch (InvalidOperationException invalidOperationException)
                {

                    OnFailed?.Invoke(queueItem.FileId, invalidOperationException);
                }
                catch (HttpRequestException httpRequestException)
                {
                    OnFailed?.Invoke(queueItem.FileId, httpRequestException);
                }
                catch (TaskCanceledException takeCancelException)
                {
                    OnFailed?.Invoke(queueItem.FileId, takeCancelException);
                }
                finally
                {
                    sem.Release();
                }
            }

        }

        public void SetDegreeOfParallelism(int degreeOfParallelism)
        {
            ParallelismValue = degreeOfParallelism;
        }

    }
}
