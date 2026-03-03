import logging

from fastapi import APIRouter, HTTPException, Request

logger = logging.getLogger(__name__)

router = APIRouter(prefix="/health", tags=["health"])


@router.get("/live")
async def liveness():
    return {"status": "alive"}


@router.get("/ready")
async def readiness(request: Request):
    errors = []

    if not hasattr(request.app.state, "model") or request.app.state.model is None:
        errors.append("model_not_loaded")

    pool = getattr(request.app.state, "db_pool", None)
    if pool is None:
        errors.append("db_unreachable")
    else:
        try:
            async with pool.acquire() as conn:
                await conn.fetchval("SELECT 1")
        except Exception as exc:
            logger.warning("Database health check failed: %s", exc)
            errors.append("db_unreachable")

    if errors:
        raise HTTPException(status_code=503, detail={"errors": errors})

    return {"status": "ready"}