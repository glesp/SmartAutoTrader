# app.py
from flask import Flask, request, jsonify
from sentence_transformers import SentenceTransformer
import numpy as np
import requests

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

@app.route('/parameters', methods=['POST'])
def get_parameters():
    data = request.json
    
    if not data or 'inputs' not in data:
        return jsonify({'error': 'No inputs provided'}), 400
    
    text = data['inputs']
    
    # Call the parameter extraction service
    try:
        response = requests.post(
            'http://localhost:5006/extract_parameters',
            json={'query': text},
            timeout=10  # Add timeout to avoid hanging
        )
        
        if response.status_code == 200:
            return response.json()
        else:
            return jsonify({'error': 'Parameter extraction failed', 'details': response.text}), 500
    except Exception as e:
        return jsonify({'error': str(e)}), 500

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5005)