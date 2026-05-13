"""Default JSON serializer.

Mirrors ``RestSharp/Serializers/JsonSerializer.cs``. Backed by the
Python standard library ``json`` module so no extra dependencies are
required.
"""

from __future__ import annotations

import dataclasses
import json
from datetime import date, datetime
from decimal import Decimal
from typing import Any


class _RestSharpJsonEncoder(json.JSONEncoder):
    """JSON encoder that handles common Python objects out of the box."""

    def default(self, o: Any) -> Any:
        if dataclasses.is_dataclass(o) and not isinstance(o, type):
            return dataclasses.asdict(o)
        if isinstance(o, (datetime, date)):
            return o.isoformat()
        if isinstance(o, Decimal):
            return float(o)
        if isinstance(o, bytes):
            return o.decode("utf-8", errors="replace")
        if hasattr(o, "__dict__"):
            return {k: v for k, v in o.__dict__.items() if not k.startswith("_")}
        return super().default(o)


class JsonSerializer:
    """Default JSON serializer for request bodies."""

    def __init__(self) -> None:
        self.content_type: str = "application/json"
        self.date_format: str | None = None
        self.root_element: str | None = None
        self.namespace: str | None = None

    def serialize(self, obj: Any) -> str:
        """Serialize ``obj`` as JSON."""
        return json.dumps(obj, cls=_RestSharpJsonEncoder, indent=2)
