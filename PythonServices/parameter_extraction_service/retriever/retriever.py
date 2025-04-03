# retriever.py
# This does offline vector matching for vague queries.

import json
import numpy as np
import os
from .embed_model_loader import load_embedding_model

CATEGORIES_PATH = os.path.join(os.path.dirname(__file__), "categories.json")
EMBEDDINGS_PATH = os.path.join(os.path.dirname(__file__), "embeddings.npy")

# We'll load categories from JSON and keep them + embeddings in memory
_categories = []
_vectors = None
_model = None

def initialize_retriever():
    global _categories, _vectors, _model
    if not _categories:
        with open(CATEGORIES_PATH, "r", encoding="utf-8") as f:
            _categories = json.load(f)

    if not os.path.exists(EMBEDDINGS_PATH):
        # We'll generate embeddings if they don't exist
        _model = load_embedding_model()
        cat_embs = _model.encode(_categories, convert_to_numpy=True)
        np.save(EMBEDDINGS_PATH, cat_embs)
        _vectors = cat_embs
    else:
        # Just load from disk
        _vectors = np.load(EMBEDDINGS_PATH)
        _model = load_embedding_model()

def cosine_sim(a, b):
    return np.dot(a, b) / (np.linalg.norm(a) * np.linalg.norm(b))

def find_best_match(user_query: str) -> (str, float):
    """
    Return the best category match and similarity score
    """
    global _model, _vectors, _categories

    if not _model or _vectors is None or not _categories:
        initialize_retriever()

    query_vec = _model.encode([user_query], convert_to_numpy=True)[0]
    best_score = -1
    best_cat = None

    for i, cat_vec in enumerate(_vectors):
        score = cosine_sim(query_vec, cat_vec)
        if score > best_score:
            best_score = score
            best_cat = _categories[i]

    return best_cat, best_score
