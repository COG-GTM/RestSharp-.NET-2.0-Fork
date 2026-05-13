"""Serializers used to write request bodies."""

from __future__ import annotations

from typing import Any, Protocol, runtime_checkable


@runtime_checkable
class ISerializer(Protocol):
    """Protocol mirroring RestSharp's ``ISerializer`` interface."""

    content_type: str
    date_format: str | None
    root_element: str | None
    namespace: str | None

    def serialize(self, obj: Any) -> str:
        """Serialize ``obj`` and return the result as a string."""
        ...


from .json_serializer import JsonSerializer  # noqa: E402
from .xml_serializer import XmlSerializer, SerializeAs  # noqa: E402

__all__ = ["ISerializer", "JsonSerializer", "XmlSerializer", "SerializeAs"]
