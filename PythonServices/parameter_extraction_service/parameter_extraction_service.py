#!/usr/bin/env python3
# Standard library imports first
import json
import logging
import re
import os
import sys
import datetime
from typing import Dict, List, Optional, Any, Tuple, Set

# Third-party imports
from flask import Flask, request, jsonify
import requests
from dotenv import load_dotenv
import numpy as np

# Local application imports
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

negation_triggers = [
    "no ", "not ", "don't want ", "dont want ", "don't like ", "dont like ",
    "except ", "excluding ", "anything but ", "anything except ", "avoid ",
    "hate ", "dislike ", "other than ", "besides ", "apart from "
]

conjunctions = [" or ", " and ", ", "]


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
        "explicitly_negated_makes": [],
        "explicitly_negated_vehicle_types": [],
        "explicitly_negated_fuel_types": [],
    }


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
    Builds an enhanced system prompt for LLM that includes conversation history,
    available options, and confirmed/rejected context.
    """
    # Format conversation history as clear context
    history_context = ""
    if conversation_history:
        history_context = "## CONVERSATION HISTORY:\n"
        for i, turn in enumerate(conversation_history[-5:]): # Last 5 turns max
            # Handle different possible keys for role and content
            role = turn.get("role")
            content = turn.get("content")
            if not role:
                 if "user" in turn:
                     role = "user"
                     content = turn.get("user")
                 elif "ai" in turn:
                     role = "assistant"
                     content = turn.get("ai")

            if role == "user" and content:
                history_context += f"User: {content}\n"
            elif role == "assistant" and content:
                history_context += f"Assistant: {content}\n"

    # Format matched category if available
    category_context = ""
    if matched_category:
        category_context = f"\n## MATCHED VEHICLE CATEGORY: {matched_category}\n"

    # Format confirmed context if available
    confirmed_context_str = ""
    if confirmed_context and any(v for v in confirmed_context.values() if v is not None and (not isinstance(v, list) or len(v) > 0)):
        confirmed_context_str = "\n## CONFIRMED PREFERENCES (Do not contradict these):\n"

        # Add confirmed makes
        if confirmed_context.get("confirmedMakes"):
            confirmed_context_str += f"- Preferred Makes: {', '.join(confirmed_context['confirmedMakes'])}\n"

        # Add confirmed price range
        price_info = []
        if confirmed_context.get("confirmedMinPrice") is not None:
            price_info.append(f"Min: {confirmed_context['confirmedMinPrice']}")
        if confirmed_context.get("confirmedMaxPrice") is not None:
            price_info.append(f"Max: {confirmed_context['confirmedMaxPrice']}")
        if price_info:
            confirmed_context_str += f"- Price Range: {', '.join(price_info)}\n"

        # Add confirmed year range
        year_info = []
        if confirmed_context.get("confirmedMinYear") is not None:
            year_info.append(f"Min: {confirmed_context['confirmedMinYear']}")
        if confirmed_context.get("confirmedMaxYear") is not None:
            year_info.append(f"Max: {confirmed_context['confirmedMaxYear']}")
        if year_info:
            confirmed_context_str += f"- Year Range: {', '.join(year_info)}\n"

        # Add confirmed mileage
        if confirmed_context.get("confirmedMaxMileage") is not None:
            confirmed_context_str += f"- Max Mileage: {confirmed_context['confirmedMaxMileage']}\n"

        # Add confirmed fuel types
        if confirmed_context.get("confirmedFuelTypes"):
            confirmed_context_str += f"- Preferred Fuel Types: {', '.join(confirmed_context['confirmedFuelTypes'])}\n"

        # Add confirmed vehicle types
        if confirmed_context.get("confirmedVehicleTypes"):
            confirmed_context_str += f"- Preferred Vehicle Types: {', '.join(confirmed_context['confirmedVehicleTypes'])}\n"

        # Add confirmed transmission
        if confirmed_context.get("confirmedTransmission"):
            confirmed_context_str += f"- Transmission: {confirmed_context['confirmedTransmission']}\n"

        # Add confirmed engine size
        engine_size_info = []
        if confirmed_context.get("confirmedMinEngineSize") is not None:
            engine_size_info.append(f"Min: {confirmed_context['confirmedMinEngineSize']}L")
        if confirmed_context.get("confirmedMaxEngineSize") is not None:
            engine_size_info.append(f"Max: {confirmed_context['confirmedMaxEngineSize']}L")
        if engine_size_info:
            confirmed_context_str += f"- Engine Size Range: {', '.join(engine_size_info)}\n"

        # Add confirmed horsepower
        hp_info = []
        if confirmed_context.get("confirmedMinHorsePower") is not None:
            hp_info.append(f"Min: {confirmed_context['confirmedMinHorsePower']}hp")
        if confirmed_context.get("confirmedMaxHorsePower") is not None:
            hp_info.append(f"Max: {confirmed_context['confirmedMaxHorsePower']}hp")
        if hp_info:
            confirmed_context_str += f"- Horsepower Range: {', '.join(hp_info)}\n"

    # Format rejected context if available
    rejected_context_str = ""
    if rejected_context and any(v for v in rejected_context.values() if v is not None and (not isinstance(v, list) or len(v) > 0)):
        rejected_context_str = "\n## REJECTED PREFERENCES (User has explicitly rejected these):\n"

        # Add rejected makes
        if rejected_context.get("rejectedMakes"):
            rejected_context_str += f"- Rejected Makes: {', '.join(rejected_context['rejectedMakes'])}\n"

        # Add rejected vehicle types
        if rejected_context.get("rejectedVehicleTypes"):
            rejected_context_str += f"- Rejected Vehicle Types: {', '.join(rejected_context['rejectedVehicleTypes'])}\n"

        # Add rejected fuel types
        if rejected_context.get("rejectedFuelTypes"):
            rejected_context_str += f"- Rejected Fuel Types: {', '.join(rejected_context['rejectedFuelTypes'])}\n"

        # Add rejected transmission if present
        if rejected_context.get("rejectedTransmission"):
            rejected_context_str += f"- Rejected Transmission: {rejected_context['rejectedTransmission']}\n"

    # Create example format for JSON output
    # Use create_default_parameters to ensure all keys are present
    default_params_example = create_default_parameters()
    # Populate with example values for clarity in the prompt
    default_params_example.update({
        "minPrice": 15000, "maxPrice": 25000, "minYear": 2018, "maxYear": 2022,
        "maxMileage": 50000, "preferredMakes": ["Toyota", "Honda"],
        "preferredFuelTypes": ["Petrol"], "preferredVehicleTypes": ["SUV"],
        "desiredFeatures": ["Bluetooth", "Backup Camera"], "intent": "new_query",
        "transmission": "Automatic", "minEngineSize": 2.0, "maxEngineSize": 3.5,
        "minHorsepower": 150, "maxHorsepower": 300
    })
    # Ensure explicitly_negated lists are included in the example structure
    default_params_example["explicitly_negated_makes"] = []
    default_params_example["explicitly_negated_vehicle_types"] = []
    default_params_example["explicitly_negated_fuel_types"] = []
    json_format_example = json.dumps(default_params_example, indent=2)


    # Build the full prompt
    # Added more specific instructions based on previous issues
    return f"""
You are an advanced automotive parameter extraction assistant for Smart Auto Trader.
Analyze the LATEST user query ONLY to extract explicitly mentioned parameters.
Use conversation history and context PRIMARILY to determine the 'intent' and understand implicit references (like 'it' or 'that one').
DO NOT infer parameters from history if they are not mentioned in the LATEST query, especially for preferredMakes, preferredFuelTypes, and preferredVehicleTypes during refinements.

YOUR RESPONSE MUST BE ONLY A SINGLE VALID JSON OBJECT containing the following keys: {list(create_default_parameters().keys())}.
Use this exact format, filling values based ONLY on the LATEST query and context:
{json_format_example}

{history_context}
{category_context}
{confirmed_context_str}
{rejected_context_str}
Latest User Query: "{user_query}"

## CORE EXTRACTION RULES:
- Focus ONLY on the LATEST user query for explicit parameters.
- Use conversation history ONLY to determine the 'intent' or resolve pronouns/references.
- Numeric values MUST be numbers (int or float), not strings. Use null if not mentioned.
- Array values MUST be lists of strings. Use [] if not mentioned.
- Extract makes/fuels/types ONLY if explicitly mentioned in the LATEST query AND they appear in the Valid lists below.
- desiredFeatures: Extract features mentioned in the LATEST query.
- isOffTopic: Set to true ONLY if the LATEST query is clearly NOT about vehicles. If true, set offTopicResponse.
- clarificationNeeded: Set to true ONLY if the LATEST query is vague AND lacks sufficient detail to proceed (e.g., "Find me something nice"). If true, list needed info in clarificationNeededFor (e.g., ["budget", "type"]). DO NOT set to true if the user is just refining or negating criteria.

## NEGATION HANDLING (CRITICAL RULES):
- If the user explicitly rejects a make/type/fuel (e.g., "not Toyota", "no SUVs", "don't want diesel"), DO NOT include it in the corresponding preferred* list. The post-processing step will handle adding it to the 'explicitly_negated_*' list.
- For transmission, if user says "not automatic" or "no manual", set the transmission field to null.

## PARAMETER HANDLING RULES:
- transmission: Set to "Automatic" or "Manual". Null otherwise.
- minEngineSize/maxEngineSize: Extract engine size in liters (e.g., "2.0L" -> 2.0). Handle ranges, minimums ("at least 2.0L"), maximums ("under 2.5L"). Convert units like "1600cc" -> 1.6. Null if not mentioned.
- minHorsepower/maxHorsepower: Extract horsepower as integers (e.g., "200hp" -> 200). Handle ranges, minimums, maximums. Accept "bhp", "PS". Null if not mentioned.

## INTENT DETERMINATION (CRITICAL):
- 'new_query': User starts a completely new search or provides initial criteria.
- 'refine_criteria': User modifies existing criteria (changes price, adds/removes makes/types/fuels, adds constraints like 'no toyota'). This is the MOST COMMON intent after the first query.
- 'add_criteria': User adds criteria without contradicting previous ones (less common than refine).
- 'clarify': User directly answers a specific question asked by the Assistant in the previous turn.
- 'off_topic': User query is unrelated to vehicles.

## CONTEXTUAL INTERPRETATION (Use ONLY for 'clarify' intent):
- If the Assistant asked about budget, interpret numbers like "15000" as maxPrice.
- If the Assistant asked about year, interpret numbers like "2018" as minYear.
- If the Assistant asked about transmission, interpret replies like "automatic" accordingly.
- Apply similar logic ONLY for direct answers to specific questions about engine size or horsepower.
- For non-clarification queries, extract parameters ONLY as explicitly stated in the LATEST query.

## VALID VALUES:
- Valid Makes: {json.dumps(valid_makes)}
- Valid Fuel Types: {json.dumps(valid_fuels)}
- Valid Vehicle Types (includes aliases): {json.dumps(valid_vehicles)}
- Valid Transmission Types: ["Automatic", "Manual"]

## EXAMPLES (Focus on LATEST query + history for intent):
Latest User Query: "I want a Toyota under 20000"
Output: {{"minPrice": null, "maxPrice": 20000, "preferredMakes": ["Toyota"], "intent": "new_query"...}}

Latest User Query: "Actually, make it a Honda instead"
Output: {{"preferredMakes": ["Honda"], "intent": "refine_criteria"...}} # Note: Other fields are null/[] as not mentioned in THIS query

Latest User Query: "I hate SUVs"
Output: {{"preferredVehicleTypes": [], "intent": "refine_criteria"...}} # Note: preferredVehicleTypes is empty, post-processing handles the negation list

Latest User Query: "What's the weather like today?"
Output: {{"isOffTopic": true, "offTopicResponse": "I specialize in helping with vehicle searches...", "intent": "off_topic"...}}

Latest User Query: "Under €15000"
History: Assistant: What's your budget?
Output: {{"maxPrice": 15000, "intent": "clarify"...}} # Intent is clarify due to history

Latest User Query: "Ok, but no toyota"
History: Assistant: Found 5 Honda SUVs...
Output: {{"intent": "refine_criteria", "preferredMakes": [] ...}} # Intent is refine, preferredMakes empty, post-processing handles negated list

Respond ONLY with the JSON object.
"""


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

    # Allow clarification needed responses
    if params.get("clarificationNeeded") is True:
         return True

    if params.get("intent") == "refine_criteria":
        has_negations = (
            len(params.get("explicitly_negated_makes", [])) > 0 or
            len(params.get("explicitly_negated_vehicle_types", [])) > 0 or
            len(params.get("explicitly_negated_fuel_types", [])) > 0
        )
        if has_negations:
             # Allow refinement if only negations were extracted
             # Check if *other* criteria were also set. If only negations, it's valid.
             has_other_criteria = (
                 params.get("minPrice") is not None or params.get("maxPrice") is not None or
                 params.get("minYear") is not None or params.get("maxYear") is not None or
                 params.get("maxMileage") is not None or
                 len(params.get("preferredMakes", [])) > 0 or
                 len(params.get("preferredFuelTypes", [])) > 0 or
                 len(params.get("preferredVehicleTypes", [])) > 0 or
                 len(params.get("desiredFeatures", [])) > 0 or
                 params.get("transmission") is not None or
                 params.get("minEngineSize") is not None or params.get("maxEngineSize") is not None or
                 params.get("minHorsepower") is not None or params.get("maxHorsepower") is not None
             )
             if not has_other_criteria:
                  logger.info("Validation: Allowing refine_criteria intent with only negations.")
                  return True
             # If it has negations AND other criteria, fall through to normal checks below

    # Check for at least one non-null parameter or non-empty list
    has_price = params.get("minPrice") is not None or params.get("maxPrice") is not None
    has_year = params.get("minYear") is not None or params.get("maxYear") is not None
    has_mileage = params.get("maxMileage") is not None
    has_makes = len(params.get("preferredMakes", [])) > 0
    has_fuel = len(params.get("preferredFuelTypes", [])) > 0
    has_type = len(params.get("preferredVehicleTypes", [])) > 0
    has_features = len(params.get("desiredFeatures", [])) > 0
    has_transmission = params.get("transmission") is not None
    has_engine = params.get("minEngineSize") is not None or params.get("maxEngineSize") is not None
    has_hp = params.get("minHorsepower") is not None or params.get("maxHorsepower") is not None

    # Consider it valid if at least one criterion is set
    is_sufficient = (
        has_price
        or has_year
        or has_mileage
        or has_makes
        or has_fuel
        or has_type
        or has_features
        or has_transmission
        or has_engine
        or has_hp
    )

    if is_sufficient:
        return True
    else:
        # Log only if it wasn't already allowed as a refine_criteria with only negations
        # Check the negation lists again to be safe before logging the warning
        was_negation_only = (params.get("intent") == "refine_criteria" and (
            len(params.get("explicitly_negated_makes", [])) > 0 or
            len(params.get("explicitly_negated_vehicle_types", [])) > 0 or
            len(params.get("explicitly_negated_fuel_types", [])) > 0
            ))
        if not was_negation_only:
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
             val = params.get(field)
             # Check for None explicitly before type check
             if val is not None and isinstance(val, (int, float)):
                 if val > 0:
                     result[field] = float(val)
                 else:
                     logger.warning(f"Invalid {field} value: {val} (must be positive)")

        for field in ["minYear", "maxYear", "maxMileage"]:
             val = params.get(field)
             if val is not None and isinstance(val, (int, float)):
                 current_year = datetime.datetime.now().year
                 if field == "minYear" and val >= 1900 and val <= current_year + 1:
                     result[field] = int(val)
                 elif field == "maxYear" and val >= 1900 and val <= current_year + 1:
                     result[field] = int(val)
                 elif field == "maxMileage" and val >= 0: # Allow 0 mileage
                     result[field] = int(val)
                 else:
                     logger.warning(f"Invalid {field} value: {val} (out of reasonable range)")

        # Convert valid lists to lowercase sets for case-insensitive matching
        valid_makes_lower = {make.lower() for make in VALID_MANUFACTURERS}
        valid_fuel_types_lower = {fuel.lower() for fuel in VALID_FUEL_TYPES}
        valid_vehicle_types_lower = {vehicle_type.lower() for vehicle_type in VALID_VEHICLE_TYPES}

        # Create lookup maps for preserving original casing
        valid_makes_map = {make.lower(): make for make in VALID_MANUFACTURERS}
        valid_fuel_types_map = {fuel.lower(): fuel for fuel in VALID_FUEL_TYPES}
        valid_vehicle_types_map = {vehicle_type.lower(): vehicle_type for vehicle_type in VALID_VEHICLE_TYPES}

        # Handle array fields with validation against known valid values (case-insensitive)
        if isinstance(params.get("preferredMakes"), list):
            result["preferredMakes"] = [
                valid_makes_map[m.lower()]  # Use the original casing from the valid list
                for m in params["preferredMakes"]
                if isinstance(m, str) and m.lower() in valid_makes_lower  # Case-insensitive validation
            ]

        if isinstance(params.get("preferredFuelTypes"), list):
            result["preferredFuelTypes"] = [
                valid_fuel_types_map[f.lower()]  # Use the original casing from the valid list
                for f in params.get("preferredFuelTypes", [])
                if isinstance(f, str) and f.lower() in valid_fuel_types_lower  # Case-insensitive validation
            ]

        if isinstance(params.get("preferredVehicleTypes"), list):
            result["preferredVehicleTypes"] = [
                valid_vehicle_types_map[v.lower()]  # Use the original casing from the valid list
                for v in params["preferredVehicleTypes"]
                if isinstance(v, str) and v.lower() in valid_vehicle_types_lower  # Case-insensitive validation
            ]

        if isinstance(params.get("desiredFeatures"), list):
            result["desiredFeatures"] = [
                f
                for f in params["desiredFeatures"]
                if isinstance(f, str) and f.strip() # Basic validation + remove empty/whitespace-only
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
            # Added 'negative_constraint' as potentially valid from LLM
            valid_intents = ["new_query", "clarify", "refine_criteria", "add_criteria", "replace_criteria", "error", "off_topic", "negative_constraint"]
            intent = params["intent"].lower().strip()
            if intent in valid_intents:
                result["intent"] = intent
            else:
                logger.warning(f"Unknown intent '{intent}', defaulting to 'new_query'")
                result["intent"] = "new_query"
        else:
            result["intent"] = "new_query" # Default if missing

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
                result["transmission"] = transmission_value.capitalize()
            # Allow null transmission from LLM
            elif params["transmission"] is None:
                 result["transmission"] = None
            else:
                logger.warning(f"Invalid transmission value: {params['transmission']}")

        # Handle engine size (as float)
        for field in ["minEngineSize", "maxEngineSize"]:
             val = params.get(field)
             if val is not None and isinstance(val, (int, float)):
                 if val >= 0.5 and val <= 10.0:
                     result[field] = float(val)
                 else:
                     logger.warning(f"Invalid {field} value: {val} (outside reasonable range)")

        # Handle horsepower (as int)
        for field in ["minHorsepower", "maxHorsepower"]:
             val = params.get(field)
             if val is not None and isinstance(val, (int, float)):
                 if val >= 20 and val <= 1500:
                     result[field] = int(val)
                 else:
                     logger.warning(f"Invalid {field} value: {val} (outside reasonable range)")

        for key in ["explicitly_negated_makes", "explicitly_negated_vehicle_types", "explicitly_negated_fuel_types"]:
             if key in params and isinstance(params[key], list):
                 # Ensure items are strings
                 result[key] = [item for item in params[key] if isinstance(item, str)]


    except Exception as e:
        logger.exception(f"Error during parameter processing: {e}")
        # Return default structure on error
        return create_default_parameters(intent="error") # Set intent to error

    return result


def find_negated_terms(text: str, valid_items: List[str]) -> Set[str]:
    """ Simpler check for negated items """
    negated = set()
    text_lower = text.lower()
    valid_items_lower_map = {item.lower(): item for item in valid_items}

    for pattern in negation_triggers:
        start_index = 0
        while start_index < len(text_lower):
            idx = text_lower.find(pattern, start_index)
            if idx == -1:
                break
            phrase_start = idx + len(pattern)
            end_match = re.search(r'[.!?,\n]| but | also | and | with | like | prefer ', text_lower[phrase_start:])
            phrase_end = phrase_start + end_match.start() if end_match else len(text_lower)
            phrase = text_lower[phrase_start:phrase_end].strip()
            potential_items_parts = [phrase]
            for conj in conjunctions:
                new_parts = []
                for part in potential_items_parts:
                    new_parts.extend(part.split(conj))
                potential_items_parts = new_parts
            for potential_item in potential_items_parts:
                potential_item = potential_item.strip().lower()
                if not potential_item:
                    continue
                for item_lower, item_original in valid_items_lower_map.items():
                    if re.search(r'\b' + re.escape(item_lower) + r'\b', potential_item):
                        logger.debug(f"Negation Match: Found '{item_original}' after '{pattern}' in phrase segment '{potential_item}'")
                        negated.add(item_original)  # Use canonical casing
            start_index = phrase_start
    return negated


def find_positive_terms(text: str, valid_items: List[str], negated_terms: Set[str]) -> Set[str]:
    """ Finds valid items mentioned that are NOT in the negated set """
    positive = set()
    text_lower = text.lower()
    valid_items_lower_map = {item.lower(): item for item in valid_items}
    negated_terms_lower = {term.lower() for term in negated_terms}
    for item_lower, item_original in valid_items_lower_map.items():
        if item_lower in negated_terms_lower:
            continue
        if re.search(r'\b' + re.escape(item_lower) + r'\b', text_lower):
            logger.debug(f"Positive Match: Found '{item_original}' (and not identified as negated)")
            positive.add(item_original)  # Use canonical casing
    return positive


def run_llm_with_history(
    user_query: str,
    conversation_history: List[Dict[str, str]],
    matched_category: Optional[str] = None,
    force_model: Optional[str] = None,
    confirmed_context: Optional[Dict] = None,
    rejected_context: Optional[Dict] = None,
    contains_override: bool = False
) -> Optional[Dict[str, Any]]:
    """
    Run LLM extraction with conversation history and context.
    Tries multiple models in order of preference.
    Includes REVISED post-processing for negations and hallucinations.
    """
    FAST_MODEL = "meta-llama/llama-3.1-8b-instruct:free"
    REFINE_MODEL = "google/gemma-3-27b-it:free"
    CLARIFY_MODEL = "mistralai/mistral-7b-instruct:free"
    if force_model == "fast": models_to_try = [FAST_MODEL, REFINE_MODEL, CLARIFY_MODEL]
    elif force_model == "refine": models_to_try = [REFINE_MODEL, CLARIFY_MODEL, FAST_MODEL]
    elif force_model == "clarify": models_to_try = [CLARIFY_MODEL, REFINE_MODEL, FAST_MODEL]
    else: models_to_try = [FAST_MODEL, REFINE_MODEL, CLARIFY_MODEL]
    logger.info(f"Will try models in sequence: {models_to_try}")

    try:
        system_prompt = build_enhanced_system_prompt(
            user_query, conversation_history, matched_category,
            VALID_MANUFACTURERS, VALID_FUEL_TYPES, VALID_VEHICLE_TYPES,
            confirmed_context, rejected_context
        )
    except Exception as e:
        logger.exception(f"Error building system prompt: {e}")
        return create_default_parameters(intent="error")

    # --- Try Models ---
    extracted_params = None
    for model in models_to_try:
        logger.info(f"Attempting extraction with model: {model}")
        extracted = None
        try:
            extracted = try_extract_with_model(model, system_prompt, user_query)
        except Exception as e:
             logger.exception(f"Error calling try_extract_with_model for model {model}: {e}")
             continue

        if extracted:
            processed = process_parameters(extracted)

            # Extract query fragment for analysis
            query_fragment = extract_newest_user_fragment(user_query)
            lower_query_fragment = query_fragment.lower()

            # --- 1. Determine Context ---
            # First, find negated terms in the query
            negated_makes_set = find_negated_terms(lower_query_fragment, VALID_MANUFACTURERS)
            negated_types_set = find_negated_terms(lower_query_fragment, VALID_VEHICLE_TYPES)
            negated_fuels_set = find_negated_terms(lower_query_fragment, VALID_FUEL_TYPES)

            # Then find positive mentions, excluding negated terms
            positive_makes_set = find_positive_terms(lower_query_fragment, VALID_MANUFACTURERS, negated_makes_set)
            positive_types_set = find_positive_terms(lower_query_fragment, VALID_VEHICLE_TYPES, negated_types_set)
            positive_fuels_set = find_positive_terms(lower_query_fragment, VALID_FUEL_TYPES, negated_fuels_set)

            # Determine basic query attributes
            has_any_positives = bool(positive_makes_set or positive_types_set or positive_fuels_set)
            has_any_negatives = bool(negated_makes_set or negated_types_set or negated_fuels_set)
            is_simple_negation_query = not has_any_positives and has_any_negatives

            # Get intent (might be overridden later in extract_parameters)
            final_intent = processed.get("intent", "new_query")

            # Log query analysis
            logger.info(f"Query analysis: intent={final_intent}, simple_negation={is_simple_negation_query}")
            logger.info(f"Positive mentions: makes={positive_makes_set}, types={positive_types_set}, fuels={positive_fuels_set}")
            logger.info(f"Negated terms: makes={negated_makes_set}, types={negated_types_set}, fuels={negated_fuels_set}")

            # Override intent for simple negation queries if needed
            if is_simple_negation_query and final_intent != "refine_criteria":
                logger.info("Setting intent to 'refine_criteria' due to simple negation.")
                final_intent = "refine_criteria"
                processed["intent"] = "refine_criteria"  # Update in processed for consistency

            # --- 1a. Define keyword sets for scalar parameter types ---
            # Keywords related to price parameters
            PRICE_KEYWORDS = {
                'price', 'budget', 'cost', 'euro', 'dollar', 'pound', 'spend', 'pay', 'afford', 
                '€', '$', '£', 'under', 'over', 'between', 'range', 'cheap', 'expensive', 
                'pricey', 'costly', 'money', 'funds', 'finances', 'affordable', 'grand', 'k'
            }

            # Keywords related to year parameters
            YEAR_KEYWORDS = {
                'year', 'older', 'newer', 'age', 'recent', 'vintage', 'yr', 'model year', 
                'registration', 'reg', 'plate', 'built', 'manufactured', 'make', 'made', 
                'new', 'old', '20', '19', '\'', 'from', 'since', 'before'  # Year indicators like '20xx, '19xx
            }

            # Keywords related to mileage parameters
            MILEAGE_KEYWORDS = {
                'mileage', 'miles', 'mile', 'km', 'kilometers', 'kilometre', 'odometer', 'clock', 
                'driven', 'used', 'low', 'high', 'distance', 'travelled', 'run', 'usage', 'wear'
            }

            # Keywords related to transmission parameters
            TRANSMISSION_KEYWORDS = {
                'transmission', 'automatic', 'manual', 'gear', 'gearbox', 'auto', 'stick', 'cvt', 
                'dsg', 'paddle', 'shift', 'clutch', 'self-shifting', 'tiptronic', 'sequential'
            }

            # Keywords related to engine size parameters
            ENGINE_KEYWORDS = {
                'engine', 'size', 'liter', 'litre', 'l engine', 'cc', 'cubic', 'displacement', 
                'capacity', 'motor', 'cylinder', 'cylinders', 'block', 'tdi', 'tsi', 'tfsi', 
                'turbo', 'small', 'big', 'large', 'displacement'
            }

            # Keywords related to horsepower parameters
            HP_KEYWORDS = {
                'horsepower', 'hp', 'bhp', 'power', 'ps', 'kw', 'performance', 'fast', 'strong', 
                'quick', 'powerful', 'output', 'torque', 'acceleration', 'pulling power', 'grunt'
            }

            # Create parameter-to-keywords mapping
            KEYWORD_SETS = {
                'minPrice': PRICE_KEYWORDS,
                'maxPrice': PRICE_KEYWORDS,
                'minYear': YEAR_KEYWORDS,
                'maxYear': YEAR_KEYWORDS,
                'maxMileage': MILEAGE_KEYWORDS,
                'transmission': TRANSMISSION_KEYWORDS,
                'minEngineSize': ENGINE_KEYWORDS,
                'maxEngineSize': ENGINE_KEYWORDS,
                'minHorsepower': HP_KEYWORDS,
                'maxHorsepower': HP_KEYWORDS
            }

            # --- 2. Initialize Final Parameters ---
            final_params = create_default_parameters()
            final_params["intent"] = final_intent

            # --- 3. Refactored Scalar Parameter Merging Logic ---
            # Define scalar parameters and their corresponding context keys
            scalar_params = {
                'minPrice': 'confirmedMinPrice', 
                'maxPrice': 'confirmedMaxPrice',
                'minYear': 'confirmedMinYear', 
                'maxYear': 'confirmedMaxYear',
                'maxMileage': 'confirmedMaxMileage',
                'transmission': 'confirmedTransmission',
                'minEngineSize': 'confirmedMinEngineSize', 
                'maxEngineSize': 'confirmedMaxEngineSize',
                'minHorsepower': 'confirmedMinHorsePower',  # Note the capital P in HorsePower
                'maxHorsepower': 'confirmedMaxHorsePower'   # Note the capital P in HorsePower
            }

            # Process each scalar parameter with improved context-awareness
            for param, context_key in scalar_params.items():
                llm_value = processed.get(param)
                context_value = confirmed_context.get(context_key) if confirmed_context else None
                relevant_keywords = KEYWORD_SETS.get(param, set())
                
                # Check if the current query mentions this parameter type
                query_mentions_param = any(kw in lower_query_fragment for kw in relevant_keywords)
                
                # Apply new logic based on query content and LLM extraction
                if llm_value is not None and query_mentions_param:
                    # LLM extracted a value AND query mentions this parameter type - use LLM value
                    final_params[param] = llm_value
                    logger.debug(f"Using explicit {param}={llm_value} from query (keywords present)")
                elif final_intent in ["refine_criteria", "clarify", "add_criteria"] and context_value is not None:
                    # Keep context for refinement/clarification if no explicit mention
                    final_params[param] = context_value
                    if query_mentions_param:
                        logger.debug(f"Query mentions {param} keywords but LLM provided no value, keeping context {param}={context_value}")
                    else:
                        logger.debug(f"Carrying over {param}={context_value} from context (no mention in query)")
                else:
                    # Default: leave as None for new queries or when no context exists
                    if llm_value is not None and not query_mentions_param:
                        logger.info(f"Ignoring potential LLM hallucination: {param}={llm_value} (no keywords in query)")

            # --- 4. Merge List Parameters ---
            # Helper function to handle list merging logic consistently
            def merge_list_param(param_name, context_key, positive_set, negated_set, is_simple_negation=False):
                # Start with confirmed values from context if intent suggests we should keep context
                if final_intent in ["refine_criteria", "clarify", "add_criteria"] and confirmed_context:
                    result_set = set(confirmed_context.get(context_key, []))
                    
                    # For simple negation, we clear everything if there are negations for this param
                    if is_simple_negation and negated_set:
                        result_set = set()
                        logger.info(f"Simple negation: Clearing all {param_name}")
                    else:
                        # Otherwise add positives and remove negatives
                        if positive_set:
                            result_set = result_set.union(positive_set)
                            logger.debug(f"Added positives to {param_name}: {positive_set}")
                        
                        # Always remove negated terms
                        if negated_set:
                            result_set = result_set.difference(negated_set)
                            logger.debug(f"Removed negatives from {param_name}: {negated_set}")
                else:
                    # For new queries or other intents, just use what was explicitly mentioned
                    result_set = set(positive_set)
                    logger.debug(f"Using only explicit mentions for {param_name}: {result_set}")
                
                # Convert back to list
                return list(result_set)

            # Apply the merging logic to each list parameter
            final_params["preferredMakes"] = merge_list_param(
                "preferredMakes", "confirmedMakes", positive_makes_set, negated_makes_set, is_simple_negation_query)

            final_params["preferredVehicleTypes"] = merge_list_param(
                "preferredVehicleTypes", "confirmedVehicleTypes", positive_types_set, negated_types_set, is_simple_negation_query)

            final_params["preferredFuelTypes"] = merge_list_param(
                "preferredFuelTypes", "confirmedFuelTypes", positive_fuels_set, negated_fuels_set, is_simple_negation_query)

            # Special handling for desiredFeatures - just union with context
            if final_intent in ["refine_criteria", "clarify", "add_criteria"] and confirmed_context:
                context_features = set(confirmed_context.get("confirmedFeatures", []))
                new_features = set(processed.get("desiredFeatures", []))
                
                # Features can't easily be analyzed directly from text, so we just trust what the LLM extracted
                final_params["desiredFeatures"] = list(context_features.union(new_features))
            else:
                final_params["desiredFeatures"] = processed.get("desiredFeatures", [])

            # --- 5. Set Negated Lists ---
            final_params["explicitly_negated_makes"] = list(negated_makes_set)
            final_params["explicitly_negated_vehicle_types"] = list(negated_types_set)
            final_params["explicitly_negated_fuel_types"] = list(negated_fuels_set)

            # --- 6. Set Final Intent & Flags ---
            # Copy over other important fields from processed
            for key in ["isOffTopic", "offTopicResponse", "clarificationNeeded", 
                       "clarificationNeededFor", "retrieverSuggestion", "matchedCategory"]:
                final_params[key] = processed.get(key)

            # --- 7. Replace Output Assignment ---
            logger.info(f"Final merged parameters: {final_params}")

            # Check validity before return
            if is_valid_extraction(final_params):
                logger.info("Post-processing complete. Parameters are valid.")
                extracted_params = final_params
                break
            else:
                logger.warning(f"Parameters became invalid after merging for model {model}. Discarded. Trying next model.")
                extracted_params = None
                continue

        else:
            logger.warning(f"Extraction from model {model} returned None or failed parsing.")
            extracted_params = None

    # --- Final Return ---
    if extracted_params:
        logger.info(f"Successful extraction with final parameters: {extracted_params}")
    else:
        logger.warning("All LLM extraction attempts failed or resulted in invalid parameters after post-processing!")
        extracted_params = create_default_parameters(intent="error")

    return extracted_params


# --- Flask App Setup ---
app = Flask(__name__)

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

        # 3) Extract newest user fragment for processing
        query_fragment = extract_newest_user_fragment(user_query)
        lower_query_fragment = query_fragment.lower()

        # Initialize force_llm here, before the keyword checking block
        force_llm = False
        
        # Check for specific make/type/fuel keywords using the now-defined lower_query_fragment
        valid_keywords_lower = set()
        valid_keywords_lower.update(make.lower() for make in VALID_MANUFACTURERS)
        valid_keywords_lower.update(fuel.lower() for fuel in VALID_FUEL_TYPES)
        valid_keywords_lower.update(vtype.lower() for vtype in VALID_VEHICLE_TYPES)

        # Check if any specific known keyword appears in the query
        words_in_query = set(re.findall(r'\b(\w+)\b', lower_query_fragment))
        specific_keywords_found = words_in_query.intersection(valid_keywords_lower)

        # If query contains specific keywords and was classified as vague, change to specific
        if specific_keywords_found and classified_intent == 'VAGUE_INQUIRY' and not force_llm:
            logger.info(f"Specific keywords found in vague query: {specific_keywords_found}. Forcing SPECIFIC_SEARCH/LLM path.")
            classified_intent = 'SPECIFIC_SEARCH'

        # 4) Initialize routing condition flags
        is_clarification_answer = False
        contains_override = False
        mentions_rejected = False
        
        # 5) Enhanced check for override keywords
        # ... (rest of the code remains unchanged)

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
                rejected_context=rejected_context,
                contains_override=contains_override
            )
            
            if extracted_params:
                # NEW CODE: Prevent clarification loops by forcing clarificationNeeded=False 
                # if this is already a clarification answer
                if is_clarification_answer:
                    if extracted_params.get("clarificationNeeded"):
                        logger.info("LOOP PREVENTION: Overriding LLM's clarificationNeeded=True because this is already a clarification answer")
                        extracted_params["clarificationNeeded"] = False
                        extracted_params["clarificationNeededFor"] = []
                        
                # Existing code for intent override continues below
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


if __name__ == "__main__":
    app.run(host="0.0.0.0", port=5006, debug=False)  # Keep debug=False
