"""
Parameter Extraction Service for Smart Auto Trader.

This Flask application serves as a microservice to extract structured vehicle search
parameters from natural language user queries. It leverages Large Language Models (LLMs)
via the OpenRouter API and a local RAG (Retrieval Augmented Generation) system
for intent classification and parameter extraction.

The service aims to understand user intent (e.g., new search, refinement, clarification)
and extract key vehicle attributes such as make, model, price range, year, mileage,
fuel type, vehicle type, and desired features. It also handles off-topic queries
and requests for clarification when input is ambiguous.

Notes:
    - Configuration: Relies on environment variables for API keys (e.g., OPENROUTER_API_KEY).
    - LLM Interaction: Uses specific models (e.g., Llama, Gemma, Mistral) for different tasks
      like fast extraction, refinement, and clarification.
    - Intent Classification: Employs a zero-shot classification approach using precomputed
      embeddings for intent labels.
    - RAG System: Utilizes a local retriever component (assumed to be in a 'retriever'
      subdirectory) for matching queries to predefined vehicle categories.
    - Context Management: The service can process conversation history and confirmed/rejected
      context to improve extraction accuracy in multi-turn dialogues.
    - Error Handling: Includes fallbacks and default responses for API failures,
      invalid extractions, or low-confidence results.
    - Validation: Extracted parameters are validated against predefined lists (e.g.,
      valid makes, fuel types) and logical constraints (e.g., minPrice <= maxPrice).

Dependencies:
    - Standard Library: datetime, json, logging, os, re, sys, typing
    - Third-party: numpy, requests, dotenv, Flask
    - Local: retriever.retriever (for cosine_sim, get_query_embedding, find_best_match,
      initialize_retriever)
"""

# !/usr/bin/env python3
# Standard library imports first
import datetime
import json
import logging
import os
import re
import sys
from typing import Any, Dict, List, Optional, Set

import numpy as np
import requests
from dotenv import load_dotenv

# Third-party imports
from flask import Flask, jsonify, request

# Local application imports
sys.path.append(os.path.dirname(os.path.abspath(__file__)))
try:
    from retriever.retriever import (
        cosine_sim,
    )  # Assumes this function exists in retriever.py
    from retriever.retriever import (
        get_query_embedding,
    )  # Assumes this function exists in retriever.py now
    from retriever.retriever import find_best_match, initialize_retriever
except ImportError as ie:
    logging.error(
        f"Could not import from retriever package: {ie}. "
        f"Make sure retriever directory is structured correctly and "
        f"contains __init__.py if needed."
    )

    def initialize_retriever():
        """Placeholder for initialize_retriever if import fails."""
        logger.error("retriever.initialize_retriever failed to import!")

    def get_query_embedding(text):
        """Placeholder for get_query_embedding if import fails."""
        logger.error("retriever.get_query_embedding failed to import!")
        return None

    def cosine_sim(a, b):
        """Placeholder for cosine_sim if import fails."""
        logger.error("retriever.cosine_sim failed to import!")
        return 0.0

    def find_best_match(text):
        """Placeholder for find_best_match if import fails."""
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
FAST_MODEL = "meta-llama/llama-3.3-8b-instruct:free"
REFINE_MODEL = "google/gemma-3-27b-it:free"
CLARIFY_MODEL = "mistralai/mistral-7b-instruct:free"
VERY_LOW_CONFIDENCE_THRESHOLD = 0.2  # Task 1: Define a constant

# Define thresholds for confidence levels
LOW_CONFIDENCE_THRESHOLD = 0.4

CONFUSED_FALLBACK_PROMPT = (
    "Sorry, I seem to have gotten a bit confused. Could you please restate your main "
    "vehicle requirements simply? (e.g., 'SUV under 50k, hybrid or petrol, 2020 or newer')"
)

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
    "no ",
    "not ",
    "don't want ",
    "dont want ",
    "don't like ",
    "dont like ",
    "except ",
    "excluding ",
    "anything but ",
    "anything except ",
    "avoid ",
    "hate ",
    "dislike ",
    "other than ",
    "besides ",
    "apart from ",
]

conjunctions = [" or ", " and ", ", "]


# --- Helper Function Definitions (Defined Before Routes) ---


def word_count_clean(query: str) -> int:
    """Counts meaningful words in a cleaned-up user query.

    This function removes punctuation and converts the query to lowercase
    before splitting it into words and counting them.

    Args:
        query: The user query string.

    Returns:
        The number of words in the cleaned query.

    Example:
        >>> word_count_clean("Hello, world!")
        2
        >>> word_count_clean("  Find me a car.  ")
        4
    """
    cleaned = re.sub(r"[^a-zA-Z0-9 ]+", "", query).lower()
    return len(cleaned.strip().split())


def extract_newest_user_fragment(query: str) -> str:
    """
    Extracts the latest user input from a potentially compound query string.

    For follow-up queries that might be formatted like "Original Query - Additional info: New Input",
    this function aims to return only the "New Input" part. If the specific
    " - Additional info:" pattern is not found, the original query is returned.

    Args:
        query: The user query string, which might contain historical context.

    Returns:
        The newest fragment of the user's input, stripped of leading/trailing whitespace.

    Example:
        >>> extract_newest_user_fragment("SUV - Additional info: with a sunroof")
        'with a sunroof'
        >>> extract_newest_user_fragment("Just a red car")
        'Just a red car'
    """
    # Use rsplit to handle multiple occurrences, splitting only once from the right
    parts = query.rsplit(" - Additional info:", 1)
    if len(parts) > 1:
        return parts[-1].strip()
    else:
        return query.strip()  # Return original if pattern not found


def initialize_app_components():
    """
    Initializes necessary components for the application.

    This function performs two main tasks:
    1. Initializes the RAG (Retrieval Augmented Generation) retriever by calling
       `initialize_retriever()`. This typically involves loading language models
       and precomputed embeddings for vehicle categories.
    2. Precomputes embeddings for the intent labels defined in `INTENT_LABELS`.
       These embeddings are stored in the global `PRECOMPUTED_LABEL_EMBEDDINGS`
       dictionary and are used for zero-shot intent classification.

    If any part of the initialization fails, appropriate error messages are logged,
    and `PRECOMPUTED_LABEL_EMBEDDINGS` might be left empty, potentially disabling
    or degrading intent classification.
    """
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
    """
    Classifies user query intent using zero-shot learning with cosine similarity.

    Compares the embedding of the user query against precomputed embeddings of
    intent labels (e.g., "SPECIFIC_SEARCH", "VAGUE_INQUIRY"). The intent with the
    highest cosine similarity score is chosen, provided it meets the specified
    threshold.

    Includes fallback logic: if the best score is below threshold, it might default
    to "VAGUE_INQUIRY" if that was the highest scoring (but below threshold) or if
    both scores are very low. Otherwise, it might default to "SPECIFIC_SEARCH".

    Args:
        query_embedding: A NumPy array representing the embedding of the user's query.
        threshold: A float representing the minimum cosine similarity score required
                   to confidently classify an intent. Defaults to 0.6.

    Returns:
        The classified intent label (e.g., "SPECIFIC_SEARCH", "VAGUE_INQUIRY") as a string
        if classification is successful and meets the threshold or fallback criteria.
        Returns `None` if label embeddings are not available, the query embedding is None,
        no similarities can be calculated, or an unexpected error occurs.
    """
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
    """
    Performs a simple heuristic check to determine if a user query is car-related.

    The function checks for the presence of common automotive keywords, including
    vehicle types, makes, fuel types, and terms related to buying/selling cars.
    It also tries to identify and filter out common off-topic greetings or
    general questions not containing car keywords. Very short queries are also
    scrutinized.

    Args:
        query: The user query string.

    Returns:
        True if the query is deemed car-related, False otherwise.

    Example:
        >>> is_car_related("I want to buy a Toyota SUV")
        True
        >>> is_car_related("What's the weather like?")
        False
        >>> is_car_related("BMW")
        True
        >>> is_car_related("Hi")
        False
    """
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
        "kpl",
        "automatic",
        "manual",
        "auto",
        "stick shift",
        "paddle shift",
        "dsg",
        "cvt",
        "engine size",
        "liter",
        "litre",
        "cc",
        "cubic",
        "displacement",
        "horsepower",
        "hp",
        "bhp",
        "power",
        "torque",
        "performance",
        "l engine",
        "cylinder",
    }

    # Add common makes dynamically from the global list
    car_keywords.update(make.lower() for make in VALID_MANUFACTURERS)

    # Check for presence of keywords
    if any(keyword in query_lower for keyword in car_keywords):
        return True

    # Enhanced off-topic detection - include more greetings
    off_topic_starts = (
        "hi",
        "hello",
        "how are you",
        "who is",
        "tell me a joke",
        "hey",
        "hey there",
        "yo",
        "sup",
        "what's up",
        "hiya",
        "howdy",
        "good morning",
        "good afternoon",
        "good evening",
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
    """
    Creates a dictionary with a default set of vehicle search parameters.

    This function is used to initialize the parameter structure or to provide
    a fallback when extraction fails or specific conditions are met (e.g.,
    off-topic query, need for clarification).

    Args:
        intent: The determined intent of the query (e.g., "new_query", "clarify").
        is_off_topic: Boolean flag indicating if the query is off-topic.
        off_topic_response: A string response for off-topic queries.
        clarification_needed: Boolean flag indicating if clarification is needed.
        clarification_needed_for: A list of strings specifying what parameters
                                  need clarification.
        retriever_suggestion: A suggestion string, often from the RAG system or
                              for clarification prompts.
        matched_category: The vehicle category matched by the RAG system, if any.

    Returns:
        A dictionary containing all standard search parameters, initialized to
        None or empty lists, along with the provided metadata (intent, flags, etc.).
    """
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
    last_question_asked: Optional[str] = None,  # ADD THIS
) -> str:
    """
    Constructs a detailed system prompt for the LLM parameter extraction task.

    The prompt includes:
    - Instructions for the LLM on its role and expected JSON output format.
    - Conversation history (last few turns).
    - Matched vehicle category from RAG (if any).
    - Confirmed preferences from previous interactions.
    - Rejected preferences from previous interactions.
    - The latest user query.
    - Core extraction rules, negation handling rules, parameter handling rules.
    - Intent determination guidelines.
    - Contextual interpretation rules (especially for clarification intents).
    - Lists of valid makes, fuel types, vehicle types, and transmission types.
    - Examples of input and expected JSON output.

    Args:
        user_query: The latest query from the user.
        conversation_history: A list of previous turns in the conversation,
                              where each turn is a dictionary with 'role' and 'content'.
        matched_category: The vehicle category matched by the RAG system, if any.
        valid_makes: A list of valid manufacturer names.
        valid_fuels: A list of valid fuel types.
        valid_vehicles: A list of valid vehicle types (including aliases).
        confirmed_context: A dictionary of parameters confirmed by the user in
                           previous turns.
        rejected_context: A dictionary of parameters explicitly rejected by the
                          user in previous turns.
        last_question_asked: The last question asked by the assistant, if any.

    Returns:
        A string representing the complete system prompt to be sent to the LLM.
    """
    # Format conversation history as clear context
    history_context = ""
    if conversation_history:
        history_context = "## CONVERSATION HISTORY:\n"
        for i, turn in enumerate(conversation_history[-5:]):  # Last 5 turns max
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
    if confirmed_context and any(
        v
        for v in confirmed_context.values()
        if v is not None and (not isinstance(v, list) or len(v) > 0)
    ):
        confirmed_context_str = (
            "\n## CONFIRMED PREFERENCES (Do not contradict these):\n"
        )

        # Add confirmed makes
        if confirmed_context.get("confirmedMakes"):
            confirmed_context_str += (
                f"- Preferred Makes: {', '.join(confirmed_context['confirmedMakes'])}\n"
            )

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
            confirmed_context_str += (
                f"- Max Mileage: {confirmed_context['confirmedMaxMileage']}\n"
            )

        # Add confirmed fuel types
        if confirmed_context.get("confirmedFuelTypes"):
            confirmed_context_str += f"- Preferred Fuel Types: {', '.join(confirmed_context['confirmedFuelTypes'])}\n"

        # Add confirmed vehicle types
        if confirmed_context.get("confirmedVehicleTypes"):
            confirmed_context_str += (
                f"- Preferred Vehicle Types: "
                f"{', '.join(confirmed_context['confirmedVehicleTypes'])}\n"
            )

        # Add confirmed transmission
        if confirmed_context.get("confirmedTransmission"):
            confirmed_context_str += (
                f"- Transmission: {confirmed_context['confirmedTransmission']}\n"
            )

        # Add confirmed engine size
        engine_size_info = []
        if confirmed_context.get("confirmedMinEngineSize") is not None:
            engine_size_info.append(
                f"Min: {confirmed_context['confirmedMinEngineSize']}L"
            )
        if confirmed_context.get("confirmedMaxEngineSize") is not None:
            engine_size_info.append(
                f"Max: {confirmed_context['confirmedMaxEngineSize']}L"
            )
        if engine_size_info:
            confirmed_context_str += (
                f"- Engine Size Range: {', '.join(engine_size_info)}\n"
            )

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
    if rejected_context and any(
        v
        for v in rejected_context.values()
        if v is not None and (not isinstance(v, list) or len(v) > 0)
    ):
        rejected_context_str = (
            "\n## REJECTED PREFERENCES (User has explicitly rejected these):\n"
        )

        # Add rejected makes
        if rejected_context.get("rejectedMakes"):
            rejected_context_str += (
                f"- Rejected Makes: {', '.join(rejected_context['rejectedMakes'])}\n"
            )

        # Add rejected vehicle types
        if rejected_context.get("rejectedVehicleTypes"):
            rejected_context_str += f"- Rejected Vehicle Types: {', '.join(rejected_context['rejectedVehicleTypes'])}\n"

        # Add rejected fuel types
        if rejected_context.get("rejectedFuelTypes"):
            rejected_context_str += f"- Rejected Fuel Types: {', '.join(rejected_context['rejectedFuelTypes'])}\n"

        # Add rejected transmission if present
        if rejected_context.get("rejectedTransmission"):
            rejected_context_str += (
                f"- Rejected Transmission: {rejected_context['rejectedTransmission']}\n"
            )

    # Create example format for JSON output
    # Use create_default_parameters to ensure all keys are present
    default_params_example = create_default_parameters()
    # Populate with example values for clarity in the prompt
    default_params_example.update(
        {
            "minPrice": 15000,
            "maxPrice": 25000,
            "minYear": 2018,
            "maxYear": 2022,
            "maxMileage": 50000,
            "preferredMakes": ["Toyota", "Honda"],
            "preferredFuelTypes": ["Petrol"],
            "preferredVehicleTypes": ["SUV"],
            "desiredFeatures": ["Bluetooth", "Backup Camera"],
            "intent": "new_query",
            "transmission": "Automatic",
            "minEngineSize": 2.0,
            "maxEngineSize": 3.5,
            "minHorsepower": 150,
            "maxHorsepower": 300,
        }
    )
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
Use conversation history and context PRIMARILY to determine the 'intent' and understand implicit
references (like 'it' or 'that one').
DO NOT infer parameters from history if they are not mentioned in the LATEST query, especially for preferredMakes,
preferredFuelTypes, and preferredVehicleTypes during refinements.
YOUR RESPONSE MUST BE ONLY A SINGLE VALID JSON OBJECT containing the following keys:
{list(create_default_parameters().keys())}.
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
- clarificationNeeded: Set to true ONLY if the LATEST query is vague AND lacks sufficient detail
   -to proceed (e.g., "Find me something nice"). If true, list needed info in clarificationNeededFor
   -(e.g., ["budget", "type"]).
   - DO NOT set to true if the user is just refining or negating criteria.
"When the user mentions a budget with a single number (e.g., 'under 50000', 'around 50000', '50000 budget'),
    - interpret this number as the maxPrice ONLY. Leave minPrice as null unless a clear
    -range (e.g., 'between 40k and 50k') is stated."

## NEGATION HANDLING (CRITICAL RULES):
- If the user explicitly rejects a make/type/fuel (e.g., "not Toyota", "no SUVs", "don't want diesel")
    -DO NOT include it in the corresponding preferred* list. The post-processing step will
    -handle adding it to the 'explicitly_negated_*' list.
- For transmission, if user says "not automatic" or "no manual", set the transmission field to null.

## PARAMETER HANDLING RULES:
- transmission: Set to "Automatic" or "Manual". Null otherwise.
- minEngineSize/maxEngineSize: Extract engine size in liters (e.g., "2.0L" -> 2.0).
    -Handle ranges, minimums ("at least 2.0L"), maximums ("under 2.5L").
    -Convert units like "1600cc" -> 1.6. Null if not mentioned.
- minHorsepower/maxHorsepower: Extract horsepower as integers (e.g., "200hp" -> 200).
    -Handle ranges, minimums, maximums. Accept "bhp", "PS". Null if not mentioned.

## INTENT DETERMINATION (CRITICAL):
- 'new_query': User starts a completely new search or provides initial criteria.
- 'refine_criteria': User modifies
    -existing criteria (changes price, adds/removes makes/types/fuels, adds constraints like 'no toyota').
    -This is the MOST COMMON intent after the first query.
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
Output: {{"preferredMakes": ["Honda"], "intent": "refine_criteria"...}}
    -# Note: Other fields are null/[] as not mentioned in THIS query

Latest User Query: "I hate SUVs"
Output: {{"preferredVehicleTypes": [], "intent": "refine_criteria"...}}
    -# Note: preferredVehicleTypes is empty, post-processing handles the negation list

Latest User Query: "What's the weather like today?"
Output: {{"isOffTopic": true, "offTopicResponse":
    -"I specialize in helping with vehicle searches...", "intent": "off_topic"...}}

Latest User Query: "Under €15000"
History: Assistant: What's your budget?
Output: {{"maxPrice": 15000, "intent": "clarify"...}} # Intent is clarify due to history

Latest User Query: "Ok, but no toyota"
History: Assistant: Found 5 Honda SUVs...
Output: {{"intent": "refine_criteria", "preferredMakes": [] ...}}
    -# Intent is refine, preferredMakes empty, post-processing handles negated list

Respond ONLY with the JSON object.
"""


def try_extract_with_model(
    model: str, system_prompt: str, user_query: str
) -> Optional[Dict[str, Any]]:
    """
    Attempts to extract parameters by calling an LLM via the OpenRouter API.

    Sends the system prompt and user query to the specified LLM model.
    It then tries to parse the LLM's response as a JSON object.
    Handles API errors, timeouts, and JSON decoding issues.

    Args:
        model: The identifier of the LLM model to use (e.g., "meta-llama/llama-3.1-8b-instruct:free").
        system_prompt: The system prompt guiding the LLM's behavior.
        user_query: The user's query to extract parameters from.

    Returns:
        A dictionary containing the extracted parameters if the API call and
        JSON parsing are successful and the JSON has the expected structure.
        Returns `None` otherwise.

    Raises:
        This function catches common exceptions (requests.exceptions.Timeout,
        requests.exceptions.RequestException, json.JSONDecodeError, general Exception)
        and logs them, returning None instead of re-raising.
    """
    if not OPENROUTER_API_KEY:
        logger.error("OpenRouter API Key is not configured. Cannot make API call.")
        return None
    try:
        headers = {
            "Authorization": f"Bearer {OPENROUTER_API_KEY}",
            "Content-Type": "application/json",
            "HTTP-Referer": "https://smartautotrader.app",
            "X-Title": "SmartAutoTraderParameterExtraction",
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
    """
    Validates if the extracted parameters dictionary is plausible for a vehicle search.

    Checks for:
    - Correct type (dictionary).
    - Valid off-topic or clarificationNeeded states.
    - For "refine_criteria" intent, allows if only negations were extracted.
    - Presence of at least one meaningful search criterion (price, year, make, etc.).

    Args:
        params: A dictionary of parameters extracted by the LLM.

    Returns:
        True if the parameters are considered valid, False otherwise.
    """
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
            len(params.get("explicitly_negated_makes", [])) > 0
            or len(params.get("explicitly_negated_vehicle_types", [])) > 0
            or len(params.get("explicitly_negated_fuel_types", [])) > 0
        )
        if has_negations:
            # Allow refinement if only negations were extracted
            # Check if *other* criteria were also set. If only negations, it's valid.
            has_other_criteria = (
                params.get("minPrice") is not None
                or params.get("maxPrice") is not None
                or params.get("minYear") is not None
                or params.get("maxYear") is not None
                or params.get("maxMileage") is not None
                or len(params.get("preferredMakes", [])) > 0
                or len(params.get("preferredFuelTypes", [])) > 0
                or len(params.get("preferredVehicleTypes", [])) > 0
                or len(params.get("desiredFeatures", [])) > 0
                or params.get("transmission") is not None
                or params.get("minEngineSize") is not None
                or params.get("maxEngineSize") is not None
                or params.get("minHorsepower") is not None
                or params.get("maxHorsepower") is not None
            )
            if not has_other_criteria:
                logger.info(
                    "Validation: Allowing refine_criteria intent with only negations."
                )
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
    has_engine = (
        params.get("minEngineSize") is not None
        or params.get("maxEngineSize") is not None
    )
    has_hp = (
        params.get("minHorsepower") is not None
        or params.get("maxHorsepower") is not None
    )

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
        was_negation_only = params.get("intent") == "refine_criteria" and (
            len(params.get("explicitly_negated_makes", [])) > 0
            or len(params.get("explicitly_negated_vehicle_types", [])) > 0
            or len(params.get("explicitly_negated_fuel_types", [])) > 0
        )
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
    Cleans, validates, and standardizes the structure of parameters extracted by the LLM.

    This function takes the raw dictionary output from the LLM and performs several operations:
    - Ensures all expected fields are present by starting with a default structure.
    - Validates and converts numeric fields (price, year, mileage, engine size, horsepower)
      to their correct types (float or int) and checks if they are within reasonable ranges.
    - Validates list fields (preferredMakes, preferredFuelTypes, preferredVehicleTypes)
      against predefined lists of valid values (case-insensitively) and standardizes
      their casing.
    - Validates desiredFeatures to ensure they are non-empty strings.
    - Validates boolean flags (isOffTopic, clarificationNeeded).
    - Validates string fields (offTopicResponse, retrieverSuggestion, matchedCategory).
    - Validates and normalizes the 'intent' field.
    - Ensures 'clarificationNeededFor' is a list of strings.
    - Validates 'transmission' against "Automatic" or "Manual".
    - Populates 'explicitly_negated_*' lists.

    Args:
        params: The dictionary of parameters as extracted by the LLM.

    Returns:
        A dictionary containing the processed and validated parameters. If a critical
        error occurs during processing, a default parameter structure with 'intent'
        set to "error" is returned.
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
                elif field == "maxMileage" and val >= 0:  # Allow 0 mileage
                    result[field] = int(val)
                else:
                    logger.warning(
                        f"Invalid {field} value: {val} (out of reasonable range)"
                    )

        # Convert valid lists to lowercase sets for case-insensitive matching
        valid_makes_lower = {make.lower() for make in VALID_MANUFACTURERS}
        valid_fuel_types_lower = {fuel.lower() for fuel in VALID_FUEL_TYPES}
        valid_vehicle_types_lower = {
            vehicle_type.lower() for vehicle_type in VALID_VEHICLE_TYPES
        }

        # Create lookup maps for preserving original casing
        valid_makes_map = {make.lower(): make for make in VALID_MANUFACTURERS}
        valid_fuel_types_map = {fuel.lower(): fuel for fuel in VALID_FUEL_TYPES}
        valid_vehicle_types_map = {
            vehicle_type.lower(): vehicle_type for vehicle_type in VALID_VEHICLE_TYPES
        }

        # Handle array fields with validation against known valid values (case-insensitive)
        if isinstance(params.get("preferredMakes"), list):
            result["preferredMakes"] = [
                valid_makes_map[
                    m.lower()
                ]  # Use the original casing from the valid list
                for m in params["preferredMakes"]
                if isinstance(m, str)
                and m.lower() in valid_makes_lower  # Case-insensitive validation
            ]

        if isinstance(params.get("preferredFuelTypes"), list):
            result["preferredFuelTypes"] = [
                valid_fuel_types_map[
                    f.lower()
                ]  # Use the original casing from the valid list
                for f in params.get("preferredFuelTypes", [])
                if isinstance(f, str)
                and f.lower() in valid_fuel_types_lower  # Case-insensitive validation
            ]

        if isinstance(params.get("preferredVehicleTypes"), list):
            result["preferredVehicleTypes"] = [
                valid_vehicle_types_map[
                    v.lower()
                ]  # Use the original casing from the valid list
                for v in params["preferredVehicleTypes"]
                if isinstance(v, str)
                and v.lower()
                in valid_vehicle_types_lower  # Case-insensitive validation
            ]

        if isinstance(params.get("desiredFeatures"), list):
            result["desiredFeatures"] = [
                f
                for f in params["desiredFeatures"]
                if isinstance(f, str)
                and f.strip()  # Basic validation + remove empty/whitespace-only
            ]

        # Handle boolean flags
        if isinstance(params.get("isOffTopic"), bool):
            result["isOffTopic"] = params["isOffTopic"]

        if isinstance(params.get("clarificationNeeded"), bool):
            result["clarificationNeeded"] = params["clarificationNeeded"]

        # Handle string fields
        if "offTopicResponse" in params and isinstance(params["offTopicResponse"], str):
            result["offTopicResponse"] = params["offTopicResponse"]

        if "retrieverSuggestion" in params and isinstance(
            params["retrieverSuggestion"], str
        ):
            result["retrieverSuggestion"] = params["retrieverSuggestion"]

        if "matchedCategory" in params and isinstance(params["matchedCategory"], str):
            result["matchedCategory"] = params["matchedCategory"]

        # Process intent with validation
        if "intent" in params and isinstance(params["intent"], str):
            # Added 'negative_constraint' as potentially valid from LLM
            valid_intents = [
                "new_query",
                "clarify",
                "refine_criteria",
                "add_criteria",
                "replace_criteria",
                "error",
                "off_topic",
                "negative_constraint",
            ]
            intent = params["intent"].lower().strip()
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
                item
                for item in params["clarificationNeededFor"]
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
                    logger.warning(
                        f"Invalid {field} value: {val} (outside reasonable range)"
                    )

        # Handle horsepower (as int)
        for field in ["minHorsepower", "maxHorsepower"]:
            val = params.get(field)
            if val is not None and isinstance(val, (int, float)):
                if val >= 20 and val <= 1500:
                    result[field] = int(val)
                else:
                    logger.warning(
                        f"Invalid {field} value: {val} (outside reasonable range)"
                    )

        for key in [
            "explicitly_negated_makes",
            "explicitly_negated_vehicle_types",
            "explicitly_negated_fuel_types",
        ]:
            if key in params and isinstance(params[key], list):
                # Ensure items are strings
                result[key] = [item for item in params[key] if isinstance(item, str)]

    except Exception as e:
        logger.exception(f"Error during parameter processing: {e}")
        # Return default structure on error
        return create_default_parameters(intent="error")  # Set intent to error

    return result


def find_negated_terms(text: str, valid_items: List[str]) -> Set[str]:
    """
    Identifies items from a valid list that are explicitly negated in the given text.

    This function iterates through a list of `negation_triggers` (e.g., "no ", "not ",
    "don't want ") and checks if any of the `valid_items` follow these triggers
    in the text. It handles simple conjunctions (like "or", "and", ",") within
    the negated phrase.

    Args:
        text: The input text string (e.g., user query) to search for negations.
        valid_items: A list of canonical string items to check for negation
                     (e.g., list of valid makes, fuel types).

    Returns:
        A set of strings, where each string is an item from `valid_items`
        that was found to be explicitly negated in the text. The items in the
        returned set will have the original casing from `valid_items`.

    Example:
        >>> find_negated_terms("I want a car, no Toyota or Honda, and not a diesel.",
        ...                    ["Toyota", "Honda", "BMW", "Diesel", "Petrol"])
        {'Toyota', 'Honda', 'Diesel'}
        >>> find_negated_terms("Anything but an SUV.", ["SUV", "Sedan"])
        {'SUV'}
    """
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
            end_match = re.search(
                r"[.!?,\n]| but | also | and | with | like | prefer ",
                text_lower[phrase_start:],
            )
            phrase_end = (
                phrase_start + end_match.start() if end_match else len(text_lower)
            )
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
                    if re.search(r"\b" + re.escape(item_lower) + r"\b", potential_item):
                        logger.debug(
                            f"Negation Match: Found '{item_original}' after '{pattern}' "
                            f"in phrase segment '{potential_item}'"
                        )
                        negated.add(item_original)  # Use canonical casing
            start_index = phrase_start
    return negated


def find_positive_terms(
    text: str, valid_items: List[str], negated_terms: Set[str]
) -> Set[str]:
    """
    Identifies items from a valid list that are mentioned positively in the text.

    "Positively mentioned" means the item is found in the text and is *not*
    present in the `negated_terms` set. This helps distinguish explicit
    preferences from rejections.

    Args:
        text: The input text string (e.g., user query).
        valid_items: A list of canonical string items to check for positive mentions.
        negated_terms: A set of strings representing items that have already been
                       identified as explicitly negated.

    Returns:
        A set of strings, where each string is an item from `valid_items`
        that was found in the text and not in `negated_terms`. The items
        in the returned set will have the original casing from `valid_items`.

    Example:
        >>> valid = ["Toyota", "Honda", "BMW", "Diesel", "Petrol"]
        >>> negated = {"Toyota", "Diesel"}
        >>> find_positive_terms("I like Honda and Petrol cars, but not Toyota or Diesel.",
        ...                     valid, negated)
        {'Honda', 'Petrol'}
    """
    positive = set()
    text_lower = text.lower()
    valid_items_lower_map = {item.lower(): item for item in valid_items}
    negated_terms_lower = {term.lower() for term in negated_terms}
    for item_lower, item_original in valid_items_lower_map.items():
        if item_lower in negated_terms_lower:
            continue
        if re.search(r"\b" + re.escape(item_lower) + r"\b", text_lower):
            logger.debug(
                f"Positive Match: Found '{item_original}' (and not identified as negated)"
            )
            positive.add(item_original)  # Use canonical casing
    return positive


def merge_list_param_corrected(
    param_name,
    context_key,
    llm_list,
    positive_set,
    negated_set,
    is_simple_negation,
    confirmed_context,
):
    """
    Merges list-based parameters from LLM output, positive/negated term analysis, and confirmed context.

    This function implements the logic for combining preferences for list-based
    parameters like `preferredMakes`, `preferredVehicleTypes`, and `preferredFuelTypes`.

    The merging strategy is:
    - If `is_simple_negation` is true for this parameter (meaning the query only
      contained negations for this parameter type and no positive mentions),
      the resulting list for this parameter will be empty.
    - Otherwise, it takes the union of what the LLM extracted (`llm_list`) and
      what was previously confirmed in the context (`confirmed_context`).
    - From this union, any items present in the `negated_set` (terms explicitly
      negated in the current query) are removed.

    Args:
        param_name (str): The name of the parameter being merged (e.g., "preferredMakes").
                          Used for logging.
        context_key (str): The key used to retrieve this parameter's list from
                           the `confirmed_context` dictionary (e.g., "confirmedMakes").
        llm_list (List[str]): The list of items for this parameter as extracted by the LLM
                              from the current query.
        positive_set (Set[str]): A set of items for this parameter type that were
                                 positively identified in the current query.
                                 (Note: This arg seems unused in the current implementation
                                 in favor of direct llm_list and context merge).
        negated_set (Set[str]): A set of items for this parameter type that were
                                explicitly negated in the current query.
        is_simple_negation (bool): A flag indicating if the current query was a
                                   "simple negation" for this parameter type (i.e.,
                                   only negations mentioned, no positive preferences).
        confirmed_context (Optional[Dict]): The dictionary of previously confirmed
                                            parameters from the conversation.

    Returns:
        List[str]: The final merged list for the parameter.
    """
    if is_simple_negation and negated_set and not positive_set:
        logger.info(
            f"Simple negation for {param_name}: clearing list due to explicit negation and no positives."
        )
        return []

    context_list = confirmed_context.get(context_key, []) if confirmed_context else []
    merged = set(llm_list) | set(context_list)
    merged -= set(negated_set)
    logger.debug(
        f"Merged {param_name}: {merged} (llm={llm_list}, context={context_list}, negated={negated_set})"
    )
    return list(merged)


def try_extract_param_from_rag(category_name: str, user_query: str) -> tuple:
    """
    Attempts to extract a single parameter name and value using RAG category and query.

    This function tries to infer a specific search parameter (like maxPrice, maxMileage,
    minYear, or maxYear) if the RAG system matched the query to a category that
    implies such a parameter (e.g., "budget cars", "low mileage SUVs", "newer vehicles").
    It uses regular expressions to find numeric values in the user query that might
    correspond to the parameter implied by the category.

    Args:
        category_name: The name of the vehicle category matched by the RAG system
                       (e.g., "Family SUVs under €30000").
        user_query: The original user query string.

    Returns:
        A tuple `(param_name, param_value)`.
        - `param_name` (str): The name of the extracted parameter (e.g., "maxPrice").
        - `param_value` (Union[float, int]): The extracted value.
        Returns `(None, None)` if no parameter can be confidently extracted.
    """
    logger.info(
        f"Attempting parameter extraction from RAG category: '{category_name}' and query: '{user_query}'"
    )

    # Convert to lowercase for easier matching
    category_lower = category_name.lower()
    query_lower = user_query.lower()

    # Extract price/budget
    if any(
        term in category_lower
        for term in ["budget", "price", "cost", "cheap", "affordable", "expensive"]
    ):
        # Look for numeric values with optional currency symbols
        price_match = re.search(r"(\d[\d,]*(?:\.\d+)?)\s*(?:k|€|£|\$)?", query_lower)
        if price_match:
            try:
                # Remove commas and convert to float
                price_value = float(price_match.group(1).replace(",", ""))

                # If the value seems to be in thousands (k)
                if "k" in query_lower and price_value < 1000:
                    price_value *= 1000

                # For budget queries, typically this is a maximum price
                logger.info(f"Extracted price parameter: maxPrice={price_value}")
                return "maxPrice", price_value
            except ValueError:
                logger.warning("Failed to convert extracted price value to float")

    # Extract mileage
    elif any(term in category_lower for term in ["mileage", "km", "miles", "odometer"]):
        # Look for numeric values with optional unit indicators
        mileage_match = re.search(r"(\d[\d,]*)\s*(?:km|miles|mi)?", query_lower)
        if mileage_match:
            try:
                mileage_value = int(mileage_match.group(1).replace(",", ""))
                logger.info(f"Extracted mileage parameter: maxMileage={mileage_value}")
                return "maxMileage", mileage_value
            except ValueError:
                logger.warning("Failed to convert extracted mileage value to int")

    # Extract year
    elif any(term in category_lower for term in ["year", "new", "older", "newer"]):
        # Look for 4-digit years or 2-digit years with apostrophe
        year_match = re.search(r"(20\d{2}|19\d{2}|'\d{2}|\d{2})", query_lower)
        if year_match:
            try:
                year_str = year_match.group(1)
                # Handle '22 format
                if year_str.startswith("'"):
                    year_value = 2000 + int(year_str[1:])
                # Handle 2-digit years (assuming 21st century)
                elif len(year_str) == 2 and year_str.isdigit():
                    year_value = 2000 + int(year_str)
                else:
                    year_value = int(year_str)

                # Check if this is likely minYear or maxYear
                if any(
                    term in query_lower
                    for term in ["after", "newer", "min", "from", "since"]
                ):
                    logger.info(f"Extracted year parameter: minYear={year_value}")
                    return "minYear", year_value
                else:
                    logger.info(f"Extracted year parameter: maxYear={year_value}")
                    return "maxYear", year_value
            except ValueError:
                logger.warning("Failed to convert extracted year value to int")

    # Add more parameter extraction cases here as needed

    logger.info("No parameters extracted from RAG category and query")
    return None, None


def try_extract_param_from_rag_category(category_name: str) -> tuple:
    """
    Extracts a single parameter and its value directly from a RAG category name.

    This function is used when a user query is vague but the RAG system
    matches it to a descriptive category (e.g., "SUVs under €20000",
    "Low mileage hatchbacks", "Petrol cars newer than 2019"). It attempts
    to parse parameters like price, mileage, year, vehicle type, fuel type,
    or make directly from the category name string itself.

    Args:
        category_name: The name of the vehicle category matched by the RAG system.

    Returns:
        A tuple `(param_name, param_value)`.
        - `param_name` (str): The name of the extracted parameter (e.g., "maxPrice",
          "preferredVehicleTypes").
        - `param_value` (Union[float, int, List[str]]): The extracted value. For list
          parameters like "preferredMakes", this will be a list containing the single
          extracted item.
        Returns `(None, None)` if no parameter can be confidently extracted from
        the category name.
    """
    logger.info(f"Attempting parameter extraction from RAG category: '{category_name}'")

    category_lower = category_name.lower()

    # Extract price range
    # Look for patterns like "under €25000", "budget 25k", "affordable (under 15000)"
    price_match = re.search(
        r"(?:budget|price|cost|under|up to|€|£|\$)\s*(\d[\d,]*(?:\.\d+)?)\s*(?:k|€|£|\$)?",
        category_lower,
    )
    if price_match:
        try:
            price_value = float(price_match.group(1).replace(",", ""))

            # If 'k' is in the category or the value seems too small to be a full price
            if "k" in category_lower and price_value < 1000:
                price_value *= 1000

            logger.info(f"Extracted from category: maxPrice={price_value}")
            return "maxPrice", price_value
        except ValueError:
            logger.warning("Failed to convert category price value to float")

    # Extract mileage
    # Look for patterns like "low mileage (under 50000 km)", "under 100000 miles"
    mileage_match = re.search(
        r"(?:mileage|miles|km|odometer).*?(\d[\d,]*)\s*(?:km|miles|mi)?", category_lower
    )
    if mileage_match:
        try:
            mileage_value = int(mileage_match.group(1).replace(",", ""))
            logger.info(f"Extracted from category: maxMileage={mileage_value}")
            return "maxMileage", mileage_value
        except ValueError:
            logger.warning("Failed to convert category mileage value to int")

    # Extract year
    # Look for patterns like "newer than 2018", "after 2020", "2015 or newer"
    year_match = re.search(
        r"(?:year|from|since|after|before|newer than|older than)\s*((?:20|19)\d{2})",
        category_lower,
    )
    if year_match:
        try:
            year_value = int(year_match.group(1))

            # Determine if it's likely minYear or maxYear based on context
            if any(
                term in category_lower
                for term in ["after", "newer", "from", "since", "min"]
            ):
                logger.info(f"Extracted from category: minYear={year_value}")
                return "minYear", year_value
            else:
                logger.info(f"Extracted from category: maxYear={year_value}")
                return "maxYear", year_value
        except ValueError:
            logger.warning("Failed to convert category year value to int")

    # Standalone year (e.g., "2018 Toyota Camry")
    standalone_year = re.search(r"\b(20\d{2}|19\d{2})\b", category_lower)
    if standalone_year:
        try:
            year_value = int(standalone_year.group(1))
            # Default to minYear for standalone years in categories
            logger.info(f"Extracted from category: minYear={year_value} (standalone)")
            return "minYear", year_value
        except ValueError:
            pass

    # Check for vehicle type (most common types)
    for vehicle_type in [
        "suv",
        "sedan",
        "saloon",
        "hatchback",
        "estate",
        "coupe",
        "convertible",
    ]:
        if vehicle_type in category_lower:
            # Find formal name in VALID_VEHICLE_TYPES
            for valid_type in VALID_VEHICLE_TYPES:
                if vehicle_type == valid_type.lower():
                    logger.info(
                        f"Extracted from category: preferredVehicleTypes=[{valid_type}]"
                    )
                    return "preferredVehicleTypes", [valid_type]

    # Check for fuel type
    for fuel_type in ["petrol", "diesel", "electric", "hybrid"]:
        if fuel_type in category_lower:
            # Find formal name in VALID_FUEL_TYPES
            for valid_fuel in VALID_FUEL_TYPES:
                if fuel_type == valid_fuel.lower():
                    logger.info(
                        f"Extracted from category: preferredFuelTypes=[{valid_fuel}]"
                    )
                    return "preferredFuelTypes", [valid_fuel]

    # Check for manufacturers
    for make in VALID_MANUFACTURERS:
        if make.lower() in category_lower:
            logger.info(f"Extracted from category: preferredMakes=[{make}]")
            return "preferredMakes", [make]

    logger.info("No parameters extracted from RAG category")
    return None, None


def try_direct_extract_from_query(user_query: str) -> Dict[str, Any]:
    """
    Directly extracts parameters from the user query using regular expression patterns.

    This function attempts a first pass at extracting common parameters like price,
    mileage, and year directly from the user's query text without relying on an LLM.
    It's intended as a faster, simpler extraction method for clearly stated parameters.

    Args:
        user_query: The user's query string.

    Returns:
        A dictionary where keys are parameter names (e.g., "maxPrice", "minYear")
        and values are the extracted parameter values. Returns an empty dictionary
        if no parameters are directly extracted.
    """
    results = {}
    query_lower = user_query.lower()

    # Extract price/budget
    price_patterns = [
        # Match formats like "€25000", "25k", "under 25000", "budget 25000"
        r"(?:budget|price|cost|under|up to|max|maximum|\€|\$|\£)\s*(\d[\d,]*(?:\.\d+)?)\s*(?:k|€|£|\$)?",
        # Match standalone numbers with currency indicators
        r"(\d[\d,]*(?:\.\d+)?)\s*(?:k|grand|euros|euro|pounds|dollars)",
    ]

    for pattern in price_patterns:
        price_match = re.search(pattern, query_lower)
        if price_match:
            try:
                # Remove commas and convert to float
                price_value = float(price_match.group(1).replace(",", ""))

                # If the value has 'k' nearby or seems small enough to be expressed in thousands
                if ("k" in query_lower and price_value < 1000) or (
                    "grand" in query_lower and price_value < 1000
                ):
                    price_value *= 1000

                results["maxPrice"] = price_value
                logger.info(f"Direct extraction: Found maxPrice={price_value}")
                break
            except ValueError:
                logger.warning("Failed to convert extracted price value to float")

    # Extract mileage
    mileage_patterns = [
        # Match formats like "50000 km", "under 50000 miles", "max mileage 50000"
        r"(?:mileage|miles|km|odometer|driven|under|max|maximum)\s*(\d[\d,]*)\s*(?:km|miles|mi)?",
    ]

    for pattern in mileage_patterns:
        mileage_match = re.search(pattern, query_lower)
        if mileage_match:
            try:
                mileage_value = int(mileage_match.group(1).replace(",", ""))
                results["maxMileage"] = mileage_value
                logger.info(f"Direct extraction: Found maxMileage={mileage_value}")
                break
            except ValueError:
                logger.warning("Failed to convert extracted mileage value to int")

    # Extract year
    # Look for years that come after qualifiers indicating min or max
    year_after_pattern = r"(?:after|since|from|newer than|min|minimum|at least)\s*((?:20|19)\d{2}|[\']\d{2})"
    year_before_pattern = r"(?:before|until|older than|max|maximum|up to|no older than)\s*((?:20|19)\d{2}|[\']\d{2})"

    year_after_match = re.search(year_after_pattern, query_lower)
    if year_after_match:
        try:
            year_str = year_after_match.group(1)
            # Handle '22 format
            if year_str.startswith("'"):
                year_value = 2000 + int(year_str[1:])
            else:
                year_value = int(year_str)

            results["minYear"] = year_value
            logger.info(f"Direct extraction: Found minYear={year_value}")
        except ValueError:
            logger.warning("Failed to convert extracted minYear value to int")

    year_before_match = re.search(year_before_pattern, query_lower)
    if year_before_match:
        try:
            year_str = year_before_match.group(1)
            # Handle '22 format
            if year_str.startswith("'"):
                year_value = 2000 + int(year_str[1:])
            else:
                year_value = int(year_str)

            results["maxYear"] = year_value
            logger.info(f"Direct extraction: Found maxYear={year_value}")
        except ValueError:
            logger.warning("Failed to convert extracted maxYear value to int")

    # If no year with qualifiers found, check for standalone 4-digit year
    if "minYear" not in results and "maxYear" not in results:
        standalone_year = re.search(r"\b(20\d{2})\b", query_lower)
        if standalone_year:
            try:
                year_value = int(standalone_year.group(1))
                # Default to minYear for standalone years
                results["minYear"] = year_value
                logger.info(
                    f"Direct extraction: Found standalone year, assuming minYear={year_value}"
                )
            except ValueError:
                pass

    return results


# Helper function for indifference (can be defined at module level)
_INDIFFERENCE_KEYWORDS_MAP = {
    "make": ["any make", "make doesn't matter", "don't care about make", "no preference on make", "all makes"],
    "type": ["any type", "type doesn't matter", "don't care about type", "no preference on type", "any vehicle type", "all types"],
    "fuel_type": ["any fuel", "fuel type doesn't matter", "don't care about fuel", "no preference on fuel", "all fuels"],
    "price": ["flexible budget", "price not important", "any budget", "budget flexible", "no budget"],
    "year": ["any year", "year doesn't matter", "don't care about year"],
    "mileage": ["any mileage", "mileage doesn't matter", "don't care about mileage"],
    "transmission": ["any transmission", "transmission doesn't matter", "don't care about transmission"]
    # Add more specific parameter keys if needed, e.g., "minPrice", "maxPrice"
}

def _detect_indifference_and_update_clarification_list(query_fragment: str, clarification_needed_for: List[str]) -> List[str]:
    """
    Detects user indifference from query_fragment and removes corresponding items from clarification_needed_for.
    Returns the updated clarification_needed_for list.
    """
    if not query_fragment or not clarification_needed_for:
        return clarification_needed_for

    query_lower = query_fragment.lower()
    indifferent_params_detected = set()

    for param_key_in_map, keywords in _INDIFFERENCE_KEYWORDS_MAP.items():
        for keyword in keywords:
            if keyword in query_lower:
                indifferent_params_detected.add(param_key_in_map)
                logger.info(f"Detected indifference for '{param_key_in_map}' due to keyword: '{keyword}' in query: '{query_fragment}'")
                break
    
    if not indifferent_params_detected:
        return clarification_needed_for

    updated_needed_for = []
    for item_needed in clarification_needed_for:
        # Map general clarification items to specific indifference keys
        is_covered = False
        if item_needed in indifferent_params_detected:
            is_covered = True
        elif item_needed == "budget" and "price" in indifferent_params_detected:
            is_covered = True
        elif item_needed == "make_or_type": # A common placeholder we might use
            if "make" in indifferent_params_detected or "type" in indifferent_params_detected:
                is_covered = True
        # Add more specific mappings if 'item_needed' (e.g. 'vehicle_type') differs from map key ('type')
        elif item_needed == "vehicle_type" and "type" in indifferent_params_detected:
            is_covered = True
        
        if not is_covered:
            updated_needed_for.append(item_needed)
        else:
            logger.info(f"'{item_needed}' removed from clarificationNeededFor due to detected indifference for related param(s): {indifferent_params_detected}")
            
    if len(updated_needed_for) < len(clarification_needed_for):
        logger.info(f"Original clarificationNeededFor: {clarification_needed_for}, Updated after indifference: {updated_needed_for}")
    return updated_needed_for


def run_llm_with_history(
    user_query: str,
    conversation_history: List[Dict[str, str]],
    matched_category: Optional[str] = None,
    force_model: Optional[str] = None,
    confirmed_context: Optional[Dict] = None,
    rejected_context: Optional[Dict] = None,
    contains_override: bool = False,
    last_question_asked: Optional[str] = None,
) -> Optional[Dict[str, Any]]:
    """
    Orchestrates parameter extraction using an LLM, incorporating conversation history and context.

    This function:
    1. Builds an enhanced system prompt for the LLM.
    2. Attempts extraction using a sequence of LLM models (e.g., a fast model first,
       then potentially more capable ones if needed, though currently set to try one).
    3. Processes the LLM's raw JSON output using `process_parameters` for validation
       and standardization.
    4. Performs extensive post-processing:
        - Validates logical consistency (e.g., minPrice <= maxPrice).
        - Analyzes the query fragment for positive and negative mentions of makes,
          types, and fuels.
        - Merges LLM-extracted parameters with `confirmed_context` based on the
          determined `intent` and whether the query explicitly mentions a parameter type.
        - Handles hallucinated parameters by checking if the query fragment contains
          keywords related to the extracted scalar parameter.
        - Populates `explicitly_negated_*` lists.
        - Overrides `clarificationNeeded` if sufficient parameters are already present,
          even if the LLM requested clarification.
    5. Returns the final, processed parameters or a fallback "confused" response if
       all attempts fail or yield invalid results.

    Args:
        user_query: The latest user query string.
        conversation_history: A list of previous turns in the conversation.
        matched_category: The vehicle category matched by RAG, if any.
        force_model: An optional string to force the use of a specific model strategy
                     (e.g., "fast", "refine"). Currently, this mainly influences the
                     order of models tried, but the list is fixed to one model.
        confirmed_context: A dictionary of parameters confirmed by the user in
                           previous turns.
        rejected_context: A dictionary of parameters explicitly rejected by the
                          user in previous turns.
        contains_override: A boolean flag indicating if the query contains override
                           keywords that might force an LLM path.
        last_question_asked: The last question asked by the assistant, if any.

    Returns:
        A dictionary containing the final extracted and processed parameters,
        or `None` if a critical error occurred before a fallback could be generated
        (though it aims to always return a dictionary, even if it's a confused state).
    """
    # Define confused fallback prompt
    CONFUSED_FALLBACK_PROMPT = (
        "Sorry, I seem to have gotten a bit confused. Could you please restate your "
        "main vehicle requirements simply? (e.g., 'SUV under 50k, hybrid or petrol, 2020 or newer')"
    )

    FAST_MODEL = "meta-llama/llama-3.1-8b-instruct:free"
    # REFINE_MODEL = "google/gemma-3-27b-it:free"
    # CLARIFY_MODEL = "mistralai/mistral-7b-instruct:free"
    # if force_model == "fast": models_to_try = [FAST_MODEL, REFINE_MODEL, CLARIFY_MODEL]
    # elif force_model == "refine": models_to_try = [REFINE_MODEL, CLARIFY_MODEL, FAST_MODEL]
    # elif force_model == "clarify": models_to_try = [CLARIFY_MODEL, REFINE_MODEL, FAST_MODEL]
    # else: models_to_try = [FAST_MODEL, REFINE_MODEL, CLARIFY_MODEL]
    # logger.info(f"Will try models in sequence: {models_to_try}")
    models_to_try = [FAST_MODEL]

    try:
        system_prompt = build_enhanced_system_prompt(
            user_query,
            conversation_history,
            matched_category,
            VALID_MANUFACTURERS,
            VALID_FUEL_TYPES,
            VALID_VEHICLE_TYPES,
            confirmed_context,
            rejected_context,
            last_question_asked,  # PASS IT THROUGH
        )
    except Exception as e:
        logger.exception(f"Error building system prompt: {e}")
        return create_default_parameters(intent="error")

    # --- Try Models ---
    extracted_params_from_llm_loop = None # Renamed to avoid confusion with final `extracted_params`
    for model in models_to_try:
        logger.info(f"Attempting extraction with model: {model}")
        extracted = None
        try:
            extracted = try_extract_with_model(model, system_prompt, user_query)
        except Exception as e:
            logger.exception(
                f"Error calling try_extract_with_model for model {model}: {e}"
            )
            continue

        if extracted:
            processed = process_parameters(extracted)
            # Task 2 & 3: Insert new validation code block
            min_price = processed.get("minPrice")
            max_price = processed.get("maxPrice")
            min_year = processed.get("minYear")
            max_year = processed.get("maxYear")

            validation_failed = False
            failure_reason = ""

            if (
                min_price is not None
                and max_price is not None
                and isinstance(min_price, (int, float))
                and isinstance(max_price, (int, float))
                and min_price > max_price
            ):
                validation_failed = True
                failure_reason = "minPrice > maxPrice"
            elif (
                min_year is not None
                and max_year is not None
                and isinstance(min_year, (int, float))
                and isinstance(max_year, (int, float))
                and min_year > max_year
            ):
                validation_failed = True
                failure_reason = "minYear > maxYear"
            elif (
                "Ferrari" in processed.get("preferredMakes", [])
                and max_price is not None
                and isinstance(max_price, (int, float))
                and max_price < 20000
            ):
                validation_failed = True
                failure_reason = "Ferrari requested with maxPrice < 20000"

            if validation_failed:
                logger.warning(
                    f"LLM output failed validation: {failure_reason}. LLM output: {processed}"
                )
                fallback_params = create_default_parameters(
                    intent="CONFUSED_FALLBACK",
                    clarification_needed=True,
                    clarification_needed_for=["implausible_result"],
                    retriever_suggestion=CONFUSED_FALLBACK_PROMPT,  # Added for consistency
                )
                return fallback_params

            # Extract query fragment for analysis
            query_fragment = extract_newest_user_fragment(user_query)

            # --- 1. Determine Context ---
            # First, find negated terms in the query
            negated_makes_set = find_negated_terms(
                query_fragment.lower(), VALID_MANUFACTURERS
            )
            negated_types_set = find_negated_terms(
                query_fragment.lower(), VALID_VEHICLE_TYPES
            )
            negated_fuels_set = find_negated_terms(
                query_fragment.lower(), VALID_FUEL_TYPES
            )

            # Then find positive mentions, excluding negated terms
            positive_makes_set = find_positive_terms(
                query_fragment.lower(), VALID_MANUFACTURERS, negated_makes_set
            )
            positive_types_set = find_positive_terms(
                query_fragment.lower(), VALID_VEHICLE_TYPES, negated_types_set
            )
            positive_fuels_set = find_positive_terms(
                query_fragment.lower(), VALID_FUEL_TYPES, negated_fuels_set
            )

            # Determine basic query attributes
            has_any_positives = bool(
                positive_makes_set or positive_types_set or positive_fuels_set
            )
            has_any_negatives = bool(
                negated_makes_set or negated_types_set or negated_fuels_set
            )
            is_simple_negation_query = not has_any_positives and has_any_negatives

            # Get intent (might be overridden later in extract_parameters)
            final_intent = processed.get("intent", "new_query")

            # Log query analysis
            logger.info(
                f"Query analysis: intent={final_intent}, simple_negation={is_simple_negation_query}"
            )
            logger.info(
                f"Positive mentions: makes={positive_makes_set}, types={positive_types_set}, fuels={positive_fuels_set}"
            )
            logger.info(
                f"Negated terms: makes={negated_makes_set}, types={negated_types_set}, fuels={negated_fuels_set}"
            )

            # Override intent for simple negation queries if needed
            if is_simple_negation_query and final_intent != "refine_criteria":
                logger.info(
                    "Setting intent to 'refine_criteria' due to simple negation."
                )
                final_intent = "refine_criteria"
                processed["intent"] = (
                    "refine_criteria"  # Update in processed for consistency
                )

            # --- 1a. Define keyword sets for scalar parameter types ---
            # Keywords related to price parameters
            PRICE_KEYWORDS = {
                "price",
                "budget",
                "cost",
                "euro",
                "dollar",
                "pound",
                "spend",
                "pay",
                "afford",
                "€",
                "$",
                "£",
                "under",
                "over",
                "between",
                "range",
                "cheap",
                "expensive",
                "pricey",
                "costly",
                "money",
                "funds",
                "finances",
                "affordable",
                "grand",
                "k",
            }

            # Keywords related to year parameters
            YEAR_KEYWORDS = {
                "year",
                "older",
                "newer",
                "age",
                "recent",
                "vintage",
                "yr",
                "model year",
                "registration",
                "reg",
                "plate",
                "built",
                "manufactured",
                "make",
                "made",
                "new",
                "old",
                "20",
                "19",
                "'",
                "from",
                "since",
                "before",  # Year indicators like '20xx, '19xx
            }

            # Keywords related to mileage parameters
            MILEAGE_KEYWORDS = {
                "mileage",
                "miles",
                "mile",
                "km",
                "kilometers",
                "kilometre",
                "odometer",
                "clock",
                "driven",
                "used",
                "low",
                "high",
                "distance",
                "travelled",
                "run",
                "usage",
                "wear",
            }

            # Keywords related to transmission parameters
            TRANSMISSION_KEYWORDS = {
                "transmission",
                "automatic",
                "manual",
                "gear",
                "gearbox",
                "auto",
                "stick",
                "cvt",
                "dsg",
                "paddle",
                "shift",
                "clutch",
                "self-shifting",
                "tiptronic",
                "sequential",
            }

            # Keywords related to engine size parameters
            ENGINE_KEYWORDS = {
                "engine",
                "size",
                "liter",
                "litre",
                "l engine",
                "cc",
                "cubic",
                "displacement",
                "capacity",
                "motor",
                "cylinder",
                "cylinders",
                "block",
                "tdi",
                "tsi",
                "tfsi",
                "turbo",
                "small",
                "big",
                "large",
                "displacement",
            }

            # Keywords related to horsepower parameters
            HP_KEYWORDS = {
                "horsepower",
                "hp",
                "bhp",
                "power",
                "ps",
                "kw",
                "performance",
                "fast",
                "strong",
                "quick",
                "powerful",
                "output",
                "torque",
                "acceleration",
                "pulling power",
                "grunt",
            }

            # Create parameter-to-keywords mapping
            KEYWORD_SETS = {
                "minPrice": PRICE_KEYWORDS,
                "maxPrice": PRICE_KEYWORDS,
                "minYear": YEAR_KEYWORDS,
                "maxYear": YEAR_KEYWORDS,
                "maxMileage": MILEAGE_KEYWORDS,
                "transmission": TRANSMISSION_KEYWORDS,
                "minEngineSize": ENGINE_KEYWORDS,
                "maxEngineSize": ENGINE_KEYWORDS,
                "minHorsepower": HP_KEYWORDS,
                "maxHorsepower": HP_KEYWORDS,
            }

            # --- 2. Initialize Final Parameters ---
            final_params = create_default_parameters()
            final_params["intent"] = final_intent

            # --- 3. Refactored Scalar Parameter Merging Logic ---
            # Define scalar parameters and their corresponding context keys
            scalar_params = {
                "minPrice": "confirmedMinPrice",
                "maxPrice": "confirmedMaxPrice",
                "minYear": "confirmedMinYear",
                "maxYear": "confirmedMaxYear",
                "maxMileage": "confirmedMaxMileage",
                "transmission": "confirmedTransmission",
                "minEngineSize": "confirmedMinEngineSize",
                "maxEngineSize": "confirmedMaxEngineSize",
                "minHorsepower": "confirmedMinHorsePower",  # Note the capital P in HorsePower
                "maxHorsepower": "confirmedMaxHorsePower",  # Note the capital P in HorsePower
            }

            # Process each scalar parameter with improved context-awareness
            for param, context_key in scalar_params.items():
                llm_value = processed.get(param)
                context_value = (
                    confirmed_context.get(context_key) if confirmed_context else None
                )
                relevant_keywords = KEYWORD_SETS.get(param, set())

                # Check if the current query mentions this parameter type
                query_mentions_param = any(
                    kw in query_fragment.lower() for kw in relevant_keywords
                )

                # Apply new logic based on query content and LLM extraction
                if llm_value is not None and query_mentions_param:
                    # LLM extracted a value AND query mentions this parameter type - use LLM value
                    final_params[param] = llm_value
                    logger.debug(
                        f"Using explicit {param}={llm_value} from query (keywords present)"
                    )
                elif (
                    final_intent in ["refine_criteria", "clarify", "add_criteria"]
                    and context_value is not None
                ):
                    # Keep context for refinement/clarification if no explicit mention
                    final_params[param] = context_value
                    if query_mentions_param:
                        logger.debug(
                            f"Query mentions {param} keywords but LLM provided no value, "
                            f"keeping context {param}={context_value}"
                        )
                    else:
                        logger.debug(
                            f"Carrying over {param}={context_value} from context (no mention in query)"
                        )
                else:
                    # Default: leave as None for new queries or when no context exists
                    if llm_value is not None and not query_mentions_param:
                        logger.info(
                            f"Ignoring potential LLM hallucination: {param}={llm_value} (no keywords in query)"
                        )

            # --- 4. Merge List Parameters ---
            # Helper function to handle list merging logic consistently
            # (currently unused, merge_list_param_corrected is used)
            def merge_list_param(
                param_name,
                context_key,
                positive_set,
                negated_set,
                is_simple_negation=False,
            ):
                # Start with confirmed values from context if intent suggests we should keep context
                if (
                    final_intent in ["refine_criteria", "clarify", "add_criteria"]
                    and confirmed_context
                ):
                    result_set = set(confirmed_context.get(context_key, []))

                    # For simple negation, we clear everything if there are negations for this param
                    if is_simple_negation and negated_set:
                        result_set = set()
                        logger.info(f"Simple negation: Clearing all {param_name}")
                    else:
                        # Otherwise add positives and remove negatives
                        if positive_set:
                            result_set = result_set.union(positive_set)
                            logger.debug(
                                f"Added positives to {param_name}: {positive_set}"
                            )

                        # Always remove negated terms
                        if negated_set:
                            result_set = result_set.difference(negated_set)
                            logger.debug(
                                f"Removed negatives from {param_name}: {negated_set}"
                            )
                else:
                    # For new queries or other intents, just use what was explicitly mentioned
                    result_set = set(positive_set)
                    logger.debug(
                        f"Using only explicit mentions for {param_name}: {result_set}"
                    )

                # Convert back to list
                return list(result_set)

            # For "new_query" intent, list parameters (Makes, VehicleTypes, FuelTypes, DesiredFeatures)
            # should be based ONLY on positive mentions or direct LLM extraction from the current query,
            # effectively replacing any previous context for these lists.
            # For other intents (refine, clarify, add), merge with context using existing logic.
            if final_intent == "new_query":
                logger.info(
                    "Intent is 'new_query'. Setting list parameters based only on"
                    " current query's positive mentions/LLM extraction."
                )
                final_params["preferredMakes"] = list(positive_makes_set)
                final_params["preferredVehicleTypes"] = list(positive_types_set)
                final_params["preferredFuelTypes"] = list(positive_fuels_set)
                # For new_query, desiredFeatures come only from the current LLM processing
                final_params["desiredFeatures"] = processed.get("desiredFeatures", [])
                logger.info(
                    f"New query: preferredMakes={final_params['preferredMakes']}, "
                    f"preferredVehicleTypes={final_params['preferredVehicleTypes']}, "
                    f"preferredFuelTypes={final_params['preferredFuelTypes']}, "
                    f"desiredFeatures={final_params['desiredFeatures']}"
                )
            else:
                logger.info(
                    f"Intent is '{final_intent}'. Merging list parameters with context."
                )
                # Apply the existing merging logic (using merge_list_param_corrected) for makes, types, fuels
                final_params["preferredMakes"] = merge_list_param_corrected(
                    "preferredMakes",
                    "confirmedMakes",
                    processed.get("preferredMakes", []),
                    positive_makes_set,
                    negated_makes_set,
                    is_simple_negation_query,
                    confirmed_context,
                )

                final_params["preferredVehicleTypes"] = merge_list_param_corrected(
                    "preferredVehicleTypes",
                    "confirmedVehicleTypes",
                    processed.get("preferredVehicleTypes", []),
                    positive_types_set,
                    negated_types_set,
                    is_simple_negation_query,
                    confirmed_context,
                )

                final_params["preferredFuelTypes"] = merge_list_param_corrected(
                    "preferredFuelTypes",
                    "confirmedFuelTypes",
                    processed.get("preferredFuelTypes", []),
                    positive_fuels_set,
                    negated_fuels_set,
                    is_simple_negation_query,
                    confirmed_context,
                )

                # For other intents, merge desiredFeatures with context
                if confirmed_context:
                    context_features = set(
                        confirmed_context.get("confirmedFeatures", [])
                    )
                    new_features = set(processed.get("desiredFeatures", []))
                    final_params["desiredFeatures"] = list(
                        context_features.union(new_features)
                    )
                else:
                    final_params["desiredFeatures"] = processed.get(
                        "desiredFeatures", []
                    )
                logger.info(
                    f"Merged query ({final_intent}): preferredMakes={final_params['preferredMakes']}, "
                    f"preferredVehicleTypes={final_params['preferredVehicleTypes']}, "
                    f"preferredFuelTypes={final_params['preferredFuelTypes']}, "
                    f"desiredFeatures={final_params['desiredFeatures']}"
                )

            # --- 5. Set Negated Lists ---
            final_params["explicitly_negated_makes"] = list(negated_makes_set)
            final_params["explicitly_negated_vehicle_types"] = list(negated_types_set)
            final_params["explicitly_negated_fuel_types"] = list(negated_fuels_set)

            # --- 6. Set Final Intent & Flags ---
            # Copy over other important fields from processed
            for key in [
                "isOffTopic",
                "offTopicResponse",
                "clarificationNeeded", # This is LLM's view
                "clarificationNeededFor", # This is LLM's view
                "retrieverSuggestion",
                "matchedCategory",
            ]:
                final_params[key] = processed.get(key)
            
            logger.info(f"Parameters after LLM processing & initial merge: {final_params}")

            # --- SUFFICIENCY OVERRIDE LOGIC ---
            if final_params.get("clarificationNeeded") is True: # If LLM thinks clarification is needed
                has_vehicle_category = (
                    len(final_params.get("preferredMakes", [])) > 0
                    or len(final_params.get("preferredVehicleTypes", [])) > 0
                )
                has_constraints = (
                    final_params.get("minPrice") is not None
                    or final_params.get("maxPrice") is not None
                    or final_params.get("minYear") is not None
                    or final_params.get("maxYear") is not None
                    or final_params.get("maxMileage") is not None
                    or len(final_params.get("preferredFuelTypes", [])) > 0
                    or final_params.get("transmission") is not None
                )
                sufficient_info = has_vehicle_category and has_constraints

                if sufficient_info:
                    logger.info(
                        "SUFFICIENCY OVERRIDE: Overriding LLM's clarificationNeeded=True because "
                        "sufficient search parameters are already present in final_params."
                    )
                    final_params["clarificationNeeded"] = False
                    final_params["clarificationNeededFor"] = []
                else:
                    # LLM said clarification is needed, AND Python agrees sufficient_info is False.
                    # Now, refine final_params["clarificationNeededFor"].
                    logger.info("Clarification is needed (LLM agreed or Python determined after override attempt). Determining specific parameters for clarificationNeededFor.")
                    
                    llm_suggested_clarification_for = processed.get("clarificationNeededFor")
                    
                    # Start with LLM's suggestion if it's valid and non-empty
                    current_clarification_list = []
                    if llm_suggested_clarification_for and isinstance(llm_suggested_clarification_for, list) and len(llm_suggested_clarification_for) > 0:
                        logger.info(f"Using base clarificationNeededFor from LLM: {llm_suggested_clarification_for}")
                        current_clarification_list.extend(llm_suggested_clarification_for)
                    else:
                        logger.info("LLM did not specify clarificationNeededFor, or it was empty. Python will determine specifics.")
                        # Python determines missing critical items if LLM didn't specify
                        if not (final_params.get("preferredMakes") or final_params.get("preferredVehicleTypes")):
                            current_clarification_list.append("type") # Suggest 'type' as a common starting point
                        
                        if final_params.get("minPrice") is None and final_params.get("maxPrice") is None:
                            current_clarification_list.append("budget")
                        
                        # Add other critical missing params if not already asked by LLM
                        if not final_params.get("preferredFuelTypes") and "fuel_type" not in current_clarification_list and "fuel" not in current_clarification_list:
                             current_clarification_list.append("fuel_type")
                        if final_params.get("minYear") is None and final_params.get("maxYear") is None and "year" not in current_clarification_list:
                            current_clarification_list.append("year")
                        # Add more specific checks as needed, ensuring not to duplicate if LLM already listed them.

                    # Ensure uniqueness and update final_params
                    final_params["clarificationNeededFor"] = list(set(current_clarification_list))
                    logger.info(f"Refined clarificationNeededFor before indifference check: {final_params['clarificationNeededFor']}")

            # --- Indifference Handling ---
            # This should apply whether clarificationNeeded was true from LLM or set by Python
            if final_params.get("clarificationNeededFor"): # Only if there's something to clarify
                final_params["clarificationNeededFor"] = _detect_indifference_and_update_clarification_list(
                    query_fragment, 
                    final_params["clarificationNeededFor"]
                )
                if not final_params["clarificationNeededFor"] and final_params.get("clarificationNeeded") is True:
                    logger.info("clarificationNeededFor became empty after indifference processing. Considering if clarification is still needed.")
                    # If all clarification points were covered by indifference,
                    # and the initial `sufficient_info` check was borderline,
                    # you might re-evaluate or decide clarification is no longer needed.
                    # For now, an empty list means C# won't ask for these specifics.
                    # Potentially, if clarificationNeededFor is now empty, set clarificationNeeded = False
                    # final_params["clarificationNeeded"] = False # Be cautious with this override
                    pass


            if is_valid_extraction(final_params):
                logger.info("Post-processing complete. Parameters are valid.")
                extracted_params_from_llm_loop = final_params
                break 
            # ... (else block for failed validation) ...
        else: # if not extracted (LLM call failed or no JSON)
            logger.warning(
                f"Extraction from model {model} returned None or failed parsing."
            )
            # extracted_params_from_llm_loop remains None or its last valid value

    # --- Final Return ---
    if extracted_params_from_llm_loop: # Use the renamed variable
        logger.info(f"Successful extraction with final parameters: {extracted_params_from_llm_loop}")
        return extracted_params_from_llm_loop
    else:
        # ... (CONFUSED_FALLBACK logic) ...
        # Ensure the variable name here matches what's returned
        confused_fallback_params = create_default_parameters(
            intent="CONFUSED_FALLBACK",
            clarification_needed=True,
            clarification_needed_for=["reset"],
            retriever_suggestion=CONFUSED_FALLBACK_PROMPT,
        )
        return confused_fallback_params


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

    MODERATE_RAG_THRESHOLD = 0.45  # Or desired value
    HIGH_RAG_THRESHOLD = 0.7  # Or desired value
    try:
        start_time = datetime.datetime.now()
        logger.info("Received request: %s", request.json)
        data = request.json or {}

        is_follow_up = data.get("isFollowUpQuery", False)
        logger.info(f"Processing as follow-up query: {is_follow_up}")

        if "query" not in data:
            logger.error("No 'query' provided in request.")
            return jsonify({"error": "No query provided"}), 400

        user_query = data["query"]
        force_model = data.get("forceModel")  # Model strategy from backend
        conversation_history = data.get("conversationHistory", [])
        # Initialize last_question_asked from the request data
        # Ensure the key "lastQuestionAsked" matches what's sent in the request payload.
        # If it's "lastQuestionAskedByAI" (like in your C# backend model), use that key instead.
        last_question_asked = data.get("lastQuestionAsked")

        # Safely retrieve context information
        confirmed_context = data.get("confirmedContext", {})
        rejected_context = data.get("rejectedContext", {})

        # Extract specific rejection lists for easier access
        rejected_makes = rejected_context.get("rejectedMakes", [])
        rejected_types = rejected_context.get("rejectedVehicleTypes", [])
        rejected_fuels = rejected_context.get("rejectedFuelTypes", [])

        # Enhanced logging for context
        if rejected_makes or rejected_types or rejected_fuels:
            logger.info(
                f"Rejected context: makes={rejected_makes}, types={rejected_types}, fuels={rejected_fuels}"
            )

        if confirmed_context and any(confirmed_context.values()):
            logger.info(
                f"Confirmed context present with {len(confirmed_context)} items"
            )

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
        intent_scores = None  # Initialize intent_scores to None
        try:
            query_embedding = get_query_embedding(user_query)
            if query_embedding is not None:
                # Adjusted threshold based on testing
                # NOTE: classify_intent_zero_shot currently only returns the intent string.
                # For the following logic to work as intended, classify_intent_zero_shot
                # would need to be modified to return an intent_scores dictionary as well.
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
                    if (
                        intent_result is None
                    ):  # This means classify_intent_zero_shot returned None
                        classified_intent = "SPECIFIC_SEARCH"  # Safe default
            else:  # query_embedding was None
                logger.error(
                    "Failed to get query embedding, defaulting intent to SPECIFIC_SEARCH."
                )
                classified_intent = "SPECIFIC_SEARCH"  # Default if embedding fails
        except Exception as e:
            logger.error(
                f"Error during embedding or classification: {e}", exc_info=True
            )
            classified_intent = "SPECIFIC_SEARCH"  # Fallback safely

        # This block checks hypothetical scores. For this to be effective,
        # classify_intent_zero_shot would need to be modified to return 'intent_scores'.
        if (
            intent_scores
            and isinstance(intent_scores, dict)
            and intent_scores.get("SPECIFIC_SEARCH", 0.0)
            < VERY_LOW_CONFIDENCE_THRESHOLD
            and intent_scores.get("VAGUE_INQUIRY", 0.0) < VERY_LOW_CONFIDENCE_THRESHOLD
        ):
            logger.warning(
                f"Both SPECIFIC_SEARCH ({intent_scores.get('SPECIFIC_SEARCH', 0.0):.2f}) and "
                f"VAGUE_INQUIRY ({intent_scores.get('VAGUE_INQUIRY', 0.0):.2f}) scores "
                f"are below VERY_LOW_CONFIDENCE_THRESHOLD ({VERY_LOW_CONFIDENCE_THRESHOLD}). "
                f"Forcing intent to CONFUSED_FALLBACK."
            )
            classified_intent = "CONFUSED_FALLBACK"

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
        words_in_query = set(re.findall(r"\b(\w+)\b", lower_query_fragment))
        specific_keywords_found = words_in_query.intersection(valid_keywords_lower)

        # If query contains specific keywords and was classified as vague, change to specific
        if (
            specific_keywords_found
            and classified_intent == "VAGUE_INQUIRY"
            and not force_llm
        ):
            logger.info(
                f"Specific keywords found in vague query: {specific_keywords_found}. Forcing SPECIFIC_SEARCH/LLM path."
            )
            classified_intent = "SPECIFIC_SEARCH"

        # 4) Initialize routing condition flags
        is_clarification_answer = False
        contains_override = False
        mentions_rejected = False

        # 5) Enhanced check for override keywords
        # ... (rest of the code remains unchanged)

        # --- Execute based on routing decision ---
        final_response = None

        # Priority routing for contextual follow-ups
        if last_question_asked and force_model in [
            "clarify",
            "refine",
        ]:  # Now last_question_asked is defined
            logger.info(
                "Contextual follow-up detected, prioritizing LLM parameter extraction with enhanced prompt."
            )

            # Direct call to LLM with enhanced context
            extracted_params = run_llm_with_history(
                user_query,
                conversation_history,
                None,  # matched_category
                force_model,
                confirmed_context=confirmed_context,
                rejected_context=rejected_context,
                contains_override=contains_override,
                last_question_asked=last_question_asked,  # Use the initialized variable
            )

            if extracted_params and extracted_params.get("intent") != "error":
                # Post-processing for contextual follow-ups
                logger.info(
                    "Contextual LLM extraction successful, performing post-processing."
                )

                # Prevent clarification loops
                if extracted_params.get("clarificationNeeded"):
                    logger.info(
                        "LOOP PREVENTION: Overriding LLM's clarificationNeeded=True because this is a "
                        "contextual follow-up answer"
                    )
                    extracted_params["clarificationNeeded"] = False
                    extracted_params["clarificationNeededFor"] = []

                # Override intent to 'clarify' for contextual answers
                if extracted_params.get("intent") != "clarify":
                    logger.info(
                        "Overriding LLM intent to 'clarify' based on contextual follow-up detection"
                    )
                    extracted_params["intent"] = "clarify"

                # Ensure all fields exist using create_default_parameters as base
                base = create_default_parameters()
                base.update(extracted_params)
                final_response = base
                logger.info(
                    "Final extracted parameters from contextual LLM: %s", final_response
                )
            else:
                # Contextual LLM call failed - fallback logic
                logger.warning(
                    "Contextual LLM extraction failed or returned error. Falling back to standard routing."
                )

                if classified_intent == "VAGUE_INQUIRY":
                    logger.info(
                        "Falling back to RAG processing for failed contextual follow-up."
                    )
                    final_response = None  # Will trigger RAG logic below
                else:
                    final_response = create_default_parameters(
                        intent="CONFUSED_FALLBACK",
                        clarification_needed=True,
                        clarification_needed_for=["reset"],
                        retriever_suggestion=CONFUSED_FALLBACK_PROMPT,
                    )

        # Existing routing logic (now as elif)
        elif force_llm or classified_intent == "SPECIFIC_SEARCH":
            if not force_llm:  # Log if it was originally specific
                logger.info("Intent classified as SPECIFIC_SEARCH, proceeding to LLM.")
            else:  # Log details if forced
                logger.info(
                    f"Routing conditions met (clarify={is_clarification_answer}, "
                    f"override={contains_override}, mentions_rejected={mentions_rejected}), proceeding to LLM."
                )

            extracted_params = run_llm_with_history(
                user_query,
                conversation_history,
                None,  # matched_category
                force_model,
                confirmed_context=confirmed_context,
                rejected_context=rejected_context,
                contains_override=contains_override,
                last_question_asked=last_question_asked,  # Use the initialized variable consistently
            )

            if extracted_params:
                # NEW CODE: Prevent clarification loops by forcing clarificationNeeded=False
                # if this is already a clarification answer
                if is_clarification_answer:
                    if extracted_params.get("clarificationNeeded"):
                        logger.info(
                            "LOOP PREVENTION: Overriding LLM's clarificationNeeded=True because this is already a "
                            "clarification answer"
                        )
                        extracted_params["clarificationNeeded"] = False
                        extracted_params["clarificationNeededFor"] = []

                # Override intent to 'clarify' for contextual answers
                if extracted_params.get("intent") != "clarify":
                    logger.info(
                        "Overriding LLM intent to 'clarify' based on context detection"
                    )
                    extracted_params["intent"] = "clarify"

                # Ensure all fields exist using create_default_parameters as base
                base = create_default_parameters()
                base.update(extracted_params)  # Overwrite defaults with LLM output
                final_response = base
                logger.info("Final extracted parameters from LLM: %s", final_response)
            else:
                logger.error("LLM models failed or no valid extraction.")
                final_response = create_default_parameters(
                    intent="error"
                )  # Indicate error

        elif classified_intent == "VAGUE_INQUIRY":
            logger.info(
                "Intent is VAGUE_INQUIRY and no override/clarification forced LLM, proceeding with RAG."
            )
            try:
                match_cat, score = find_best_match(query_fragment)
                logger.info(f"RAG result: Category='{match_cat}', Score={score:.2f}")

                # Define RAG confidence thresholds
                LOW_CONFIDENCE_THRESHOLD = 0.4  # Very low confidence
                MODERATE_RAG_THRESHOLD = 0.45  # Moderate confidence
                HIGH_RAG_THRESHOLD = 0.7  # High confidence

                # Check if this is a follow-up query
                is_follow_up = data.get("isFollowUpQuery", False)

                # Try direct extraction from query first (highest priority)
                extracted_params_direct = try_direct_extract_from_query(query_fragment)

                if extracted_params_direct:
                    # Direct extraction succeeded - use these parameters
                    logger.info(
                        "Direct parameter extraction from query text successful."
                    )
                    final_response = create_default_parameters(
                        intent="refine_criteria",
                        clarification_needed=False,
                        matched_category=(
                            match_cat if score >= MODERATE_RAG_THRESHOLD else None
                        ),  # Include matched category if score is reasonable
                    )

                    # Update with extracted parameters
                    for param, value in extracted_params_direct.items():
                        final_response[param] = value

                    # Merge with confirmed context
                    if confirmed_context:
                        logger.info(
                            "Merging direct extracted parameters with confirmed context"
                        )

                        # Handle scalar parameters - only copy those not extracted directly
                        scalar_params = [
                            ("minPrice", "confirmedMinPrice"),
                            ("maxPrice", "confirmedMaxPrice"),
                            ("minYear", "confirmedMinYear"),
                            ("maxYear", "confirmedMaxYear"),
                            ("maxMileage", "confirmedMaxMileage"),
                            ("transmission", "confirmedTransmission"),
                            ("minEngineSize", "confirmedMinEngineSize"),
                            ("maxEngineSize", "confirmedMaxEngineSize"),
                            ("minHorsepower", "confirmedMinHorsePower"),
                            ("maxHorsepower", "confirmedMaxHorsePower"),
                        ]

                        for param, context_key in scalar_params:
                            if (
                                param not in extracted_params_direct
                                and confirmed_context.get(context_key) is not None
                            ):
                                final_response[param] = confirmed_context[context_key]

                        # Handle list parameters - use context for those not directly extracted
                        list_params = [
                            ("preferredMakes", "confirmedMakes"),
                            ("preferredFuelTypes", "confirmedFuelTypes"),
                            ("preferredVehicleTypes", "confirmedVehicleTypes"),
                        ]

                        for param, context_key in list_params:
                            if (
                                param not in extracted_params_direct
                                and confirmed_context.get(context_key)
                            ):
                                final_response[param] = confirmed_context[context_key]
                else:
                    # Direct extraction failed - fallback to RAG-based approaches
                    logger.info(
                        "Direct extraction failed. Proceeding with RAG-based approaches."
                    )

                    # High confidence match - provide category-specific clarification
                    if score >= HIGH_RAG_THRESHOLD:
                        logger.info(
                            f"High confidence RAG match ({score:.2f}) for '{match_cat}'"
                        )
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

                    # Follow-up query with moderate confidence - try parameter extraction from category
                    elif is_follow_up and score >= MODERATE_RAG_THRESHOLD:
                        logger.info(
                            f"Follow-up query with moderate RAG confidence ({score:.2f}). "
                            f"Attempting parameter extraction."
                        )
                        param_name, param_value = try_extract_param_from_rag_category(
                            match_cat
                        )

                        if param_name and param_value is not None:
                            # Successfully extracted a parameter from category
                            final_response = create_default_parameters(
                                intent="refine_criteria",
                                clarification_needed=False,
                                matched_category=match_cat,
                            )
                            # Set the extracted parameter
                            final_response[param_name] = param_value
                            logger.info(
                                f"Category parameter extraction successful: {param_name}={param_value}"
                            )

                            # Merge with confirmed context
                            if confirmed_context:
                                logger.info(
                                    "Merging extracted parameters with confirmed context"
                                )

                                # Handle scalar parameters (excluding the one we just extracted)
                                scalar_params = [
                                    ("minPrice", "confirmedMinPrice"),
                                    ("maxPrice", "confirmedMaxPrice"),
                                    ("minYear", "confirmedMinYear"),
                                    ("maxYear", "confirmedMaxYear"),
                                    ("maxMileage", "confirmedMaxMileage"),
                                    ("transmission", "confirmedTransmission"),
                                    ("minEngineSize", "confirmedMinEngineSize"),
                                    ("maxEngineSize", "confirmedMaxEngineSize"),
                                    ("minHorsepower", "confirmedMinHorsePower"),
                                    ("maxHorsepower", "confirmedMaxHorsePower"),
                                ]

                                for p, context_key in scalar_params:
                                    if (
                                        p != param_name
                                        and confirmed_context.get(context_key)
                                        is not None
                                    ):
                                        final_response[p] = confirmed_context[
                                            context_key
                                        ]

                                # Handle list parameters (excluding the one we just extracted)
                                list_params = [
                                    ("preferredMakes", "confirmedMakes"),
                                    ("preferredFuelTypes", "confirmedFuelTypes"),
                                    ("preferredVehicleTypes", "confirmedVehicleTypes"),
                                ]

                                for p, context_key in list_params:
                                    if p != param_name and confirmed_context.get(
                                        context_key
                                    ):
                                        final_response[p] = confirmed_context[
                                            context_key
                                        ]
                        else:
                            # Category extraction failed - request clarification
                            logger.info(
                                "Category extraction failed. Requesting clarification."
                            )
                            final_response = create_default_parameters(
                                intent="clarify",
                                clarification_needed=True,
                                clarification_needed_for=["details"],
                                matched_category=match_cat,
                                retriever_suggestion=(
                                    f"I understand you're interested in {match_cat}. "
                                    f"Could you provide more specifics about what you're looking for?"
                                ),
                            )

                    # Very low confidence - use confused fallback
                    elif score < LOW_CONFIDENCE_THRESHOLD:
                        logger.warning(
                            f"RAG score ({score:.2f}) is below confidence threshold ({LOW_CONFIDENCE_THRESHOLD}). "
                            f"Triggering CONFUSED_FALLBACK."
                        )
                        final_response = create_default_parameters(
                            intent="CONFUSED_FALLBACK",
                            clarification_needed=True,
                            clarification_needed_for=["reset"],
                            retriever_suggestion=CONFUSED_FALLBACK_PROMPT,
                        )

                    # Low-moderate confidence or not a follow-up - general clarification
                    else:
                        logger.info(
                            f"Low-moderate RAG score ({score:.2f}) or not a follow-up. "
                            f"Requesting general clarification."
                        )
                        final_response = create_default_parameters(
                            intent="clarify",
                            clarification_needed=True,
                            clarification_needed_for=["details"],
                            retriever_suggestion=(
                                "Could you provide more specific details about the "
                                "type of vehicle you need?"
                            ),
                        )

            except Exception as e:
                logger.error(f"Error during RAG processing: {e}", exc_info=True)
                logger.warning("RAG failed, falling back to generic clarification.")
                final_response = create_default_parameters(
                    intent="clarify",
                    clarification_needed=True,
                    clarification_needed_for=["details"],
                    retriever_suggestion="Could you tell me more about what you're looking for in a vehicle?",
                )
        else:
            logger.warning(
                f"Unhandled classified_intent: {classified_intent}. Defaulting to error."
            )
            final_response = create_default_parameters(intent="error")

        # Ensure final_response is always set
        if final_response is None:
            logger.error(
                "Reached end of processing without setting final_response. Defaulting to error."
            )
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
