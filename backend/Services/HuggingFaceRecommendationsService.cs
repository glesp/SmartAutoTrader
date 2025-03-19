using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SmartAutoTrader.API.Data;
using SmartAutoTrader.API.Models;

namespace SmartAutoTrader.API.Services;

public class HuggingFaceRecommendationService : IAIRecommendationService
{
    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _context;
    private readonly HttpClient _httpClient;
    private readonly ILogger<HuggingFaceRecommendationService> _logger;

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
    
    private static Dictionary<int, float[]> _vehicleEmbeddingsCache = new Dictionary<int, float[]>();
    private static SemaphoreSlim _embeddingSemaphore = new SemaphoreSlim(1, 1);
    private static DateTime _lastEmbeddingCall = DateTime.MinValue;
    private static readonly TimeSpan _minTimeBetweenCalls = TimeSpan.FromMilliseconds(100);

    public async Task<IEnumerable<Vehicle>> GetRecommendationsAsync(int userId, RecommendationParameters parameters)
    {
        try
        {
            _logger.LogInformation("Fetching recommendations for user ID: {UserId}", userId);

            // Fetch user separately to avoid SQLite APPLY issue
            var user = await _context.Users
                .Include(u => u.Preferences)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                _logger.LogWarning("User with ID {UserId} not found, returning empty recommendations.", userId);
                return new List<Vehicle>();
            }

            // Apply filters based on the parameters directly
            _logger.LogInformation("Applying filters based on extracted parameters: {@Parameters}", parameters);
            var filteredVehicles = await GetFilteredVehiclesAsync(parameters);

            if (!filteredVehicles.Any())
            {
                _logger.LogWarning("No available vehicles found for filtering criteria.");
                return new List<Vehicle>();
            }

            // If we have too many vehicles, get a reasonable subset 
            // based on most recent listings or best match to preferences
            if (filteredVehicles.Count > parameters.MaxResults)
            {
                // Sort by most recently listed
                filteredVehicles = filteredVehicles
                    .OrderByDescending(v => v.DateListed)
                    .Take(parameters.MaxResults ?? 5)
                    .ToList();
            }
            //log sql query for debugging
            
            _logger.LogInformation("Returning {Count} vehicles based on parameter filtering", filteredVehicles.Count);
            
        
            // Return the filtered vehicles
            return filteredVehicles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recommendations for user ID {UserId}", userId);
            return await GetFallbackRecommendationsAsync(userId, parameters, new List<int>(),
                parameters.MaxResults ?? 5);
        }
    }


    private async Task<List<Vehicle>> GetFilteredVehiclesAsync(RecommendationParameters parameters)
{
    var query = _context.Vehicles.AsQueryable();

    // Apply basic filters from parameters
    if (parameters.MinPrice.HasValue)
    {
        _logger.LogInformation("Filtering by MinPrice: {MinPrice}", parameters.MinPrice.Value);
        query = query.Where(v => v.Price >= parameters.MinPrice.Value);
    }

    if (parameters.MaxPrice.HasValue)
    {
        _logger.LogInformation("Filtering by MaxPrice: {MaxPrice}", parameters.MaxPrice.Value);
        query = query.Where(v => v.Price <= parameters.MaxPrice.Value);
    }

    if (parameters.MinYear.HasValue)
    {
        _logger.LogInformation("Filtering by MinYear: {MinYear}", parameters.MinYear.Value);
        query = query.Where(v => v.Year >= parameters.MinYear.Value);
    }

    if (parameters.MaxYear.HasValue)
    {
        _logger.LogInformation("Filtering by MaxYear: {MaxYear}", parameters.MaxYear.Value);
        query = query.Where(v => v.Year <= parameters.MaxYear.Value);
    }

    if (parameters.PreferredFuelTypes?.Any() == true)
    {
        _logger.LogInformation("Filtering by FuelTypes: {@FuelTypes}", parameters.PreferredFuelTypes);
        query = query.Where(v => parameters.PreferredFuelTypes.Contains(v.FuelType));
    }

    if (parameters.PreferredVehicleTypes?.Any() == true)
    {
        _logger.LogInformation("Filtering by VehicleTypes: {@VehicleTypes}", parameters.PreferredVehicleTypes);
        query = query.Where(v => parameters.PreferredVehicleTypes.Contains(v.VehicleType));
    }

    if (parameters.PreferredMakes?.Any() == true)
    {
        _logger.LogInformation("Filtering by Makes: {@Makes}", parameters.PreferredMakes);
        query = query.Where(v => parameters.PreferredMakes.Contains(v.Make));
    }

    // If we have desired features, filter by those
    if (parameters.DesiredFeatures?.Any() == true)
    {
        _logger.LogInformation("Filtering by DesiredFeatures: {@Features}", parameters.DesiredFeatures);
        query = query.Where(v => v.Features.Any(f => parameters.DesiredFeatures.Contains(f.Name)));
    }

    // Only available vehicles
    query = query.Where(v => v.Status == VehicleStatus.Available);

    // If no specific parameters are provided but we have a text prompt,
    // just return recent vehicles as we're now skipping embedding similarity
    if (!parameters.MinPrice.HasValue && 
        !parameters.MaxPrice.HasValue && 
        !parameters.MinYear.HasValue && 
        !parameters.MaxYear.HasValue &&
        !(parameters.PreferredFuelTypes?.Any() == true) &&
        !(parameters.PreferredVehicleTypes?.Any() == true) &&
        !(parameters.PreferredMakes?.Any() == true) &&
        !(parameters.DesiredFeatures?.Any() == true) &&
        !string.IsNullOrEmpty(parameters.TextPrompt))
    {
        _logger.LogInformation("No specific parameters extracted from text prompt. Returning recent vehicles.");
        // Order by most recently listed
        query = query.OrderByDescending(v => v.DateListed);
    }

    // Include needed relationships
    query = query.Include(v => v.Features)
                 .Include(v => v.Images);

    // Log the SQL query for debugging (if needed)
    _logger.LogDebug("SQL Query: {Query}", query.ToQueryString());
    _logger.LogInformation("SQL Query: {Query}", query.ToQueryString());


    return await query.ToListAsync();
}

    private async Task<float[]> GenerateUserEmbeddingAsync(User user, RecommendationParameters parameters)
{
    // Prepare text that represents the user preferences and history
    var userText = new StringBuilder();

    // If a text prompt is provided, give it more weight by adding it multiple times
    // and formatting it to stand out in the embedding text
    if (!string.IsNullOrEmpty(parameters.TextPrompt))
    {
        userText.AppendLine("USER DIRECT REQUEST (HIGH PRIORITY):");
        userText.AppendLine(parameters.TextPrompt);
        userText.AppendLine();
        
        // Repeat the prompt to increase its weight in the embedding
        userText.AppendLine("IMPORTANT USER NEEDS:");
        userText.AppendLine(parameters.TextPrompt);
        userText.AppendLine();
        
        // Add a third repetition with variations to help the embedding model
        userText.AppendLine("KEY SEARCH CRITERIA:");
        userText.AppendLine(parameters.TextPrompt);
        userText.AppendLine();
    }

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

    // Add browsing history (with lower weight than the direct prompt)
    var recentlyViewed = user.BrowsingHistory?.OrderByDescending(h => h.ViewDate).Take(5).ToList();
    if (recentlyViewed?.Any() == true)
    {
        userText.AppendLine("\nUser recently viewed these vehicles:");
        foreach (var history in recentlyViewed)
            userText.AppendLine($"{history.Vehicle.Year} {history.Vehicle.Make} {history.Vehicle.Model}, " +
                                $"Price: {history.Vehicle.Price:C0}, Type: {history.Vehicle.VehicleType}, " +
                                $"Fuel: {history.Vehicle.FuelType}, View duration: {history.ViewDurationSeconds}s");
    }

    // Add favorites (with lower weight than the direct prompt)
    var favorites = user.Favorites?.Select(f => f.Vehicle).ToList();
    if (favorites?.Any() == true)
    {
        userText.AppendLine("\nUser favorited these vehicles:");
        foreach (var favorite in favorites)
            userText.AppendLine($"{favorite.Year} {favorite.Make} {favorite.Model}, " +
                                $"Price: {favorite.Price:C0}, Type: {favorite.VehicleType}, " +
                                $"Fuel: {favorite.FuelType}");
    }

    // Get user preferences (with lower weight than the direct prompt)
    if (user.Preferences?.Any() == true)
    {
        userText.AppendLine("\nUser has these saved preferences:");
        foreach (var pref in user.Preferences)
            userText.AppendLine($"{pref.PreferenceType}: {pref.Value} (Weight: {pref.Weight})");
    }

    // If text prompt was provided, repeat it once more at the end to reinforce its importance
    if (!string.IsNullOrEmpty(parameters.TextPrompt))
    {
        userText.AppendLine("\nCURRENT SEARCH REQUEST (HIGHEST PRIORITY):");
        userText.AppendLine(parameters.TextPrompt);
    }

    // Generate embedding via Hugging Face
    return await GetEmbeddingFromHuggingFaceAsync(userText.ToString());
}

    private async Task<Dictionary<int, float[]>> GenerateVehicleEmbeddingsAsync(List<Vehicle> vehicles)
    {
        var embeddings = new Dictionary<int, float[]>();
        var vehiclesToProcess = new List<Vehicle>();

        // Check cache first
        foreach (var vehicle in vehicles)
        {
            if (_vehicleEmbeddingsCache.TryGetValue(vehicle.Id, out var embedding))
            {
                embeddings[vehicle.Id] = embedding;
            }
            else
            {
                vehiclesToProcess.Add(vehicle);
            }
        }

        // Only generate embeddings for vehicles not in cache
        if (vehiclesToProcess.Any())
        {
            _logger.LogInformation("Generating embeddings for {Count} vehicles not in cache", vehiclesToProcess.Count);
        
            // Process in batches to avoid rate limits
            var batchSize = 10;
            for (var i = 0; i < vehiclesToProcess.Count; i += batchSize)
            {
                var batch = vehiclesToProcess.Skip(i).Take(batchSize).ToList();
                var tasks = batch.Select(vehicle => GetVehicleEmbedding(vehicle)).ToList();
                var results = await Task.WhenAll(tasks);

                for (var j = 0; j < batch.Count; j++)
                {
                    var vehicleId = batch[j].Id;
                    var embedding = results[j];
                
                    embeddings[vehicleId] = embedding;
                    _vehicleEmbeddingsCache[vehicleId] = embedding;  // Add to cache
                }
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
            vehicleText.AppendLine("Features: " +
                                   string.Join(", ", vehicle.Features.Select(f => f.Name)));

        // Generate embedding via Hugging Face
        return await GetEmbeddingFromHuggingFaceAsync(vehicleText.ToString());
    }

    private async Task<float[]> GetEmbeddingFromHuggingFaceAsync(string text)
{
    try
    {
        // Rate limiting to prevent overwhelming the embedding service
        await _embeddingSemaphore.WaitAsync();
        try
        {
            var timeSinceLastCall = DateTime.UtcNow - _lastEmbeddingCall;
            if (timeSinceLastCall < _minTimeBetweenCalls)
            {
                await Task.Delay(_minTimeBetweenCalls - timeSinceLastCall);
            }

            // Create a request for the local embedding service
            var request = new
            {
                inputs = text,
                options = new { wait_for_model = true }
            };

            var content = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json");

            // Create cancellation token for timeout
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)); // 5 second timeout

            // Call local Flask service instead of Hugging Face
            _logger.LogInformation("Calling local embedding service");

            var response = await _httpClient.PostAsync(
                "http://localhost:5005/embeddings", // Local Flask service 
                content,
                cts.Token);

            _logger.LogInformation("Received response from local embedding service: {StatusCode}", response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Local embedding service error: {StatusCode}, {ErrorContent}", response.StatusCode,
                    errorContent);
                return new float[384]; // Default embedding size for the model
            }

            // Parse response - the local service returns a simple float array
            var responseContent = await response.Content.ReadAsStringAsync();
            var embeddings = JsonSerializer.Deserialize<float[]>(responseContent);
            
            _lastEmbeddingCall = DateTime.UtcNow;
            return embeddings ?? new float[384];
        }
        finally
        {
            _embeddingSemaphore.Release();
        }
    }
    catch (OperationCanceledException)
    {
        _logger.LogWarning("Embedding request timed out");
        return new float[384]; // Return empty embedding on timeout
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error getting embedding from local service");
        return new float[384]; // Return empty embedding as fallback
    }
}

    private async Task<float[]> FallbackToFeatureExtractionAsync(string text, string apiKey)
    {
        try
        {
            _logger.LogInformation("Using fallback approach with local embedding service");
        
            // Use the same request format as the primary method
            var request = new
            {
                inputs = text,
                options = new { wait_for_model = true }
            };
        
            var content = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json");
        
            // Call local Flask service
            var response = await _httpClient.PostAsync(
                "http://localhost:5005/embeddings",
                content);
        
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Fallback embedding error: {StatusCode}, {ErrorContent}", 
                    response.StatusCode, errorContent);
                return new float[384];
            }
        
            var responseContent = await response.Content.ReadAsStringAsync();
        
            // Parse the direct array of floats returned by our local service
            var embeddings = JsonSerializer.Deserialize<float[]>(responseContent);
            return embeddings ?? new float[384];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in fallback embedding generation");
            return new float[384];
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
            if (vehicleEmbeddings.TryGetValue(vehicleId, out var vehicleEmbedding))
                similarities[vehicleId] = CalculateCosineSimilarity(userEmbedding, vehicleEmbedding);

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
        if (a.Length != b.Length) return 0;

        double dotProduct = 0;
        double normA = 0;
        double normB = 0;

        for (var i = 0; i < a.Length; i++)
        {
            dotProduct += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        // Avoid division by zero
        if (normA == 0 || normB == 0) return 0;

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
            query = query.Where(v => v.Features.Any(f =>
                parameters.DesiredFeatures.Contains(f.Name)));

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

    private string ConvertFuelTypeToString(int fuelType)
    {
        return fuelType switch
        {
            0 => "Petrol",
            1 => "Diesel",
            2 => "Electric",
            3 => "Hybrid",
            4 => "Hydrogen",
            _ => "Unknown"
        };
    }

    private string ConvertTransmissionToString(int transmission)
    {
        return transmission switch
        {
            0 => "Manual",
            1 => "Automatic",
            2 => "Semi-Automatic",
            _ => "Unknown"
        };
    }

    private string ConvertVehicleTypeToString(int vehicleType)
    {
        return vehicleType switch
        {
            0 => "Hatchback",
            1 => "Sedan",
            2 => "SUV",
            3 => "Coupe",
            4 => "Convertible",
            5 => "Wagon",
            6 => "Pickup",
            7 => "Minivan",
            _ => "Unknown"
        };
    }
}