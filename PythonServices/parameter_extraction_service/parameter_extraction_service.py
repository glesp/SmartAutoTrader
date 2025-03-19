from flask import Flask, request, jsonify
import requests
import json

app = Flask(__name__)

# Ollama API endpoint (local)
OLLAMA_API = "http://localhost:11434/api/generate"

@app.route('/extract_parameters', methods=['POST'])
def extract_parameters():
    try:
        # Log incoming request
        print("Received request: ", request.json)

        # Get the query from the request
        data = request.json
        if not data or 'query' not in data:
            print("ERROR: No 'query' provided in request.")
            return jsonify({"error": "No query provided"}), 400
        
        user_query = data['query']
        print("Processing query:", user_query)

        # Create the system prompt
        system_prompt = f"""
        You are an advanced vehicle search assistant. Your job is to extract structured parameters from a user's natural language query.

🚗 **User Query:** '{user_query}'

💡 **Extract the following fields as JSON:**
- `"MinPrice"`: Extract the lowest price mentioned or set to `null`.
- `"MaxPrice"`: Extract the highest price mentioned or set to `null`.
- `"MinYear"`: Extract the minimum model year mentioned or set to `null`.
- `"MaxYear"`: Extract the maximum model year mentioned or set to `null`.
- `"PreferredMakes"`: Extract **brand names** (e.g., ["Toyota", "BMW"]) or `null` if unspecified.
- `"PreferredVehicleTypes"`: Extract **car types** (e.g., ["SUV", "Sedan", "Truck"]) or `null` if unspecified.
- `"PreferredFuelTypes"`: Extract **fuel types** (e.g., ["Diesel", "Electric", "Hybrid", "Petrol"]) or `null` if unspecified.
- `"DesiredFeatures"`: Extract additional requirements (e.g., ["Low Mileage", "Luxury", "Sunroof", "AWD"]) or `null` if unspecified.
- `"MaxResults"`: Always return `5` (unless the user specifies another number).

**⚠️ IMPORTANT RULES:**
- If the user says "diesel SUV", set `"PreferredVehicleTypes": ["SUV"]` and `"PreferredFuelTypes": ["Diesel"]`.
- If the user says "electric car", set `"PreferredFuelTypes": ["Electric"]` and `"PreferredVehicleTypes": null`.
- If no specific type of vehicle is mentioned, set `"PreferredVehicleTypes": null`.
- If the user mentions a budget like "under 30k", set `"MaxPrice": 30000`.
- If the user says "newer than 2018", set `"MinYear": 2018`.
- If no numbers are mentioned, leave prices and years as `null`.

🚨 **DO NOT INCLUDE EXPLANATIONS!** Just return valid JSON.
        """

        print("Sending request to Ollama...")
        
        # Call Ollama API
        response = requests.post(
            OLLAMA_API,
            json={
                "model": "deepseek-r1:7b",
                "prompt": system_prompt,
                "stream": False
            }
        )

        print("Ollama response status code:", response.status_code)

        if response.status_code != 200:
            print("ERROR: Failed to call Ollama API. Response:", response.text)
            return jsonify({"error": "Failed to call Ollama API"}), 500
        
        # Extract the generated text
        response_data = response.json()
        generated_text = response_data.get('response', '')
        print("Raw Ollama response:", generated_text)

        # Try to parse the JSON from the response
        try:
            if '{' in generated_text and '}' in generated_text:
                json_start = generated_text.find('{')
                json_end = generated_text.rfind('}') + 1
                json_str = generated_text[json_start:json_end].strip()  # Strip spaces
                parameters = json.loads(json_str)
            else:
                parameters = json.loads(generated_text)

            print("Extracted parameters:", parameters)
            return jsonify(parameters)
        
        except json.JSONDecodeError as e:
            print("ERROR: Failed to parse JSON from model response")
            return jsonify({
                "error": "Failed to parse JSON from model response",
                "raw_response": generated_text
            }), 500
    
    except Exception as e:
        print("Exception occurred:", str(e))
        return jsonify({"error": str(e)}), 500

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5006, debug=True)