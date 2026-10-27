namespace FitnessCenterBlazor.Models
{
    // This class describes one column in a database table.
    public class ColumnInfo
    {
        // Column name, for example "MemberID".
        public string Name { get; set; } = "";

        // MySQL data type, for example "int", "varchar", "datetime".
        public string DataType { get; set; } = "";

        // True when this column is the primary key.
        public bool IsPrimaryKey { get; set; }

        // True when MySQL generates the value automatically.
        public bool IsAutoIncrement { get; set; }

        // True when the database allows NULL.
        public bool IsNullable { get; set; }
        public int MaxLength { get; set; }
    }
}