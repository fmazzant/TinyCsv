namespace TinyCsv
{
    public sealed class CsvOptions<T>
    {
        public bool HasHeaderRecord { get; set; }
        public CsvOptionsColumns<T> Columns { get; internal set; }
        public string Delimiter { get; set; } = ";";
        public UseSystemSeperator UseSystemSeperators { get; internal set; }

        public CsvOptions()
        {
            Columns = new CsvOptionsColumns<T>();
            UseSystemSeperators = new UseSystemSeperator();
        }
    }
}
