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
        IRedisConnectionProvider redisConnectionProvider,
        IDatabase database) : ControllerBase
    {
        private const string _firstPersonIdKey = "FirstPersonId";
        private const string _numPeopleCreatedKey = "NumPeopleCreated";
        private const string _testCaseKey = "TestCase";

        [HttpPost("CreateIndex")]
        public async Task<IActionResult> CreateIndex(
            TestCase testCase = TestCase.None,
            int numPeopleToCreate = 10_000,
            int secondsToTest = 60)
        {
            redisConnectionProvider.Connection.DropIndexAndAssociatedRecords(typeof(Person));
            await redisConnectionProvider.Connection.CreateIndexAsync(typeof(Person));

            await Task.WhenAll(
                database.StringSetAsync(_numPeopleCreatedKey, numPeopleToCreate),
                database.StringSetAsync(_testCaseKey, testCase.ToString()));

            var people = new List<Person>(numPeopleToCreate);
            for (var i = 0; i < numPeopleToCreate; i++)
            {
                var person = await InsertPersonAsync(redisConnectionProvider);

                people.Add(person);

                if (testCase == TestCase.ExpireSoon || testCase == TestCase.ExpireHalf && i < numPeopleToCreate / 2)
                {
                    await ExpireRandomlyInFutureAsync(database, secondsToTest, person);
                }
            }

            if (testCase == TestCase.ExpireOne)
            {
                await database.KeyExpireAsync($"{nameof(Person)}:{people[0].Id}", TimeSpan.FromSeconds(1));
            }

            await database.StringSetAsync(_firstPersonIdKey, people[0].Id);

            return Ok($"Created {numPeopleToCreate} people. First person ID is {people[0].Id}");
        }

        private static async Task<Person> InsertPersonAsync(IRedisConnectionProvider redisConnectionProvider)
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

            return person;
        }

        private static async Task ExpireRandomlyInFutureAsync(IDatabase database, int secondsAllottedForTesting, Person person)
        {
            var msToAllowForTesting = secondsAllottedForTesting * 1000;
            var msToCreateEveryPerson = 30_000;

            // Create an even distribution so that people are expiring in the middle of testing
            var expirationMilliseconds = Random.Shared.Next(msToCreateEveryPerson, msToCreateEveryPerson + msToAllowForTesting);

            await database.KeyExpireAsync($"{nameof(Person)}:{person.Id}", TimeSpan.FromMilliseconds(expirationMilliseconds));
        }

        [HttpGet("EnumeratorTest_GetFirstPerson")]
        public async Task<Person?> GetFirstPersonEnumerated(int batchSize = 2000, string? personId = null)
        {
            string idToSearch = string.IsNullOrWhiteSpace(personId) ? 
                (await database.StringGetAsync(_firstPersonIdKey))! 
                : personId;

            if(!int.TryParse(await database.StringGetAsync(_numPeopleCreatedKey), out var numPeopleCreated))
            {
                numPeopleCreated = 0;
            }

            if(!Enum.TryParse<TestCase>(await database.StringGetAsync(_testCaseKey), out var testCase))
            {
                testCase = TestCase.None;
            }

            Person? firstPerson = null;
            var people = new List<Person>();
            // Retrieve from redisom
            await foreach(var person in redisConnectionProvider.RedisCollection<Person>(batchSize))
            {
                if (string.IsNullOrWhiteSpace(person?.Id))
                {
                    throw new Exception("Eek! This was null!");
                }

                if (person.Id == idToSearch)
                {
                    firstPerson = person;
                }

                people.Add(person);
            }

            if(testCase == TestCase.None && people.Count < numPeopleCreated)
            {
                throw new Exception($"{TestCase.None}: Got less people than expected.");
            }
            else if (testCase == TestCase.ExpireOne && people.Count < numPeopleCreated - 1)
            {
                throw new Exception($"{TestCase.ExpireOne}: Got less people than expected.");
            }
            else if (testCase == TestCase.ExpireHalf && people.Count < numPeopleCreated / 2 - 1)
            {
                throw new Exception($"{TestCase.ExpireHalf}: Got less people than expected {people.Count}.");
            }
            else if (testCase == TestCase.ExpireSoon && people.Count < numPeopleCreated / 2)
            {
                throw new Exception($"{TestCase.ExpireSoon}: Got less people than expected {people.Count}.");
            }

            return firstPerson;
        }

        [HttpGet("GetFirstPersonIndexed")]
        public async Task<Person?> GetFirstPersonIndexed()
        {
            string firstPersonId = (await database.StringGetAsync(_firstPersonIdKey))!;

            return await redisConnectionProvider.RedisCollection<Person>().FirstAsync(Person => Person.Id == firstPersonId);
        }

        [HttpGet("GetPersonById")]
        public Task<Person?> GetPersonById(string id)
        {
            return redisConnectionProvider.RedisCollection<Person>().FirstOrDefaultAsync(Person => Person.Id == id);
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
