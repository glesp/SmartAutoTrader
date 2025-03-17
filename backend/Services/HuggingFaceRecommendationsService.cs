using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartAutoTrader.API.Data;
using SmartAutoTrader.API.Models;

namespace SmartAutoTrader.API.Services
{
    public class HuggingFaceRecommendationService : IAIRecommendationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<HuggingFaceRecommendationService> _logger;
        private readonly HttpClient _httpClient;
        
        public HuggingFaceRecommendationService(
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<HuggingFaceRecommendationService> logger,
            HttpClient httpClient)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClient;
        }
        
        public async Task<IEnumerable<Vehicle>> GetRecommendationsAsync(int userId, RecommendationParameters parameters)
        {
            try
            {
                // 1. Get user data
                var user = await _context.Users
                    .Include(u => u.Preferences)
                    .Include(u => u.Favorites)
                        .ThenInclude(f => f.Vehicle)
                    .Include(u => u.BrowsingHistory.OrderByDescending(h => h.ViewDate).Take(10))
                        .ThenInclude(h => h.Vehicle)
                    .FirstOrDefaultAsync(u => u.Id == userId);
                
                if (user == null)
                {
                    _logger.LogWarning($"User with ID {userId} not found for recommendations");
                    return new List<Vehicle>();
                }
                
                // 2. Get available vehicles that match basic criteria
                var availableVehicles = await GetFilteredVehiclesAsync(parameters);
                
                if (!availableVehicles.Any())
                {
                    return new List<Vehicle>();
                }
                
                // 3. Generate embeddings for user preferences and history
                var userEmbedding = await GenerateUserEmbeddingAsync(user, parameters);
                
                // 4. Generate embeddings for vehicles
                var vehicleEmbeddings = await GenerateVehicleEmbeddingsAsync(availableVehicles);
                
                // 5. Calculate similarity and rank vehicles
                var recommendedVehicleIds = RankVehiclesBySimilarity(
                    userEmbedding, 
                    vehicleEmbeddings, 
                    availableVehicles.Select(v => v.Id).ToList(),
                    parameters.MaxResults ?? 5);
                
                // 6. Get recommended vehicles with details
                var recommendedVehicles = await _context.Vehicles
                    .Where(v => recommendedVehicleIds.Contains(v.Id))
                    .Include(v => v.Images)
                    .Include(v => v.Features)
                    .ToListAsync();
                
                // 7. Add fallback recommendations if needed
                if (recommendedVehicles.Count < (parameters.MaxResults ?? 5))
                {
                    var additionalVehicles = await GetFallbackRecommendationsAsync(
                        userId,
                        parameters,
                        recommendedVehicles.Select(v => v.Id).ToList(),
                        (parameters.MaxResults ?? 5) - recommendedVehicles.Count);
                    
                    recommendedVehicles.AddRange(additionalVehicles);
                }
                
                return recommendedVehicles;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Hugging Face recommendations");
                return await GetFallbackRecommendationsAsync(userId, parameters, new List<int>(), parameters.MaxResults ?? 5);
            }
        }
        
        private async Task<List<Vehicle>> GetFilteredVehiclesAsync(RecommendationParameters parameters)
        {
            var query = _context.Vehicles.AsQueryable();
            
            // Apply basic filters from parameters
            if (parameters.MinPrice.HasValue)
                query = query.Where(v => v.Price >= parameters.MinPrice.Value);
                
            if (parameters.MaxPrice.HasValue)
                query = query.Where(v => v.Price <= parameters.MaxPrice.Value);
                
            if (parameters.MinYear.HasValue)
                query = query.Where(v => v.Year >= parameters.MinYear.Value);
                
            if (parameters.MaxYear.HasValue)
                query = query.Where(v => v.Year <= parameters.MaxYear.Value);
                
            if (parameters.PreferredFuelTypes?.Any() == true)
                query = query.Where(v => parameters.PreferredFuelTypes.Contains(v.FuelType));
                
            if (parameters.PreferredVehicleTypes?.Any() == true)
                query = query.Where(v => parameters.PreferredVehicleTypes.Contains(v.VehicleType));
                
            if (parameters.PreferredMakes?.Any() == true)
                query = query.Where(v => parameters.PreferredMakes.Contains(v.Make));
            
            // Only available vehicles
            query = query.Where(v => v.Status == VehicleStatus.Available);
            
            // Include features for more detailed filtering
            return await query
                .Include(v => v.Features)
                .ToListAsync();
        }
        
        private async Task<float[]> GenerateUserEmbeddingAsync(User user, RecommendationParameters parameters)
        {
            // Prepare text that represents the user preferences and history
            var userText = new StringBuilder();
            
            // Add explicit parameters
            userText.AppendLine("User is looking for a car with these preferences:");
            if (parameters.MinPrice.HasValue) userText.AppendLine($"Minimum price: {parameters.MinPrice:C0}");
            if (parameters.MaxPrice.HasValue) userText.AppendLine($"Maximum price: {parameters.MaxPrice:C0}");
            if (parameters.MinYear.HasValue) userText.AppendLine($"Minimum year: {parameters.MinYear}");
            if (parameters.MaxYear.HasValue) userText.AppendLine($"Maximum year: {parameters.MaxYear}");
            
            if (parameters.PreferredFuelTypes?.Any() == true)
                userText.AppendLine($"Fuel types: {string.Join(", ", parameters.PreferredFuelTypes)}");
                
            if (parameters.PreferredVehicleTypes?.Any() == true)
                userText.AppendLine($"Vehicle types: {string.Join(", ", parameters.PreferredVehicleTypes)}");
                
            if (parameters.PreferredMakes?.Any() == true)
                userText.AppendLine($"Makes: {string.Join(", ", parameters.PreferredMakes)}");
                
            if (parameters.DesiredFeatures?.Any() == true)
                userText.AppendLine($"Desired features: {string.Join(", ", parameters.DesiredFeatures)}");
            
            // Add browsing history
            var recentlyViewed = user.BrowsingHistory?.OrderByDescending(h => h.ViewDate).Take(5).ToList();
            if (recentlyViewed?.Any() == true)
            {
                userText.AppendLine("\nUser recently viewed these vehicles:");
                foreach (var history in recentlyViewed)
                {
                    userText.AppendLine($"{history.Vehicle.Year} {history.Vehicle.Make} {history.Vehicle.Model}, " +
                                       $"Price: {history.Vehicle.Price:C0}, Type: {history.Vehicle.VehicleType}, " +
                                       $"Fuel: {history.Vehicle.FuelType}, View duration: {history.ViewDurationSeconds}s");
                }
            }
            
            // Add favorites
            var favorites = user.Favorites?.Select(f => f.Vehicle).ToList();
            if (favorites?.Any() == true)
            {
                userText.AppendLine("\nUser favorited these vehicles:");
                foreach (var favorite in favorites)
                {
                    userText.AppendLine($"{favorite.Year} {favorite.Make} {favorite.Model}, " +
                                       $"Price: {favorite.Price:C0}, Type: {favorite.VehicleType}, " +
                                       $"Fuel: {favorite.FuelType}");
                }
            }
            
            // Get user preferences
            if (user.Preferences?.Any() == true)
            {
                userText.AppendLine("\nUser has these saved preferences:");
                foreach (var pref in user.Preferences)
                {
                    userText.AppendLine($"{pref.PreferenceType}: {pref.Value} (Weight: {pref.Weight})");
                }
            }
            
            // Generate embedding via Hugging Face
            return await GetEmbeddingFromHuggingFaceAsync(userText.ToString());
        }
        
        private async Task<Dictionary<int, float[]>> GenerateVehicleEmbeddingsAsync(List<Vehicle> vehicles)
        {
            var embeddings = new Dictionary<int, float[]>();
            
            // Process in batches to avoid rate limits
            int batchSize = 10;
            for (int i = 0; i < vehicles.Count; i += batchSize)
            {
                var batch = vehicles.Skip(i).Take(batchSize).ToList();
                var tasks = batch.Select(vehicle => GetVehicleEmbedding(vehicle)).ToList();
                var results = await Task.WhenAll(tasks);
                
                for (int j = 0; j < batch.Count; j++)
                {
                    embeddings[batch[j].Id] = results[j];
                }
            }
            
            return embeddings;
        }
        
        private async Task<float[]> GetVehicleEmbedding(Vehicle vehicle)
        {
            // Generate a text description of the vehicle
            var vehicleText = new StringBuilder();
            
            vehicleText.AppendLine($"{vehicle.Year} {vehicle.Make} {vehicle.Model}");
            vehicleText.AppendLine($"Price: {vehicle.Price:C0}");
            vehicleText.AppendLine($"Mileage: {vehicle.Mileage} miles");
            vehicleText.AppendLine($"Type: {vehicle.VehicleType}");
            vehicleText.AppendLine($"Fuel: {vehicle.FuelType}");
            vehicleText.AppendLine($"Transmission: {vehicle.Transmission}");
            vehicleText.AppendLine($"Engine: {vehicle.EngineSize}L, {vehicle.HorsePower}hp");
            vehicleText.AppendLine($"Country: {vehicle.Country}");
            vehicleText.AppendLine(vehicle.Description);
            
            // Add features
            if (vehicle.Features?.Any() == true)
            {
                vehicleText.AppendLine("Features: " + 
                    string.Join(", ", vehicle.Features.Select(f => f.Name)));
            }
            
            // Generate embedding via Hugging Face
            return await GetEmbeddingFromHuggingFaceAsync(vehicleText.ToString());
        }
        
        private async Task<float[]> GetEmbeddingFromHuggingFaceAsync(string text)
        {
            try
            {
                // Get Hugging Face API configuration
                var apiKey = _configuration["HuggingFace:ApiKey"];
                var modelId = _configuration["HuggingFace:EmbeddingModel"] ?? 
                    "sentence-transformers/all-MiniLM-L6-v2"; // Small, efficient model
                
                // Create request
                var request = new
                {
                    inputs = text,
                    options = new { wait_for_model = true }
                };
                
                var content = new StringContent(
                    JsonSerializer.Serialize(request),
                    Encoding.UTF8,
                    "application/json");
                
                // Set up headers
                _httpClient.DefaultRequestHeaders.Clear();
                if (!string.IsNullOrEmpty(apiKey))
                {
                    _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                }
                
                // Call Hugging Face API
                var response = await _httpClient.PostAsync(
                    $"https://api-inference.huggingface.co/models/{modelId}",
                    content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Hugging Face API error: {response.StatusCode}, {errorContent}");
                    return new float[384]; // Default embedding size for the model
                }
                
                // Parse response
                var responseContent = await response.Content.ReadAsStringAsync();
                
                // The response format depends on the model. For sentence-transformers it's typically a single array
                using var doc = JsonDocument.Parse(responseContent);
                var root = doc.RootElement;
                
                if (root.ValueKind == JsonValueKind.Array)
                {
                    // Handle array response (most common for embeddings)
                    var embeddingArray = JsonSerializer.Deserialize<float[]>(responseContent);
                    return embeddingArray ?? new float[384];
                }
                else
                {
                    // Handle object response with nested array
                    var firstProperty = root.EnumerateObject().FirstOrDefault();
                    if (firstProperty.Value.ValueKind == JsonValueKind.Array)
                    {
                        var embeddingArray = JsonSerializer.Deserialize<float[]>(firstProperty.Value.ToString());
                        return embeddingArray ?? new float[384];
                    }
                }
                
                _logger.LogWarning("Unable to parse embedding response from Hugging Face");
                return new float[384];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting embedding from Hugging Face");
                return new float[384]; // Return empty embedding as fallback
            }
        }
        
        private List<int> RankVehiclesBySimilarity(
            float[] userEmbedding, 
            Dictionary<int, float[]> vehicleEmbeddings, 
            List<int> vehicleIds,
            int maxResults)
        {
            // Calculate cosine similarity between user and each vehicle
            var similarities = new Dictionary<int, double>();
            
            foreach (var vehicleId in vehicleIds)
            {
                if (vehicleEmbeddings.TryGetValue(vehicleId, out var vehicleEmbedding))
                {
                    similarities[vehicleId] = CalculateCosineSimilarity(userEmbedding, vehicleEmbedding);
                }
            }
            
            // Return top N vehicle IDs by similarity
            return similarities
                .OrderByDescending(s => s.Value)
                .Take(maxResults)
                .Select(s => s.Key)
                .ToList();
        }
        
        private double CalculateCosineSimilarity(float[] a, float[] b)
        {
            // Ensure vectors are same length
            if (a.Length != b.Length)
            {
                return 0;
            }
            
            double dotProduct = 0;
            double normA = 0;
            double normB = 0;
            
            for (int i = 0; i < a.Length; i++)
            {
                dotProduct += a[i] * b[i];
                normA += a[i] * a[i];
                normB += b[i] * b[i];
            }
            
            // Avoid division by zero
            if (normA == 0 || normB == 0)
            {
                return 0;
            }
            
            return dotProduct / (Math.Sqrt(normA) * Math.Sqrt(normB));
        }
        
        private async Task<List<Vehicle>> GetFallbackRecommendationsAsync(
            int userId,
            RecommendationParameters parameters,
            List<int> excludeIds,
            int count)
        {
            // Simple fallback logic based on preferences but without AI
            var query = _context.Vehicles.AsQueryable();
            
            // Apply filters from parameters
            if (parameters.MinPrice.HasValue)
                query = query.Where(v => v.Price >= parameters.MinPrice.Value);
                
            if (parameters.MaxPrice.HasValue)
                query = query.Where(v => v.Price <= parameters.MaxPrice.Value);
                
            if (parameters.MinYear.HasValue)
                query = query.Where(v => v.Year >= parameters.MinYear.Value);
                
            if (parameters.MaxYear.HasValue)
                query = query.Where(v => v.Year <= parameters.MaxYear.Value);
                
            if (parameters.PreferredFuelTypes?.Any() == true)
                query = query.Where(v => parameters.PreferredFuelTypes.Contains(v.FuelType));
                
            if (parameters.PreferredVehicleTypes?.Any() == true)
                query = query.Where(v => parameters.PreferredVehicleTypes.Contains(v.VehicleType));
                
            if (parameters.PreferredMakes?.Any() == true)
                query = query.Where(v => parameters.PreferredMakes.Contains(v.Make));
                
            if (parameters.DesiredFeatures?.Any() == true)
            {
                query = query.Where(v => v.Features.Any(f => 
                    parameters.DesiredFeatures.Contains(f.Name)));
            }
            
            // Exclude already recommended vehicles
            if (excludeIds.Any())
                query = query.Where(v => !excludeIds.Contains(v.Id));
            
            // Get most recently listed vehicles matching criteria
            return await query
                .Where(v => v.Status == VehicleStatus.Available)
                .OrderByDescending(v => v.DateListed)
                .Take(count)
                .Include(v => v.Images)
                .Include(v => v.Features)
                .ToListAsync();
        }
    }
}