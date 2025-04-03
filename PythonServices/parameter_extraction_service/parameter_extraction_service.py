#!/usr/bin/env python3
# filepath: /Users/conorgillespie/Projects/SmartAutoTrader/PythonServices/parameter_extraction_service/parameter_extraction_service.py
from flask import Flask, request, jsonify
import requests
import json
import logging
import os
from typing import Dict, List, Optional, Union, Any
from dotenv import load_dotenv
load_dotenv()

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)

app = Flask(__name__)

# OpenRouter API configuration
OPENROUTER_API_KEY = os.environ.get("OPENROUTER_API_KEY")
if not OPENROUTER_API_KEY:
    logger.warning("OPENROUTER_API_KEY environment variable not set. API calls will fail.")

OPENROUTER_URL = "https://openrouter.ai/api/v1/chat/completions"

# Model configuration with fallbacks
FAST_MODEL = "meta-llama/llama-3-8b-instruct:free"   # Make sure this matches what's offered by OpenRouter
REFINE_MODEL = "google/gemini-2.5-pro-exp-03-25:free"
CLARIFY_MODEL = "mistral/mistral-small-3.1-24b:free"

@app.route('/extract_parameters', methods=['POST'])
def extract_parameters():
    """
    Main endpoint for parameter extraction from user queries.
    Uses a tiered approach with multiple models for robustness.
    """
    try:
        logger.info("Received request: %s", request.json)
        data = request.json
        if not data or 'query' not in data:
            logger.error("No 'query' provided in request.")
            return jsonify({"error": "No query provided"}), 400

        user_query = data['query']
        logger.info("Processing query: %s", user_query)

        # Optional: force a specific model
        force_model = data.get("forceModel")  # can be "fast", "refine", "clarify" or None

        # Define valid values for later processing
        valid_manufacturers = [
            "BMW", "Audi", "Mercedes", "Toyota", "Honda", "Ford",
            "Volkswagen", "Nissan", "Hyundai", "Kia", "Tesla", "Volvo", "Mazda"
        ]
        valid_fuel_types = ["Petrol", "Diesel", "Electric", "Hybrid"]
        valid_vehicle_types = [
            "Sedan", "SUV", "Hatchback", "Coupe", "Convertible", "Wagon", "Van", "Truck"
        ]

        # Define available models
        model_order = {
            "fast": FAST_MODEL,
            "refine": REFINE_MODEL,
            "clarify": CLARIFY_MODEL
        }

        # If no forceModel is provided but additional info is detected, use clarify
        if force_model is None and "Additional info:" in user_query:
            logger.info("Detected additional info in query; switching to clarify model.")
            force_model = "clarify"

        # Build the comprehensive system prompt
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

        # Determine which model(s) to try based on force_model
        if force_model is None:
            logger.info("No forceModel provided; using fast model only.")
            try_models = [FAST_MODEL]
        else:
            if force_model in model_order:
                try_models = [model_order[force_model]] + [m for k, m in model_order.items() if k != force_model]
                logger.info("Using forced model strategy. Starting with: %s", force_model)
            else:
                logger.warning("Invalid forceModel provided; defaulting to fast model.")
                try_models = [FAST_MODEL]

        extracted_params = None
        model_used = None

        if force_model is None:
            # Use only the fast model; return its extraction even if incomplete
            extracted_params = try_extract_with_model(FAST_MODEL, system_prompt, user_query)
            model_used = FAST_MODEL
        else:
            # Use the forced model order with fallback
            for model in try_models:
                logger.info("Trying with model: %s", model)
                extracted_params = try_extract_with_model(model, system_prompt, user_query)
                model_used = model
                if extracted_params and is_valid_extraction(extracted_params):
                    break  # Exit loop on success

        # Process the extracted parameters if any model succeeded
        if extracted_params:
            parameters = process_parameters(
                extracted_params,
                valid_manufacturers,
                valid_fuel_types,
                valid_vehicle_types
            )
            logger.info("Final extracted parameters (using %s): %s", model_used, parameters)
            return jsonify(parameters), 200
        else:
            logger.error("All models failed to extract parameters")
            return jsonify(create_default_parameters(
                valid_manufacturers,
                valid_fuel_types,
                valid_vehicle_types
            )), 200

    except Exception as e:
        logger.exception("Exception occurred: %s", str(e))
        return jsonify(create_default_parameters()), 200

def try_extract_with_model(model: str, system_prompt: str, user_query: str) -> Optional[Dict[str, Any]]:
    """
    Try to extract parameters using the specified model via OpenRouter API.
    
    Args:
        model: The OpenRouter model identifier
        system_prompt: The system prompt with instructions
        user_query: The user's query text
    
    Returns:
        Dictionary of extracted parameters or None if extraction failed
    """
    try:
        headers = {
            "Authorization": f"Bearer {OPENROUTER_API_KEY}",
            "Content-Type": "application/json",
            "HTTP-Referer": "https://smartautotrader.app",  # Required by OpenRouter
            "X-Title": "Smart Auto Trader"                 # Optional but good practice
        }
        payload = {
            "model": model,
            "messages": [
                {"role": "system", "content": system_prompt},
                {"role": "user", "content": user_query}
            ],
            "temperature": 0.3,
            "max_tokens": 500
            # IMPROVEMENT: Potentially add top_p, frequency_penalty, presence_penalty, etc.
        }
        logger.info("Sending request to OpenRouter for model %s...", model)
        response = requests.post(
            OPENROUTER_URL,
            headers=headers,
            json=payload,
            timeout=30  # IMPROVEMENT: might be configurable
        )
        if response.status_code != 200:
            logger.error("OpenRouter API call failed. Status: %s, Response: %s", response.status_code, response.text)
            return None
        response_data = response.json()
        if not response_data.get("choices") or not response_data["choices"][0].get("message"):
            logger.error("Invalid response format from OpenRouter: %s", response_data)
            return None
        generated_text = response_data["choices"][0]["message"]["content"]
        logger.info("Raw response from %s: %s", model, generated_text)
        # Attempt to parse JSON from the response text
        if '{' in generated_text and '}' in generated_text:
            json_start = generated_text.find('{')
            json_end = generated_text.rfind('}') + 1
            json_str = generated_text[json_start:json_end].strip()
            try:
                extracted_params = json.loads(json_str)
                return extracted_params
            except json.JSONDecodeError as json_err:
                logger.error("JSON decoding error from %s: %s", model, json_err)
                return None
        else:
            logger.error("No JSON found in %s response", model)
            return None
    except Exception as e:
        logger.exception("Exception when calling %s: %s", model, str(e))
        return None

def is_valid_extraction(params: Dict[str, Any]) -> bool:
    if not isinstance(params, dict):
        return False
    # Count numeric fields that are not null
    numeric_fields = ['minPrice', 'maxPrice', 'minYear', 'maxYear', 'maxMileage']
    numeric_count = sum(
        1 for field in numeric_fields 
        if params.get(field) is not None and isinstance(params[field], (int, float))
    )
    # Count array fields that are non-empty
    array_fields = ['preferredMakes', 'preferredFuelTypes', 'preferredVehicleTypes', 'desiredFeatures']
    array_count = sum(
        1 for field in array_fields 
        if isinstance(params.get(field), list) and len(params[field]) > 0
    )
    # Off-topic response is considered valid regardless of other parameters
    is_off_topic = (
        params.get('isOffTopic') is True and
        params.get('offTopicResponse') and
        isinstance(params['offTopicResponse'], str)
    )
    total_valid = numeric_count + array_count
    # Require at least 3 valid fields for the extraction to be considered sufficient
    return is_off_topic or total_valid >= 3

@app.route('/health', methods=['GET'])
def health_check():
    """Simple health check endpoint to verify the service is running"""
    return jsonify({
        "status": "ok",
        "message": "Parameter extraction service is running",
        "models": {
            "fast": FAST_MODEL,
            "refine": REFINE_MODEL,
            "clarify": CLARIFY_MODEL
        }
    }), 200

def create_default_parameters(
    makes: Optional[List[str]] = None, 
    fuel_types: Optional[List[str]] = None, 
    vehicle_types: Optional[List[str]] = None
) -> Dict[str, Any]:
    makes = makes or []
    fuel_types = fuel_types or []
    vehicle_types = vehicle_types or []
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

def process_parameters(
    params: Dict[str, Any],
    valid_makes: List[str],
    valid_fuel_types: List[str],
    valid_vehicle_types: List[str]
) -> Dict[str, Any]:
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
    for field in ["minPrice", "maxPrice", "minYear", "maxYear", "maxMileage"]:
        if field in params and params[field] is not None and isinstance(params[field], (int, float)):
            result[field] = params[field]
    if isinstance(params.get("preferredMakes"), list):
        result["preferredMakes"] = [
            m for m in params["preferredMakes"]
            if isinstance(m, str) and m in valid_makes
        ]
    if isinstance(params.get("preferredFuelTypes"), list):
        result["preferredFuelTypes"] = [
            f for f in params["preferredFuelTypes"]
            if isinstance(f, str) and f in valid_fuel_types
        ]
    if isinstance(params.get("preferredVehicleTypes"), list):
        result["preferredVehicleTypes"] = [
            v for v in params["preferredVehicleTypes"]
            if isinstance(v, str) and v in valid_vehicle_types
        ]
    if isinstance(params.get("desiredFeatures"), list):
        result["desiredFeatures"] = [
            f for f in params["desiredFeatures"] 
            if isinstance(f, str)
        ]
    if isinstance(params.get("isOffTopic"), bool):
        result["isOffTopic"] = params["isOffTopic"]
    if result["isOffTopic"] and isinstance(params.get("offTopicResponse"), str):
        result["offTopicResponse"] = params["offTopicResponse"]
    if not result["isOffTopic"]:
        if not result["preferredMakes"]:
            result["preferredMakes"] = valid_makes.copy()
        if not result["preferredFuelTypes"]:
            result["preferredFuelTypes"] = valid_fuel_types.copy()
        if not result["preferredVehicleTypes"]:
            result["preferredVehicleTypes"] = valid_vehicle_types.copy()
    return result

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5006, debug=True)
