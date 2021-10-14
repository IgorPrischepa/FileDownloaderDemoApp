namespace FileDownloader.Models
{
    public class QueueItem
    {
        public string FileId { get; set; }

        public string Url { get; set; }

        public string PathToSave { get; set; }
    }
}
