from flask import Flask, request, jsonify
import requests
import json

app = Flask(__name__)

# Ollama API endpoint (local)
OLLAMA_API = "http://localhost:11434/api/generate"

@app.route('/extract_parameters', methods=['POST'])
def extract_parameters():
    try:
        # Log incoming request
        print("Received request: ", request.json)

        # Get the query from the request
        data = request.json
        if not data or 'query' not in data:
            print("ERROR: No 'query' provided in request.")
            return jsonify({"error": "No query provided"}), 400
        
        user_query = data['query']
        print("Processing query:", user_query)

        # Create the system prompt - using raw string to avoid format issues
        # IMPORTANT: Changed field names to match C# model expectations
        system_prompt = r"""
You are an AI assistant for Smart Auto Trader, a vehicle marketplace application.
Your task is to generate vehicle search recommendations based on user input.

IMPORTANT: Your response must only contain valid JSON that conforms to these exact specifications:

{
  "priceRange": {
    "min": null,
    "max": null
  },
  "preferredMakes": [],
  "preferredFuelTypes": [],
  "transmission": [],
  "preferredVehicleTypes": [],
  "mileage": {
    "max": null
  },
  "yearRange": {
    "min": null,
    "max": null
  }
}

Context handling:
- If the user mentions "cheap" or "affordable", set priceRange.max to no more than 15000
- If the user mentions "new" or "recent", set yearRange.min to at least 2020
- If the user mentions "low mileage", set mileage.max to no more than 30000
- Only include manufacturers from this list: ["BMW", "Audi", "Mercedes", "Toyota", "Honda", "Ford", "Volkswagen", "Nissan", "Hyundai", "Kia", "Tesla", "Volvo", "Mazda"]
- Only include fuel types from this list: ["Petrol", "Diesel", "Electric", "Hybrid"]
- Only include transmission types from this list: ["Automatic", "Manual"]
- Only include vehicle types from this list: ["Sedan", "SUV", "Hatchback", "Coupe", "Convertible", "Wagon", "Van", "Truck"]

USER QUERY: """ + user_query + """

For categorical fields (preferredFuelTypes, transmission, preferredVehicleTypes, preferredMakes), select ONLY values from the provided options.
All fields must match the exact case shown above.
Return valid JSON only, with no additional text or explanation.
"""

        print("Sending request to Ollama...")
        
        # Call Ollama API
        response = requests.post(
            OLLAMA_API,
            json={
                "model": "deepseek-r1:7b",
                "prompt": system_prompt,
                "stream": False
            }
        )

        print("Ollama response status code:", response.status_code)

        if response.status_code != 200:
            print("ERROR: Failed to call Ollama API. Response:", response.text)
            return jsonify(create_default_parameters()), 200  # Return 200 with default parameters
        
        # Extract the generated text
        response_data = response.json()
        generated_text = response_data.get('response', '')
        print("Raw Ollama response:", generated_text)

        # Try to parse the JSON from the response
        try:
            if '{' in generated_text and '}' in generated_text:
                json_start = generated_text.find('{')
                json_end = generated_text.rfind('}') + 1
                json_str = generated_text[json_start:json_end].strip()  # Strip spaces
                parameters = json.loads(json_str)
                
                # Ensure the parameters are in the correct format
                parameters = validate_and_format_parameters(parameters)
            else:
                print("ERROR: No JSON found in model response")
                parameters = create_default_parameters()

            print("Extracted parameters:", parameters)
            return jsonify(parameters), 200
        
        except json.JSONDecodeError as e:
            print(f"ERROR: Failed to parse JSON from model response: {e}")
            return jsonify(create_default_parameters()), 200  # Return 200 with default parameters
    
    except Exception as e:
        print("Exception occurred:", str(e))
        # Return default parameters instead of an error
        return jsonify(create_default_parameters()), 200

def create_default_parameters():
    """Create default parameters based on user query"""
    return {
        "priceRange": {"min": None, "max": None},
        "preferredMakes": [],
        "preferredFuelTypes": [],
        "transmission": [],
        "preferredVehicleTypes": [],
        "mileage": {"max": None},
        "yearRange": {"min": None, "max": None}
    }

def validate_and_format_parameters(params):
    """Ensure parameters are in the correct format"""
    valid_params = create_default_parameters()
    
    # Copy over valid values
    if "priceRange" in params and isinstance(params["priceRange"], dict):
        if "min" in params["priceRange"]:
            valid_params["priceRange"]["min"] = params["priceRange"]["min"]
        if "max" in params["priceRange"]:
            valid_params["priceRange"]["max"] = params["priceRange"]["max"]
    
    if "mileage" in params and isinstance(params["mileage"], dict):
        if "max" in params["mileage"]:
            valid_params["mileage"]["max"] = params["mileage"]["max"]
    
    if "yearRange" in params and isinstance(params["yearRange"], dict):
        if "min" in params["yearRange"]:
            valid_params["yearRange"]["min"] = params["yearRange"]["min"]
        if "max" in params["yearRange"]:
            valid_params["yearRange"]["max"] = params["yearRange"]["max"]
    
    # Handle array fields
    valid_manufacturers = ["BMW", "Audi", "Mercedes", "Toyota", "Honda", "Ford", 
                          "Volkswagen", "Nissan", "Hyundai", "Kia", "Tesla", "Volvo", "Mazda"]
    valid_fuel_types = ["Petrol", "Diesel", "Electric", "Hybrid"]
    valid_transmission_types = ["Automatic", "Manual"]
    valid_vehicle_types = ["Sedan", "SUV", "Hatchback", "Coupe", "Convertible", "Wagon", "Van", "Truck"]
    
    # Check for manufacturers in both possible field names
    for field_name in ["manufacturers", "preferredMakes"]:
        if field_name in params and isinstance(params[field_name], list):
            valid_params["preferredMakes"] = [m for m in params[field_name] 
                                        if isinstance(m, str) and m in valid_manufacturers]
    
    # Check for fuel types in both possible field names
    for field_name in ["fuelType", "preferredFuelTypes"]:
        if field_name in params and isinstance(params[field_name], list):
            valid_params["preferredFuelTypes"] = [f for f in params[field_name] 
                                   if isinstance(f, str) and f in valid_fuel_types]
    
    if "transmission" in params and isinstance(params["transmission"], list):
        valid_params["transmission"] = [t for t in params["transmission"] 
                                       if isinstance(t, str) and t in valid_transmission_types]
    
    # Check for vehicle types in both possible field names
    for field_name in ["bodyType", "preferredVehicleTypes"]:
        if field_name in params and isinstance(params[field_name], list):
            valid_params["preferredVehicleTypes"] = [b for b in params[field_name] 
                                   if isinstance(b, str) and b in valid_vehicle_types]
    
    return valid_params

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5006, debug=True)