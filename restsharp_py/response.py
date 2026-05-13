"""``RestResponse`` ported from ``RestSharp/RestResponse.cs``.

A single typed Python class replaces the .NET pair of
``RestResponse`` and ``RestResponse<T>``. ``data`` is ``None`` when
the response has not been deserialized.
"""

from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any, Generic, List, Optional, TypeVar

from .enums import ParameterType, ResponseStatus
from .request import Parameter


T = TypeVar("T")


@dataclass
class RestResponseCookie:
    """A cookie returned in a response."""

    name: str = ""
    value: str = ""
    comment: str | None = None
    comment_uri: Any = None
    discard: bool = False
    domain: str | None = None
    expired: bool = False
    expires: Any = None
    http_only: bool = False
    path: str | None = None
    port: str | None = None
    secure: bool = False
    time_stamp: Any = None
    version: int = 0


@dataclass
class RestResponse(Generic[T]):
    """Container for data sent back from an API.

    ``RestResponse`` is generic. Use ``RestResponse[MyType]`` for
    documentation purposes; deserialized data is always available
    on the ``data`` attribute.
    """

    request: Any = None
    content: str = ""
    raw_bytes: bytes = b""
    status_code: int = 0
    status_description: str | None = None
    response_uri: str | None = None
    server: str | None = None
    content_type: str | None = None
    content_length: int | None = None
    content_encoding: str | None = None
    headers: List[Parameter] = field(default_factory=list)
    cookies: List[RestResponseCookie] = field(default_factory=list)
    response_status: ResponseStatus = ResponseStatus.NONE
    error_message: str | None = None
    error_exception: BaseException | None = None
    data: Optional[T] = None

    def header(self, name: str) -> str | None:
        """Return the first header value matching ``name`` (case-insensitive)."""
        lowered = name.lower()
        for header in self.headers:
            if header.type == ParameterType.HTTP_HEADER and header.name.lower() == lowered:
                value = header.value
                return value if isinstance(value, str) else str(value)
        return None
