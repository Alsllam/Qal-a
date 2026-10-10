"""Model roles. Each role maps to a deployment name in configuration (``DEPLOYMENTS__<ROLE>``)."""

from ai_service.settings import Deployments, ModelRole

__all__ = ["Deployments", "ModelRole"]
