FROM python:3.12-slim
WORKDIR /app
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt
# CPU-only PyTorch for the LSTM baseline (kept off the default index to avoid
# pulling the multi-GB CUDA build).
RUN pip install --no-cache-dir --index-url https://download.pytorch.org/whl/cpu torch==2.3.1
COPY main.py .
CMD ["uvicorn", "main:app", "--host", "0.0.0.0", "--port", "8000"]
