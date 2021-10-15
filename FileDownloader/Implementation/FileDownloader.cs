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
        private static int ThreadCount = 0;

        static Semaphore sem = new Semaphore(ParallelismValue, ParallelismValue);

        private ConcurrentQueue<QueueItem> downloadQueue = new();

        Thread downloadThread;

        private static object sync_creating_task = new();

        public FileDownloader()
        {
            OnAddToQueue += TryToGetItemForDownload;
        }

        public void AddFileToDownloadingQueue(string fileId, string url, string pathToSave)
        {
            downloadQueue.Enqueue(new QueueItem() { FileId = fileId, Url = url, PathToSave = pathToSave });
            OnAddToQueue();
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
                        lock (sync_creating_task)
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
                        using (Stream stream = await httpClient.GetStreamAsync(queueItem.Url))
                        {
                            using (FileStream fileStream = new FileStream(pathname, FileMode.Create))
                            {
                                await stream.CopyToAsync(fileStream);
                            }
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
                catch (IOException ioException)
                {
                    OnFailed?.Invoke(queueItem.FileId, ioException);
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
