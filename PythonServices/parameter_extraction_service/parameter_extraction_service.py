#!/usr/bin/env python3
# filepath:
# /Users/conorgillespie/Projects/SmartAutoTrader/PythonServices/parameter_extraction_service/parameter_extraction_service.py

from retriever.retriever import find_best_match
from flask import Flask, request, jsonify
import requests
import json
import logging
import re
import os
import sys
from typing import Dict, List, Optional, Any
from dotenv import load_dotenv
load_dotenv()

# If your 'retriever' folder is inside the same directory, you may do:
sys.path.append(os.path.dirname(os.path.abspath(__file__)))


# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)


def word_count_clean(query: str) -> int:
    """Counts meaningful words in a cleaned-up user query."""
    cleaned = re.sub(r'[^a-zA-Z0-9 ]+', '', query).lower()
    return len(cleaned.strip().split())


def extract_newest_user_fragment(query: str) -> str:
    """
    For follow-up queries like "Original - Additional info: blah",
    return only the latest user input.
    """
    return query.split(" - Additional info:")[-1].strip()


app = Flask(__name__)

# OpenRouter configuration
OPENROUTER_API_KEY = os.environ.get("OPENROUTER_API_KEY")
if not OPENROUTER_API_KEY:
    logger.warning("OPENROUTER_API_KEY environment variable not set. API calls will fail.")

OPENROUTER_URL = "https://openrouter.ai/api/v1/chat/completions"

# Model configuration
FAST_MODEL = "meta-llama/llama-3-8b-instruct:free"
REFINE_MODEL = "openchat/openchat-3.5:free"
CLARIFY_MODEL = "mistralai/mixtral-8x7b-instruct:free"


@app.route('/extract_parameters', methods=['POST'])
def extract_parameters():
    """
    Main endpoint for parameter extraction from user queries.
    Includes local retrieval for short or vague queries,
    plus fallback logic for LLM calls.
    """
    try:
        logger.info("Received request: %s", request.json)
        data = request.json or {}
        if 'query' not in data:
            logger.error("No 'query' provided in request.")
            return jsonify({"error": "No query provided"}), 400

        user_query = data['query']
        force_model = data.get("forceModel")

        logger.info("Processing query: %s (forceModel=%s)", user_query, force_model)

        # 1) Quick check for off-topic or trivial
        if not is_car_related(user_query):
            logger.info("Skipping LLM for off-topic/trivial query.")
            return jsonify({
                "minPrice": None,
                "maxPrice": None,
                "minYear": None,
                "maxYear": None,
                "maxMileage": None,
                "preferredMakes": [],
                "preferredFuelTypes": [],
                "preferredVehicleTypes": [],
                "desiredFeatures": [],
                "isOffTopic": True,
                "offTopicResponse": (
                    "I'm your automotive assistant. Let me know what kind of vehicle you're looking for!"
                )
            }), 200

        # 2) If short or vague but at least car-related, try local retrieval
        query_fragment = extract_newest_user_fragment(user_query)
        if word_count_clean(query_fragment) < 4:
            match_cat, score = find_best_match(query_fragment)
            if score < 0.5:
                logger.info("Local retrieval found weak match: %s (%.2f)", match_cat, score)
                return jsonify({
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
                    "offTopicResponse": None,
                    "clarificationNeeded": True,
                    "matchedCategory": match_cat,
                    "retrieverSuggestion": (
                        f"Sounds like you're after something like '{match_cat}'. "
                        "Could you give me more details — maybe a budget or fuel type?"
                    )
                }), 200

            else:
                logger.info("Local retrieval matched: %s (%.2f)", match_cat, score)
                return jsonify({
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
                    "offTopicResponse": None,
                    "retrieverSuggestion": (
                        f"Maybe you're looking for: '{match_cat}'? "
                        "Add more details or say 'change' if needed!"
                    )
                }), 200

        # 3) Otherwise, run normal model fallback
        extracted_params = run_llm_fallback(user_query, force_model)
        if extracted_params:
            logger.info("Final extracted parameters: %s", extracted_params)
            return jsonify(extracted_params), 200
        else:
            logger.error("All models failed or no valid extraction.")
            return jsonify(create_default_parameters()), 200

    except Exception as e:
        logger.exception("Exception occurred: %s", str(e))
        return jsonify(create_default_parameters()), 200


def run_llm_fallback(user_query: str, force_model: Optional[str]) -> Optional[Dict[str, Any]]:
    """
    Fallback chain: fast → refine → clarify (or forced model).
    """
    valid_manufacturers = [
        "BMW", "Audi", "Mercedes", "Toyota", "Honda", "Ford",
        "Volkswagen", "Nissan", "Hyundai", "Kia", "Tesla", "Volvo", "Mazda"
    ]
    valid_fuel_types = ["Petrol", "Diesel", "Electric", "Hybrid"]
    valid_vehicle_types = [
        "Sedan", "SUV", "Hatchback", "Coupe", "Convertible", "Wagon", "Van", "Truck"
    ]

    model_order = {
        "fast": FAST_MODEL,
        "refine": REFINE_MODEL,
        "clarify": CLARIFY_MODEL
    }

    if force_model is None:
        logger.info("No forceModel => use fast only.")
        models_to_try = [FAST_MODEL]
    else:
        if force_model in model_order:
            logger.info("Using forced model strategy with %s", force_model)
            # forced model first, then the others
            models_to_try = [model_order[force_model]] + [
                m for k, m in model_order.items() if k != force_model
            ]
        else:
            logger.warning("Invalid forceModel => defaulting to fast.")
            models_to_try = [FAST_MODEL]

    system_prompt = build_system_prompt(
        user_query,
        valid_manufacturers,
        valid_fuel_types,
        valid_vehicle_types
    )

    for model in models_to_try:
        extracted = try_extract_with_model(model, system_prompt, user_query)
        logger.info("Model %s returned: %s", model, extracted)

        if extracted and is_valid_extraction(extracted):
            processed = process_parameters(
                extracted,
                valid_manufacturers,
                valid_fuel_types,
                valid_vehicle_types
            )
            return processed

    return None


def build_system_prompt(user_query: str,
                        valid_makes: List[str],
                        valid_fuels: List[str],
                        valid_vehicles: List[str]) -> str:
    """
    The system prompt with your instructions about valid JSON, etc.
    """
    # You can refine or shorten these instructions if you want.
    return f"""
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
- ONLY include values the user explicitly requests
- All numeric values must be numbers or null
- Leave arrays EMPTY unless specifically mentioned
- DO NOT guess or infer parameters not clearly stated
- "low miles" => maxMileage=30000
- "new"/"recent" => minYear=2020
- "older"/"used" => maxYear=2018
- "cheap"/"affordable" => maxPrice=15000
- "luxury"/"high-end" => minPrice=30000

CHANGE HANDLING:
- If user says "change from Mazda SUV to Toyota", only Toyota in preferredMakes
- If user says "make it electric instead", only Electric in preferredFuelTypes

OFF-TOPIC:
- isOffTopic = true if not about vehicles
- If isOffTopic, put a friendly offTopicResponse

VALID VALUES:
- preferredMakes: {json.dumps(valid_makes)}
- preferredFuelTypes: {json.dumps(valid_fuels)}
- preferredVehicleTypes: {json.dumps(valid_vehicles)}

EXAMPLES:
- "electric car" => fuelTypes=["Electric"], etc.
- "I need a spaceship" => isOffTopic=true + offTopicResponse
- "hello" => isOffTopic=false

User query: {user_query}

IMPORTANT: Return ONLY the JSON object with no additional text.
"""


def try_extract_with_model(model: str, system_prompt: str, user_query: str) -> Optional[Dict[str, Any]]:
    """
    Attempt to get valid JSON from an LLM model via OpenRouter.
    Returns a dict if successful, else None.
    """
    try:
        headers = {
            "Authorization": f"Bearer {OPENROUTER_API_KEY}",
            "Content-Type": "application/json",
            "HTTP-Referer": "https://smartautotrader.app",
            "X-Title": "Smart Auto Trader"
        }
        payload = {
            "model": model,
            "messages": [
                {"role": "system", "content": system_prompt},
                {"role": "user", "content": user_query}
            ],
            "temperature": 0.3,
            "max_tokens": 500
        }

        logger.info("Sending request to OpenRouter with model: %s", model)
        response = requests.post(
            OPENROUTER_URL,
            headers=headers,
            json=payload,
            timeout=30
        )

        if response.status_code != 200:
            logger.error(
                "OpenRouter API call failed. Status: %s, Body: %s",
                response.status_code,
                response.text
            )
            return None

        response_data = response.json()
        logger.debug("Full OpenRouter response: %s", json.dumps(response_data, indent=2))

        if not response_data.get("choices") or not response_data["choices"][0].get("message"):
            logger.error("Invalid response format from OpenRouter: %s", response_data)
            return None

        generated_text = response_data["choices"][0]["message"]["content"]
        logger.info("Raw model output: %s", generated_text)

        # Attempt to parse JSON
        if '{' in generated_text and '}' in generated_text:
            json_start = generated_text.find('{')
            json_end = generated_text.rfind('}') + 1
            json_str = generated_text[json_start:json_end].strip()

            try:
                extracted = json.loads(json_str)
                logger.info("Successfully parsed JSON: %s", extracted)
                return extracted
            except json.JSONDecodeError as je:
                logger.error("JSON decoding failed: %s", je)
                return None
        else:
            logger.warning("No JSON object found in model output.")
            return None

    except Exception as e:
        logger.exception("Unhandled exception in try_extract_with_model: %s", str(e))
        return None


def is_valid_extraction(params: Dict[str, Any]) -> bool:
    """
    Decide if the extracted JSON is "good enough."
    By default, require at least 3 valid fields or offTopic is True.
    Adjust to your preference.
    """
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

    # Off-topic is valid if true + offTopicResponse
    is_off_topic = (
        params.get('isOffTopic') is True and
        params.get('offTopicResponse') and
        isinstance(params['offTopicResponse'], str)
    )

    total_valid = numeric_count + array_count

    # Require at least 3 valid fields or off-topic
    return is_off_topic or total_valid >= 3


def create_default_parameters(
    makes: Optional[List[str]] = None,
    fuel_types: Optional[List[str]] = None,
    vehicle_types: Optional[List[str]] = None
) -> Dict[str, Any]:
    """
    Returns a fallback set of parameters if no extraction succeeded.
    """
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
    """
    Convert raw extracted dict into final structure, applying validation
    and default fill for empty arrays if not off-topic.
    """
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

    # numeric fields
    for field in ["minPrice", "maxPrice", "minYear", "maxYear", "maxMileage"]:
        if field in params and params[field] is not None and isinstance(params[field], (int, float)):
            result[field] = params[field]

    # array fields with validation
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

    # off-topic check
    if isinstance(params.get("isOffTopic"), bool):
        result["isOffTopic"] = params["isOffTopic"]

    if result["isOffTopic"] and isinstance(params.get("offTopicResponse"), str):
        result["offTopicResponse"] = params["offTopicResponse"]

    # If not off-topic, fill in defaults for empty arrays
    if not result["isOffTopic"]:
        if not result["preferredMakes"]:
            result["preferredMakes"] = valid_makes.copy()
        if not result["preferredFuelTypes"]:
            result["preferredFuelTypes"] = valid_fuel_types.copy()
        if not result["preferredVehicleTypes"]:
            result["preferredVehicleTypes"] = valid_vehicle_types.copy()

    return result


def is_car_related(msg: str) -> bool:
    """
    Quick local check to skip LLM if definitely not about cars.
    Adjust for your domain or remove if you prefer always calling LLM.
    """
    if not msg or len(msg.strip()) < 3:
        return False

    # minimal approach
    car_keywords = [
        "car", "vehicle", "suv", "bmw", "ford", "toyota", "honda", "cheap",
        "fuel", "mileage", "hybrid", "electric", "kia", "tesla", "under", "over"
    ]
    msg_lower = msg.lower()

    return any(kw in msg_lower for kw in car_keywords)


if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5006, debug=True)
