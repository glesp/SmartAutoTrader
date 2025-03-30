// Updated VehicleSeeder with realistic vehicle type and fuel mappings (fixed 'Truck' -> 'Pickup')
using SmartAutoTrader.API.Models;
using SmartAutoTrader.API.Data;

namespace SmartAutoTrader.API.DataSeeding
{
    public class VehicleSeeder
    {
        public void SeedVehicles(IServiceProvider serviceProvider, int count = 200)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                if (!context.Vehicles.Any())
                {
                    var vehicles = GenerateVehicles(count);
                    context.Vehicles.AddRange(vehicles);
                    context.SaveChanges();
                    Console.WriteLine($"Added {count} vehicles to the database.");
                }
                else
                {
                    Console.WriteLine("Database already contains vehicles. Skipping seed.");
                }
            }
        }

        private static readonly Dictionary<string, (VehicleType, FuelType)> ModelMeta = new()
        {
            ["Cybertruck"] = (VehicleType.Pickup, FuelType.Electric),
            ["Model 3"] = (VehicleType.Sedan, FuelType.Electric),
            ["Model S"] = (VehicleType.Sedan, FuelType.Electric),
            ["Model X"] = (VehicleType.SUV, FuelType.Electric),
            ["Model Y"] = (VehicleType.SUV, FuelType.Electric),
            ["F-150"] = (VehicleType.Pickup, FuelType.Petrol),
            ["RAV4"] = (VehicleType.SUV, FuelType.Hybrid),
            ["CR-V"] = (VehicleType.SUV, FuelType.Petrol),
            ["Camry"] = (VehicleType.Sedan, FuelType.Hybrid),
            ["Civic"] = (VehicleType.Sedan, FuelType.Petrol),
            ["CX-5"] = (VehicleType.SUV, FuelType.Petrol),
            ["Golf"] = (VehicleType.Hatchback, FuelType.Petrol),
            ["Altima"] = (VehicleType.Sedan, FuelType.Petrol),
            ["Leaf"] = (VehicleType.Hatchback, FuelType.Electric),
            ["Highlander"] = (VehicleType.SUV, FuelType.Hybrid),
            ["Pilot"] = (VehicleType.SUV, FuelType.Petrol),
            ["Forester"] = (VehicleType.SUV, FuelType.Petrol),
            ["X5"] = (VehicleType.SUV, FuelType.Diesel),
            ["A4"] = (VehicleType.Sedan, FuelType.Petrol),
            ["Mustang"] = (VehicleType.Coupe, FuelType.Petrol),
            ["S-Class"] = (VehicleType.Sedan, FuelType.Petrol),
        };

        private static List<Vehicle> GenerateVehicles(int count)
        {
            var random = new Random();
            var vehicles = new List<Vehicle>();

            var makes = new[] { "Toyota", "Honda", "Ford", "Volkswagen", "BMW", "Mercedes-Benz", "Audi", "Nissan", "Hyundai", "Kia", "Tesla", "Mazda", "Subaru", "Chevrolet", "Lexus" };

            var modelsByMake = new Dictionary<string, string[]>
            {
                { "Toyota", new[] { "Corolla", "Camry", "RAV4", "Prius", "Highlander", "Tacoma", "4Runner" } },
                { "Honda", new[] { "Civic", "Accord", "CR-V", "Pilot", "Fit", "HR-V", "Odyssey" } },
                { "Ford", new[] { "F-150", "Focus", "Escape", "Explorer", "Mustang", "Edge", "Ranger" } },
                { "Volkswagen", new[] { "Golf", "Passat", "Tiguan", "Atlas", "Jetta", "ID.4", "Arteon" } },
                { "BMW", new[] { "3 Series", "5 Series", "X3", "X5", "7 Series", "i4", "iX" } },
                { "Mercedes-Benz", new[] { "C-Class", "E-Class", "GLC", "GLE", "S-Class", "A-Class", "EQC" } },
                { "Audi", new[] { "A4", "A6", "Q5", "Q7", "e-tron", "A3", "Q3" } },
                { "Nissan", new[] { "Altima", "Rogue", "Sentra", "Pathfinder", "Murano", "Leaf", "Kicks" } },
                { "Hyundai", new[] { "Elantra", "Tucson", "Santa Fe", "Kona", "Sonata", "Palisade", "Ioniq" } },
                { "Kia", new[] { "Forte", "Sportage", "Sorento", "Telluride", "Soul", "Seltos", "EV6" } },
                { "Tesla", new[] { "Model 3", "Model Y", "Model S", "Model X", "Cybertruck" } },
                { "Mazda", new[] { "Mazda3", "CX-5", "CX-9", "MX-5", "CX-30", "Mazda6", "CX-50" } },
                { "Subaru", new[] { "Outback", "Forester", "Crosstrek", "Impreza", "Legacy", "Ascent", "WRX" } },
                { "Chevrolet", new[] { "Silverado", "Equinox", "Malibu", "Traverse", "Tahoe", "Bolt", "Camaro" } },
                { "Lexus", new[] { "RX", "ES", "NX", "IS", "GX", "UX", "LS" } }
            };

            var featuresList = new[] {
                "Leather Seats", "Sunroof", "Navigation System", "Bluetooth", "Backup Camera",
                "Heated Seats", "Cruise Control", "Parking Sensors", "Blind Spot Monitor",
                "Lane Departure Warning", "Keyless Entry", "Push Button Start", "Apple CarPlay",
                "Android Auto", "Premium Sound System", "Third Row Seating", "Tow Package",
                "Roof Rack", "Alloy Wheels", "Adaptive Cruise Control", "Remote Start",
                "Ventilated Seats", "Heads-up Display", "360 Camera", "Wireless Charging"
            };

            for (int i = 0; i < count; i++)
            {
                var make = makes[random.Next(makes.Length)];
                var model = modelsByMake[make][random.Next(modelsByMake[make].Length)];
                var year = random.Next(2010, 2026);
                var country = GetCountryForMake(make);

                (VehicleType vType, FuelType fType) = ModelMeta.ContainsKey(model)
                    ? ModelMeta[model]
                    : ((VehicleType)random.Next(Enum.GetValues(typeof(VehicleType)).Length),
                       (FuelType)random.Next(Enum.GetValues(typeof(FuelType)).Length));

                var vehicle = new Vehicle
                {
                    Make = make,
                    Model = model,
                    Year = year,
                    Price = random.Next(5000, 100001),
                    Mileage = year == 2025 ? random.Next(0, 1000) : random.Next(1000, 150001),
                    FuelType = fType,
                    Transmission = (TransmissionType)random.Next(Enum.GetValues(typeof(TransmissionType)).Length),
                    VehicleType = vType,
                    EngineSize = Math.Round(random.NextDouble() * 4 + 1, 1),
                    HorsePower = random.Next(100, 601),
                    Country = country,
                    Description = GenerateDescription(make, model, year),
                    DateListed = DateTime.Now.AddDays(-random.Next(1, 60)),
                    Status = GetRandomStatusWithWeights(random),
                    Images = new List<VehicleImage> {
                        new VehicleImage {
                            ImageUrl = "https://images.unsplash.com/photo-1533473359331-0135ef1b58bf?w=400&h=300&fit=crop",
                            IsPrimary = true
                        }
                    },
                    Features = featuresList.OrderBy(_ => random.Next()).Take(random.Next(3, 9))
                        .Select(f => new VehicleFeature { Name = f }).ToList(),
                    FavoritedBy = new List<Models.UserFavorite>()
                };

                vehicles.Add(vehicle);
            }

            return vehicles;
        }

        private static string GetCountryForMake(string make) => make switch
        {
            "Toyota" or "Honda" or "Nissan" or "Mazda" or "Subaru" or "Lexus" => "Japan",
            "BMW" or "Mercedes-Benz" or "Audi" or "Volkswagen" => "Germany",
            "Ford" or "Chevrolet" or "Tesla" => "USA",
            "Hyundai" or "Kia" => "South Korea",
            _ => "Other"
        };

        private static VehicleStatus GetRandomStatusWithWeights(Random random) => random.Next(100) switch
        {
            < 80 => VehicleStatus.Available,
            < 95 => VehicleStatus.Reserved,
            _ => VehicleStatus.Sold
        };

        private static string GenerateDescription(string make, string model, int year)
        {
            var descriptions = new[]
            {
                $"Excellent condition {year} {make} {model}.",
                $"Beautiful {year} {make} {model} with low mileage.",
                $"One owner {year} {make} {model}, garage kept.",
                $"Immaculate {year} {make} {model}.",
                $"Like new {year} {make} {model} with warranty.",
                $"Pristine {year} {make} {model}, adult owned.",
                $"Well-maintained {year} {make} {model}, recently serviced.",
                $"Sporty {year} {make} {model}, fun to drive.",
                $"Luxury {year} {make} {model} loaded with features.",
                $"Family-friendly {year} {make} {model} with spacious interior."
            };

            return descriptions[new Random().Next(descriptions.Length)];
        }
    }
}
