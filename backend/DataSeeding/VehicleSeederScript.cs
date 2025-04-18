// <copyright file="VehicleSeederScript.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SmartAutoTrader.API.DataSeeding
{
    using System.Globalization;
    using SmartAutoTrader.API.Data;
    using SmartAutoTrader.API.Enums;
    using SmartAutoTrader.API.Models;

    public class VehicleSeeder
    {
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

        private static readonly string[] Value =
            ["Corolla", "Camry", "RAV4", "Prius", "Highlander", "Tacoma", "4Runner"];

        private static readonly string[] ValueArray = ["Civic", "Accord", "CR-V", "Pilot", "Fit", "HR-V", "Odyssey"];

        private static readonly string[] ValueValue =
            ["F-150", "Focus", "Escape", "Explorer", "Mustang", "Edge", "Ranger"];

        private static readonly string[] Value1 = ["Golf", "Passat", "Tiguan", "Atlas", "Jetta", "ID.4", "Arteon"];
        private static readonly string[] Value2 = ["3 Series", "5 Series", "X3", "X5", "7 Series", "i4", "iX"];

        public void SeedVehicles(IServiceProvider serviceProvider, int count = 200)
        {
            using IServiceScope scope = serviceProvider.CreateScope();
            ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            if (!context.Vehicles.Any())
            {
                List<Vehicle> vehicles = GenerateVehicles(count);
                context.Vehicles.AddRange(vehicles);
                _ = context.SaveChanges();
                Console.WriteLine($"Added {count} vehicles to the database.");
            }
            else
            {
                Console.WriteLine("Database already contains vehicles. Skipping seed.");
            }
        }

        private static List<Vehicle> GenerateVehicles(int count)
        {
            Random random = new();
            List<Vehicle> vehicles = [];

            string[] makes =
            [
                "Toyota",
                "Honda",
                "Ford",
                "Volkswagen",
                "BMW",
                "Mercedes-Benz",
                "Audi",
                "Nissan",
                "Hyundai",
                "Kia",
                "Tesla",
                "Mazda",
                "Subaru",
                "Chevrolet",
                "Lexus"
            ];

            Dictionary<string, string[]> modelsByMake = new()
            {
                { "Toyota", Value },
                { "Honda", ValueArray },
                { "Ford", ValueValue },
                { "Volkswagen", Value1 },
                { "BMW", Value2 },
                { "Mercedes-Benz", new[] { "C-Class", "E-Class", "GLC", "GLE", "S-Class", "A-Class", "EQC" } },
                { "Audi", new[] { "A4", "A6", "Q5", "Q7", "e-tron", "A3", "Q3" } },
                { "Nissan", new[] { "Altima", "Rogue", "Sentra", "Pathfinder", "Murano", "Leaf", "Kicks" } },
                { "Hyundai", new[] { "Elantra", "Tucson", "Santa Fe", "Kona", "Sonata", "Palisade", "Ioniq" } },
                { "Kia", new[] { "Forte", "Sportage", "Sorento", "Telluride", "Soul", "Seltos", "EV6" } },
                { "Tesla", new[] { "Model 3", "Model Y", "Model S", "Model X", "Cybertruck" } },
                { "Mazda", new[] { "Mazda3", "CX-5", "CX-9", "MX-5", "CX-30", "Mazda6", "CX-50" } },
                { "Subaru", new[] { "Outback", "Forester", "Crosstrek", "Impreza", "Legacy", "Ascent", "WRX" } },
                { "Chevrolet", new[] { "Silverado", "Equinox", "Malibu", "Traverse", "Tahoe", "Bolt", "Camaro" } },
                { "Lexus", new[] { "RX", "ES", "NX", "IS", "GX", "UX", "LS" } },
            };

            string[] featuresList =
            [
                "Leather Seats",
                "Sunroof",
                "Navigation System",
                "Bluetooth",
                "Backup Camera",
                "Heated Seats",
                "Cruise Control",
                "Parking Sensors",
                "Blind Spot Monitor",
                "Lane Departure Warning",
                "Keyless Entry",
                "Push Button Start",
                "Apple CarPlay",
                "Android Auto",
                "Premium Sound System",
                "Third Row Seating",
                "Tow Package",
                "Roof Rack",
                "Alloy Wheels",
                "Adaptive Cruise Control",
                "Remote Start",
                "Ventilated Seats",
                "Heads-up Display",
                "360 Camera",
                "Wireless Charging"
            ];

            for (int i = 0; i < count; i++)
            {
                string make = makes[random.Next(makes.Length)];
                string model = modelsByMake[make][random.Next(modelsByMake[make].Length)];
                int year = random.Next(2010, 2026);
                string country = GetCountryForMake(make);

                (VehicleType vType, FuelType fType) = ModelMeta.TryGetValue(model, out (VehicleType, FuelType) value)
                    ? value
                    : ((VehicleType)random.Next(Enum.GetValues(typeof(VehicleType)).Length),
                        (FuelType)random.Next(Enum.GetValues(typeof(FuelType)).Length));

                string slug = $"{make}-{model}".Replace(" ", string.Empty).ToLower(CultureInfo.CurrentCulture);
                string imageFileName = $"{slug}.jpg";

                Vehicle vehicle = new()
                {
                    Make = make,
                    Model = model,
                    Year = year,
                    Price = random.Next(5000, 100001),
                    Mileage = year == 2025 ? random.Next(0, 1000) : random.Next(1000, 150001),
                    FuelType = fType,
                    Transmission = (TransmissionType)random.Next(Enum.GetValues(typeof(TransmissionType)).Length),
                    VehicleType = vType,
                    EngineSize = Math.Round((random.NextDouble() * 4) + 1, 1),
                    HorsePower = random.Next(100, 601),
                    Country = country,
                    Description = GenerateDescription(make, model, year),
                    DateListed = DateTime.Now.AddDays(-random.Next(1, 60)),
                    Status = GetRandomStatusWithWeights(random),
                    Images = new List<VehicleImage>
                    {
                        new()
                        {
                            ImageUrl = $"images/vehicles/{imageFileName}",
                            IsPrimary = true,
                        },
                    },
                    Features = featuresList.OrderBy(_ => random.Next()).Take(random.Next(3, 9))
                        .Select(f => new VehicleFeature { Name = f }).ToList(),
                    FavoritedBy = new List<UserFavorite>(),
                };

                vehicles.Add(vehicle);
            }

            return vehicles;
        }

        private static string GetCountryForMake(string make)
        {
            return make switch
            {
                "Toyota" or "Honda" or "Nissan" or "Mazda" or "Subaru" or "Lexus" => "Japan",
                "BMW" or "Mercedes-Benz" or "Audi" or "Volkswagen" => "Germany",
                "Ford" or "Chevrolet" or "Tesla" => "USA",
                "Hyundai" or "Kia" => "South Korea",
                _ => "Other",
            };
        }

        private static VehicleStatus GetRandomStatusWithWeights(Random random)
        {
            return random.Next(100) switch
            {
                < 80 => VehicleStatus.Available,
                < 95 => VehicleStatus.Reserved,
                _ => VehicleStatus.Sold,
            };
        }

        private static string GenerateDescription(string make, string model, int year)
        {
            string[] descriptions =
            [
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
            ];

            return descriptions[new Random().Next(descriptions.Length)];
        }
    }
}