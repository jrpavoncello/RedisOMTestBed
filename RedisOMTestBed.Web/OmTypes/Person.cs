using Redis.OM.Modeling;

namespace RedisOMTestBed.Web.OmTypes
{

    [Document(StorageType = StorageType.Json, Prefixes = ["Person"])]
    public class Person
    {
        [RedisIdField]
        [Indexed]
        public string Id { get; set; } = string.Empty;

        [Searchable]
        public string Name { get; set; } = string.Empty;

        [Indexed(Sortable = true)]
        public int Age { get; set; }

        [Indexed]
        public double HeightFeet { get; set; }

        [Indexed]
        public decimal MoneyInBank { get; set; }

        [Indexed]
        public bool IsMarried { get; set; }

        public DateTime DateOfBirth { get; set; }
    }
}
