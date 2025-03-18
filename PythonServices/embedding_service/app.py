# app.py
from flask import Flask, request, jsonify
from sentence_transformers import SentenceTransformer
import numpy as np

app = Flask(__name__)

# Load the model once at startup - this is where the M1 acceleration helps
print("Loading model...")
model = SentenceTransformer('sentence-transformers/all-MiniLM-L6-v2')
print("Model loaded successfully!")

@app.route('/embeddings', methods=['POST'])
def get_embeddings():
    data = request.json
    
    # Extract text from request
    if not data or 'inputs' not in data:
        return jsonify({'error': 'No inputs provided'}), 400
    
    text = data['inputs']
    
    # Generate embeddings
    embeddings = model.encode([text])
    
    # Convert to list for JSON serialization
    embeddings_list = embeddings[0].tolist()
    
    return jsonify(embeddings_list)

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5005)