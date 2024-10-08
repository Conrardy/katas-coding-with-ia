
# Train Office Backend C# Exercise

This exercise focuses on implementing a backend system for a train office using C#. The main task is to add a query to retrieve the list of available trains for booking seats.

## Project Structure - AI tips

To get a better understanding of the project structure, you can refer to the `project_structure.txt` file. This file is automatically generated using the `export-files-structure.ps1` script and provides an overview of the project's file and directory layout.

The `project_structure.txt` file can be particularly helpful when you need additional context about the project's organization or when working with AI assistants that require information about the codebase structure.

To avoid running the script manually, it is run each time TrainOffice.csproj is built:

```c#
  <Target Name="RunExportScript" AfterTargets="AfterBuild">
    <Exec Command="powershell -ExecutionPolicy Bypass -File &quot;$(ProjectDir)\..\..\export-files-structure.ps1&quot;" />
  </Target>
```

## Exercise Overview

1. Create models for Train, Coach, and Seat entities.
2. Add a GetTrains class as a use case that use ApplicationDbContext to return a projection of a Trains with coach and seats into a GetTrainsDto
3. Add the integration tests using InMemoryDb
4. Add Controllers
5. Add HTTP tests
6. Add migrations to run it in a real database
7. Manually test the API

## Example of flow "Implementing the Query GetTrains"

To add a query for getting the list of available trains, follow these steps:

1. Create a `Train` `Coach` `CoachType` `Seat` model:

    ```c#
    public class Train
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string DepartureStation { get; set; }
        public string ArrivalStation { get; set; }
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public List<Coach> Coaches { get; set; }
    }

    public class Coach
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public CoachType Type { get; set; }
        public int TrainId { get; set; }
        public Train Train { get; set; }
        public List<Seat> Seats { get; set; }
    }

    public enum CoachType
    {
        FirstClass,
        SecondClass,
        DiningCar
    }

    public class Seat
    {
        public int Id { get; set; }
        public string SeatNumber { get; set; }
        public bool IsOccupied { get; set; }
        public int CoachId { get; set; }
        public Coach Coach { get; set; }
    }
    ```

    Add DbSet of Trains in `ApplicationDbContext` :

    ```c#
    public DbSet<Train> Trains { get; set; }
    ```

2. Create a `GetTrains` class with `IGetTrainsQuery` interface. `GetTrains` class use `ApplicationDbContext` to return a projection of `Trains` with `Coach` and `Seats` into a `GetTrainsDto`:

    ```c#
    public interface IGetTrainsQuery
    {
        Task<List<GetTrainsDto>> Execute();
    }

    public class GetTrains : IGetTrainsQuery
    {
        private readonly ApplicationDbContext _context;

        public GetTrains(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<GetTrainsDto>> Execute()
        {
            return await _context.Trains
                .Include(t => t.Coaches)
                .ThenInclude(c => c.Seats)
                .Select(t => new GetTrainsDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    DepartureStation = t.DepartureStation,
                    ArrivalStation = t.ArrivalStation,
                    DepartureTime = t.DepartureTime,
                    ArrivalTime = t.ArrivalTime,
                    Coaches = t.Coaches.Select(c => new GetTrainsDto.CoachDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Type = c.Type,
                        Seats = c.Seats.Select(s => new GetTrainsDto.SeatDto
                        {
                            Id = s.Id,
                            SeatNumber = s.SeatNumber,
                            IsOccupied = s.IsOccupied
                        }).ToList()
                    }).ToList()
                })
                .ToListAsync();
        }
    }

    public class GetTrainsDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string DepartureStation { get; set; }
        public string ArrivalStation { get; set; }
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public List<CoachDto> Coaches { get; set; }

        public class CoachDto
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public CoachType Type { get; set; }
            public List<SeatDto> Seats { get; set; }
        }

        public class SeatDto
        {
            public int Id { get; set; }
            public string SeatNumber { get; set; }
            public bool IsOccupied { get; set; }
        }
    }
    ```

    Add IGetTrains and GetTrains to the services in ConfigureApplications.cs :

    ```c#
    public static class ConfigureApplications
    {
        public static void AddApplications(this IServiceCollection services)
        {
            // other services

            services.AddScoped<IGetTrainsQuery, GetTrains>();
        }
    }
    ```

3. Add integration tests using `InMemoryDb`:

    ```c#
    [TestClass]
    public class GetTrainsTests
    {
        [TestMethod]
        public async Task GetTrains_ReturnsListOfTrainsWithCoachesAndSeats()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "GetTrains_ReturnsListOfTrainsWithCoachesAndSeats")
                .Options;

            using (var context = new ApplicationDbContext(options))
            {
                context.Trains.Add(new Train
                {
                    Name = "Train 1",
                    DepartureStation = "Station 1",
                    ArrivalStation = "Station 2",
                    DepartureTime = DateTime.Now,
                    ArrivalTime = DateTime.Now.AddHours(1),
                    Coaches = new List<Coach>
                    {
                        new Coach
                        {
                            Name = "Coach 1",
                            Type = CoachType.FirstClass,
                            Seats = new List<Seat>
                            {
                                new Seat
                                {
                                    SeatNumber = "1A",
                                    IsOccupied = false
                                },
                                new Seat
                                {
                                    SeatNumber = "1B",
                                    IsOccupied = false
                                }
                            }
                        }
                    }
                });
                context.SaveChanges();
            }

            using (var context = new ApplicationDbContext(options))
            {
                var getTrains = new GetTrains(context);
                var trains = await getTrains.Execute();

                Assert.AreEqual(1, trains.Count);
                Assert.AreEqual("Train 1", trains[0].Name);
                Assert.AreEqual("Station 1", trains[0].DepartureStation);
                Assert.AreEqual("Station 2", trains[0].ArrivalStation);
                Assert.AreEqual(1, trains[0].Coaches.Count);
                Assert.AreEqual("Coach 1", trains[0].Coaches[0].Name);
                Assert.AreEqual(CoachType.FirstClass, trains[0].Coaches[0].Type);
                Assert.AreEqual(2, trains[0].Coaches[0].Seats.Count);
                Assert.AreEqual("1A", trains[0].Coaches[0].Seats[0].SeatNumber);
                Assert.IsFalse(trains[0].Coaches[0].Seats[0].IsOccupied);
                Assert.AreEqual("1B", trains[0].Coaches[0].Seats[1].SeatNumber);
                Assert.IsFalse(trains[0].Coaches[0].Seats[1].IsOccupied);
            }
        }
    }
    ```

4. Add controllers and use `IGetTrains` in constructor :

    ```c#
    [Route("api/[controller]")]
    [ApiController]
    public class TrainsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TrainsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet(Name = "GetTrains")]
        public async Task<ActionResult<List<GetTrainsDto>>> GetTrains()
        {
            var getTrains = new GetTrains(_context);
            var trains = await getTrains.Execute();

            return trains;
        }
    }
    ```

5. Add Http tests `TrainsControllerTests` and seed data with a `TrainsControllerWebFactory`:

    ```c#
    public class TrainsControllerWebFactory : BaseWebApplicationFactory<Program>
    {
        protected override void SeedData(ApplicationDbContext context)
        {
            base.SeedData(context);

            var trains = new List<Train>
            {
                new Train
                {
                    Name = "Train 1",
                    DepartureStation = "Station 1",
                    ArrivalStation = "Station 2",
                    DepartureTime = DateTime.Now,
                    ArrivalTime = DateTime.Now.AddHours(1),
                    Coaches = new List<Coach>
                    {
                        new Coach
                        {
                            Name = "Coach 1",
                            Type = CoachType.FirstClass,
                            Seats = new List<Seat>
                            {
                                new Seat
                                {
                                    SeatNumber = "1A",
                                    IsOccupied = false
                                },
                                new Seat
                                {
                                    SeatNumber = "1B",
                                    IsOccupied = false
                                }
                            }
                        }
                    }
                }
            };

            context.Trains.AddRange(trains);
            context.SaveChanges();
        }
    }

    public class TrainsControllerTests : IClassFixture<TrainsControllerWebFactory>
    {
        private readonly TrainsControllerWebFactory factory;
        private readonly LinkGenerator linkGenerator;

        public TrainsControllerTests(TrainsControllerWebFactory factory)
        {
            this.factory = factory;
            this.linkGenerator = factory.Services.GetRequiredService<LinkGenerator>();
        }

        [Fact]
        public async Task GetTrains_ReturnsOkResponseWithTrainsList()
        {
            // Arrange
            var client = factory.CreateClient();
            var url = linkGenerator.GetPathByName("GetTrains", values: null);

            // Act
            var response = await client.GetAsync(url);

            // Assert
            response.EnsureSuccessStatusCode(); // Status Code 200-299
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<IEnumerable<GetTrainsDto>>>();
            Assert.NotNull(apiResponse);
            Assert.NotNull(apiResponse.Data);
            Assert.NotEmpty(apiResponse.Data);
        }
    }
    ```

6. Add a migration to run it in a real database:

    update setting DatabaseProvider to SqlServer in appsettings.json then run command to create migration and seed data:

    ```sh
    dotnet ef migrations add CreateTrainsCoachSeatTable
    dotnet ef migrations add SeedFakeTrainsData
    ```

    Complete the seed migration:

    ```c#
    public partial class SeedFakeTrainsData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add fake data of trains
            migrationBuilder.InsertData(
                table: "Trains",
                columns: new[] { "Name", "DepartureStation", "ArrivalStation", "DepartureTime", "ArrivalTime" },
                values: new object[,]
                {
                    { "Train 1", "Station 1", "Station 2", DateTime.UtcNow, DateTime.UtcNow.AddHours(1) },
                    { "Train 2", "Station 2", "Station 3", DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddHours(2) }
                }
            );

            // Add fake data of coaches
            migrationBuilder.InsertData(
                table: "Coach",
                columns: new[] { "Name", "Type", "TrainId" },
                values: new object[,]
                {
                    { "Coach 1", 1, 1 },
                    { "Coach 2", 2, 1 },
                    { "Coach 3", 1, 2 }
                }
            );

            // Add fake data of seats
            migrationBuilder.InsertData(
                table: "Seat",
                columns: new[] { "SeatNumber", "IsOccupied", "CoachId" },
                values: new object[,]
                {
                    { "1A", false, 1 },
                    { "1B", true, 1 },
                    { "2A", false, 2 },
                    { "2B", true, 2 },
                    { "3A", false, 3 },
                    { "3B", true, 3 }
                }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Seat",
                keyColumn: "Id",
                keyValues: new object[] { 1, 2, 3, 4, 5, 6 }
            );

            migrationBuilder.DeleteData(
                table: "Coach",
                keyColumn: "Id",
                keyValues: new object[] { 1, 2, 3 }
            );

            migrationBuilder.DeleteData(
                table: "Trains",
                keyColumn: "Id",
                keyValues: new object[] { 1, 2 }
            );
        }
    }
    ```

    and update the database:

    ```sh
    dotnet ef database update
    ```

7. Manually test the API:

    Run the application and using the swagger test 'api/GetTrains' to see the list of available trains with coaches and seats.

    ```json
    {
    "Data": [
        {
        "id": 1,
        "name": "Train 1",
        "departureStation": "Station 1",
        "arrivalStation": "Station 2",
        "departureTime": "2024-09-29T15:11:33.62974Z",
        "arrivalTime": "2024-09-29T16:11:33.62974Z",
        "coaches": [
            {
            "id": 1,
            "name": "Coach 1",
            "type": 1,
            "seats": [
                {
                "id": 1,
                "seatNumber": "1A",
                "isOccupied": false
                },
                {
                "id": 2,
                "seatNumber": "1B",
                "isOccupied": true
                }
            ]
            },
            {
            "id": 2,
            "name": "Coach 2",
            "type": 2,
            "seats": [
                {
                "id": 3,
                "seatNumber": "2A",
                "isOccupied": false
                },
                {
                "id": 4,
                "seatNumber": "2B",
                "isOccupied": true
                }
            ]
            }
        ]
        },
        {
        "id": 2,
        "name": "Train 2",
        "departureStation": "Station 2",
        "arrivalStation": "Station 3",
        "departureTime": "2024-09-29T16:11:33.629742Z",
        "arrivalTime": "2024-09-29T17:11:33.629742Z",
        "coaches": [
            {
            "id": 3,
            "name": "Coach 3",
            "type": 1,
            "seats": [
                {
                "id": 5,
                "seatNumber": "3A",
                "isOccupied": false
                },
                {
                "id": 6,
                "seatNumber": "3B",
                "isOccupied": true
                }
            ]
            }
        ]
        }
    ],
    "Errors": null,
    "Meta": null
    }
    ```
