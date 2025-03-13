using SmartAutoTrader.API.Models; // Updated to match your namespace
using SmartAutoTrader.API.Data; // Assuming this is where your DbContext is

namespace SmartAutoTrader.API.DataSeeding
{
    public class VehicleSeeder
    {
        public void SeedVehicles(IServiceProvider serviceProvider, int count = 200)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(); // Replace with your actual DbContext name

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

        private static List<Vehicle> GenerateVehicles(int count)
        {
            var random = new Random();
            var vehicles = new List<Vehicle>();

            // Lists for random data generation
            var makes = new[] { "Toyota", "Honda", "Ford", "Volkswagen", "BMW", "Mercedes-Benz", "Audi", "Nissan", "Hyundai", "Kia", "Tesla", "Mazda", "Subaru", "Chevrolet", "Lexus" };
            var colors = new[] { "Black", "White", "Silver", "Grey", "Blue", "Red", "Green", "Yellow", "Brown", "Orange" };
            var countries = new[] { "Japan", "Germany", "USA", "South Korea", "Italy", "France", "Sweden", "UK", "China" };

            // Model mappings
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

            // Common features
            var featuresList = new[]
            {
                "Leather Seats", "Sunroof", "Navigation System", "Bluetooth", "Backup Camera",
                "Heated Seats", "Cruise Control", "Parking Sensors", "Blind Spot Monitor",
                "Lane Departure Warning", "Keyless Entry", "Push Button Start", "Apple CarPlay",
                "Android Auto", "Premium Sound System", "Third Row Seating", "Tow Package",
                "Roof Rack", "Alloy Wheels", "Adaptive Cruise Control", "Remote Start",
                "Ventilated Seats", "Heads-up Display", "360 Camera", "Wireless Charging"
            };

            // Generate vehicles
            for (int i = 0; i < count; i++)
            {
                var make = makes[random.Next(makes.Length)];
                var models = modelsByMake[make];
                var model = models[random.Next(models.Length)];
                var year = random.Next(2010, 2026);
                var country = GetCountryForMake(make);

                // Create vehicle
                var vehicle = new Vehicle
                {
                    Make = make,
                    Model = model,
                    Year = year,
                    Price = random.Next(5000, 100001), // $5,000 to $100,000
                    Mileage = year == 2025 ? random.Next(0, 1000) : random.Next(1000, 150001), // Low mileage for new cars
                    FuelType = (FuelType)random.Next(Enum.GetValues(typeof(FuelType)).Length),
                    Transmission = (TransmissionType)random.Next(Enum.GetValues(typeof(TransmissionType)).Length),
                    VehicleType = (VehicleType)random.Next(Enum.GetValues(typeof(VehicleType)).Length),
                    EngineSize = Math.Round(random.NextDouble() * 4 + 1, 1), // 1.0L to 5.0L
                    HorsePower = random.Next(100, 601), // 100hp to 600hp
                    Country = country,
                    Description = GenerateDescription(make, model, year),
                    DateListed = DateTime.Now.AddDays(-random.Next(1, 60)), // Random listing date within last 60 days
                    Status = GetRandomStatusWithWeights(random),
                    // Initialize collections
                    Images = new List<VehicleImage>(),
                    Features = new List<VehicleFeature>(),
                    FavoritedBy = new List<Models.UserFavorite>()
                };

                // Add images
                int imageCount = random.Next(1, 6); // 1-5 images per vehicle
                for (int j = 0; j < imageCount; j++)
                {
                    int imageId = random.Next(1, 20); // Assuming you have 20 placeholder images
                    vehicle.Images.Add(new VehicleImage
                    {
                        ImageUrl = $"https://placeholder.com/vehicles/{make.ToLower()}-{model.ToLower().Replace(" ", "-")}-{imageId}.jpg",
                        IsPrimary = j == 0, // First image is primary
                        Vehicle = vehicle
                    });
                }

                // Add features (3-8 random features)
                var featureCount = random.Next(3, 9);
                var selectedFeatures = featuresList.OrderBy(x => random.Next()).Take(featureCount);
                foreach (var feature in selectedFeatures)
                {
                    vehicle.Features.Add(new VehicleFeature
                    {
                        Name = feature,
                        Vehicle = vehicle
                    });
                }

                vehicles.Add(vehicle);
            }

            return vehicles;
        }

        private static string GetCountryForMake(string make)
        {
            // Return appropriate country of origin for each make
            return make switch
            {
                "Toyota" or "Honda" or "Nissan" or "Mazda" or "Subaru" or "Lexus" => "Japan",
                "BMW" or "Mercedes-Benz" or "Audi" or "Volkswagen" => "Germany",
                "Ford" or "Chevrolet" or "Tesla" => "USA",
                "Hyundai" or "Kia" => "South Korea",
                _ => "Other"
            };
        }

        private static VehicleStatus GetRandomStatusWithWeights(Random random)
        {
            // Weight probabilities: 80% Available, 15% Reserved, 5% Sold
            int value = random.Next(1, 101);
            if (value <= 80)
                return VehicleStatus.Available;
            else if (value <= 95)
                return VehicleStatus.Reserved;
            else
                return VehicleStatus.Sold;
        }

        private static string GenerateDescription(string make, string model, int year)
        {
            var random = new Random();
            var descriptions = new[]
            {
                $"Excellent condition {year} {make} {model}. Well maintained with service history available. Must see to appreciate.",
                $"Beautiful {year} {make} {model} with low mileage. No accidents, clean title.",
                $"One owner {year} {make} {model}. Garage kept and regularly serviced at dealership.",
                $"Immaculate {year} {make} {model}. All highway miles, never been in an accident.",
                $"Like new {year} {make} {model}. Still under manufacturer warranty with all service up to date.",
                $"Pristine {year} {make} {model}. Adult owned and driven, smoke-free interior.",
                $"Well-maintained {year} {make} {model}. Recent service completed, ready for new owner.",
                $"Sporty {year} {make} {model} in great condition. Fun to drive with excellent fuel economy.",
                $"Luxury {year} {make} {model} loaded with features. Premium package included.",
                $"Family-friendly {year} {make} {model} with spacious interior and excellent safety ratings."
            };

            return descriptions[random.Next(descriptions.Length)];
        }
    }
    
}
