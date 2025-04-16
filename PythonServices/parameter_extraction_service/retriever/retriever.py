# retriever.py
# This does offline vector matching for vague queries.

import json
import numpy as np
import os
import logging
from typing import Optional, Tuple
from .embed_model_loader import load_embedding_model

logger = logging.getLogger(__name__)

CATEGORIES_PATH = os.path.join(os.path.dirname(__file__), "categories.json")
EMBEDDINGS_PATH = os.path.join(os.path.dirname(__file__), "embeddings.npy")

# We'll load categories from JSON and keep them + embeddings in memory
_categories = []
_vectors = None
_model = None


def initialize_retriever():
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
    Ensures the retriever (and thus the model) is initialized.
    Returns None if encoding fails.
    """
    global _model
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
    Return the best category match and similarity score.
    Returns (None, 0.0) if matching fails.
    """
    global _model, _vectors, _categories
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
        best_match_idx = np.argmax(similarities)
        score = similarities[best_match_idx]

        # Ensure score is a standard float, not numpy float
        score = float(score)

        return _categories[best_match_idx], score

    except Exception as e:
        logger.error(f"Error finding best match for query '{user_query[:50]}...': {e}")
        return None, 0.0
