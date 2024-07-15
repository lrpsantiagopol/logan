using System.ComponentModel;

namespace LogAnalyzer
{
    public static class DataGridViewExtensions
    {
        public static void SortByColumn(this DataGridView dgv, string columnName, bool asc = true)
        {
            dgv.Sort(dgv.Columns[columnName], asc ? ListSortDirection.Ascending : ListSortDirection.Descending);
        }
    }
}
