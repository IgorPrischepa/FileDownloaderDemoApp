using FileDownloader.Contract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileDownloader.Implementation
{
    class FileDownloader : IFileDownloader
    {
        public event Action<string> OnDownloaded;
        public event Action<string, Exception> OnFailed;

        public void AddFileToDownloadingQueue(string fileId, string url, string pathToSave)
        {
            throw new NotImplementedException();
        }
    }
}
