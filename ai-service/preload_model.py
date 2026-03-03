import sys

from sentence_transformers import SentenceTransformer

model_name = sys.argv[1] if len(sys.argv) > 1 else "all-MiniLM-L6-v2"
SentenceTransformer(model_name)
print(f"Model '{model_name}' downloaded and cached.")