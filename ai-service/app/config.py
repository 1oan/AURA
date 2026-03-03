from pydantic_settings import BaseSettings


class Settings(BaseSettings):
    database_url: str
    model_name: str = "all-MiniLM-L6-v2"
    embedding_dim: int = 384


settings = Settings()