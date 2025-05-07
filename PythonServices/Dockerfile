# Use Python 3.10 slim version
FROM python:3.10-slim

# Set working directory
WORKDIR /app

# --- Dependencies ---
# Copy requirements file first for caching
COPY ./parameter_extraction_service/requirements.txt .

# Install dependencies from requirements.txt
RUN pip install --no-cache-dir -r requirements.txt

# --- Application Code ---
# Copy the application code into the container
COPY ./parameter_extraction_service ./parameter_extraction_service

# --- Runtime Configuration ---
# Make port 5006 available (matches your Python Container App ingress target port)
EXPOSE 5006

# Command to run the application
CMD ["python", "./parameter_extraction_service/parameter_extraction_service.py"]