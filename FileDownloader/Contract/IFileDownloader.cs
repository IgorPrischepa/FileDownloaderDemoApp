using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileDownloader.Contract
{
    public interface IFileDownloader
    {
        void AddFileToDownloadingQueue(string fileId, string url, string pathToSave);

        event Action<string> OnDownloaded;

        event Action<string, Exception> OnFailed;
    }
}
