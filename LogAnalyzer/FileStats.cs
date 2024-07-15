namespace LogAnalyzer
{
    public class FileStats
    {
        public DateTime CreatedAt { get; set; }

        public string FileName { get; set; }

        public string FullPath { get; set; }

        public bool HasWarnings { get; set; }

        public bool HasErrors { get; set; }

        public float Similarity { get; set; }
    }
}
