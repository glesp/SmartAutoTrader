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
import datetime

load_dotenv()

# If your 'retriever' folder is inside the same directory, you may do:
sys.path.append(os.path.dirname(os.path.abspath(__file__)))


# Configure logging
logging.basicConfig(
    level=logging.INFO, format="%(asctime)s - %(name)s - %(levelname)s - %(message)s"
)
logger = logging.getLogger(__name__)


def word_count_clean(query: str) -> int:
    """Counts meaningful words in a cleaned-up user query."""
    cleaned = re.sub(r"[^a-zA-Z0-9 ]+", "", query).lower()
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
    logger.warning(
        "OPENROUTER_API_KEY environment variable not set. API calls will fail."
    )

OPENROUTER_URL = "https://openrouter.ai/api/v1/chat/completions"

# Model configuration
FAST_MODEL = "meta-llama/llama-3-8b-instruct:free"
REFINE_MODEL = "google/gemini-2.0-flash-exp:free"
CLARIFY_MODEL = "mistralai/mixtral-8x7b-instruct:free"


@app.route("/extract_parameters", methods=["POST"])
def extract_parameters():
    """
    Main endpoint for parameter extraction from user queries.
    Now includes conversation history and detects user intent.
    """
    try:
        logger.info("Received request: %s", request.json)
        data = request.json or {}

        if "query" not in data:
            logger.error("No 'query' provided in request.")
            return jsonify({"error": "No query provided"}), 400

        user_query = data["query"]
        force_model = data.get("forceModel")
        # NEW: Get conversation history from request
        conversation_history = data.get("conversationHistory", [])

        logger.info(
            "Processing query: %s (forceModel=%s) with %d history items",
            user_query,
            force_model,
            len(conversation_history),
        )

        # 1) Quick check for off-topic or trivial
        if not is_car_related(user_query):
            logger.info("Skipping LLM for off-topic/trivial query.")
            return (
                jsonify(
                    {
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
                        ),
                        "intent": "new_query",  # Default intent for off-topic
                    }
                ),
                200,
            )

        # 2) If short or vague but at least car-related, try local retrieval
        query_fragment = extract_newest_user_fragment(user_query)
        if word_count_clean(query_fragment) < 4:
            match_cat, score = find_best_match(query_fragment)

            if score < 0.5:
                logger.info(
                    "Local retrieval found weak match: %s (%.2f)", match_cat, score
                )
                return (
                    jsonify(
                        {
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
                            "clarificationNeededFor": [
                                "category"
                            ],  # NEW: specific clarification reason
                            "matchedCategory": match_cat,
                            "intent": "new_query",
                            "retrieverSuggestion": (
                                f"Sounds like you're after something like '{match_cat}'. "
                                "Could you give me more details — maybe a budget or fuel type?"
                            ),
                        }
                    ),
                    200,
                )

            else:
                logger.info("Local retrieval matched: %s (%.2f)", match_cat, score)
                # NEW: Instead of directly responding, use the matched category to ground the LLM
                # We'll capture the category and pass it through to the LLM call
                matched_category = match_cat
        else:
            matched_category = None

        # 3) Run LLM with conversation history and matched category (if any)
        extracted_params = run_llm_with_history(
            user_query, conversation_history, matched_category, force_model
        )

        if extracted_params:
            logger.info("Final extracted parameters: %s", extracted_params)
            return jsonify(extracted_params), 200
        else:
            logger.error("All models failed or no valid extraction.")
            return jsonify(create_default_parameters(intent="new_query")), 200

    except Exception as e:
        logger.exception("Exception occurred: %s", str(e))
        return jsonify(create_default_parameters(intent="new_query")), 200


def run_llm_with_history(
    user_query: str,
    conversation_history: List[Dict[str, str]],
    matched_category: Optional[str] = None,
    force_model: Optional[str] = None,
) -> Optional[Dict[str, Any]]:
    """
    Enhanced LLM call that considers conversation history and detected categories.
    """
    valid_manufacturers = [
        "BMW",
        "Audi",
        "Mercedes",
        "Toyota",
        "Honda",
        "Ford",
        "Volkswagen",
        "Nissan",
        "Hyundai",
        "Kia",
        "Tesla",
        "Volvo",
        "Mazda",
    ]
    valid_fuel_types = ["Petrol", "Diesel", "Electric", "Hybrid"]
    valid_vehicle_types = [
        # Sedan Aliases
        "Sedan", "Saloon",
        # SUV Aliases
        "SUV", "Crossover", "CUV",
        # Hatchback Aliases
        "Hatchback", "5-door", "hot hatch",
        # Estate/Wagon Aliases
        "Estate", "Wagon", "Touring",
        # Coupe Aliases
        "Coupe", "2-door", "sports car",
        # Convertible Aliases
        "Convertible", "Cabriolet", "Roadster",
        # Pickup Aliases
        "Pickup", "Truck", "flatbed", # Note: C# uses Pickup, Python list had Truck
        # Van Aliases
        "Van", "Minivan", "MPV",
    ]

    model_order = {"fast": FAST_MODEL, "refine": REFINE_MODEL, "clarify": CLARIFY_MODEL}

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

    system_prompt = build_enhanced_system_prompt(
        user_query,
        conversation_history,
        matched_category,
        valid_manufacturers,
        valid_fuel_types,
        valid_vehicle_types,
    )

    # Track if this is likely a follow-up based on conversation history
    is_likely_followup = len(conversation_history) > 0
    has_followup_indicators = any(
        marker in user_query.lower()
        for marker in ["change", "instead", "also show", "but", "rather", "make it"]
    )

    for model in models_to_try:
        extracted = try_extract_with_model(model, system_prompt, user_query)
        logger.info("Model %s returned: %s", model, extracted)

        if extracted and is_valid_extraction(extracted):
            processed = process_parameters(
                extracted, valid_manufacturers, valid_fuel_types, valid_vehicle_types
            )

            # --- Intent Correction Logic  ---
            current_intent = processed.get("intent")
            # Aggressively correct 'new_query' to 'refine_criteria' if any history exists
            if is_likely_followup and current_intent == "new_query":
                logger.info("Correcting intent from new_query to refine_criteria based on history presence.")
                processed["intent"] = "refine_criteria"
            # Ensure intent field exists and has a valid default if missing
            elif not current_intent:
                default_intent = "refine_criteria" if is_likely_followup else "new_query"
                logger.info(f"Setting missing intent to default: {default_intent}")
                processed["intent"] = default_intent

            # --- Post-processing Keyword Overrides ---
            latest_query_fragment = extract_newest_user_fragment(user_query).lower()

            # Make Override
            make_override_phrases = [
                "any make", "any manufacturer", "any brand",
                "dont mind the brand", "don't mind the brand",
                "no specific brand", "no preference on make",
            ]
            if any(phrase in latest_query_fragment for phrase in make_override_phrases):
                if processed.get("preferredMakes"):
                    logger.info("Overriding preferredMakes to empty based on user query trigger words.")
                processed["preferredMakes"] = []

            # Fuel Type Override
            fuel_override_phrases = ["any fuel", "any fuel type", "dont mind fuel", "don't mind fuel"]
            if any(phrase in latest_query_fragment for phrase in fuel_override_phrases):
                if processed.get("preferredFuelTypes"):
                    logger.info("Overriding preferredFuelTypes to empty based on user query.")
                processed["preferredFuelTypes"] = []

            # Vehicle Type Override
            type_override_phrases = ["any type", "any vehicle type", "dont mind type", "don't mind type"]
            if any(phrase in latest_query_fragment for phrase in type_override_phrases):
                if processed.get("preferredVehicleTypes"):
                    logger.info("Overriding preferredVehicleTypes to empty based on user query.")
                processed["preferredVehicleTypes"] = []

            # Price Limit Override
            price_override_phrases = ["any price", "no price limit", "price doesnt matter", "price doesn't matter"]
            if any(phrase in latest_query_fragment for phrase in price_override_phrases):
                if processed.get("minPrice") is not None or processed.get("maxPrice") is not None:
                    logger.info("Overriding price limits to null based on user query.")
                processed["minPrice"] = None
                processed["maxPrice"] = None

            # Year Override
            year_override_phrases = ["any year", "year doesnt matter", "year doesn't matter"]
            if any(phrase in latest_query_fragment for phrase in year_override_phrases):
                if processed.get("minYear") is not None or processed.get("maxYear") is not None:
                    logger.info("Overriding year limits to null based on user query.")
                processed["minYear"] = None
                processed["maxYear"] = None
            # --- End Post-processing Keyword Overrides ---


            # --- Heuristic Corrections (Your existing code) ---
            # Examine query for price indicators and correct if needed
            if "under" in latest_query_fragment or "less than" in latest_query_fragment:
                # Find numbers in the query fragment
                import re  # Make sure re is imported earlier or import here

                numbers = re.findall(r"\d+(?:\.\d+)?", latest_query_fragment)
                # Only correct if the LLM *didn't* already set maxPrice based on the prompt rules
                if numbers and not processed.get("maxPrice"):
                    try:
                        price = float(numbers[-1])  # Use last number found in fragment
                        if price > 1000:  # Likely a price, not a year
                            processed["maxPrice"] = price
                            # Set minPrice too, matching prompt rule for 'under'
                            processed["minPrice"] = None
                            logger.info(
                                f"Corrected missing maxPrice to {price} based on query text trigger 'under'"
                            )
                    except (ValueError, IndexError):
                        pass
            # Add similar block for "over" / "more than" if needed for minPrice correction

            # If the query mentions "relatively new" or "recent" but no year was extracted
            if (
                "relatively new" in latest_query_fragment or "recent" in latest_query_fragment
            ) and not processed.get("minYear"):
                current_year = datetime.datetime.now().year
                processed["minYear"] = current_year - 2
                logger.info(
                    f"Setting minYear to {current_year-2} based on 'relatively new' in query fragment"
                )
            # --- End Heuristic Corrections ---


            # --- Handle clarification fields (Your existing code) ---
            if (
                processed.get("clarificationNeeded", False)
                and "clarificationNeededFor" not in processed
            ):
                missing_params = []
                # Check latest fragment for "any" phrases when determining if clarification needed
                if (
                    not processed.get("preferredMakes")
                    and "any make" not in latest_query_fragment
                    and "any manufacturer" not in latest_query_fragment
                ):
                    missing_params.append("make")
                if (
                    not processed.get("preferredVehicleTypes")
                    and "any type" not in latest_query_fragment
                    and "any vehicle" not in latest_query_fragment
                ):
                    missing_params.append("vehicle_type")
                if (
                    not processed.get("preferredFuelTypes")
                    and "any fuel" not in latest_query_fragment
                ):
                    missing_params.append("fuel_type")
                if not processed.get("minPrice") and not processed.get("maxPrice"):
                    missing_params.append("price")
                if not processed.get("minYear") and not processed.get("maxYear"):
                    missing_params.append("year")

                processed["clarificationNeededFor"] = missing_params
                # We might want to set clarificationNeeded based on missing_params count here
                if missing_params and not processed["clarificationNeeded"]:
                    logger.info(f"Setting clarificationNeeded=True based on missing params: {missing_params}")
                    processed["clarificationNeeded"] = True

            # Don't request clarification if we already have enough info
            if processed.get("clarificationNeeded", False):
                # Key parameters that make clarification unnecessary
                has_price = (
                    processed.get("minPrice") is not None
                    or processed.get("maxPrice") is not None
                )
                # Check the final processed list here
                has_vehicle_type = len(processed.get("preferredVehicleTypes", [])) > 0
                has_makes = len(processed.get("preferredMakes", [])) > 0  # Added makes check

                # If we have type AND (price OR makes), we likely have enough
                if has_vehicle_type and (has_price or has_makes):
                    if processed["clarificationNeeded"]:  # Log only if changing
                        logger.info("Overriding clarificationNeeded to False as minimum viable params exist (Type + Price/Make).")
                    processed["clarificationNeeded"] = False
                    processed["clarificationNeededFor"] = []

            return processed  # Return the fully processed dictionary

    logger.error("All models failed or no valid extraction.")
    return None


def run_llm_fallback(
    user_query: str, force_model: Optional[str]
) -> Optional[Dict[str, Any]]:
    """
    Fallback chain: fast → refine → clarify (or forced model).
    """
    valid_manufacturers = [
        "BMW",
        "Audi",
        "Mercedes",
        "Toyota",
        "Honda",
        "Ford",
        "Volkswagen",
        "Nissan",
        "Hyundai",
        "Kia",
        "Tesla",
        "Volvo",
        "Mazda",
    ]
    valid_fuel_types = ["Petrol", "Diesel", "Electric", "Hybrid"]
    valid_vehicle_types = [
        "Sedan",
        "SUV",
        "Hatchback",
        "Coupe",
        "Convertible",
        "Wagon",
        "Van",
        "Truck",
    ]

    model_order = {"fast": FAST_MODEL, "refine": REFINE_MODEL, "clarify": CLARIFY_MODEL}

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
        user_query, valid_manufacturers, valid_fuel_types, valid_vehicle_types
    )

    for model in models_to_try:
        extracted = try_extract_with_model(model, system_prompt, user_query)
        logger.info("Model %s returned: %s", model, extracted)

        if extracted and is_valid_extraction(extracted):
            processed = process_parameters(
                extracted, valid_manufacturers, valid_fuel_types, valid_vehicle_types
            )
            return processed

    return None


def build_system_prompt(
    user_query: str,
    valid_makes: List[str],
    valid_fuels: List[str],
    valid_vehicles: List[str],
) -> str:
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
# Ensure ALL mentioned makes/types/fuels that are in the VALID lists below are extracted.
# Example: If user says "Toyota or Honda", extract ["Toyota", "Honda"].
- Extract ALL mentioned makes that appear in the Valid Makes list. If the user lists multiple valid makes (e.g., 'Toyota or Honda or BMW'), include all of them in the preferredMakes array.
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
- User: "Maybe Toyota or BMW" -> {{"preferredMakes": ["Toyota", "BMW"], "intent": "new_query"}}
- "hello" => isOffTopic=false

User query: {user_query}

IMPORTANT: Return ONLY the JSON object with no additional text.
"""


def build_enhanced_system_prompt(
    user_query: str,
    conversation_history: List[Dict[str, str]],
    matched_category: Optional[str],
    valid_makes: List[str],
    valid_fuels: List[str],
    valid_vehicles: List[str],
) -> str:
    """
    Build an enhanced system prompt that includes conversation history
    and detected categories to guide the LLM.
    """
    history_context = ""
    if conversation_history:
        history_context = "Recent conversation history:\n"
        # Format the last 2-3 turns (limited for context window)
        for turn in conversation_history[-3:]:
            if "user" in turn:
                history_context += f"User: {turn['user']}\n"
            if "ai" in turn:
                history_context += f"Assistant: {turn['ai']}\n"
        history_context += "\n"

    category_context = ""
    if matched_category:
        category_context = (
            f"Context: The user might be asking about {matched_category} vehicles.\n\n"
        )

    current_year = datetime.datetime.now().year

    return f"""
You are an automotive assistant for Smart Auto Trader, helping customers find their ideal vehicle.
Extract search parameters from the user's query, considering the conversation history.

{history_context}
{category_context}

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
  "offTopicResponse": null,
  "clarificationNeeded": false,
  "clarificationNeededFor": [],
  "intent": "new_query"
}}

RULES:
- ONLY include values the user explicitly requests
- All numeric values must be numbers or null
- Leave arrays EMPTY unless specifically mentioned
- Set 'isOffTopic' to true ONLY for non-car-related queries
- For 'intent', choose one of these values:
  * "new_query" - This is a completely new search with no relation to previous searches
  * "replace_criteria" - User wants to replace previous search criteria
  * "add_criteria" - User wants to add to previous criteria (e.g., "also show me...")
  * "refine_criteria" - User wants to narrow down previous results or change parameters

PRICE HANDLING:
- When the user says "under X" or "less than X", set maxPrice to X and minPrice to null
- When the user says "over X" or "more than X", set minPrice to X and maxPrice to null
- When the user says "between X and Y", set minPrice to X and maxPrice to Y
- When the user says "around X", set minPrice to 0.8*X and maxPrice to 1.2*X

YEAR AND AGE HANDLING:
- When the user asks for "relatively new" or "recent" models, set minYear to {current_year-2}
- When the user asks for "new" vehicles, set minYear to {current_year-1}
- When the user asks for "brand new" vehicles, set minYear to {current_year}
- When the user specifies "older", set maxYear to {current_year-5}

GENERAL PREFERENCES:
- If the user says "any make" or "any manufacturer", keep preferredMakes as an empty array []
- If the user says "any fuel type", keep preferredFuelTypes as an empty array []
- If the user says "any vehicle type", keep preferredVehicleTypes as an empty array []

INTENT DETERMINATION (CRITICAL):
- Any follow-up message should almost never be "new_query" unless it's completely unrelated to previous messages
- If the user is modifying a previous search (changing price, adding makes, etc.), use "refine_criteria"
- If the user says "change that to", "make it", or similar phrases, use "refine_criteria"
- If the user says "also include", "also show", "add", use "add_criteria"
- If the user completely changes focus (e.g., from SUVs to sedans only), use "replace_criteria"
- When in doubt for follow-ups, prefer "refine_criteria" over "new_query"

EXAMPLES:
- "I need an SUV under 50000" → {{"maxPrice": 50000, "preferredVehicleTypes": ["SUV"], "intent": "new_query"}}
- "Change it to under 70000" → {{"maxPrice": 70000, "intent": "refine_criteria"}}
- "Also show me sedans" → {{"preferredVehicleTypes": ["Sedan"], "intent": "add_criteria"}}
- "I want a Tesla" → {{"preferredMakes": ["Tesla"], "intent": "replace_criteria"}}
- "BMW SUVs only" → {{"preferredMakes": ["BMW"], "preferredVehicleTypes": ["SUV"], "intent": "replace_criteria"}}

Valid makes: {', '.join(valid_makes)}
Valid fuel types: {', '.join(valid_fuels)}
Valid vehicle types: {', '.join(valid_vehicles)}

The user's query is: "{user_query}"
"""


def try_extract_with_model(
    model: str, system_prompt: str, user_query: str
) -> Optional[Dict[str, Any]]:
    """
    Attempt to get valid JSON from an LLM model via OpenRouter.
    Returns a dict if successful, else None.
    """
    try:
        headers = {
            "Authorization": f"Bearer {OPENROUTER_API_KEY}",
            "Content-Type": "application/json",
            "HTTP-Referer": "https://smartautotrader.app",
            "X-Title": "Smart Auto Trader",
        }
        payload = {
            "model": model,
            "messages": [
                {"role": "system", "content": system_prompt},
                {"role": "user", "content": user_query},
            ],
            "temperature": 0.3,
            "max_tokens": 500,
        }

        logger.info("Sending request to OpenRouter with model: %s", model)
        response = requests.post(
            OPENROUTER_URL, headers=headers, json=payload, timeout=30
        )

        if response.status_code != 200:
            logger.error(
                "OpenRouter API call failed. Status: %s, Body: %s",
                response.status_code,
                response.text,
            )
            return None

        response_data = response.json()
        logger.debug(
            "Full OpenRouter response: %s", json.dumps(response_data, indent=2)
        )

        if not response_data.get("choices") or not response_data["choices"][0].get(
            "message"
        ):
            logger.error("Invalid response format from OpenRouter: %s", response_data)
            return None

        generated_text = response_data["choices"][0]["message"]["content"]
        logger.info("Raw model output: %s", generated_text)

        # Attempt to parse JSON
        if "{" in generated_text and "}" in generated_text:
            json_start = generated_text.find("{")
            json_end = generated_text.rfind("}") + 1
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
    More lenient for follow-up or refinement queries than for new queries.
    """
    if not isinstance(params, dict):
        return False

    # Check for intent to determine validation strictness
    intent = params.get("intent", "new_query").lower()
    is_refinement = intent in ["refine_criteria", "add_criteria", "replace_criteria"]

    # Count numeric fields that are not null
    numeric_fields = ["minPrice", "maxPrice", "minYear", "maxYear", "maxMileage"]
    numeric_count = sum(
        1
        for field in numeric_fields
        if params.get(field) is not None and isinstance(params[field], (int, float))
    )

    # Count array fields that are non-empty
    array_fields = [
        "preferredMakes",
        "preferredFuelTypes",
        "preferredVehicleTypes",
        "desiredFeatures",
    ]
    array_count = sum(
        1
        for field in array_fields
        if isinstance(params.get(field), list) and len(params[field]) > 0
    )

    # Off-topic is valid if true + offTopicResponse
    is_off_topic = (
        params.get("isOffTopic") is True
        and params.get("offTopicResponse")
        and isinstance(params["offTopicResponse"], str)
    )

    # Check specifically for price and vehicle type as they are high-value parameters
    has_price = params.get("minPrice") is not None or params.get("maxPrice") is not None
    has_vehicle_type = (
        isinstance(params.get("preferredVehicleTypes"), list)
        and len(params.get("preferredVehicleTypes", [])) > 0
    )

    total_valid = numeric_count + array_count

    # Multiple validation paths for different scenarios
    if is_off_topic:
        return True
    elif is_refinement:
        # For refinement queries, be very lenient - only 1 field needed
        return total_valid >= 1
    elif has_price and has_vehicle_type:
        # Price + vehicle type is a high-quality combination
        return True
    elif has_price and array_count > 0:
        # Price + any array field is good enough
        return True
    elif has_vehicle_type and numeric_count > 0:
        # Vehicle type + any numeric field is good enough
        return True
    else:
        # For other cases, require only 2 valid fields instead of 3
        return total_valid >= 2


def create_default_parameters(
    makes: Optional[List[str]] = None,
    fuel_types: Optional[List[str]] = None,
    vehicle_types: Optional[List[str]] = None,
    intent: str = "new_query",
) -> Dict[str, Any]:
    """
    Returns a fallback set of parameters if no extraction succeeded.
    """
    return {
        "minPrice": None,
        "maxPrice": None,
        "minYear": None,
        "maxYear": None,
        "maxMileage": None,
        "preferredMakes": makes or [],
        "preferredFuelTypes": fuel_types or [],
        "preferredVehicleTypes": vehicle_types or [],
        "desiredFeatures": [],
        "isOffTopic": False,
        "offTopicResponse": None,
        "clarificationNeeded": False,
        "clarificationNeededFor": [],
        "intent": intent,
    }


def process_parameters(
    params: Dict[str, Any],
    valid_makes: List[str],
    valid_fuel_types: List[str],
    valid_vehicle_types: List[str],
) -> Dict[str, Any]:
    """
    Convert raw extracted dict into final structure, applying validation
    and only filling defaults when appropriate.
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
        "offTopicResponse": None,
        "intent": "new_query",
        "clarificationNeeded": False,
        "clarificationNeededFor": [],
    }

    # numeric fields
    for field in ["minPrice", "maxPrice", "minYear", "maxYear", "maxMileage"]:
        if (
            field in params
            and params[field] is not None
            and isinstance(params[field], (int, float))
        ):
            result[field] = params[field]

    # array fields with validation - no default filling
    if isinstance(params.get("preferredMakes"), list):
        result["preferredMakes"] = [
            m
            for m in params["preferredMakes"]
            if isinstance(m, str) and m in valid_makes
        ]

    if isinstance(params.get("preferredFuelTypes"), list):
        result["preferredFuelTypes"] = [
            f
            for f in params["preferredFuelTypes"]
            if isinstance(f, str) and f in valid_fuel_types
        ]

    if isinstance(params.get("preferredVehicleTypes"), list):
        result["preferredVehicleTypes"] = [
            v
            for v in params["preferredVehicleTypes"]
            if isinstance(v, str) and v in valid_vehicle_types
        ]

    if isinstance(params.get("desiredFeatures"), list):
        result["desiredFeatures"] = [
            f for f in params["desiredFeatures"] if isinstance(f, str)
        ]

    # Copy intent field if it exists
    if "intent" in params and isinstance(params["intent"], str):
        result["intent"] = params["intent"].lower()

    # Copy clarification fields
    if "clarificationNeeded" in params and isinstance(
        params["clarificationNeeded"], bool
    ):
        result["clarificationNeeded"] = params["clarificationNeeded"]

    if "clarificationNeededFor" in params and isinstance(
        params["clarificationNeededFor"], list
    ):
        result["clarificationNeededFor"] = [
            item for item in params["clarificationNeededFor"] if isinstance(item, str)
        ]

    # off-topic check
    if isinstance(params.get("isOffTopic"), bool):
        result["isOffTopic"] = params["isOffTopic"]

    if result["isOffTopic"] and isinstance(params.get("offTopicResponse"), str):
        result["offTopicResponse"] = params["offTopicResponse"]

    # Copy retriever suggestion if present
    if "retrieverSuggestion" in params and isinstance(
        params["retrieverSuggestion"], str
    ):
        result["retrieverSuggestion"] = params["retrieverSuggestion"]

    # Don't add default values - let the backend handle this case

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
        "car",
        "vehicle",
        "suv",
        "bmw",
        "ford",
        "toyota",
        "honda",
        "cheap",
        "fuel",
        "mileage",
        "hybrid",
        "electric",
        "kia",
        "tesla",
        "under",
        "over",
    ]
    msg_lower = msg.lower()

    return any(kw in msg_lower for kw in car_keywords)


if __name__ == "__main__":
    app.run(host="0.0.0.0", port=5006, debug=True)
