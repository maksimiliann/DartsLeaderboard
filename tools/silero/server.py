"""Local Silero TTS used by the scoreboard to read the final standings."""

import io
import os
import threading
from contextlib import asynccontextmanager

import numpy as np
import torch
import uvicorn
from fastapi import FastAPI, HTTPException
from fastapi.responses import Response
from pydantic import BaseModel, Field
from scipy.io import wavfile

SAMPLE_RATE = 24000
SPEAKERS = {"aidar", "baya", "kseniya", "xenia", "eugene"}
MAX_TEXT = 4000
MODEL_URL = "https://models.silero.ai/models/tts/ru/v5_5_ru.pt"
MODEL_PATH = os.environ.get("SILERO_MODEL_PATH", "").strip()

_lock = threading.Lock()
_model = None


class SpeakRequest(BaseModel):
    text: str = Field(min_length=1, max_length=MAX_TEXT)
    speaker: str = "eugene"


def load_model():
    torch.set_num_threads(int(os.environ.get("SILERO_THREADS", "4")))
    if MODEL_PATH:
        if not os.path.isfile(MODEL_PATH):
            torch.hub.download_url_to_file(MODEL_URL, MODEL_PATH)
        model = torch.package.PackageImporter(MODEL_PATH).load_pickle("tts_models", "model")
    else:
        model, _ = torch.hub.load(
            repo_or_dir="snakers4/silero-models",
            model="silero_tts",
            language="ru",
            speaker="v5_5_ru",
            trust_repo=True,
        )
    model.to(torch.device("cpu"))
    return model


@asynccontextmanager
async def lifespan(_: FastAPI):
    global _model
    _model = load_model()
    yield


app = FastAPI(lifespan=lifespan)


@app.get("/health")
def health() -> dict[str, str]:
    return {"status": "ok" if _model is not None else "loading"}


@app.post("/speak")
def speak(request: SpeakRequest) -> Response:
    if request.speaker not in SPEAKERS:
        raise HTTPException(status_code=400, detail="unknown speaker")
    if _model is None:
        raise HTTPException(status_code=503, detail="model is not loaded")

    with _lock:
        audio = _model.apply_tts(
            text=request.text,
            speaker=request.speaker,
            sample_rate=SAMPLE_RATE,
        )

    pcm = np.clip(audio.detach().cpu().numpy(), -1.0, 1.0)
    pcm = (pcm * 32767).astype(np.int16)
    buffer = io.BytesIO()
    wavfile.write(buffer, SAMPLE_RATE, pcm)
    return Response(content=buffer.getvalue(), media_type="audio/wav")


if __name__ == "__main__":
    uvicorn.run(app, host="127.0.0.1", port=8765)
