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
        f"Could not import from retriever package: {ie}. Make sure retriever directory is structured correctly and contains __init__.py if needed."
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
    "SPECIFIC_SEARCH": "Search query containing specific vehicle parameters like make (e.g., Toyota), model (e.g., Camry), exact year (e.g., 2021), price range (e.g., under 20k), fuel type (e.g., petrol), mileage, or explicit technical features (e.g., sunroof).",
    "VAGUE_INQUIRY": "General question seeking recommendations (e.g., 'what car is good for families', 'recommend something reliable'), advice, or stating general qualities (e.g. 'reliable', 'cheap', 'safe', 'economical') without providing specific vehicle parameters like make, model, year, or price.",
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

        logger.info(
            f"Intent classification scores: {{k: f'{v:.2f}' for k, v in similarities.items()} }"
        )  # Log formatted scores

        if best_score >= threshold:
            logger.info(f"Classified intent: {best_label} (Score: {best_score:.2f})")
            return best_label
        else:
            # --- NEW FALLBACK LOGIC ---
            logger.info(
                f"Intent classification score ({
    best_score:.2f}) below threshold ({threshold}). Applying fallback logic."
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
        "car",
        "vehicle",
        "auto",
        "automobile",
        "sedan",
        "suv",
        "truck",
        "hatchback",
        "coupe",
        "convertible",
        "van",
        "minivan",
        "electric",
        "hybrid",
        "diesel",
        "petrol",
        "gasoline",
        "make",
        "model",
        "year",
        "price",
        "mileage",
        "engine",
        "transmission",
        "drive",
        "buy",
        "sell",
        "lease",
        "dealer",
        "used",
        "new",
        "road tax",
        "nct",
        "insurance",
        "mpg",
        "kpl",  # Added context
    }
    # Add common makes dynamically from the global list
    car_keywords.update(make.lower() for make in VALID_MANUFACTURERS)

    # Check for presence of keywords
    if any(keyword in query_lower for keyword in car_keywords):
        return True

    # Basic check for greetings or clearly unrelated questions
    # Be careful not to exclude car-related questions like "what is an SUV"
    off_topic_starts = ("hi", "hello", "how are you", "who is", "tell me a joke")
    if query_lower.startswith(off_topic_starts):
        return False
    # Check for questions that are unlikely car related unless containing keywords
    if query_lower.startswith(("what is", "what are", "where is")) and not any(
        kw in query_lower for kw in ["car", "vehicle", "suv", "sedan"]
    ):
        return False

    # Consider very short queries potentially off-topic unless they are follow-ups
    # (We don't have history here, so assume short non-keyword queries are off-topic)
    if word_count_clean(query) < 2 and not any(
        keyword in query_lower for keyword in car_keywords
    ):
        return False

    return (
        True  # Default to assuming it might be car-related if not caught by above rules
    )


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
                    f"JSON decoding failed for model {model}: {je}. JSON string was: '{json_str}'"
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
            f"Extracted parameters deemed invalid (no criteria set and no clarification needed): {params}"
        )
        return False


def process_parameters(
    params: Dict[str, Any],
) -> Dict[str, Any]:
    """
    Cleans and validates the structure of parameters extracted by the LLM.
    Uses globally defined VALID_ lists.
    """
    result = create_default_parameters()  # Start with default structure

    # Overwrite with values from params if they exist and are valid types
    # Numeric fields
    for field in ["minPrice", "maxPrice"]:
        if field in params and isinstance(params[field], (int, float)):
            result[field] = float(params[field])  # Ensure float
    for field in ["minYear", "maxYear", "maxMileage"]:
        if field in params and isinstance(params[field], (int, float)):
            result[field] = int(params[field])  # Ensure int

    # Array fields with validation
    if isinstance(params.get("preferredMakes"), list):
        result["preferredMakes"] = [
            m
            for m in params["preferredMakes"]
            if isinstance(m, str) and m in VALID_MANUFACTURERS  # Validate against list
        ]
    if isinstance(params.get("preferredFuelTypes"), list):
        # Convert strings to enum values if needed by backend (or handle strings)
        # Assuming backend expects strings matching VALID_FUEL_TYPES for now
        result["preferredFuelTypes"] = [
            f
            for f in params["preferredFuelTypes"]
            if isinstance(f, str) and f in VALID_FUEL_TYPES  # Validate against list
        ]
    if isinstance(params.get("preferredVehicleTypes"), list):
        # Validate against VALID_VEHICLE_TYPES (includes aliases)
        result["preferredVehicleTypes"] = [
            v
            for v in params["preferredVehicleTypes"]
            if isinstance(v, str) and v in VALID_VEHICLE_TYPES  # Validate against list
        ]
    if isinstance(params.get("desiredFeatures"), list):
        result["desiredFeatures"] = [
            f
            for f in params["desiredFeatures"]
            if isinstance(f, str) and f  # Ensure non-empty strings
        ]

    # Boolean and string fields
    if "isOffTopic" in params and isinstance(params["isOffTopic"], bool):
        result["isOffTopic"] = params["isOffTopic"]
    if (
        result["isOffTopic"]
        and "offTopicResponse" in params
        and isinstance(params["offTopicResponse"], str)
    ):
        result["offTopicResponse"] = params["offTopicResponse"]
    if "clarificationNeeded" in params and isinstance(
        params["clarificationNeeded"], bool
    ):
        result["clarificationNeeded"] = params["clarificationNeeded"]
    if "clarificationNeededFor" in params and isinstance(
        params["clarificationNeededFor"], list
    ):
        result["clarificationNeededFor"] = [
            str(item) for item in params["clarificationNeededFor"]
        ]  # Ensure strings
    if "retrieverSuggestion" in params and isinstance(
        params["retrieverSuggestion"], str
    ):
        result["retrieverSuggestion"] = params["retrieverSuggestion"]
    if "matchedCategory" in params and isinstance(params["matchedCategory"], str):
        result["matchedCategory"] = params["matchedCategory"]
    if "intent" in params and isinstance(params["intent"], str):
        # Validate intent? For now, just convert to lowercase
        result["intent"] = params["intent"].lower()
    else:
        result["intent"] = "new_query"  # Default if missing

    return result


def build_system_prompt(
    valid_makes: List[str],
    valid_fuels: List[str],
    valid_vehicles: List[str],
) -> str:
    """Builds the basic system prompt for simple extraction."""
    # This prompt needs to be kept updated if the JSON format changes
    # Using the keys from create_default_parameters for consistency
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
- Array values (preferredMakes, preferredFuelTypes, preferredVehicleTypes, desiredFeatures, clarificationNeededFor) MUST be lists of strings. Use [] if not mentioned.
- Booleans (isOffTopic, clarificationNeeded) must be true or false.
- Strings (offTopicResponse, retrieverSuggestion, matchedCategory, intent) must be strings or null.
- Extract makes ONLY if they appear in the Valid Makes list. If multiple valid makes are mentioned, include all.
- Extract fuel types ONLY if they appear in the Valid Fuel Types list. Include all mentioned valid types.
- Extract vehicle types ONLY if they appear in the Valid Vehicle Types list. Include all mentioned valid types.
- desiredFeatures: Extract any mentioned features (e.g., "sunroof", "leather seats", "parking sensors").
- intent: Set to "new_query".
- isOffTopic: Set to true ONLY if the query is clearly NOT about buying/searching for vehicles. If true, set offTopicResponse to a brief explanation.
- clarificationNeeded: Set to false for this basic prompt.
- clarificationNeededFor: Set to [] for this basic prompt.

VALID VALUES:
- Valid Makes: {json.dumps(valid_makes)}
- Valid Fuel Types: {json.dumps(valid_fuels)}
- Valid Vehicle Types (includes aliases): {json.dumps(valid_vehicles)}

EXAMPLES:
- User: "Show me electric SUVs under 40k" -> {{..., "maxPrice": 40000.0, "preferredFuelTypes": ["Electric"], "preferredVehicleTypes": ["SUV", "Crossover", "CUV"], ...}}
- User: "Toyota or Honda" -> {{..., "preferredMakes": ["Toyota", "Honda"], ...}}
- User: "Used car with low miles" -> {{..., "maxMileage": 30000, ...}} (Assuming "low miles" implies a max)
- User: "Something blue" -> {{..., "desiredFeatures": ["blue"], ...}}
- User: "Tell me about space" -> {{..., "isOffTopic": true, "offTopicResponse": "I can only help with vehicle searches.", ...}}

Respond ONLY with the JSON object.
"""


def build_enhanced_system_prompt(
    user_query: str,
    conversation_history: List[Dict[str, str]],
    matched_category: Optional[str],
    valid_makes: List[str],
    valid_fuels: List[str],
    valid_vehicles: List[str],
) -> str:
    """Builds an enhanced system prompt including history, context, and more complex intent logic."""
    history_context = ""
    if conversation_history:
        history_context = "Recent conversation history (User/Assistant):\n"
        # Format the last few turns
        for turn in conversation_history[-3:]:  # Limit context window
            # Adjust based on actual history format if needed
            role = (
                "User" if turn.get("role") == "user" or "user" in turn else "Assistant"
            )
            content = turn.get("content") or turn.get("user") or turn.get("ai", "")
            history_context += f"{role}: {content}\n"
        history_context += "---\n"

    category_context = ""
    if matched_category:
        category_context = f"Context Suggestion: The user might be interested in '{matched_category}' based on earlier analysis.\n"

    current_year = datetime.datetime.now().year
    default_json_keys = create_default_parameters().keys()
    json_format_example = json.dumps(create_default_parameters(), indent=2)

    # NOTE: This prompt is complex. Ensure the LLM can handle it.
    return f"""
You are an advanced automotive parameter extraction assistant for Smart Auto Trader.
Analyze the latest user query in the context of the conversation history.
Extract search parameters EXPLICITLY mentioned in the LATEST user query, but use history to understand context and determine INTENT.
YOUR RESPONSE MUST BE ONLY A SINGLE VALID JSON OBJECT containing the following keys: {list(default_json_keys)}.
Use this exact format, filling values based on the LATEST query and context:
{json_format_example}

{history_context}
{category_context}
Latest User Query: "{user_query}"

RULES:
- Focus on the LATEST user query for explicit parameters.
- Use conversation history PRIMARILY to determine the 'intent'.
- Numeric values MUST be numbers (int or float), not strings. Use null if not mentioned in the latest query.
- Array values MUST be lists of strings. Use [] if not mentioned in the latest query.
- Extract makes/fuels/types ONLY if explicitly mentioned in the LATEST query AND they appear in the Valid lists.
- desiredFeatures: Extract features mentioned in the LATEST query.
- isOffTopic: Set to true ONLY if the LATEST query is clearly NOT about vehicles. If true, set offTopicResponse.
- clarificationNeeded: Set to true if the LATEST query is vague and requires more information (e.g., "Find me something nice"). If true, list needed info in clarificationNeededFor (e.g., ["budget", "type"]).

# --- ADD/ENSURE THIS RULE EXISTS ---
- **CONTEXTUAL INTERPRETATION:** Pay close attention to the IMMEDIATELY PRECEDING Assistant message in the history.
    - If the Assistant asked a question (e.g., "What's your budget?", "Which year?", "Max mileage?"), interpret the LATEST user query as the answer to that question.
    - **Numbers in Context:** If the Assistant asked for budget/price, interpret a number like '50000' or '€50k' in the user's reply as `{{"maxPrice": 50000.0}}` (or minPrice/etc. based on phrasing like 'over'/'under'). If the assistant asked for mileage, interpret it as `{{"maxMileage": 50000}}`. Prioritize this contextual interpretation. Use currency symbols (€, $) if provided as a hint for price.
# --- END RULE ---

# Explain change: Added priority note for intent determination.
- **INTENT PRIORITY:** Determining the correct 'intent' is critical. Pay close attention to the 'clarify' intent rule below.

- intent: Determine the intent based on the relationship between the LATEST query and the history:
    * "new_query": Latest query starts a completely new search, unrelated to history.
    * "replace_criteria": Latest query completely changes the core subject (e.g., was asking about SUVs, now asks only about Sedans).
    * "add_criteria": Latest query adds constraints (e.g., "also make it petrol", "show me BMWs too").
    * "refine_criteria": Latest query modifies existing constraints (e.g., "change price to under 30k", "actually, make it newer").
# Explain change: Made 'clarify' intent definition more precise and emphasized.
    * "clarify": **CRITICAL RULE:** Use this intent ONLY when the Latest query DIRECTLY and SIMPLY ANSWERS a question asked by the Assistant in the IMMEDIATELY PRECEDING turn (e.g., Assistant asked for budget, User provides ONLY a number like '25000' or a price phrase like 'under 20k'). DO NOT use 'new_query' in this case.

PRICE/YEAR/MILEAGE HANDLING (Apply if explicitly mentioned in LATEST query OR contextually interpreted):
- "under X" / "less than X" -> maxPrice/maxYear/maxMileage = X, min = null
- "over X" / "more than X" -> minPrice/minYear = X, max = null
- "between X and Y" -> min = X, max = Y
- "around X" (for price) -> minPrice = 0.8*X, maxPrice = 1.2*X
- "low miles" -> maxMileage = 30000
- "new" / "recent" -> minYear = {current_year - 2}
- "brand new" -> minYear = {current_year}
- "older" / "used" (vague) -> maxYear = {current_year - 4}

VALID VALUES:
- Valid Makes: {json.dumps(valid_makes)}
- Valid Fuel Types: {json.dumps(valid_fuels)}
- Valid Vehicle Types (includes aliases): {json.dumps(valid_vehicles)}

EXAMPLE (Focus on LATEST query + history for intent):
# Explain change: Added more examples demonstrating the 'clarify' intent for different types of answers.
# --- ENSURE/ADD THESE EXAMPLES ---
History: Assistant: What's your budget?
Latest User Query: "Under €20000"
Output: {{..., "maxPrice": 20000.0, "intent": "clarify", ...}}

History: Assistant: What year are you looking for?
Latest User Query: "2021"
Output: {{..., "minYear": 2021, "intent": "clarify", ...}}

History: Assistant: What's the maximum mileage you'd consider?
Latest User Query: "50000"
Output: {{..., "maxMileage": 50000, "intent": "clarify", ...}}

History: Assistant: What fuel type do you prefer?
Latest User Query: "Petrol"
Output: {{..., "preferredFuelTypes": ["Petrol"], "intent": "clarify", ...}}

History: Assistant: Any preferred makes?
Latest User Query: "Toyota please"
Output: {{..., "preferredMakes": ["Toyota"], "intent": "clarify", ...}}
# --- END EXAMPLES ---

History: User: looking for suv / Assistant: Found 5 SUVs...
Latest User Query: "Show me sedans instead"
Output: {{..., "preferredVehicleTypes": ["Sedan", "Saloon"], "intent": "replace_criteria", ...}}

History: User: looking for suv / Assistant: Found 5 SUVs...
Latest User Query: "also make it hybrid"
Output: {{..., "preferredFuelTypes": ["Hybrid"], "intent": "add_criteria", ...}}

Respond ONLY with the JSON object.
"""


def run_llm_with_history(
    user_query: str,
    conversation_history: List[Dict[str, str]],
    matched_category: Optional[str] = None,
    force_model: Optional[str] = None,
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

    # Build the appropriate system prompt
    system_prompt = build_enhanced_system_prompt(
        user_query,
        conversation_history,
        matched_category,
        VALID_MANUFACTURERS,
        VALID_FUEL_TYPES,
        VALID_VEHICLE_TYPES,
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
            # Example: based on conversation_history or latest_query_fragment
            latest_query_fragment = extract_newest_user_fragment(user_query).lower()
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
    Main endpoint for parameter extraction. Uses zero-shot intent classification
    to route between RAG and LLM.
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

        logger.info(
            "Processing query: %s (forceModel=%s) with %d history items",
            user_query,
            force_model,
            len(conversation_history),
        )

        # 1) Quick check for off-topic
        if not is_car_related(user_query):
            logger.info("Query classified as off-topic.")
            # Return structured default response for off-topic
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
                # Adjusted threshold based on testing, might need further tuning
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
                    # If it returns None, we default below
                    if intent_result is None:  # Explicitly check if fallback failed
                        classified_intent = "SPECIFIC_SEARCH"  # Safe default
            else:
                logger.error(
                    "Failed to get query embedding, defaulting intent to SPECIFIC_SEARCH."
                )
        except Exception as e:
            logger.error(
                f"Error during embedding or classification: {e}", exc_info=True
            )
            # Fallback safely if classification fails
            classified_intent = "SPECIFIC_SEARCH"

        # 3) Routing based on Intent
        final_response = None
        query_fragment = extract_newest_user_fragment(
            user_query
        )  # Use for RAG and checks

        if classified_intent == "VAGUE_INQUIRY":
            logger.info(
                "Intent initially classified as VAGUE_INQUIRY. Checking history for clarification context..."
            )

            # --- New Logic: Check if this is likely an answer to a clarification question ---
            is_clarification_answer = False
            if conversation_history:
                try:
                    last_turn = conversation_history[-1]
                    # Assuming 'ai' role for assistant, adjust if needed
                    if last_turn.get("ai"):
                        last_ai_message = last_turn["ai"].lower()
                        # Check if last AI message was likely a question
                        is_question = "?" in last_ai_message or any(
                            q_word in last_ai_message
                            for q_word in [
                                "what",
                                "which",
                                "how",
                                "budget",
                                "price",
                                "year",
                                "make",
                                "type",
                                "mileage",
                            ]
                        )

                        if is_question:
                            # Check if the user's current query fragment is short and looks like an answer
                            word_count = word_count_clean(query_fragment)
                            is_short_answer = (
                                word_count < 6
                            )  # Adjust threshold as needed
                            # Check for numbers, simple keywords, or common answer patterns
                            looks_like_answer = (
                                re.search(r"\d", query_fragment)  # Contains numbers
                                or any(
                                    ans_word in query_fragment.lower()
                                    for ans_word in [
                                        "yes",
                                        "no",
                                        "petrol",
                                        "diesel",
                                        "electric",
                                        "hybrid",
                                        "under",
                                        "over",
                                        "between",
                                        "around",
                                    ]
                                )
                                or query_fragment.lower()
                                in VALID_MANUFACTURERS  # Is just a make
                                or query_fragment.lower()
                                in VALID_FUEL_TYPES  # Is just a fuel type
                                or query_fragment.lower()
                                in VALID_VEHICLE_TYPES  # Is just a vehicle type
                            )

                            if is_short_answer and looks_like_answer:
                                logger.info(
                                    "Detected likely clarification answer to previous question. Re-routing to LLM (SPECIFIC_SEARCH path)."
                                )
                                is_clarification_answer = True
                                classified_intent = (
                                    "SPECIFIC_SEARCH"  # Override intent to use LLM path
                                )
                except IndexError:
                    logger.warning(
                        "Could not access last turn in conversation history."
                    )
                except Exception as hist_ex:
                    logger.error(
                        f"Error checking conversation history for clarification context: {hist_ex}",
                        exc_info=True,
                    )
            # --- End New Logic ---

            # Only proceed with RAG if it wasn't identified as a clarification answer
            if not is_clarification_answer:
                logger.info("Proceeding with RAG for VAGUE_INQUIRY.")
                try:
                    match_cat, score = find_best_match(query_fragment)
                    logger.info(
                        f"RAG result: Category='{match_cat}', Score={score:.2f}"
                    )

                    if score < 0.6:  # Threshold for weak RAG match
                        logger.info(
                            "RAG score too low. Requesting general clarification."
                        )
                        final_response = create_default_parameters(
                            intent="clarify",
                            clarification_needed=True,
                            clarification_needed_for=["details"],
                            retriever_suggestion="Could you provide more specific details about the type of vehicle you need?",  # Simple suggestion
                        )
                    else:  # Medium/High RAG score >= 0.6
                        logger.info(
                            "RAG score sufficient. Requesting specific clarification based on matched category."
                        )
                        final_response = create_default_parameters(
                            intent="clarify",
                            clarification_needed=True,
                            clarification_needed_for=[
                                "budget",
                                "year",
                                "make",
                            ],  # Suggest common next questions
                            matched_category=match_cat,
                            # Simple suggestion
                            retriever_suggestion=f"Okay, thinking about {match_cat}s. What's your budget or preferred year range?",
                        )
                except Exception as e:
                    logger.error(f"Error during RAG processing: {e}", exc_info=True)
                    # Fallback if RAG fails: treat as specific search
                    logger.warning(
                        "RAG failed, falling back to LLM (SPECIFIC_SEARCH path)."
                    )
                    classified_intent = "SPECIFIC_SEARCH"
                    final_response = (
                        None  # Reset response, will proceed to LLM block below
                    )

        # Proceed to LLM if intent is specific OR if VAGUE path decided not to
        # return early (e.g., RAG error or clarification answer detected)
        if classified_intent == "SPECIFIC_SEARCH" and final_response is None:
            logger.info(
                "Intent is SPECIFIC_SEARCH (or fallback/clarification answer), proceeding to LLM."
            )
            # Note: matched_category is None here as we didn't use RAG or it failed/was bypassed
            extracted_params = run_llm_with_history(
                user_query, conversation_history, None, force_model
            )

            if extracted_params:
                # Ensure the intent reflects reality, fallback to classified if needed
                if not extracted_params.get("intent"):
                    logger.warning(
                        f"LLM did not return intent, using classified intent: {classified_intent}"
                    )
                    # If we rerouted a clarification answer, the LLM *should* set intent to 'clarify'
                    # If it didn't, we might default back to SPECIFIC_SEARCH here, which might be okay.
                    extracted_params["intent"] = classified_intent
                # Ensure all fields exist using create_default_parameters as base
                base = create_default_parameters()
                base.update(extracted_params)  # Overwrite defaults with LLM output
                final_response = base
                logger.info("Final extracted parameters from LLM: %s", final_response)
            else:
                logger.error(
                    "LLM models failed or no valid extraction after SPECIFIC_SEARCH intent."
                )
                final_response = create_default_parameters(
                    intent="error"
                )  # Indicate error

        # Handle cases where final_response wasn't set (should be rare)
        if final_response is None:
            logger.error(
                f"Reached end of processing without a valid final_response. Intent was '{classified_intent}'. Defaulting to error."
            )
            final_response = create_default_parameters(intent="error")

        end_time = datetime.datetime.now()
        duration = (end_time - start_time).total_seconds()
        logger.info(f"Request processing completed in {duration:.2f} seconds.")

        return jsonify(final_response), 200

    except Exception as e:
        logger.exception(f"Unhandled exception in /extract_parameters: {e}")
        # Use create_default_parameters for error response structure
        return jsonify(create_default_parameters(intent="error")), 500


# --- Main Execution ---
if __name__ == "__main__":
    # Run initialization once before starting the server
    # Note: In production, use a proper WSGI server like Gunicorn or Waitress
    # initialize_app_components() # Already called via app_context
    app.run(host="0.0.0.0", port=5006, debug=False)  # Keep debug=False
