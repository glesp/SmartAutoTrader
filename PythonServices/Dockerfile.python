FROM python:3.10-slim
WORKDIR /app

COPY ./parameter_extraction_service/requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

COPY ./parameter_extraction_service ./parameter_extraction_service
EXPOSE 5006
CMD ["python", "./parameter_extraction_service/parameter_extraction_service.py"]