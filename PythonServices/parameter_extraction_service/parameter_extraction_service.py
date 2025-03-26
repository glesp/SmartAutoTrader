from flask import Flask, request, jsonify
import requests
import json
import logging

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)

app = Flask(__name__)

OLLAMA_API = "http://localhost:11434/api/generate"

@app.route('/extract_parameters', methods=['POST'])
def extract_parameters():
    try:
        logger.info("Received request: %s", request.json)

        data = request.json
        if not data or 'query' not in data:
            logger.error("No 'query' provided in request.")
            return jsonify({"error": "No query provided"}), 400

        user_query = data['query']
        logger.info("Processing query: %s", user_query)

        # Define our valid values
        valid_manufacturers = ["BMW", "Audi", "Mercedes", "Toyota", "Honda", "Ford",
                             "Volkswagen", "Nissan", "Hyundai", "Kia", "Tesla", "Volvo", "Mazda"]
        valid_fuel_types = ["Petrol", "Diesel", "Electric", "Hybrid"]
        valid_vehicle_types = ["Sedan", "SUV", "Hatchback", "Coupe", "Convertible", "Wagon", "Van", "Truck"]

        system_prompt = f"""
You are an automotive assistant for Smart Auto Trader, helping customers find their ideal vehicle.
Extract ONLY the search parameters that are EXPLICITLY mentioned in the user's query.

YOUR RESPONSE MUST ONLY CONTAIN VALID JSON with this exact format:

{{
  "minPrice": null,
  "maxPrice": null,
  "minYear": null,
  "maxYear": null,
  "maxMileage": null,
  "preferredMakes": [],
  "preferredFuelTypes": [],
  "preferredVehicleTypes": [],
  "desiredFeatures": [],
  "isOffTopic": false,
  "offTopicResponse": null
}}

RULES:
- ONLY include values that the user EXPLICITLY requests
- All numeric values must be numbers or null
- Leave arrays EMPTY unless specifically mentioned in the query
- DO NOT guess or infer parameters that aren't clearly stated
- For "low miles" or "low mileage" set maxMileage to 30000
- For "new" or "recent" set minYear to 2020
- For "older" or "used" set maxYear to 2018
- For "cheap" or "affordable" set maxPrice to 15000
- For "luxury" or "high-end" set minPrice to 30000

CHANGE HANDLING RULES:
- When the user says "change", "instead", "switch to", or similar phrasing, ONLY include the NEW values they want
- For example, if the user says "change from Mazda SUV to Toyota", ONLY include Toyota in preferredMakes
- For example, if the user says "make it electric instead", ONLY include Electric in preferredFuelTypes
- For follow-up queries like "can you show me sedans instead", ONLY include Sedan in preferredVehicleTypes
- Do not include both old and new values when the user is clearly requesting a change

OFF-TOPIC HANDLING RULES:
- Set "isOffTopic" to true if the query is not about automobiles, vehicles, or car shopping
- If isOffTopic is true, provide a friendly response in "offTopicResponse" suggesting they ask about cars
- For example, if user asks about spaceships, set offTopicResponse to "I'm your automotive assistant, so I can't help with spaceships. However, I'd be happy to help you find a vehicle here on Earth! What kind of car are you looking for?"
- Examples of off-topic queries: "I need a spaceship", "What's the weather like?", "Can you recommend a restaurant?"
- Empty or very vague queries like "hello" or "help" are NOT off-topic; respond with guidance about cars

VALID VALUES:
- preferredMakes: {json.dumps(valid_manufacturers)}
- preferredFuelTypes: {json.dumps(valid_fuel_types)}
- preferredVehicleTypes: {json.dumps(valid_vehicle_types)}

EXAMPLES:
- "electric car" → preferredFuelTypes: ["Electric"], isOffTopic: false
- "BMW or Audi" → preferredMakes: ["BMW", "Audi"], isOffTopic: false
- "SUV under 30000" → preferredVehicleTypes: ["SUV"], maxPrice: 30000, isOffTopic: false
- "Mazda SUV please! Can I change the brand to Toyota?" → preferredMakes: ["Toyota"], preferredVehicleTypes: ["SUV"], isOffTopic: false
- "I need a spaceship" → isOffTopic: true, offTopicResponse: "I'm your automotive assistant, so I can't help with spaceships. However, I'd be happy to help you find a vehicle here on Earth! What kind of car are you looking for?"
- "hello" → isOffTopic: false (this is vague but not off-topic; guide them to ask about cars)

User query: {user_query}

IMPORTANT: Return ONLY the JSON object with no additional text.
"""

        logger.info("Sending request to Ollama...")
        response = requests.post(
            OLLAMA_API,
            json={
                "model": "deepseek-r1:7b",
                "prompt": system_prompt,
                "stream": False
            },
            timeout=30
        )

        logger.info("Ollama response status code: %s", response.status_code)

        if response.status_code != 200:
            logger.error("Failed to call Ollama API. Response: %s", response.text)
            return jsonify(create_default_parameters(valid_manufacturers, valid_fuel_types, valid_vehicle_types)), 200

        response_data = response.json()
        generated_text = response_data.get('response', '')
        logger.info("Raw Ollama response: %s", generated_text)

        try:
            if '{' in generated_text and '}' in generated_text:
                json_start = generated_text.find('{')
                json_end = generated_text.rfind('}') + 1
                json_str = generated_text[json_start:json_end].strip()
                extracted_params = json.loads(json_str)
                
                # Get the final parameters with appropriate defaults
                parameters = process_parameters(
                    extracted_params, 
                    valid_manufacturers, 
                    valid_fuel_types, 
                    valid_vehicle_types
                )
                
                logger.info("Final extracted parameters: %s", parameters)
                return jsonify(parameters), 200
            else:
                logger.error("No JSON found in model response")
                return jsonify(create_default_parameters(valid_manufacturers, valid_fuel_types, valid_vehicle_types)), 200

        except json.JSONDecodeError as e:
            logger.error("Failed to parse JSON from model response: %s", e)
            return jsonify(create_default_parameters(valid_manufacturers, valid_fuel_types, valid_vehicle_types)), 200

    except Exception as e:
        logger.exception("Exception occurred: %s", str(e))
        return jsonify(create_default_parameters()), 200


@app.route('/health', methods=['GET'])
def health_check():
    """Simple health check endpoint to verify the service is running"""
    return jsonify({"status": "ok", "message": "Parameter extraction service is running"}), 200


def create_default_parameters(makes=None, fuel_types=None, vehicle_types=None):
    """Create default parameters with all possible values for arrays"""
    if makes is None:
        makes = []
    if fuel_types is None:
        fuel_types = []
    if vehicle_types is None:
        vehicle_types = []
        
    return {
        "minPrice": None,
        "maxPrice": None,
        "minYear": None,
        "maxYear": None,
        "maxMileage": None,
        "preferredMakes": makes,
        "preferredFuelTypes": fuel_types,
        "preferredVehicleTypes": vehicle_types,
        "desiredFeatures": [],
        "isOffTopic": False,
        "offTopicResponse": None
    }


def process_parameters(params, valid_makes, valid_fuel_types, valid_vehicle_types):
    """Process extracted parameters and apply defaults where needed"""
    # Start with clean parameters
    result = {
        "minPrice": None,
        "maxPrice": None,
        "minYear": None,
        "maxYear": None,
        "maxMileage": None,
        "preferredMakes": [],
        "preferredFuelTypes": [],
        "preferredVehicleTypes": [],
        "desiredFeatures": [],
        "isOffTopic": False,
        "offTopicResponse": None
    }
    
    # Copy numerical values if present
    for field in ["minPrice", "maxPrice", "minYear", "maxYear", "maxMileage"]:
        if field in params and params[field] is not None and isinstance(params[field], (int, float)):
            result[field] = params[field]
    
    # Process arrays with validation
    if params.get("preferredMakes") and isinstance(params["preferredMakes"], list):
        result["preferredMakes"] = [m for m in params["preferredMakes"] 
                                   if isinstance(m, str) and m in valid_makes]
    
    if params.get("preferredFuelTypes") and isinstance(params["preferredFuelTypes"], list):
        result["preferredFuelTypes"] = [f for f in params["preferredFuelTypes"] 
                                       if isinstance(f, str) and f in valid_fuel_types]
    
    if params.get("preferredVehicleTypes") and isinstance(params["preferredVehicleTypes"], list):
        result["preferredVehicleTypes"] = [v for v in params["preferredVehicleTypes"] 
                                          if isinstance(v, str) and v in valid_vehicle_types]
    
    if params.get("desiredFeatures") and isinstance(params["desiredFeatures"], list):
        result["desiredFeatures"] = [f for f in params["desiredFeatures"] if isinstance(f, str)]
    
    # Copy off-topic status and response if present
    if "isOffTopic" in params and isinstance(params["isOffTopic"], bool):
        result["isOffTopic"] = params["isOffTopic"]
    
    if "offTopicResponse" in params and isinstance(params["offTopicResponse"], str):
        result["offTopicResponse"] = params["offTopicResponse"]
    
    # Only fill in defaults for arrays if not off-topic
    if not result["isOffTopic"]:
        # Fill in defaults for empty arrays
        if not result["preferredMakes"]:
            result["preferredMakes"] = valid_makes.copy()
        
        if not result["preferredFuelTypes"]:
            result["preferredFuelTypes"] = valid_fuel_types.copy()
        
        if not result["preferredVehicleTypes"]:
            result["preferredVehicleTypes"] = valid_vehicle_types.copy()
    
    return result


if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5006, debug=True)