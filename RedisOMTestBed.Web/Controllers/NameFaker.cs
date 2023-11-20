namespace RedisOMTestBed.Web.Controllers
{
    public static class NameFaker
    {
        private static readonly string[] _firstNames =
        [
            "James",
            "John",
            "Robert",
            "Michael",
            "William",
            "David",
            "Richard",
            "Nate",
            "Jonathan",
            "Josh",
            "Elizabeth",
            "Mary",
            "Patricia",
            "Jennifer",
            "Linda",
            "Barbara",
            "Susan",
            "Jessica",
            "Sarah",
            "Karen",
        ];

        private static readonly string[] _lastNames =
        [
            "Smith",
            "Johnson",
            "Williams",
            "Jones",
            "Brown",
            "Davis",
            "Riley",
            "Wagner",
            "Groff",
            "Pavoncello",
            "Martin",
            "Thompson",
            "Garcia",
            "Martinez",
            "Robinson",
            "Clark",
            "Rodriguez",
            "Lewis",
            "Lee",
            "Walker",
        ];

        public static string Generate()
        {
            string firstName = _firstNames[Random.Shared.Next(_firstNames.Length)];
            string lastName = _lastNames[Random.Shared.Next(_lastNames.Length)];
            return $"{firstName} {lastName}";
        }
    }
}
