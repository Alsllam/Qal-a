from __future__ import annotations

from fastapi import APIRouter
from fastapi.responses import JSONResponse

from ai_service.api.deps import ContainerDep

router = APIRouter(prefix="/health", tags=["health"])


@router.get("/live")
async def live() -> dict[str, str]:
    return {"status": "ok"}


@router.get("/ready")
async def ready(container: ContainerDep) -> JSONResponse:
    checks = await container.check_ready()
    ok = all(v == "ok" for v in checks.values())
    return JSONResponse({"status": "ok" if ok else "fail", "checks": checks}, status_code=200 if ok else 503)
