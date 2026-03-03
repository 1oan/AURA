from app.config import settings
from sentence_transformers import SentenceTransformer

SentenceTransformer(settings.model_name)
print("Model downloaded and cached.")