using Microsoft.AspNetCore.Mvc;
using Redis.OM;
using Redis.OM.Contracts;
using StackExchange.Redis;
using RedisOMTestBed.Web.OmTypes;

namespace RedisOMTestBed.Web.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PersonController(
        ILogger<PersonController> logger,
        IRedisConnectionProvider redisConnectionProvider,
        IDatabase database) : ControllerBase
    {
        private const string _firstPersonIdKey = "FirstPersonId";

        [HttpPost("CreateIndex")]
        public async Task<IActionResult> CreateIndex(bool setExpiration = true, int numPeopleToCreate = 20_000, int secondsToTest = 120)
        {
            redisConnectionProvider.Connection.DropIndexAndAssociatedRecords(typeof(Person));
            await redisConnectionProvider.Connection.CreateIndexAsync(typeof(Person));

            var people = new List<Person>(numPeopleToCreate);
            for (var i = 0; i < people.Capacity; i++)
            {
                var person = new Person
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = NameFaker.Generate(),
                    Age = Random.Shared.Next(1, 100),
                    HeightFeet = Random.Shared.Next(1, 10) + Random.Shared.NextDouble(),
                    MoneyInBank = Convert.ToDecimal(Random.Shared.Next(1, 100000) + Random.Shared.NextDouble()),
                    IsMarried = Random.Shared.Next(1, 100) % 2 == 0,
                    DateOfBirth = DateTime.Now.AddDays(Random.Shared.Next(1, 100))
                };

                await redisConnectionProvider.Connection.SetAsync(person);

                people.Add(person);

                if (setExpiration)
                {
                    var msToAllowForTesting = secondsToTest * 1000;
                    var msToCreateEveryPerson = 30_000;

                    // Create an even distribution so that people are expiring in the middle of testing
                    var expirationMilliseconds = Random.Shared.Next(msToCreateEveryPerson, msToCreateEveryPerson + msToAllowForTesting);

                    await database.KeyExpireAsync($"{nameof(Person)}:{person.Id}", TimeSpan.FromMilliseconds(expirationMilliseconds));
                }
            }

            await database.StringSetAsync(_firstPersonIdKey, people[0].Id);

            return Ok($"Created {numPeopleToCreate} people. First person ID is {people[0].Id}");
        }

        [HttpGet("GetFirstPersonEnumerated")]
        public async Task<Person?> GetFirstPersonEnumerated()
        {
            var firstPersonId = await database.StringGetAsync(_firstPersonIdKey);

            Person? firstPerson = null;
            var people = new List<Person>();
            // Retrieve from redisom
            await foreach(var person in redisConnectionProvider.RedisCollection<Person>(1000))
            {
                if (string.IsNullOrWhiteSpace(person?.Id))
                {
                    logger.LogError("Eek! This was null!");
                    throw new Exception("Eek! This was null!");
                }

                if (person.Id == firstPersonId)
                {
                    firstPerson = person;
                }

                people.Add(person);
            }

            return firstPerson;
        }

        [HttpGet("GetFirstPersonIndexed")]
        public async Task<Person?> GetFirstPersonIndexed()
        {
            var firstPersonId = await database.StringGetAsync(_firstPersonIdKey);

            return await redisConnectionProvider.Connection.GetAsync<Person>(firstPersonId!);
        }

        [HttpGet("GetByName")]
        public async Task<IEnumerable<Person>> GetByName(string name = "John")
        {
            return await redisConnectionProvider
                .RedisCollection<Person>(1000)
                .Where(person => person.Name.Contains(name))
                .ToListAsync();
        }
    }
}
