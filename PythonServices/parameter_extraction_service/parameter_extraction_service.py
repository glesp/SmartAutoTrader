
from flask import Flask, request, jsonify
import requests
import json

app = Flask(__name__)

# Ollama API endpoint (local)
OLLAMA_API = "http://localhost:11434/api/generate"

@app.route('/extract_parameters', methods=['POST'])
def extract_parameters():
    try:
        print("Received request: ", request.json)

        data = request.json
        if not data or 'query' not in data:
            print("ERROR: No 'query' provided in request.")
            return jsonify({"error": "No query provided"}), 400

        user_query = data['query']
        print("Processing query:", user_query)

        system_prompt = f"""
You are an AI assistant for Smart Auto Trader, a vehicle marketplace application.
Your task is to generate vehicle search recommendations based on user input.

IMPORTANT: Your response must only contain valid JSON that conforms to these exact property names and structure:

{{
  "minPrice": null,
  "maxPrice": null,
  "minYear": null,
  "maxYear": null,
  "preferredMakes": [],
  "preferredFuelTypes": [],
  "preferredVehicleTypes": [],
  "desiredFeatures": []
}}

Field Requirements:
- "minPrice" and "maxPrice" must be numbers or null.
- "minYear" and "maxYear" must be integers or null.
- "preferredMakes", "preferredFuelTypes", "preferredVehicleTypes", and "desiredFeatures" must be arrays of strings using only the allowed values.
- All field names must match exactly.

Context Rules:
- If the user mentions "cheap" or "affordable", set "maxPrice" to no more than 15000.
- If the user mentions "expensive", set "minPrice" to at least 30000.
- If the user mentions "new" or "recent", set "minYear" to at least 2020.
- If the user mentions a specific year or range, reflect it in "minYear" or "maxYear".
- If the user mentions "low mileage", optionally include "desiredFeatures": ["Low Mileage"].

Allowed categorical values:
- preferredMakes: ["BMW", "Audi", "Mercedes", "Toyota", "Honda", "Ford", "Volkswagen", "Nissan", "Hyundai", "Kia", "Tesla", "Volvo", "Mazda"]
- preferredFuelTypes: ["Petrol", "Diesel", "Electric", "Hybrid"]
- preferredVehicleTypes: ["Sedan", "SUV", "Hatchback", "Coupe", "Convertible", "Wagon", "Van", "Truck"]

USER QUERY: {user_query}

Return **only valid JSON** matching the specified format and nothing else — no comments, no explanation, no prose.
"""

        print("Sending request to Ollama...")
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
            return jsonify(create_default_parameters()), 200

        response_data = response.json()
        generated_text = response_data.get('response', '')
        print("Raw Ollama response:", generated_text)

        try:
            if '{' in generated_text and '}' in generated_text:
                json_start = generated_text.find('{')
                json_end = generated_text.rfind('}') + 1
                json_str = generated_text[json_start:json_end].strip()
                parameters = json.loads(json_str)
                parameters = validate_and_format_parameters(parameters)
            else:
                print("ERROR: No JSON found in model response")
                parameters = create_default_parameters()

            print("Extracted parameters:", parameters)
            return jsonify(parameters), 200

        except json.JSONDecodeError as e:
            print(f"ERROR: Failed to parse JSON from model response: {e}")
            return jsonify(create_default_parameters()), 200

    except Exception as e:
        print("Exception occurred:", str(e))
        return jsonify(create_default_parameters()), 200


def create_default_parameters():
    return {
        "minPrice": None,
        "maxPrice": None,
        "minYear": None,
        "maxYear": None,
        "preferredMakes": [],
        "preferredFuelTypes": [],
        "preferredVehicleTypes": [],
        "desiredFeatures": []
    }


def validate_and_format_parameters(params):
    valid_params = create_default_parameters()

    for field in ["minPrice", "maxPrice", "minYear", "maxYear"]:
        if field in params and (params[field] is None or isinstance(params[field], (int, float))):
            valid_params[field] = params[field]

    valid_manufacturers = ["BMW", "Audi", "Mercedes", "Toyota", "Honda", "Ford",
                           "Volkswagen", "Nissan", "Hyundai", "Kia", "Tesla", "Volvo", "Mazda"]
    valid_fuel_types = ["Petrol", "Diesel", "Electric", "Hybrid"]
    valid_vehicle_types = ["Sedan", "SUV", "Hatchback", "Coupe", "Convertible", "Wagon", "Van", "Truck"]

    for field_name in ["manufacturers", "preferredMakes"]:
        if field_name in params and isinstance(params[field_name], list):
            valid_params["preferredMakes"] = [m for m in params[field_name] if isinstance(m, str) and m in valid_manufacturers]

    for field_name in ["fuelType", "preferredFuelTypes"]:
        if field_name in params and isinstance(params[field_name], list):
            valid_params["preferredFuelTypes"] = [f for f in params[field_name] if isinstance(f, str) and f in valid_fuel_types]

    for field_name in ["bodyType", "preferredVehicleTypes"]:
        if field_name in params and isinstance(params[field_name], list):
            valid_params["preferredVehicleTypes"] = [b for b in params[field_name] if isinstance(b, str) and b in valid_vehicle_types]

    if "desiredFeatures" in params and isinstance(params["desiredFeatures"], list):
        valid_params["desiredFeatures"] = [f for f in params["desiredFeatures"] if isinstance(f, str)]

    return valid_params


if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5006, debug=True)
