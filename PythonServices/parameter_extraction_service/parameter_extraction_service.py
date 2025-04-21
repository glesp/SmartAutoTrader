#!/usr/bin/env python3
# Standard library imports first
import json
import logging
import re
import os
import sys
import datetime
from typing import Dict, List, Optional, Any

# Third-party imports
from flask import Flask, request, jsonify
import requests
from dotenv import load_dotenv
import numpy as np

# Local application imports
# Ensure the path is correct for importing from the sibling 'retriever' directory
# This assumes parameter_extraction_service.py is run from its own directory
sys.path.append(os.path.dirname(os.path.abspath(__file__)))
try:
    from retriever.retriever import (
        initialize_retriever,
        get_query_embedding,  # Assumes this function exists in retriever.py now
        cosine_sim,  # Assumes this function exists in retriever.py
        find_best_match,
    )
except ImportError as ie:
    logging.error(
        f"Could not import from retriever package: {ie}. "
        f"Make sure retriever directory is structured correctly and "
        f"contains __init__.py if needed."
    )

    # Define dummy functions to prevent NameErrors if import fails, allowing app to potentially start
    # but functionality will be broken. Proper fix is ensuring retriever package is correct.
    def initialize_retriever():
        logger.error("retriever.initialize_retriever failed to import!")

    def get_query_embedding(text):
        logger.error("retriever.get_query_embedding failed to import!")
        return None

    def cosine_sim(a, b):
        logger.error("retriever.cosine_sim failed to import!")
        return 0.0

    def find_best_match(text):
        logger.error("retriever.find_best_match failed to import!")
        return "error", 0.0


load_dotenv()

# Configure logging
logging.basicConfig(
    level=logging.INFO, format="%(asctime)s - %(name)s - %(levelname)s - %(message)s"
)
logger = logging.getLogger(__name__)

# --- Constants and Globals ---
OPENROUTER_API_KEY = os.environ.get("OPENROUTER_API_KEY")
if not OPENROUTER_API_KEY:
    logger.warning(
        "OPENROUTER_API_KEY environment variable not set. API calls will fail."
    )
OPENROUTER_URL = "https://openrouter.ai/api/v1/chat/completions"
FAST_MODEL = "meta-llama/llama-3.1-8b-instruct:free"
REFINE_MODEL = "google/gemma-3-27b-it:free"
CLARIFY_MODEL = "mistralai/mistral-7b-instruct:free"

# Define intent labels for Zero-Shot Classification
INTENT_LABELS = {
    "SPECIFIC_SEARCH": "Search query containing specific vehicle parameters like make "
    "(e.g., Toyota), model (e.g., Camry), exact year (e.g., 2021), price range "
    "(e.g., under 20k), fuel type (e.g., petrol), mileage, or explicit technical features "
    "(e.g., sunroof).",
    "VAGUE_INQUIRY": "General question seeking recommendations "
    "(e.g., 'what car is good for families', 'recommend something reliable'), advice, "
    "or stating general qualities (e.g. 'reliable', 'cheap', 'safe', 'economical') "
    "without providing specific vehicle parameters like make, model, year, or price.",
}
# Dictionary to hold precomputed embeddings for labels
PRECOMPUTED_LABEL_EMBEDDINGS = {}

# Lists for validating extracted parameters (moved here for potential reuse)
VALID_MANUFACTURERS = [
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
    "Skoda",
    "Lexus",
]
VALID_FUEL_TYPES = ["Petrol", "Diesel", "Electric", "Hybrid"]
VALID_VEHICLE_TYPES = [
    "Sedan",
    "Saloon",
    "SUV",
    "Crossover",
    "CUV",
    "Hatchback",
    "5-door",
    "hot hatch",
    "Estate",
    "Wagon",
    "Touring",
    "Coupe",
    "2-door",
    "sports car",
    "Convertible",
    "Cabriolet",
    "Roadster",
    "Pickup",
    "Truck",
    "flatbed",
    "Van",
    "Minivan",
    "MPV",
]


# --- Helper Function Definitions (Defined Before Routes) ---


def word_count_clean(query: str) -> int:
    """Counts meaningful words in a cleaned-up user query."""
    cleaned = re.sub(r"[^a-zA-Z0-9 ]+", "", query).lower()
    return len(cleaned.strip().split())


def extract_newest_user_fragment(query: str) -> str:
    """
    For follow-up queries like "Original - Additional info: blah",
    return only the latest user input.
    """
    # Use rsplit to handle multiple occurrences, splitting only once from the right
    parts = query.rsplit(" - Additional info:", 1)
    if len(parts) > 1:
        return parts[-1].strip()
    else:
        return query.strip()  # Return original if pattern not found


def initialize_app_components():
    """Initialize retriever and precompute intent embeddings."""
    global PRECOMPUTED_LABEL_EMBEDDINGS
    logger.info("Initializing app components...")
    try:
        initialize_retriever()  # Initialize RAG retriever (loads model, category embeddings)
        logger.info("Retriever initialized successfully.")

        logger.info("Pre-computing intent label embeddings...")
        embeddings_computed = True
        temp_embeddings = {}
        for label, description in INTENT_LABELS.items():
            embedding = get_query_embedding(
                description
            )  # Use the embedding function from retriever
            if embedding is not None:
                temp_embeddings[label] = embedding
            else:
                logger.error(f"Failed to compute embedding for intent label: {label}")
                embeddings_computed = False

        PRECOMPUTED_LABEL_EMBEDDINGS = temp_embeddings  # Assign after loop finishes

        if embeddings_computed and PRECOMPUTED_LABEL_EMBEDDINGS:
            logger.info("Successfully pre-computed all intent label embeddings.")
        elif not PRECOMPUTED_LABEL_EMBEDDINGS:
            logger.error(
                "No intent label embeddings were computed. Intent classification disabled."
            )
        else:
            logger.warning(
                "Failed to compute some intent label embeddings. Classification might be degraded."
            )

    except Exception as e:
        logger.error(f"Error during app component initialization: {e}", exc_info=True)
        PRECOMPUTED_LABEL_EMBEDDINGS = {}  # Ensure it's empty on error


def classify_intent_zero_shot(
    query_embedding: np.ndarray, threshold: float = 0.6
) -> Optional[str]:
    """Classifies intent using cosine similarity against precomputed label embeddings."""
    if not PRECOMPUTED_LABEL_EMBEDDINGS:
        logger.warning(
            "Label embeddings not available, skipping intent classification."
        )
        return None  # Return None if embeddings aren't ready

    if query_embedding is None:
        logger.error("Received None for query_embedding in classify_intent_zero_shot.")
        return None

    try:
        similarities = {}
        for label, label_embedding in PRECOMPUTED_LABEL_EMBEDDINGS.items():
            # Ensure embeddings are valid numpy arrays before calculating similarity
            if isinstance(label_embedding, np.ndarray) and isinstance(
                query_embedding, np.ndarray
            ):
                similarities[label] = cosine_sim(query_embedding, label_embedding)
            else:
                logger.warning(
                    f"Skipping invalid embedding type for label '{label}' or query during classification."
                )

        if not similarities:
            logger.warning(
                "No similarities calculated (embeddings might be missing/invalid)."
            )
            return None

        best_label = max(similarities, key=similarities.get)
        best_score = float(similarities[best_label])  # Cast to float explicitly

        formatted_scores = {k: f"{v:.2f}" for k, v in similarities.items()}
        logger.info(
            f"Intent classification scores: {formatted_scores}"
        )  # Fixed log statement

        if best_score >= threshold:
            logger.info(f"Classified intent: {best_label} (Score: {best_score:.2f})")
            return best_label
        else:
            # --- NEW FALLBACK LOGIC ---
            logger.info(
                f"Intent classification score ({best_score:.2f}) below threshold ({threshold}).Applying fallback logic"
            )
            # Check if VAGUE_INQUIRY had the highest (but below threshold) score, OR if both scores are extremely low
            specific_score = similarities.get("SPECIFIC_SEARCH", 0.0)
            vague_score = similarities.get("VAGUE_INQUIRY", 0.0)
            # You might adjust this lower threshold (e.g., 0.25 or 0.3) based on testing
            very_low_threshold = 0.3

            if best_label == "VAGUE_INQUIRY" or (
                specific_score < very_low_threshold and vague_score < very_low_threshold
            ):
                logger.info(
                    "Defaulting to VAGUE_INQUIRY based on fallback logic (Vague was highest or both very low)."
                )
                return "VAGUE_INQUIRY"
            else:
                # Default to SPECIFIC_SEARCH only if it had the highest score (but was still below the main threshold)
                # or if something unexpected happened.
                logger.info("Defaulting to SPECIFIC_SEARCH based on fallback logic.")
                return "SPECIFIC_SEARCH"
            # --- END NEW FALLBACK LOGIC ---
    except Exception as e:
        logger.error(
            f"Error during cosine similarity or intent classification logic: {e}",
            exc_info=True,
        )
        return None  # Return None on error


def is_car_related(query: str) -> bool:
    """Simple check if the query seems car-related."""
    if not query:
        return False
    query_lower = query.lower()
    
    # Combine keywords for better readability
    car_keywords = {
        "car", "vehicle", "auto", "automobile", "sedan", "suv", "truck",
        "hatchback", "coupe", "convertible", "van", "minivan", "electric",
        "hybrid", "diesel", "petrol", "gasoline", "make", "model", "year", 
        "price", "mileage", "engine", "transmission", "drive", "buy", "sell",
        "lease", "dealer", "used", "new", "road tax", "nct", "insurance", "mpg", "kpl",
        "automatic", "manual", "auto", "stick shift", "paddle shift", "dsg", "cvt",
        "engine size", "liter", "litre", "cc", "cubic", "displacement", "horsepower",
        "hp", "bhp", "power", "torque", "performance", "l engine", "cylinder"
    }
    
    # Add common makes dynamically from the global list
    car_keywords.update(make.lower() for make in VALID_MANUFACTURERS)

    # Check for presence of keywords
    if any(keyword in query_lower for keyword in car_keywords):
        return True

    # Enhanced off-topic detection - include more greetings
    off_topic_starts = (
        "hi", "hello", "how are you", "who is", "tell me a joke", 
        "hey", "hey there", "yo", "sup", "what's up", "hiya", "howdy", 
        "good morning", "good afternoon", "good evening"
    )
    
    # Improved check: either starts with or equals one of these phrases
    if query_lower.startswith(off_topic_starts) or query_lower in off_topic_starts:
        return False
        
    # Check for questions that are unlikely car related unless containing keywords
    if query_lower.startswith(("what is", "what are", "where is")) and not any(
        kw in query_lower for kw in ["car", "vehicle", "suv", "sedan"]
    ):
        return False

    # Consider very short queries potentially off-topic unless they meet certain conditions
    if word_count_clean(query) < 2:
        # Short query is only car-related if it contains specific car terminology
        if not any(keyword in query_lower for keyword in car_keywords):
            # Single make names like "BMW" or "Audi" should be considered car-related
            if not any(make.lower() == query_lower for make in VALID_MANUFACTURERS):
                return False

    # Default to assuming it might be car-related if not caught by above rules
    return True


def create_default_parameters(
    intent: str = "new_query",
    is_off_topic: bool = False,
    off_topic_response: Optional[str] = None,
    clarification_needed: bool = False,
    clarification_needed_for: Optional[List[str]] = None,
    retriever_suggestion: Optional[str] = None,
    matched_category: Optional[str] = None,
) -> Dict[str, Any]:
    """Returns a fallback or default set of parameters."""
    return {
        "minPrice": None,
        "maxPrice": None,
        "minYear": None,
        "maxYear": None,
        "maxMileage": None,
        "preferredMakes": [],
        "preferredFuelTypes": [],
        "preferredVehicleTypes": [],
        "desiredFeatures": [],
        "isOffTopic": is_off_topic,
        "offTopicResponse": off_topic_response,
        "clarificationNeeded": clarification_needed,
        "clarificationNeededFor": clarification_needed_for or [],
        "retrieverSuggestion": retriever_suggestion,
        "matchedCategory": matched_category,
        "intent": intent,
        "transmission": None,
        "minEngineSize": None,
        "maxEngineSize": None,
        "minHorsepower": None,
        "maxHorsepower": None,
    }


def try_extract_with_model(
    model: str, system_prompt: str, user_query: str
) -> Optional[Dict[str, Any]]:
    """Attempt to get valid JSON from an LLM model via OpenRouter."""
    if not OPENROUTER_API_KEY:
        logger.error("OpenRouter API Key is not configured. Cannot make API call.")
        return None
    try:
        headers = {
            "Authorization": f"Bearer {OPENROUTER_API_KEY}",
            "Content-Type": "application/json",
            "HTTP-Referer": "https://smartautotrader.app",  # Replace with your actual site URL if possible
            "X-Title": "SmartAutoTraderParameterExtraction",  # Replace with your actual app name if possible
        }
        payload = {
            "model": model,
            "messages": [
                {"role": "system", "content": system_prompt},
                {"role": "user", "content": user_query},
            ],
            "temperature": 0.2,  # Slightly lower for more deterministic extraction
            "max_tokens": 600,  # Increased slightly just in case
            # Consider adding response_format if supported by all models used
            # "response_format": {"type": "json_object"},
        }

        logger.info(f"Sending request to OpenRouter (Model: {model})...")
        response = requests.post(
            OPENROUTER_URL,
            headers=headers,
            json=payload,
            timeout=45,  # Increased timeout
        )

        if response.status_code != 200:
            logger.error(
                f"OpenRouter API call failed for model {model}. Status: {response.status_code}, Body: {response.text}"
            )
            return None

        response_data = response.json()
        logger.debug(
            f"Full OpenRouter response for model {model}: {json.dumps(response_data, indent=2)}"
        )

        if not response_data.get("choices") or not response_data["choices"][0].get(
            "message"
        ):
            logger.error(
                f"Invalid response format from OpenRouter model {model}: {response_data}"
            )
            return None

        generated_text = response_data["choices"][0]["message"]["content"]
        logger.info(f"Raw output from model {model}: {generated_text}")

        # Attempt to parse JSON robustly
        # Look for ```json ... ``` blocks first
        match = re.search(
            r"```json\s*(\{.*?\})\s*```", generated_text, re.DOTALL | re.IGNORECASE
        )
        json_str = None
        if match:
            json_str = match.group(1).strip()
            logger.info("Extracted JSON from Markdown block.")
        elif "{" in generated_text and "}" in generated_text:
            # Fallback: find first '{' and last '}'
            json_start = generated_text.find("{")
            json_end = generated_text.rfind("}") + 1
            if json_start != -1 and json_end != -1:
                json_str = generated_text[json_start:json_end].strip()
                logger.info("Extracted JSON using find method.")

        if json_str:
            try:
                extracted = json.loads(json_str)
                # Basic check for expected structure
                if isinstance(extracted, dict) and "intent" in extracted:
                    logger.info(
                        f"Successfully parsed JSON from model {model}: {extracted}"
                    )
                    return extracted
                else:
                    logger.warning(
                        f"Parsed JSON from model {model} lacks expected structure: {extracted}"
                    )
                    return None
            except json.JSONDecodeError as je:
                logger.error(
                    f"JSON decoding failed for model {model}: {je}. "
                    f"JSON string was: '{json_str}'"
                )
                return None
        else:
            logger.warning(f"No JSON object found in model {model} output.")
            return None

    except requests.exceptions.Timeout:
        logger.error(f"Request timed out calling OpenRouter model {model}.")
        return None
    except (
        requests.exceptions.RequestException
    ) as req_ex:  # Use req_ex (or request_exception, or e)
        logger.error(
            f"Network error calling OpenRouter model {model}: {req_ex}", exc_info=True
        )  # Update the log too
        return None
    except Exception as e:
        logger.exception(
            f"Unhandled exception in try_extract_with_model (Model: {model}): {e}"
        )
        return None


def is_valid_extraction(params: Dict[str, Any]) -> bool:
    """Decide if the extracted JSON seems plausible."""
    if not isinstance(params, dict):
        return False

    # Allow off-topic responses
    if params.get("isOffTopic") is True and isinstance(
        params.get("offTopicResponse"), str
    ):
        return True

    # Check for at least one non-null parameter or non-empty list
    # More specific checks could be added if needed
    has_price = params.get("minPrice") is not None or params.get("maxPrice") is not None
    has_year = params.get("minYear") is not None or params.get("maxYear") is not None
    has_mileage = params.get("maxMileage") is not None
    has_makes = (
        isinstance(params.get("preferredMakes"), list)
        and len(params["preferredMakes"]) > 0
    )
    has_fuel = (
        isinstance(params.get("preferredFuelTypes"), list)
        and len(params["preferredFuelTypes"]) > 0
    )
    has_type = (
        isinstance(params.get("preferredVehicleTypes"), list)
        and len(params["preferredVehicleTypes"]) > 0
    )
    has_features = (
        isinstance(params.get("desiredFeatures"), list)
        and len(params["desiredFeatures"]) > 0
    )

    # Consider it valid if at least one criterion is set (or clarification is needed)
    is_sufficient = (
        has_price
        or has_year
        or has_mileage
        or has_makes
        or has_fuel
        or has_type
        or has_features
    )
    needs_clarification = params.get("clarificationNeeded", False) is True

    if is_sufficient or needs_clarification:
        return True
    else:
        logger.warning(
            f"Extracted parameters deemed invalid (no criteria set and no clarification needed): "
            f"{params}"
        )
        return False


def process_parameters(
    params: Dict[str, Any],
) -> Dict[str, Any]:
    """
    Cleans and validates the structure of parameters extracted by the LLM.
    Uses globally defined VALID_ lists for validation.
    """
    # Start with default structure to ensure all fields exist
    result = create_default_parameters()  

    try:
        # Handle numeric fields with proper type validation
        for field in ["minPrice", "maxPrice"]:
            if field in params and isinstance(params[field], (int, float)) and params[field] is not None:
                # Ensure values are positive
                if params[field] > 0:
                    result[field] = float(params[field])
                else:
                    logger.warning(f"Invalid {field} value: {params[field]} (must be positive)")
                    
        for field in ["minYear", "maxYear", "maxMileage"]:
            if field in params and isinstance(params[field], (int, float)) and params[field] is not None:
                # Basic range validation
                current_year = datetime.datetime.now().year
                if field == "minYear" and params[field] > 1900 and params[field] <= current_year + 1:
                    result[field] = int(params[field])
                elif field == "maxYear" and params[field] > 1900 and params[field] <= current_year + 1:
                    result[field] = int(params[field])
                elif field == "maxMileage" and params[field] > 0:
                    result[field] = int(params[field])
                else:
                    logger.warning(f"Invalid {field} value: {params[field]} (out of reasonable range)")

        # Handle array fields with validation against known valid values
        if isinstance(params.get("preferredMakes"), list):
            result["preferredMakes"] = [
                m
                for m in params["preferredMakes"]
                if isinstance(m, str) and m in VALID_MANUFACTURERS  # Validate against list
            ]
            
        if isinstance(params.get("preferredFuelTypes"), list):
            result["preferredFuelTypes"] = [
                f
                for f in params["preferredFuelTypes"]
                if isinstance(f, str) and f in VALID_FUEL_TYPES  # Validate against list
            ]
            
        if isinstance(params.get("preferredVehicleTypes"), list):
            result["preferredVehicleTypes"] = [
                v
                for v in params["preferredVehicleTypes"]
                if isinstance(v, str) and v in VALID_VEHICLE_TYPES  # Validate against list
            ]
            
        if isinstance(params.get("desiredFeatures"), list):
            result["desiredFeatures"] = [
                f
                for f in params["desiredFeatures"]
                if isinstance(f, str)  # Basic validation only (feature list is more open-ended)
            ]
            
        # Handle boolean flags
        if isinstance(params.get("isOffTopic"), bool):
            result["isOffTopic"] = params["isOffTopic"]
            
        if isinstance(params.get("clarificationNeeded"), bool):
            result["clarificationNeeded"] = params["clarificationNeeded"]
            
        # Handle string fields
        if "offTopicResponse" in params and isinstance(params["offTopicResponse"], str):
            result["offTopicResponse"] = params["offTopicResponse"]
            
        if "retrieverSuggestion" in params and isinstance(params["retrieverSuggestion"], str):
            result["retrieverSuggestion"] = params["retrieverSuggestion"]
            
        if "matchedCategory" in params and isinstance(params["matchedCategory"], str):
            result["matchedCategory"] = params["matchedCategory"]
            
        # Process intent with validation
        if "intent" in params and isinstance(params["intent"], str):
            valid_intents = ["new_query", "clarify", "refine_criteria", "add_criteria", "replace_criteria", "error", "off_topic"]
            intent = params["intent"].lower()
            if intent in valid_intents:
                result["intent"] = intent
            else:
                logger.warning(f"Unknown intent '{intent}', defaulting to 'new_query'")
                result["intent"] = "new_query"
        else:
            result["intent"] = "new_query"  # Default if missing
            
        # Process clarificationNeededFor as array of strings
        if isinstance(params.get("clarificationNeededFor"), list):
            result["clarificationNeededFor"] = [
                item for item in params["clarificationNeededFor"] 
                if isinstance(item, str)
            ]
        
        # Handle transmission
        if "transmission" in params and isinstance(params["transmission"], str):
            transmission_value = params["transmission"].strip().lower()
            if transmission_value in ["automatic", "manual"]:
                # Store with first letter capitalized for consistency
                result["transmission"] = transmission_value.capitalize()
            else:
                logger.warning(f"Invalid transmission value: {params['transmission']}")
        
        # Handle engine size (as float)
        for field in ["minEngineSize", "maxEngineSize"]:
            if field in params and isinstance(params[field], (int, float)) and params[field] is not None:
                # Basic range validation for engine size (0.5L to 10.0L is reasonable)
                if params[field] >= 0.5 and params[field] <= 10.0:
                    result[field] = float(params[field])
                else:
                    logger.warning(f"Invalid {field} value: {params[field]} (outside reasonable range)")
        
        # Handle horsepower (as int)
        for field in ["minHorsepower", "maxHorsepower"]:
            if field in params and isinstance(params[field], (int, float)) and params[field] is not None:
                # Basic range validation for horsepower (20 to 1500 is reasonable)
                if params[field] >= 20 and params[field] <= 1500:
                    result[field] = int(params[field])
                else:
                    logger.warning(f"Invalid {field} value: {params[field]} (outside reasonable range)")
        
    except Exception as e:
        logger.exception(f"Error during parameter processing: {e}")
        # We return the default result if processing fails
            
    return result


def build_system_prompt(
    valid_makes: List[str],
    valid_fuels: List[str],
    valid_vehicles: List[str],
) -> str:
    """Builds the basic system prompt for simple extraction."""
    default_json_keys = create_default_parameters().keys()
    json_format_example = json.dumps(create_default_parameters(), indent=2)

    return f"""
You are an automotive parameter extraction assistant.
Analyze the user query and extract ONLY the search parameters EXPLICITLY mentioned.
YOUR RESPONSE MUST BE ONLY A SINGLE VALID JSON OBJECT containing the following keys: {list(default_json_keys)}.
Use this exact format, filling values where mentioned, otherwise use null or empty lists []:
{json_format_example}

RULES:
- ONLY include values the user explicitly states. DO NOT INFER.
- Numeric values MUST be numbers (int or float), not strings. Use null if not mentioned.
- Array values (preferredMakes, preferredFuelTypes, preferredVehicleTypes,
  desiredFeatures, clarificationNeededFor) MUST be lists of strings.
  Use [] if not mentioned.
- Booleans (isOffTopic, clarificationNeeded) must be true or false.
- Strings (offTopicResponse, retrieverSuggestion, matchedCategory, intent) must be strings or null.
- Extract makes ONLY if they appear in the Valid Makes list.
  If multiple valid makes are mentioned, include all.
- Extract fuel types ONLY if they appear in the Valid Fuel Types list.
  Include all mentioned valid types.
- Extract vehicle types ONLY if they appear in the Valid Vehicle Types list.
  Include all mentioned valid types.
- desiredFeatures: Extract any mentioned features (e.g., "sunroof", "leather seats", "parking sensors").
- intent: Set to "new_query".
- isOffTopic: Set to true ONLY if the query is clearly NOT about buying/searching for vehicles.
  If true, set offTopicResponse to a brief explanation.
- clarificationNeeded: Set to false for this basic prompt.
- clarificationNeededFor: Set to [] for this basic prompt.

VALID VALUES:
- Valid Makes: {json.dumps(valid_makes)}
- Valid Fuel Types: {json.dumps(valid_fuels)}
- Valid Vehicle Types (includes aliases): {json.dumps(valid_vehicles)}

EXAMPLES:
- User: "Show me electric SUVs under 40k" -> {{..., "maxPrice": 40000.0,
  "preferredFuelTypes": ["Electric"], "preferredVehicleTypes": ["SUV", "Crossover", "CUV"], ...}}
- User: "Toyota or Honda" -> {{..., "preferredMakes": ["Toyota", "Honda"], ...}}
- User: "Used car with low miles" -> {{..., "maxMileage": 30000, ...}} (Assuming "low miles" implies a max)
- User: "Something blue" -> {{..., "desiredFeatures": ["blue"], ...}}
- User: "Tell me about space" -> {{..., "isOffTopic": true,
  "offTopicResponse": "I can only help with vehicle searches.", ...}}

Respond ONLY with the JSON object.
"""


def build_enhanced_system_prompt(
    user_query: str,
    conversation_history: List[Dict[str, str]],
    matched_category: Optional[str],
    valid_makes: List[str],
    valid_fuels: List[str],
    valid_vehicles: List[str],
    confirmed_context: Optional[Dict] = None,
    rejected_context: Optional[Dict] = None,
) -> str:
    """
    Builds an enhanced system prompt including history, context, and more complex intent logic.
    Includes explicit handling of confirmed and rejected preferences.
    """
    # Format conversation history
    history_context = ""
    if conversation_history:
        history_context = "## RECENT CONVERSATION HISTORY:\n"
        # Format the last few turns
        for turn in conversation_history[-3:]:  # Limit context window 
            role = "User" if turn.get("role") == "user" or "user" in turn else "Assistant"
            content = turn.get("content") or turn.get("user") or turn.get("ai", "")
            history_context += f"{role}: {content}\n"
        history_context += "---\n"

    # Format category context if provided
    category_context = ""
    if matched_category:
        category_context = (
            "## SUGGESTED CATEGORY:\n"
            f"The user might be interested in '{matched_category}' based on earlier analysis.\n"
            "---\n"
        )

    # Format rejected context with strong emphasis
    rejected_context_str = ""
    if rejected_context and any(rejected_context.values()):
        rejected_makes = rejected_context.get("rejectedMakes", [])
        rejected_types = rejected_context.get("rejectedVehicleTypes", [])
        rejected_fuels = rejected_context.get("rejectedFuelTypes", [])
        rejected_features = rejected_context.get("rejectedFeatures", [])
        rejected_transmission = rejected_context.get("rejectedTransmission", [])

        if rejected_makes or rejected_types or rejected_fuels or rejected_features or rejected_transmission:
            rejected_context_str = "## USER HAS EXPLICITLY REJECTED (CRITICAL):\n"

            if rejected_makes:
                rejected_context_str += f"- Rejected Makes: {json.dumps(rejected_makes)}\n"
            if rejected_types:
                rejected_context_str += f"- Rejected Vehicle Types: {json.dumps(rejected_types)}\n"
            if rejected_fuels:
                rejected_context_str += f"- Rejected Fuel Types: {json.dumps(rejected_fuels)}\n"
            if rejected_features:
                rejected_context_str += f"- Rejected Features: {json.dumps(rejected_features)}\n"
            if rejected_transmission:
                rejected_context_str += f"- Rejected Transmission: {json.dumps(rejected_transmission)}\n"

            rejected_context_str += "\n⚠️ IMPORTANT: You MUST NOT include any of these in your extracted parameters!\n---\n"

    # Format confirmed context with clear structure
    confirmed_context_str = ""
    if confirmed_context and any(confirmed_context.values()):
        confirmed_context_str = "## CURRENT CONFIRMED CRITERIA:\n"

        # Format confirmed context for clarity
        if confirmed_context.get("confirmedMakes"):
            confirmed_context_str += f"- Confirmed Makes: {json.dumps(confirmed_context.get('confirmedMakes', []))}\n"
        if confirmed_context.get("confirmedVehicleTypes"):
            confirmed_context_str += f"- Confirmed Vehicle Types: {json.dumps(confirmed_context.get('confirmedVehicleTypes', []))}\n"
        if confirmed_context.get("confirmedFuelTypes"):
            confirmed_context_str += f"- Confirmed Fuel Types: {json.dumps(confirmed_context.get('confirmedFuelTypes', []))}\n"
        if confirmed_context.get("confirmedFeatures"):
            confirmed_context_str += f"- Confirmed Features: {json.dumps(confirmed_context.get('confirmedFeatures', []))}\n"

        # Add price/year/mileage info if available
        price_info = []
        if confirmed_context.get("confirmedMinPrice"):
            price_info.append(f"Min: {confirmed_context['confirmedMinPrice']}")
        if confirmed_context.get("confirmedMaxPrice"):
            price_info.append(f"Max: {confirmed_context['confirmedMaxPrice']}")
        if price_info:
            confirmed_context_str += f"- Price Range: {', '.join(price_info)}\n"

        year_info = []
        if confirmed_context.get("confirmedMinYear"):
            year_info.append(f"Min: {confirmed_context['confirmedMinYear']}")
        if confirmed_context.get("confirmedMaxYear"):
            year_info.append(f"Max: {confirmed_context['confirmedMaxYear']}")
        if year_info:
            confirmed_context_str += f"- Year Range: {', '.join(year_info)}\n"

        if confirmed_context.get("confirmedMaxMileage"):
            confirmed_context_str += f"- Max Mileage: {confirmed_context['confirmedMaxMileage']}\n"

        if confirmed_context.get("confirmedTransmission"):
            confirmed_context_str += f"- Confirmed Transmission: {confirmed_context['confirmedTransmission']}\n"

        engine_size_info = []
        if confirmed_context.get("confirmedMinEngineSize"):
            engine_size_info.append(f"Min: {confirmed_context['confirmedMinEngineSize']}L")
        if confirmed_context.get("confirmedMaxEngineSize"):
            engine_size_info.append(f"Max: {confirmed_context['confirmedMaxEngineSize']}L")
        if engine_size_info:
            confirmed_context_str += f"- Engine Size Range: {', '.join(engine_size_info)}\n"

        hp_info = []
        if confirmed_context.get("confirmedMinHorsePower"):
            hp_info.append(f"Min: {confirmed_context['confirmedMinHorsePower']}hp")
        if confirmed_context.get("confirmedMaxHorsePower"):
            hp_info.append(f"Max: {confirmed_context['confirmedMaxHorsePower']}hp")
        if hp_info:
            confirmed_context_str += f"- Horsepower Range: {', '.join(hp_info)}\n"

        confirmed_context_str += "---\n"

    current_year = datetime.datetime.now().year
    default_json_keys = create_default_parameters().keys()
    json_format_example = json.dumps(create_default_parameters(), indent=2)

    return f"""
You are an advanced automotive parameter extraction assistant for Smart Auto Trader.
Analyze the latest user query in the context of the conversation history.
Extract search parameters EXPLICITLY mentioned in the LATEST user query, but use history to understand context and determine INTENT.

YOUR RESPONSE MUST BE ONLY A SINGLE VALID JSON OBJECT containing the following keys: {list(default_json_keys)}.
Use this exact format, filling values based on the LATEST query and context:
{json_format_example}

{history_context}
{category_context}
{confirmed_context_str}
{rejected_context_str}
Latest User Query: "{user_query}"

## CORE EXTRACTION RULES:
- Focus on the LATEST user query for explicit parameters.
- Use conversation history PRIMARILY to determine the 'intent'.
- Numeric values MUST be numbers (int or float), not strings. Use null if not mentioned in the latest query.
- Array values MUST be lists of strings. Use [] if not mentioned in the latest query.
- Extract makes/fuels/types ONLY if explicitly mentioned in the LATEST query AND they appear in the Valid lists.
- desiredFeatures: Extract features mentioned in the LATEST query.
- isOffTopic: Set to true ONLY if the LATEST query is clearly NOT about vehicles. If true, set offTopicResponse.
- clarificationNeeded: Set to true if the LATEST query is vague and requires more information
  (e.g., "Find me something nice"). If true, list needed info in clarificationNeededFor (e.g., ["budget", "type"]).

## NEW PARAMETER RULES:
- transmission: 
  * Set to "Automatic" or "Manual" (exactly as written) when user mentions transmission preference.
  * Examples: "automatic", "manual", "stick shift" (→ "Manual"), "self-shifting" (→ "Automatic").
  * Place ONLY in transmission field, NOT in desiredFeatures.
  * Set to null if no transmission type is mentioned.
  
- minEngineSize/maxEngineSize:
  * Extract engine size as floating-point liters. Examples: "2.0L" → 2.0, "1.6 liters" → 1.6
  * For ranges, use both fields: "between 1.5L and 3.0L" → minEngineSize: 1.5, maxEngineSize: 3.0
  * For minimum only: "at least 2.0L" → minEngineSize: 2.0, maxEngineSize: null
  * For maximum only: "under 2.5L" → minEngineSize: null, maxEngineSize: 2.5
  * Accept various units and convert to liters: "1600cc" → 1.6, "2000 cubic centimeters" → 2.0
  * Place ONLY in engine size fields, NOT in desiredFeatures.
  
- minHorsepower/maxHorsepower:
  * Extract horsepower as integers. Examples: "200hp" → 200, "150 horsepower" → 150
  * For ranges, use both fields: "between 150hp and 300hp" → minHorsepower: 150, maxHorsepower: 300
  * For minimum only: "at least 200hp" → minHorsepower: 200, maxHorsepower: null
  * For maximum only: "under 150hp" → minHorsepower: null, maxHorsepower: 150
  * Accept variations: "bhp", "PS", "power"
  * Place ONLY in horsepower fields, NOT in desiredFeatures.

## NEGATION HANDLING (CRITICAL RULES):
- Existing negation rules...
- For transmission, if user says "not automatic" or "no manual", DO NOT set the transmission field to that value.

## CONTEXTUAL INTERPRETATION (CRITICAL RULES):
- Existing contextual interpretation rules...
- If the Assistant asked about transmission, interpret simple replies like "automatic" or "auto" accordingly.
- If the Assistant asked about engine size, interpret numbers like "2.0" or "3 liters" as engine size.
- If the Assistant asked about power or performance, interpret numbers like "200" or "over 150" as horsepower.

## INTENT DETERMINATION (CRITICAL):
- Existing intent determination rules...

## PARAMETER HANDLING RULES:
- Existing parameter handling rules...

## VALID VALUES:
- Valid Makes: {json.dumps(valid_makes)}
- Valid Fuel Types: {json.dumps(valid_fuels)}
- Valid Vehicle Types (includes aliases): {json.dumps(valid_vehicles)}
- Valid Transmission Types: ["Automatic", "Manual"]

## EXAMPLES (Focus on LATEST query + history for intent):
# --- STANDARD EXAMPLES ---
Latest User Query: "Looking for an automatic BMW with at least 2.0L engine and 200hp"
Output: {{..., "preferredMakes": ["BMW"], "transmission": "Automatic", "minEngineSize": 2.0, "minHorsepower": 200, ...}}

Latest User Query: "I need a manual car with under 1.6L engine for fuel efficiency"
Output: {{..., "transmission": "Manual", "maxEngineSize": 1.6, ...}}

Latest User Query: "Something with more power, at least 300 horsepower"
Output: {{..., "minHorsepower": 300, ...}}

# --- CLARIFICATION ANSWER EXAMPLES ---
History: Assistant: What transmission do you prefer?
Latest User Query: "Automatic"
Output: {{..., "transmission": "Automatic", "intent": "clarify", ...}}

History: Assistant: Do you have preferences regarding engine size?
Latest User Query: "Under 2 liters please"
Output: {{..., "maxEngineSize": 2.0, "intent": "clarify", ...}}

History: Assistant: Any minimum horsepower requirement?
Latest User Query: "At least 150hp"
Output: {{..., "minHorsepower": 150, "intent": "clarify", ...}}

# --- NEGATION EXAMPLES ---
Latest User Query: "I want a car but not automatic transmission"
Output: {{..., "transmission": null, ...}}  # Note: don't set transmission to "Manual", unless explicitly stated

Latest User Query: "I need an SUV, not interested in anything over 2.5L engine size"
Output: {{..., "preferredVehicleTypes": ["SUV"], "maxEngineSize": 2.5, ...}}

Respond ONLY with the JSON object.
"""


def run_llm_with_history(
    user_query: str,
    conversation_history: List[Dict[str, str]],
    matched_category: Optional[str] = None,
    force_model: Optional[str] = None,
    confirmed_context: Optional[Dict] = None,
    rejected_context: Optional[Dict] = None,
) -> Optional[Dict[str, Any]]:
    """
    Main function to run LLM extraction, potentially trying multiple models.
    Now uses the enhanced system prompt.
    """
    # Moved VALID_ lists to global scope, accessible here

    # Determine model strategy
    model_order = {"fast": FAST_MODEL, "refine": REFINE_MODEL, "clarify": CLARIFY_MODEL}
    models_to_try = []

    if force_model and force_model in model_order:
        logger.info(f"Using forced model strategy starting with: {force_model}")
        # Start with forced model, then add others as fallbacks
        models_to_try.append(model_order[force_model])
        for key, model_name in model_order.items():
            if key != force_model:
                models_to_try.append(model_name)
    else:
        if force_model:
            logger.warning(
                f"Invalid forceModel '{force_model}' specified. Defaulting to fast model only."
            )
        else:
            logger.info("No forceModel specified. Using fast model only.")
        models_to_try = [
            FAST_MODEL
        ]  # Default to only the fast model if no valid strategy

    # Build the appropriate system prompt, now with context
    system_prompt = build_enhanced_system_prompt(
        user_query,
        conversation_history,
        matched_category,
        VALID_MANUFACTURERS,
        VALID_FUEL_TYPES,
        VALID_VEHICLE_TYPES,
        confirmed_context,
        rejected_context,
    )

    # Try models in order
    extracted_params = None
    for model in models_to_try:
        logger.info(f"Attempting extraction with model: {model}")
        extracted = try_extract_with_model(model, system_prompt, user_query)

        if extracted and is_valid_extraction(extracted):
            logger.info(f"Valid extraction received from model: {model}")
            # Process parameters to clean/validate structure and types
            processed = process_parameters(extracted)

            # --- Post-processing logic (like overrides) can be added here if needed ---
            # (Keep the override logic if desired - apply it to 'processed' dict)
            # ... override logic here modifying 'processed' ...

            # Return the first valid and processed result
            extracted_params = processed
            break  # Stop trying other models
        else:
            logger.warning(f"Extraction from model {model} was invalid or failed.")

    if not extracted_params:
        logger.error(
            f"All attempted models ({models_to_try}) failed to provide a valid extraction."
        )
        return None

    return extracted_params


# --- Flask App Setup ---
app = Flask(__name__)

# Call initialization function within app context before first request
# Using with app.app_context() ensures it has access to Flask application context if needed
with app.app_context():
    initialize_app_components()


# --- Flask Routes ---


@app.route("/extract_parameters", methods=["POST"])
def extract_parameters():
    """
    Main endpoint for parameter extraction with improved routing logic.
    Uses intent classification, conversation context, and special conditions
    to determine the best processing path.
    """
    try:
        start_time = datetime.datetime.now()
        logger.info("Received request: %s", request.json)
        data = request.json or {}

        if "query" not in data:
            logger.error("No 'query' provided in request.")
            return jsonify({"error": "No query provided"}), 400

        user_query = data["query"]
        force_model = data.get("forceModel")  # Model strategy from backend
        conversation_history = data.get("conversationHistory", [])
        
        # Safely retrieve context information
        confirmed_context = data.get("confirmedContext", {})
        rejected_context = data.get("rejectedContext", {})
        
        # Extract specific rejection lists for easier access
        rejected_makes = rejected_context.get("rejectedMakes", [])
        rejected_types = rejected_context.get("rejectedVehicleTypes", [])
        rejected_fuels = rejected_context.get("rejectedFuelTypes", [])

        # Enhanced logging for context
        if rejected_makes or rejected_types or rejected_fuels:
            logger.info(f"Rejected context: makes={rejected_makes}, types={rejected_types}, fuels={rejected_fuels}")
        
        if confirmed_context and any(confirmed_context.values()):
            logger.info(f"Confirmed context present with {len(confirmed_context)} items")

        logger.info(
            "Processing query: %s (forceModel=%s) with %d history items",
            user_query,
            force_model,
            len(conversation_history),
        )

        # 1) Quick check for off-topic
        if not is_car_related(user_query):
            logger.info("Query classified as off-topic.")
            return (
                jsonify(
                    create_default_parameters(
                        intent="off_topic",
                        is_off_topic=True,
                        off_topic_response="I specialize in vehicles. How can I help with your car search?",
                    )
                ),
                200,
            )

        # 2) Intent Classification (Zero-Shot)
        classified_intent = "SPECIFIC_SEARCH"  # Default assumption
        try:
            query_embedding = get_query_embedding(user_query)
            if query_embedding is not None:
                # Adjusted threshold based on testing
                intent_result = classify_intent_zero_shot(
                    query_embedding, threshold=0.35
                )
                if intent_result:
                    classified_intent = intent_result
                else:
                    logger.info(
                        "Intent classification score below threshold, using fallback logic."
                    )
                    # Fallback logic is now inside classify_intent_zero_shot
                    if intent_result is None:
                        classified_intent = "SPECIFIC_SEARCH"  # Safe default
            else:
                logger.error(
                    "Failed to get query embedding, defaulting intent to SPECIFIC_SEARCH."
                )
        except Exception as e:
            logger.error(
                f"Error during embedding or classification: {e}", exc_info=True
            )
            classified_intent = "SPECIFIC_SEARCH"  # Fallback safely

        # 3) Extract newest query fragment for processing
        query_fragment = extract_newest_user_fragment(user_query)
        lower_query_fragment = query_fragment.lower()

        # 4) Initialize routing condition flags
        is_clarification_answer = False
        contains_override = False
        mentions_rejected = False

        # 5) Enhanced check for override keywords
        override_keywords = [
            # Core negation words with spacing to avoid false matches
            "not ", " no ", " no.", "don't ", "dont ", "doesn't ", "doesnt ", "won't ", "wont ", "can't ", "cant ",
            # More specific negation patterns for rejecting brands/types
            "don't want", "dont want", "no more", "not interested in", "not looking for",
            # Preference change indicators
            "nevermind", "never mind", "actually", "instead", "rather", "prefer", "like", 
            "update", "modify", "switch", "different", "other than", "replace",
            # Exclusion words
            "except", "excluding", "without", "but not", "avoid", "remove", "skip", "exclude",
            # Emphasis on specific preference
            "only", "just", "specifically", "exclusively", "must be", "has to be",
            # Change of mind phrases
            "changed my mind", "thinking again", "on second thought", "actually i want", 
            "i meant", "i said", "forget", "ignore",
            # Additional negation phrases
            "anything but", "anything except", "don't need", "dont need", "not interested in",
            "not looking for", "apart from", "besides", "other than", "something else",
            # Toyota-specific patterns from the example
            "not toyota", "no toyota", "dont toyota", "don't toyota", "except toyota", "but toyota"
        ]

        # Simplified and reliable substring check - easier to debug and more dependable
        contains_override = any(keyword in lower_query_fragment for keyword in override_keywords)

        # Direct substring check for very specific example to ensure reliability
        if "dont want a toyota" in lower_query_fragment:
            contains_override = True

        # Log the result with the full query fragment for easier debugging
        logger.info(f"Override check result: {contains_override} for query fragment: '{lower_query_fragment}'")

        # Check for brand-specific negation patterns (as a fallback for cases the keyword list might miss)
        if not contains_override:
            # Pattern for "don't/dont want [brand]"
            dont_want_pattern = re.compile(r"don'?t\s+want\s+[a-z]+", re.IGNORECASE)
            # Pattern for "no [brand]" where brand is a word
            no_brand_pattern = re.compile(r"no\s+[a-z]+\b", re.IGNORECASE)
            # Pattern for "not [brand]" where brand is a word
            not_brand_pattern = re.compile(r"not\s+[a-z]+\b", re.IGNORECASE)
            
            if (dont_want_pattern.search(lower_query_fragment) or 
                no_brand_pattern.search(lower_query_fragment) or
                not_brand_pattern.search(lower_query_fragment)):
                contains_override = True
                logger.info(f"Negation pattern for brand detected in: '{lower_query_fragment}'")

        # Final log after all checks
        logger.info(f"Final override check result: {contains_override} for query fragment: '{lower_query_fragment}'")

        # 6) Enhanced check if query mentions any rejected items
        if rejected_makes or rejected_types or rejected_fuels:
            # Check all rejection lists - use full word boundaries when possible
            mentions_rejected = False
            all_rejected = rejected_makes + rejected_types + rejected_fuels
            
            for item in all_rejected:
                item_lower = item.lower()
                # Look for whole word matches when possible
                if re.search(r'\b' + re.escape(item_lower) + r'\b', lower_query_fragment):
                    mentions_rejected = True
                    break
                # For partial matches, be more cautious
                elif item_lower in lower_query_fragment and len(item_lower) > 3:
                    mentions_rejected = True
                    break
            
            logger.info(f"Rejected items check result: {mentions_rejected}")

        # 7) Sophisticated check for clarification answers
        if conversation_history:
            try:
                last_turn = conversation_history[-1]
                
                # Try to get assistant's last message
                last_ai_message = ""
                if last_turn.get("ai"):
                    last_ai_message = last_turn["ai"].lower()
                elif last_turn.get("role") == "assistant" and last_turn.get("content"):
                    last_ai_message = last_turn["content"].lower()
                
                if last_ai_message:
                    # Enhanced detection of questions in the last AI message
                    is_question = (
                        "?" in last_ai_message or 
                        any(q_word in last_ai_message for q_word in [
                            "what", "which", "how", "would you", "do you", "could you", "are you",
                            "budget", "price", "year", "make", "type", "mileage", "consider",
                            "looking for", "preferred", "want", "like", "interested in"
                        ])
                    )

                    is_transmission_question = any(
                        trans_term in last_ai_message for trans_term in 
                        ["transmission", "automatic", "manual", "stick shift", "gearbox", "shifting"]
                    )

                    is_engine_question = any(
                        engine_term in last_ai_message for engine_term in 
                        ["engine size", "engine capacity", "displacement", "liter", "cc", "cylinder size"]
                    )

                    is_horsepower_question = any(
                        hp_term in last_ai_message for hp_term in 
                        ["horsepower", "power", "hp", "bhp", "performance", "powerful"]
                    )

                    if is_question:
                        logger.info("Detected potential question in last AI message")
                        
                        # Is the input short enough to likely be an answer?
                        word_count = word_count_clean(query_fragment)
                        is_short_answer = word_count < 10  # Slightly increased to catch natural answers
                        
                        # Identify question type for context-specific matching
                        is_budget_question = any(
                            budget_term in last_ai_message for budget_term in 
                            ["budget", "price", "afford", "cost", "spend", "money", "how much", "pay", "value", "worth"]
                        )
                        
                        is_year_question = any(
                            year_term in last_ai_message for year_term in 
                            ["year", "old", "new", "age", "recent", "modern", "how old", "when", "latest", "vintage"]
                        )
                        
                        is_make_question = any(
                            make_term in last_ai_message for make_term in 
                            ["make", "brand", "manufacturer", "company", "which car", "preferred make", "what brand"]
                        )
                        
                        is_type_question = any(
                            type_term in last_ai_message for type_term in 
                            ["type", "style", "body", "suv", "sedan", "vehicle type", "what kind", "model", "category"]
                        )
                        
                        is_fuel_question = any(
                            fuel_term in last_ai_message for fuel_term in 
                            ["fuel", "petrol", "diesel", "electric", "hybrid", "power", "engine"]
                        )
                        
                        is_mileage_question = any(
                            mileage_term in last_ai_message for mileage_term in 
                            ["mileage", "kilometers", "miles", "km", "odometer", "distance", "driven"]
                        )
                        
                        is_general_question = any(
                            general_term in last_ai_message for general_term in 
                            ["tell me more", "details", "specify", "looking for", "interested in", "help you find"]
                        )

                        # Check for context-specific answer patterns
                        
                        # 1. Budget/Price context-specific detection
                        if is_budget_question:
                            logger.info("AI asked about budget/price")
                            # Look for numbers with currency symbols/terms
                            currency_pattern = re.compile(r'[€$£]?\s*\d+[k.]?\d*|\d+[k.]?\d*\s*[€$£]')
                            price_keywords = ["under", "over", "around", "between", "less than", "more than", "maximum", 
                                             "budget", "afford", "spend", "cheap", "expensive", "grand", "k", "thousand"]
                            
                            has_currency_match = bool(currency_pattern.search(lower_query_fragment))
                            has_price_keywords = any(word in lower_query_fragment for word in price_keywords)
                            
                            if has_currency_match or has_price_keywords:
                                is_clarification_answer = True
                                logger.info("MATCH: Budget/price clarification answer detected (currency or price keywords)")
                            # Additional check for just numbers in a budget context
                            elif re.search(r'\b\d{4,6}\b', lower_query_fragment):
                                is_clarification_answer = True
                                logger.info("MATCH: Budget clarification answer detected (number range typical for price)")
                        
                        # 2. Year context-specific detection
                        elif is_year_question:
                            logger.info("AI asked about year")
                            # Check for 4-digit years or year-related words
                            year_pattern = re.compile(r'\b(19|20)\d{2}\b')  # years like 1990-2099
                            year_keywords = ["new", "recent", "old", "older", "newer", "latest", "young", "fresh", 
                                            "vintage", "classic", "year", "modern", "decade", "current"]
                            relative_year_pattern = re.compile(r'\b\d{1,2}\s*years?\s*(old|ago|new)')
                            
                            has_year_match = bool(year_pattern.search(lower_query_fragment))
                            has_relative_year = bool(relative_year_pattern.search(lower_query_fragment))
                            has_year_keywords = any(word in lower_query_fragment.split() for word in year_keywords)
                            
                            if has_year_match or has_relative_year or has_year_keywords:
                                is_clarification_answer = True
                                logger.info("MATCH: Year clarification answer detected")
                        
                        # 3. Make/Brand context-specific detection  
                        elif is_make_question:
                            logger.info("AI asked about vehicle make/brand")
                            # Check if response predominantly contains valid manufacturers
                            content_words = set(re.findall(r'\b\w+\b', lower_query_fragment))
                            filler_words = {"a", "the", "and", "or", "maybe", "possibly", "prefer", "like", "want", 
                                           "would", "either", "both", "all", "none", "not", "also", "i", "my", "me", 
                                           "please", "thanks", "you", "looking", "for", "something", "by"}
                            
                            # Remove filler words for analysis
                            content_words = content_words - filler_words
                            
                            # Check if any of the content words match manufacturers
                            valid_makes_lower = [make.lower() for make in VALID_MANUFACTURERS]
                            matches = [word for word in content_words if word in valid_makes_lower or 
                                      any(make.lower() in word for make in valid_makes_lower)]
                            
                            if matches:
                                is_clarification_answer = True
                                logger.info(f"MATCH: Make clarification answer detected with {matches}")
                            
                            # Also check for answers like "no preference" or "any make is fine"
                            any_make_patterns = ["any", "doesn't matter", "don't care", "not important", "whatever", "no preference"]
                            if any(pattern in lower_query_fragment for pattern in any_make_patterns):
                                is_clarification_answer = True
                                logger.info("MATCH: Make clarification with 'any make' pattern detected")
                        
                        # 4. Vehicle Type context-specific detection
                        elif is_type_question:
                            logger.info("AI asked about vehicle type")
                            # Check if response predominantly contains valid vehicle types
                            valid_types_lower = [vtype.lower() for vtype in VALID_VEHICLE_TYPES]
                            
                            # Look for matching vehicle types in the response
                            matches = []
                            for vtype in valid_types_lower:
                                if vtype in lower_query_fragment:
                                    matches.append(vtype)
                            
                            if matches:
                                is_clarification_answer = True
                                logger.info(f"MATCH: Vehicle type clarification answer detected with {matches}")
                            
                            # Check for common vehicle categories not explicitly in the list
                            extra_categories = ["family car", "city car", "small car", "big car", "sport car", 
                                              "luxury", "compact", "spacious", "large", "4x4", "off-road"]
                            if any(category in lower_query_fragment for category in extra_categories):
                                is_clarification_answer = True
                                logger.info("MATCH: Vehicle type clarification with category description detected")
                                
                        # 5. Fuel Type context-specific detection
                        elif is_fuel_question:
                            logger.info("AI asked about fuel type")
                            valid_fuels_lower = [fuel.lower() for fuel in VALID_FUEL_TYPES]
                            
                            # Look for matching fuel types in the response
                            matches = []
                            for fuel in valid_fuels_lower:
                                if fuel in lower_query_fragment:
                                    matches.append(fuel)
                            
                            if matches:
                                is_clarification_answer = True
                                logger.info(f"MATCH: Fuel type clarification answer detected with {matches}")
                                
                            # Check for related fuel descriptions
                            fuel_descriptions = ["gas", "gasoline", "unleaded", "plug-in", "ev", "ice", 
                                               "combustion", "phev", "alternative", "clean", "green"]
                            if any(desc in lower_query_fragment for desc in fuel_descriptions):
                                is_clarification_answer = True
                                logger.info("MATCH: Fuel type clarification with description detected")
                        
                        # 6. Mileage context-specific detection
                        elif is_mileage_question:
                            logger.info("AI asked about mileage")
                            # Look for numbers with distance units
                            mileage_pattern = re.compile(r'\b\d+[k.]?\d*\s*(km|kilometers|miles|mi|k)\b|\b(km|kilometers|miles|mi)\s*\d+[k.]?\d*\b')
                            mileage_keywords = ["low", "high", "maximum", "under", "below", "less than", "more than",
                                               "over", "mileage", "odometer", "driven", "kilometers", "miles", "km"]
                            
                            has_mileage_match = bool(mileage_pattern.search(lower_query_fragment))
                            has_mileage_keywords = any(word in lower_query_fragment for word in mileage_keywords)
                            
                            # Check for just numbers that could be mileage
                            has_likely_mileage_number = bool(re.search(r'\b\d{4,6}\b', lower_query_fragment))
                            
                            if has_mileage_match or (has_mileage_keywords and has_likely_mileage_number):
                                is_clarification_answer = True
                                logger.info("MATCH: Mileage clarification answer detected")

                        # Transmission clarification detection
                        elif is_transmission_question:
                            logger.info("AI asked about transmission preference")
                            transmission_keywords = ["automatic", "manual", "auto", "stick", "shift", "dsg", 
                                                 "cvt", "self-shifting", "paddle", "gearbox"]
                            
                            # Check for matching keywords
                            has_transmission_keywords = any(word in lower_query_fragment for word in transmission_keywords)
                            
                            if has_transmission_keywords:
                                is_clarification_answer = True
                                logger.info("MATCH: Transmission clarification answer detected")

                        # Engine size clarification detection
                        elif is_engine_question:
                            logger.info("AI asked about engine size")
                            engine_keywords = ["liter", "litre", "l", "cc", "cubic", "engine", "size", 
                                             "capacity", "displacement", "cylinder"]
                            engine_size_pattern = re.compile(r'\b\d+(?:\.\d+)?\s*(?:l|cc|liter|litre)s?\b|\b\d+\s*(?:cubic|engine|size)\b')
                            
                            has_engine_keywords = any(word in lower_query_fragment for word in engine_keywords)
                            has_engine_size_match = bool(engine_size_pattern.search(lower_query_fragment))
                            
                            if has_engine_keywords or has_engine_size_match:
                                is_clarification_answer = True
                                logger.info("MATCH: Engine size clarification answer detected")

                        # Horsepower clarification detection
                        elif is_horsepower_question:
                            logger.info("AI asked about horsepower")
                            hp_keywords = ["horsepower", "hp", "bhp", "power", "powerful", "performance", 
                                        "strong", "weak", "fast"]
                            hp_pattern = re.compile(r'\b\d+\s*(?:hp|bhp|horsepower|ps)\b|\b(?:power|performance).*?\b\d+\b')
                            
                            has_hp_keywords = any(word in lower_query_fragment for word in hp_keywords)
                            has_hp_match = bool(hp_pattern.search(lower_query_fragment))
                            
                            if has_hp_keywords or has_hp_match:
                                is_clarification_answer = True
                                logger.info("MATCH: Horsepower clarification answer detected")
                        
                        # 7. General question context - stricter detection requiring multiple parameter types
                        elif is_general_question and is_short_answer:
                            logger.info("AI asked general question about vehicle preferences")
                            
                            # Count how many different parameter types the response contains
                            param_count = 0
                            
                            # Check for price info
                            if re.search(r'[€$£]?\s*\d+[k.]?\d*|\d+[k.]?\d*\s*[€$£]', lower_query_fragment):
                                param_count += 1
                            
                            # Check for year info
                            if re.search(r'\b(19|20)\d{2}\b', lower_query_fragment):
                                param_count += 1
                            
                            # Check for make info
                            if any(make.lower() in lower_query_fragment for make in VALID_MANUFACTURERS):
                                param_count += 1
                            
                            # Check for vehicle type info
                            if any(vtype.lower() in lower_query_fragment for vtype in VALID_VEHICLE_TYPES):
                                param_count += 1
                            
                            # Check for fuel type info
                            if any(fuel.lower() in lower_query_fragment for fuel in VALID_FUEL_TYPES):
                                param_count += 1
                            
                            # Only consider it a clarification if multiple parameters are provided
                            if param_count >= 2:
                                is_clarification_answer = True
                                logger.info(f"MATCH: General clarification with multiple ({param_count}) parameter types detected")
                        
                        # 8. Fallback for simple direct answers to other questions
                        elif is_short_answer and word_count <= 3:
                            logger.info("Checking for very short direct answer")
                            # Only consider very short responses that look like direct answers
                            
                            # Direct yes/no answers
                            if re.match(r'^(yes|no|yeah|nope|not really|sure|ok|okay)$', lower_query_fragment.strip()):
                                is_clarification_answer = True
                                logger.info("MATCH: Direct yes/no answer detected")
                            
                            # Single-word or very short answers that look like direct responses
                            elif (any(fuel.lower() == lower_query_fragment.strip() for fuel in VALID_FUEL_TYPES) or
                                  any(make.lower() == lower_query_fragment.strip() for make in VALID_MANUFACTURERS) or
                                  any(vtype.lower() == lower_query_fragment.strip() for vtype in VALID_VEHICLE_TYPES)):
                                is_clarification_answer = True
                                logger.info("MATCH: Single-word exact match to valid option detected")
                            
                            # Just a number with optional units/symbols
                            elif re.match(r'^[\d€$£\.k,]+(?:\s*(?:km|miles|years|k|thousand))?$', lower_query_fragment.strip()):
                                is_clarification_answer = True
                                logger.info("MATCH: Just a number or value detected as direct answer")

                    # Log the final determination
                    if is_clarification_answer:
                        logger.info(f"Final determination: This IS a clarification answer (length={word_count})")
                    else:
                        logger.info(f"Final determination: This is NOT a clarification answer")
                        
            except Exception as hist_ex:
                logger.error(
                    f"Error checking conversation history for clarification context: {hist_ex}",
                    exc_info=True,
                )
                
        logger.info(f"Clarification answer check result: {is_clarification_answer}")

        # --- Determine if LLM is required based on priority conditions ---
        force_llm = False
        if is_clarification_answer:
            logger.info("Prioritizing LLM path due to: clarification answer detected.")
            force_llm = True
        elif contains_override:
            logger.info("Prioritizing LLM path due to: override keyword detected.")
            force_llm = True
        elif mentions_rejected:
            logger.info("Prioritizing LLM path due to: query mentions rejected item.")
            force_llm = True
        # Optional: Add more sophisticated checks here if desired
        # elif classified_intent == 'VAGUE_INQUIRY' and context_has_enough_info(confirmedContext):
        #    logger.info("Intent is VAGUE but context is sufficient, forcing LLM for refinement.")
        #    force_llm = True

        # --- Execute based on routing decision ---
        final_response = None
        if force_llm or classified_intent == 'SPECIFIC_SEARCH':
            if not force_llm: # Log if it was originally specific
                logger.info(f"Intent classified as SPECIFIC_SEARCH, proceeding to LLM.")
            else: # Log details if forced
                logger.info(f"Routing conditions met (clarify={is_clarification_answer}, "
                          f"override={contains_override}, mentions_rejected={mentions_rejected}), "
                          f"proceeding to LLM.")

            extracted_params = run_llm_with_history(
                user_query, 
                conversation_history, 
                None, # matched_category
                force_model,
                confirmed_context=confirmed_context,
                rejected_context=rejected_context
            )
            
            if extracted_params:
                # Post-processing override intent if needed
                if is_clarification_answer and extracted_params.get("intent") != "clarify":
                    logger.info("Overriding LLM intent to 'clarify' based on context detection")
                    extracted_params["intent"] = "clarify"
                    
                # Ensure all fields exist using create_default_parameters as base
                base = create_default_parameters()
                base.update(extracted_params)  # Overwrite defaults with LLM output
                final_response = base
                logger.info("Final extracted parameters from LLM: %s", final_response)
            else:
                logger.error("LLM models failed or no valid extraction.")
                final_response = create_default_parameters(intent="error") # Indicate error

        elif classified_intent == 'VAGUE_INQUIRY':
            logger.info("Intent is VAGUE_INQUIRY and no override/clarification forced LLM, proceeding with RAG.")
            try:
                match_cat, score = find_best_match(query_fragment)
                logger.info(f"RAG result: Category='{match_cat}', Score={score:.2f}")
                
                if score < 0.6: # Threshold for weak RAG match
                    logger.info("RAG score too low. Requesting general clarification.")
                    final_response = create_default_parameters(
                        intent="clarify",
                        clarification_needed=True,
                        clarification_needed_for=["details"],
                        retriever_suggestion=(
                            "Could you provide more specific details about the "
                            "type of vehicle you need?"
                        ),
                    )
                else: # Medium/High RAG score >= 0.6
                    logger.info("RAG score sufficient. Requesting specific clarification based on matched category.")
                    final_response = create_default_parameters(
                        intent="clarify",
                        clarification_needed=True,
                        clarification_needed_for=["budget", "year", "make"],
                        matched_category=match_cat,
                        retriever_suggestion=(
                            f"Okay, thinking about {match_cat}s. What's your "
                            f"budget or preferred year range?"
                        ),
                    )
            except Exception as e:
                logger.error(f"Error during RAG processing: {e}", exc_info=True)
                logger.warning("RAG failed, falling back to generic clarification.")
                final_response = create_default_parameters(
                    intent="clarify", 
                    clarification_needed=True,
                    clarification_needed_for=["details"],
                    retriever_suggestion="Could you tell me more about what you're looking for in a vehicle?"
                )
        else:
            logger.warning(f"Unhandled classified_intent: {classified_intent}. Defaulting to error.")
            final_response = create_default_parameters(intent="error")

        # Ensure final_response is always set
        if final_response is None:
            logger.error("Reached end of processing without setting final_response. Defaulting to error.")
            final_response = create_default_parameters(intent="error")

        end_time = datetime.datetime.now()
        duration = (end_time - start_time).total_seconds()
        logger.info(f"Request processing completed in {duration:.2f} seconds.")

        return jsonify(final_response), 200

    except Exception as e:
        logger.exception(f"Unhandled exception in /extract_parameters: {e}")
        return jsonify(create_default_parameters(intent="error")), 500


# --- Main Execution ---
if __name__ == "__main__":
    # Run initialization once before starting the server
    # Note: In production, use a proper WSGI server like Gunicorn or Waitress
    # initialize_app_components() # Already called via app_context
    app.run(host="0.0.0.0", port=5006, debug=False)  # Keep debug=False
