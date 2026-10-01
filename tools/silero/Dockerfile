FROM python:3.12-slim-bookworm

WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends libgomp1 \
    && rm -rf /var/lib/apt/lists/*

COPY requirements.txt .
RUN pip install --no-cache-dir --upgrade pip \
    && pip install --no-cache-dir torch --index-url https://download.pytorch.org/whl/cpu \
    && pip install --no-cache-dir -r requirements.txt

COPY server.py .
RUN python -c "import torch; torch.hub.download_url_to_file('https://models.silero.ai/models/tts/ru/v5_5_ru.pt', '/app/v5_5_ru.pt')"

RUN useradd --create-home --uid 10001 silero \
    && chown -R silero:silero /app
USER silero

ENV SILERO_MODEL_PATH=/app/v5_5_ru.pt
EXPOSE 8765
CMD ["python", "-m", "uvicorn", "server:app", "--host", "0.0.0.0", "--port", "8765", "--no-access-log"]
