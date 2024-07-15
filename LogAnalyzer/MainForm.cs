
using Microsoft.VisualBasic.FileIO;
using System.Collections.Concurrent;

namespace LogAnalyzer
{
    public partial class MainForm : Form
    {
        private Dictionary<string, string> _loadedFile;
        private ConcurrentDictionary<Tuple<string, string>, float> _similarities;
        private List<FileStats> _fileList;
        private List<FileStats> _filteredFileList;

        public MainForm()
        {
            InitializeComponent();

            _loadedFile = new Dictionary<string, string>();
            _similarities = new ConcurrentDictionary<Tuple<string, string>, float>();

            LoadFolders();
        }

        private void LoadFolders()
        {
            const string baseFolder = "C:\\Temp";
            var folders = FileSystem.GetDirectories(baseFolder, Microsoft.VisualBasic.FileIO.SearchOption.SearchAllSubDirectories, "Logs").ToList();
            var data = folders.Select(x => new
            {
                Directory = x,
            })
            .ToList();

            dgvFolders.DataSource = data;
            dgvFolders.Refresh();
        }

        private void OpenFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var result = folderBrowserDialog1.ShowDialog();
        }

        private async void DgvFolders_SelectionChanged(object sender, EventArgs e)
        {
            var selectedRowIndex = dgvFolders.Rows.GetFirstRow(DataGridViewElementStates.Selected);

            if (selectedRowIndex < 0)
            {
                return;
            }

            var row = dgvFolders.Rows[selectedRowIndex];

            if (row == null)
            {
                return;
            }

            var directory = row.Cells[0].Value.ToString();
            _fileList = FileSystem.GetFiles(directory + "\\", Microsoft.VisualBasic.FileIO.SearchOption.SearchAllSubDirectories)
                .OrderByDescending(x => x)
                .Select(CreateFileStats)
                .ToList();

            ClearFilters();
        }

        private async void DgvFiles_SelectionChanged(object sender, EventArgs e)
        {
            var selectedRowIndex = dgvFiles.Rows.GetFirstRow(DataGridViewElementStates.Selected);

            if (selectedRowIndex < 0)
            {
                return;
            }

            var row = dgvFiles.Rows[selectedRowIndex];

            if (row == null)
            {
                return;
            }

            var directory = row.Cells[2].Value.ToString();
            var text = LoadTextFromFile(directory);

            textBox1.Clear();
            textBox1.Text = text;
            textBox1.Refresh();

            await CalculateSimilaritiesAsync(directory);
        }

        private async Task CalculateSimilaritiesAsync(string directory)
        {
            await Task.Run(() =>
            {
                var dirList = new List<string>();

                for (int i = 0; i < dgvFiles.RowCount; i++)
                {
                    var dir = dgvFiles["FullPath", i].Value.ToString();
                    dirList.Add(dir);
                }

                var text1 = LoadTextFromFile(directory);

                for (int i = 0; i < dirList.Count; i++)
                {
                    var dir = dirList[i];
                    var pair = new Tuple<string, string>(directory, dir);

                    if (_similarities.TryGetValue(pair, out var similarity))
                    {
                        dgvFiles["Similarity", i].Value = Math.Round(similarity, 3);
                        continue;
                    }

                    var text2 = LoadTextFromFile(dir);
                    similarity = CalculateSimilarity(text1, text2);

                    dgvFiles["Similarity", i].Value = Math.Round(similarity, 3);
                    _similarities.TryAdd(pair, similarity);
                }

                //dgvFiles.Refresh();
                //var maxSim = _similarities
                //   .Where(x => x.Key.Item1.Equals(directory)
                //       && !x.Key.Item2.Equals(directory))
                //   .MaxBy(x => x.Value);

                //var fileName = Path.GetFileName(maxSim.Key.Item2);

                //lblMaxSimilarity.Text = $"Max Similarity: {Math.Round(maxSim.Value, 3)} ({fileName})";
            });
        }

        private FileStats CreateFileStats(string directory)
        {
            var info = FileSystem.GetFileInfo(directory);
            var tz = TimeZoneInfo.FindSystemTimeZoneById("CST");
            var stats = new FileStats
            {
                CreatedAt = TimeZoneInfo.ConvertTime(info.LastWriteTime, tz),
                FileName = info.Name,
                FullPath = info.FullName,
                HasWarnings = ContainsText(directory, "WARNING"),
                HasErrors = ContainsText(directory, "ERROR"),
                Similarity = 0
            };

            return stats;
        }

        private static bool ContainsText(string fileDir, string text)
        {
            var fullText = File.ReadAllText(fileDir);

            return fullText.Contains(text, StringComparison.InvariantCultureIgnoreCase);
        }

        private string LoadTextFromFile(string directory)
        {
            if (_loadedFile.TryGetValue(directory, out var value))
            {
                return value;
            }

            var text = File.ReadAllText(directory);
            _loadedFile.Add(directory, text);

            return text;
        }


        private static float CalculateSimilarity(string text1, string text2)
        {
            var words1 = text1.Split([' ', '\n']);
            var words2 = text2.Split([' ', '\n']);

            var intersection = words1.Intersect(words2);
            var union = words1.Union(words2);
            var similarity = (float)intersection.Count() / union.Count();

            return similarity;
        }

        private void ClearFilters()
        {
            tbContentFilter.Clear();
            cbLogType.SelectedIndex = 0;

            dgvFiles.DataSource = _fileList;
            dgvFiles.Refresh();
        }

        private void ApplyFilters()
        {
            var logType = (LogType)cbLogType.SelectedIndex;
            var list = _fileList.Where(x =>
                    logType == LogType.ErrorsOnly ? x.HasErrors : true
                    && logType == LogType.WarningsOnly ? x.HasWarnings : true
                    && logType == LogType.SuccessOnly ? !x.HasErrors : true);

            var filterText = tbContentFilter.Text.Trim();

            if (!string.IsNullOrWhiteSpace(filterText))
            {
                list = list.Where(f => LoadTextFromFile(f.FullPath)
                        .Contains(filterText, StringComparison.InvariantCultureIgnoreCase))
                    .ToList();
            }

            dgvFiles.DataSource = list.ToList();
            dgvFiles.Refresh();
        }

        private void BtnClearFilters_Click(object sender, EventArgs e)
        {
            ClearFilters();
        }

        private void TbContentFilter_TextChanged(object sender, EventArgs e)
        {
            var text = tbContentFilter.Text.Trim();

            if (string.IsNullOrWhiteSpace(text) || text.Length < 3)
            {
                return;
            }

            ApplyFilters();
        }

        private void CbLogType_SelectedIndexChanged(object sender, EventArgs e)
        {
            ApplyFilters();
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
