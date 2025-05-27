"""
Module for offline vector matching of vague user queries to predefined categories.

This module provides functionality to load a set of categories, generate or load
their corresponding text embeddings, and then find the best matching category for
a given user query using cosine similarity. It is designed for scenarios where
a fast, offline lookup is needed to map natural language queries to a known
set of items.

Notes:
    - The retriever uses global, in-memory caches for the embedding model,
      categories, and their vector embeddings to optimize performance for
      repeated lookups.
    - Initialization (`initialize_retriever`) is idempotent and handles the
      lazy loading of the model and data, including generating embeddings
      if they are not found locally.
    - Error handling primarily relies on logging, allowing the application
      to continue operating in a degraded state if some components fail to load.

Dependencies:
    - `json`: Used for loading category data from a JSON file.
    - `logging`: Used for application-level logging.
    - `os`: Used for operating system-dependent functionalities like path manipulation.
    - `typing.Optional`: Used for type hinting optional return values.
    - `numpy`: Used for numerical operations, particularly for handling vector
      embeddings and calculating cosine similarity.
    - `.embed_model_loader.load_embedding_model`: Internal module used to load
      the sentence embedding model (e.g., from Sentence Transformers).
"""

# retriever.py
# This does offline vector matching for vague queries.

import json
import logging
import os
from typing import Optional

import numpy as np

from .embed_model_loader import load_embedding_model

logger = logging.getLogger(__name__)

CATEGORIES_PATH = os.path.join(os.path.dirname(__file__), "categories.json")
EMBEDDINGS_PATH = os.path.join(os.path.dirname(__file__), "embeddings.npy")

# We'll load categories from JSON and keep them + embeddings in memory
_categories = []
_vectors = None
_model = None


def initialize_retriever():
    """
    Initializes the retriever by loading the embedding model, categories, and embeddings.

    This function performs the necessary setup for the retriever to function:
    1. Loads the sentence embedding model.
    2. Loads categories from a predefined JSON file.
    3. Loads pre-computed embeddings for these categories from a .npy file.
       If the embeddings file doesn't exist but categories and the model are
       available, it generates the embeddings and saves them.

    The model, categories, and embeddings are stored in global variables
    `_model`, `_categories`, and `_vectors` respectively for efficient access.
    This function is designed to be called before any retrieval operations.
    It logs errors encountered during initialization but does not raise exceptions
    to the caller, allowing the application to potentially continue if, for example,
    only some components fail to load.
    """
    global _categories, _vectors, _model
    if _model is None:  # Ensure model is loaded if not already
        _model = load_embedding_model()
        logger.info("Embedding model loaded for retriever.")

    if not _categories:
        try:
            with open(CATEGORIES_PATH, "r", encoding="utf-8") as f:
                _categories = json.load(f)
            logger.info("Categories loaded.")
        except Exception as e:
            logger.error(f"Failed to load categories from {CATEGORIES_PATH}: {e}")
            _categories = []  # Ensure it's an empty list on failure

    if _vectors is None:
        if not os.path.exists(EMBEDDINGS_PATH) and _categories and _model:
            try:
                logger.info(
                    f"Generating embeddings for categories and saving to {EMBEDDINGS_PATH}..."
                )
                cat_embs = _model.encode(_categories, convert_to_numpy=True)
                np.save(EMBEDDINGS_PATH, cat_embs)
                _vectors = cat_embs
                logger.info("Embeddings generated and saved.")
            except Exception as e:
                logger.error(f"Failed to generate or save embeddings: {e}")
        elif os.path.exists(EMBEDDINGS_PATH):
            try:
                _vectors = np.load(EMBEDDINGS_PATH)
                logger.info("Embeddings loaded from file.")
            except Exception as e:
                logger.error(f"Failed to load embeddings from {EMBEDDINGS_PATH}: {e}")
        else:
            logger.warning(
                "Embeddings file not found and cannot generate (missing model or categories)."
            )


def cosine_sim(a, b):
    """
    Calculates the cosine similarity between two vectors.

    Args:
        a (np.ndarray or array-like): The first vector.
        b (np.ndarray or array-like): The second vector.

    Returns:
        float: The cosine similarity score between `a` and `b`.
               Returns 0.0 if either vector has a zero norm to prevent
               division by zero errors.

    Example:
        >>> import numpy as np
        >>> vec1 = np.array([1, 2, 3])
        >>> vec2 = np.array([4, 5, 6])
        >>> round(cosine_sim(vec1, vec2), 5)
        0.97463
        >>> cosine_sim(np.array([0,0,0]), vec2)
        0.0
    """
    # Ensure inputs are numpy arrays
    a = np.asarray(a)
    b = np.asarray(b)
    # Handle potential zero vectors
    norm_a = np.linalg.norm(a)
    norm_b = np.linalg.norm(b)
    if norm_a == 0 or norm_b == 0:
        return 0.0
    return np.dot(a, b) / (norm_a * norm_b)


def get_query_embedding(text: str) -> Optional[np.ndarray]:
    """
    Encodes the input text using the loaded embedding model.

    This function first checks if the embedding model (`_model`) is loaded.
    If not, it attempts to initialize the retriever. If the model is still
    unavailable after initialization, or if an error occurs during encoding,
    it logs the error and returns None.

    Args:
        text (str): The text string to encode.

    Returns:
        Optional[np.ndarray]: A NumPy array representing the embedding of the input text.
                              Returns `None` if the embedding model is not loaded or
                              if an error occurs during the encoding process.
    """
    try:
        if _model is None:
            logger.warning("Embedding model not loaded. Initializing retriever...")
            initialize_retriever()  # Attempt to initialize if not already done
            if _model is None:  # Check again after attempting initialization
                logger.error("Failed to load embedding model for get_query_embedding.")
                return None
        # Encode the text
        embedding = _model.encode(text, convert_to_numpy=True)
        return embedding
    except Exception as e:
        logger.error(f"Error getting query embedding for text '{text[:50]}...': {e}")
        return None


def find_best_match(user_query: str) -> (Optional[str], float):
    """
    Finds the best category match for a user query and its similarity score.

    This function attempts to initialize the retriever if its components
    (_model, _vectors, _categories) are not already loaded. It then generates
    an embedding for the `user_query`. This query embedding is compared against
    all pre-computed category embeddings using cosine similarity. The category
    with the highest similarity score is returned.

    Args:
        user_query (str): The user's query string.

    Returns:
        tuple[Optional[str], float]: A tuple containing:
            - The best matching category name (str), or `None` if no match
              could be determined (e.g., due to initialization failures,
              inability to embed the query, or no categories loaded).
            - The similarity score (float) of the best match. This score is
              0.0 if no match is found or an error occurs.

    Raises:
        Logs errors via `logger` if critical components are missing or if any
        step in the matching process fails, but does not raise exceptions to
        the caller.

    Example:
        >>> # This example assumes the retriever has been initialized with categories
        >>> # like "sports car" and "family suv" and their embeddings.
        >>> # The actual output will depend on the specific model and data.
        >>> # initialize_retriever() # (Ensure this is called appropriately in a real scenario)
        >>> # For demonstration, let's mock the necessary globals if not initialized:
        >>> # _model = True; _vectors = np.array([[0.1, 0.2], [0.8, 0.9]]); _categories = ["cat1", "cat2"]
        >>> # def get_query_embedding(query): return np.array([0.7, 0.8])
        >>> # find_best_match("looking for a spacious vehicle")
        ("family suv", 0.92) # Illustrative output
    """
    try:
        if _model is None or _vectors is None or not _categories:
            logger.warning(
                "Retriever not fully initialized. Attempting initialization."
            )
            initialize_retriever()
            if _model is None or _vectors is None or not _categories:
                logger.error("Cannot find best match: Retriever components missing.")
                return None, 0.0

        query_embedding = get_query_embedding(user_query)
        if query_embedding is None:
            logger.error("Failed to get embedding for query in find_best_match.")
            return None, 0.0

        similarities = [cosine_sim(query_embedding, vec) for vec in _vectors]
        if not similarities:
            logger.error(
                "No similarities computed, _vectors might be empty or invalid."
            )
            return None, 0.0

        best_match_idx = np.argmax(similarities)
        score = similarities[best_match_idx]

        # Ensure score is a standard float, not numpy float
        score = float(score)

        return _categories[best_match_idx], score

    except Exception as e:
        logger.error(f"Error finding best match for query '{user_query[:50]}...': {e}")
        return None, 0.0
