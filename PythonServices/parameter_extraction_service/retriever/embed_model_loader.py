# embed_model_loader.py
# Loads a CPU-friendly embedding model once at startup

import os
from sentence_transformers import SentenceTransformer

_model = None

def load_embedding_model():
    global _model
    if _model is None:
        # Using a small, CPU-friendly model
        # If you're REALLY tight on memory, consider 'sentence-transformers/all-MiniLM-L6-v2'
        print("Loading local embedding model... (CPU only)")
        _model = SentenceTransformer("all-MiniLM-L6-v2")
    return _model
