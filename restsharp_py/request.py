"""``RestRequest`` ported from ``RestSharp/RestRequest.cs``.

A request is a container for parameters, files, and the chosen
HTTP method. Bodies are serialized lazily via ``add_body`` using
the request's configured serializer.
"""

from __future__ import annotations

import os
from dataclasses import dataclass
from typing import Any, BinaryIO, Callable, Iterable, List, Optional

from .enums import DataFormat, Method, ParameterType
from .extensions import get_public_properties
from .serializers import JsonSerializer, XmlSerializer
from .serializers.xml_serializer import XmlSerializer as _XmlSerializer


@dataclass
class Parameter:
    """Container for a single parameter passed with a request."""

    name: str
    value: Any
    type: ParameterType = ParameterType.GET_OR_POST

    def __str__(self) -> str:
        return f"{self.name}={self.value}"


@dataclass
class FileParameter:
    """Container describing a file to be uploaded as part of a request."""

    name: str
    file_name: str
    content_type: str | None = None
    content_length: int | None = None
    data: bytes | None = None
    writer: Callable[[BinaryIO], None] | None = None

    def read(self) -> bytes:
        """Materialize the file's bytes."""
        if self.data is not None:
            return self.data
        if self.writer is not None:
            import io

            buf = io.BytesIO()
            self.writer(buf)
            return buf.getvalue()
        raise ValueError("FileParameter has neither data nor writer")


class RestRequest:
    """Container for data used to make a request."""

    def __init__(
        self,
        resource: str | None = None,
        method: Method = Method.GET,
        request_format: DataFormat = DataFormat.XML,
    ) -> None:
        self.resource: str = resource or ""
        self.method: Method = method
        self.request_format: DataFormat = request_format
        self.parameters: List[Parameter] = []
        self.files: List[FileParameter] = []
        self.json_serializer = JsonSerializer()
        self.xml_serializer: _XmlSerializer = XmlSerializer()
        self.root_element: str | None = None
        self.date_format: str | None = None
        self.xml_namespace: str | None = None
        self.credentials: Any = None
        self.user_state: Any = None
        self.timeout: int = 0
        self.on_before_deserialization: Callable[[Any], None] = lambda r: None
        self._attempts: int = 0

    # ------------------------------------------------------------------
    # Parameter helpers
    # ------------------------------------------------------------------
    def add_parameter(
        self,
        name_or_parameter: str | Parameter,
        value: Any = None,
        parameter_type: ParameterType = ParameterType.GET_OR_POST,
    ) -> "RestRequest":
        """Add a parameter to the request."""
        if isinstance(name_or_parameter, Parameter):
            self.parameters.append(name_or_parameter)
        else:
            self.parameters.append(
                Parameter(name=name_or_parameter, value=value, type=parameter_type)
            )
        return self

    def add_header(self, name: str, value: str) -> "RestRequest":
        """Add an HTTP header parameter."""
        return self.add_parameter(name, value, ParameterType.HTTP_HEADER)

    def add_cookie(self, name: str, value: str) -> "RestRequest":
        """Add a cookie parameter."""
        return self.add_parameter(name, value, ParameterType.COOKIE)

    def add_url_segment(self, name: str, value: str) -> "RestRequest":
        """Add a URL segment substitution parameter."""
        return self.add_parameter(name, value, ParameterType.URL_SEGMENT)

    # ------------------------------------------------------------------
    # Files
    # ------------------------------------------------------------------
    def add_file(
        self,
        name: str,
        path_or_bytes: str | bytes,
        file_name: str | None = None,
        content_type: str | None = None,
    ) -> "RestRequest":
        """Attach a file to the request from a path or raw bytes."""
        if isinstance(path_or_bytes, (bytes, bytearray)):
            data = bytes(path_or_bytes)
            fp = FileParameter(
                name=name,
                file_name=file_name or "",
                content_type=content_type,
                content_length=len(data),
                data=data,
            )
        else:
            path = str(path_or_bytes)
            with open(path, "rb") as fh:
                data = fh.read()
            fp = FileParameter(
                name=name,
                file_name=file_name or os.path.basename(path),
                content_type=content_type,
                content_length=len(data),
                data=data,
            )
        self.files.append(fp)
        return self

    # ------------------------------------------------------------------
    # Bodies
    # ------------------------------------------------------------------
    def add_body(self, obj: Any, xml_namespace: str | None = None) -> "RestRequest":
        """Serialize ``obj`` using the configured serializer and add it as the request body."""
        if self.request_format == DataFormat.JSON:
            serialized = self.json_serializer.serialize(obj)
            content_type = self.json_serializer.content_type
        elif self.request_format == DataFormat.XML:
            if xml_namespace is not None:
                self.xml_serializer.namespace = xml_namespace
            serialized = self.xml_serializer.serialize(obj)
            content_type = self.xml_serializer.content_type
        else:
            serialized = ""
            content_type = ""
        # Per upstream "hack": store the content type in Parameter.name
        return self.add_parameter(content_type, serialized, ParameterType.REQUEST_BODY)

    def add_object(self, obj: Any, *whitelist: str) -> "RestRequest":
        """Add a parameter for each public property on ``obj``."""
        allowed = set(whitelist)
        for name, value in get_public_properties(obj):
            if allowed and name not in allowed:
                continue
            if value is None:
                continue
            if isinstance(value, (list, tuple)):
                value = ",".join(str(v) for v in value)
            self.add_parameter(name, value)
        return self

    # ------------------------------------------------------------------
    # Bookkeeping
    # ------------------------------------------------------------------
    def increase_num_attempts(self) -> None:
        """Increment the number of attempts made."""
        self._attempts += 1

    @property
    def attempts(self) -> int:
        """Number of attempts made to send this request."""
        return self._attempts

    def parameters_of(self, parameter_type: ParameterType) -> Iterable[Parameter]:
        """Return an iterable of parameters of the given type."""
        return (p for p in self.parameters if p.type == parameter_type)

    def first_parameter_of(
        self, parameter_type: ParameterType
    ) -> Optional[Parameter]:
        """Return the first parameter of the given type, if any."""
        return next(self.parameters_of(parameter_type), None)
