namespace TinyCsv.Tests
{
    using System;
    using TinyCsv.Attributes;

    public class Person
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string City { get; set; }
    }

    [Delimiter(",")]
    [HasHeaderRecord(true)]
    [TrimData(true)]
    public class AttributePerson
    {
        [Column]
        public int Id { get; set; }

        [Column]
        public string Name { get; set; }

        [Column(format: "yyyy-MM-dd")]
        public DateTime BirthDate { get; set; }
    }

    internal static class Csv
    {
        public static TinyCsv<Person> Person(Action<CsvOptions<Person>> configure = null)
        {
            return new TinyCsv<Person>(options =>
            {
                options.Delimiter = ";";
                options.HasHeaderRecord = false;
                options.Columns.AddColumn(m => m.Id);
                options.Columns.AddColumn(m => m.Name);
                options.Columns.AddColumn(m => m.City);
                configure?.Invoke(options);
            });
        }
    }
}
