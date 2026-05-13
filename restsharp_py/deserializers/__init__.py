"""Deserializers used to parse response bodies into Python objects."""

from __future__ import annotations

from typing import Any, Protocol, runtime_checkable


@runtime_checkable
class IDeserializer(Protocol):
    """Protocol mirroring RestSharp's ``IDeserializer`` interface."""

    root_element: str | None
    namespace: str | None
    date_format: str | None

    def deserialize(self, response, target_type: type) -> Any:
        """Convert ``response.content`` into an instance of ``target_type``."""
        ...


from .json_deserializer import JsonDeserializer  # noqa: E402
from .xml_deserializer import XmlDeserializer  # noqa: E402

__all__ = ["IDeserializer", "JsonDeserializer", "XmlDeserializer"]
