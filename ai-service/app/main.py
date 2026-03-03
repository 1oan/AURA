import asyncio
from contextlib import asynccontextmanager

import asyncpg
from fastapi import FastAPI
from pgvector.asyncpg import register_vector
from sentence_transformers import SentenceTransformer

from app.config import settings
from app.routers import health


@asynccontextmanager
async def lifespan(app: FastAPI):
    loop = asyncio.get_running_loop()
    app.state.model = await loop.run_in_executor(
        None, SentenceTransformer, settings.model_name
    )

    app.state.db_pool = await asyncpg.create_pool(
        dsn=settings.database_url,
        init=register_vector,
        min_size=2,
        max_size=10,
    )

    yield

    await app.state.db_pool.close()


app = FastAPI(
    title="AURA AI Service",
    description="Roommate compatibility scoring via text embeddings",
    lifespan=lifespan,
)

app.include_router(health.router)