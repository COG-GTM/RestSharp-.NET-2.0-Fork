"""``RestClient`` ported from ``RestSharp/RestClient*.cs``.

Backed by :class:`requests.Session` for synchronous dispatch and by
:class:`httpx.AsyncClient` for the asynchronous ``execute_async``.
"""

from __future__ import annotations

import asyncio
from typing import Any, Awaitable, Callable, Dict, Iterable, List, Type, TypeVar

import requests

from .deserializers import IDeserializer, JsonDeserializer, XmlDeserializer
from .enums import Method, ParameterType, ResponseStatus
from .extensions import has_value, url_encode
from .request import Parameter, RestRequest
from .response import RestResponse, RestResponseCookie


T = TypeVar("T")

_DEFAULT_HANDLERS: Dict[str, Callable[[], IDeserializer]] = {
    "application/json": JsonDeserializer,
    "text/json": JsonDeserializer,
    "text/x-json": JsonDeserializer,
    "text/javascript": JsonDeserializer,
    "application/xml": XmlDeserializer,
    "text/xml": XmlDeserializer,
    "*": XmlDeserializer,
}


class RestClient:
    """Client to translate :class:`RestRequest` instances into HTTP calls."""

    def __init__(self, base_url: str | None = None) -> None:
        self._base_url: str | None = None
        self.base_url = base_url
        self.authenticator = None
        self.user_agent: str = "RestSharp-Py 0.1"
        self.timeout: int = 0
        self.cookie_container: Dict[str, str] = {}
        self.default_parameters: List[Parameter] = []
        self.follow_redirects: bool = True
        self.max_redirects: int | None = None
        self.proxy: Dict[str, str] | None = None
        self.client_certificates: Any = None
        self._handlers: Dict[str, IDeserializer] = {}
        self._accept_types: List[str] = []
        for content_type, factory in _DEFAULT_HANDLERS.items():
            self.add_handler(content_type, factory())
        self._session: requests.Session = requests.Session()

    # ------------------------------------------------------------------
    # Base URL
    # ------------------------------------------------------------------
    @property
    def base_url(self) -> str | None:
        """Base URL prepended to ``request.resource`` when assembling URLs."""
        return self._base_url

    @base_url.setter
    def base_url(self, value: str | None) -> None:
        if value is None:
            self._base_url = None
            return
        if value.endswith("/"):
            value = value[:-1]
        self._base_url = value

    # ------------------------------------------------------------------
    # Handler / deserializer registration
    # ------------------------------------------------------------------
    def add_handler(self, content_type: str, deserializer: IDeserializer) -> None:
        """Register a deserializer for a MIME ``content_type``."""
        self._handlers[content_type] = deserializer
        if content_type != "*" and content_type not in self._accept_types:
            self._accept_types.append(content_type)

    def remove_handler(self, content_type: str) -> None:
        """Remove a previously registered deserializer."""
        self._handlers.pop(content_type, None)
        if content_type in self._accept_types:
            self._accept_types.remove(content_type)

    def clear_handlers(self) -> None:
        """Remove every registered deserializer."""
        self._handlers.clear()
        self._accept_types.clear()

    def get_handler(self, content_type: str | None) -> IDeserializer | None:
        """Return the deserializer for ``content_type``, falling back to ``*``."""
        if not content_type and "*" in self._handlers:
            return self._handlers["*"]
        if content_type:
            ct = content_type.split(";", 1)[0]
            if ct in self._handlers:
                return self._handlers[ct]
        return self._handlers.get("*")

    # ------------------------------------------------------------------
    # Default parameters
    # ------------------------------------------------------------------
    def add_default_parameter(
        self,
        name_or_parameter: str | Parameter,
        value: Any = None,
        parameter_type: ParameterType = ParameterType.GET_OR_POST,
    ) -> None:
        """Add a parameter that should be sent with every request."""
        if isinstance(name_or_parameter, Parameter):
            parameter = name_or_parameter
        else:
            parameter = Parameter(
                name=name_or_parameter, value=value, type=parameter_type
            )
        if parameter.type == ParameterType.REQUEST_BODY:
            raise ValueError(
                "Cannot set request body from default parameters. Use RestRequest.add_body() instead."
            )
        self.default_parameters.append(parameter)

    def add_default_header(self, name: str, value: str) -> None:
        """Shortcut for :py:meth:`add_default_parameter` with ``HTTP_HEADER``."""
        self.add_default_parameter(name, value, ParameterType.HTTP_HEADER)

    def add_default_url_segment(self, name: str, value: str) -> None:
        """Shortcut for :py:meth:`add_default_parameter` with ``URL_SEGMENT``."""
        self.add_default_parameter(name, value, ParameterType.URL_SEGMENT)

    # ------------------------------------------------------------------
    # URL building
    # ------------------------------------------------------------------
    def build_uri(self, request: RestRequest) -> str:
        """Assemble the final URL for ``request``."""
        assembled = request.resource or ""
        for parameter in request.parameters_of(ParameterType.URL_SEGMENT):
            assembled = assembled.replace(
                "{" + parameter.name + "}", url_encode(str(parameter.value))
            )

        if assembled.startswith("/"):
            assembled = assembled[1:]

        if has_value(self._base_url):
            if not assembled:
                assembled = self._base_url
            else:
                assembled = f"{self._base_url}/{assembled}"

        if request.method not in (Method.POST, Method.PUT, Method.PATCH):
            params = list(request.parameters_of(ParameterType.GET_OR_POST))
            if params:
                if assembled.endswith("/"):
                    assembled = assembled[:-1]
                query = self._encode_parameters(params)
                assembled = f"{assembled}?{query}"

        if assembled is None:
            return ""

        # Append trailing slash if the URL is just the host (mirrors .NET Uri normalization)
        if assembled and "//" in assembled:
            scheme_split = assembled.split("//", 1)
            if "/" not in scheme_split[1]:
                assembled += "/"
        return assembled

    @staticmethod
    def _encode_parameters(parameters: Iterable[Parameter]) -> str:
        pairs = []
        for parameter in parameters:
            pairs.append(
                f"{url_encode(parameter.name)}={url_encode(str(parameter.value))}"
            )
        return "&".join(pairs)

    # ------------------------------------------------------------------
    # Synchronous execution
    # ------------------------------------------------------------------
    def execute(self, request: RestRequest) -> RestResponse:
        """Execute ``request`` synchronously and return a :class:`RestResponse`."""
        self._authenticate_if_needed(request)
        if self._accept_types:
            accepts = ", ".join(self._accept_types)
            if not any(
                p.name.lower() == "accept" and p.type == ParameterType.HTTP_HEADER
                for p in self.default_parameters
            ):
                self.add_default_header("Accept", accepts)

        response = RestResponse[Any]()
        try:
            response = self._dispatch(request)
            response.request = request
            request.increase_num_attempts()
        except Exception as exc:  # noqa: BLE001 - matches RestSharp's behavior
            response.response_status = ResponseStatus.ERROR
            response.error_message = str(exc)
            response.error_exception = exc
            response.request = request
            request.increase_num_attempts()
        return response

    def execute_typed(
        self, request: RestRequest, response_type: Type[T]
    ) -> RestResponse[T]:
        """Execute ``request`` and deserialize the body into ``response_type``."""
        raw = self.execute(request)
        return self._deserialize(request, raw, response_type)

    def download_data(self, request: RestRequest) -> bytes:
        """Execute ``request`` and return the raw response bytes."""
        response = self.execute(request)
        return response.raw_bytes

    # ------------------------------------------------------------------
    # Asynchronous execution
    # ------------------------------------------------------------------
    async def execute_async(
        self,
        request: RestRequest,
        callback: Callable[[RestResponse], Awaitable[None] | None] | None = None,
    ) -> RestResponse:
        """Execute ``request`` asynchronously using ``httpx.AsyncClient``."""
        import httpx

        self._authenticate_if_needed(request)
        merged_request = self._merge_default_parameters(request)
        url = self.build_uri(merged_request)
        method = merged_request.method.value
        headers, cookies, data, files = self._build_payload(merged_request)
        timeout = self._effective_timeout(merged_request)

        response = RestResponse[Any]()
        async with httpx.AsyncClient(
            timeout=timeout,
            follow_redirects=self.follow_redirects,
            cookies=cookies or None,
        ) as client:
            try:
                http_response = await client.request(
                    method=method,
                    url=url,
                    headers=headers,
                    content=data["data"] if data and "data" in data else None,
                    data=data["form"] if data and "form" in data else None,
                    files=files or None,
                )
                self._populate_from_httpx(response, http_response, merged_request)
            except httpx.HTTPError as exc:
                response.response_status = ResponseStatus.ERROR
                response.error_message = str(exc)
                response.error_exception = exc
                response.request = merged_request

        merged_request.increase_num_attempts()
        if callback is not None:
            result = callback(response)
            if asyncio.iscoroutine(result):
                await result
        return response

    async def execute_async_typed(
        self,
        request: RestRequest,
        response_type: Type[T],
        callback: Callable[[RestResponse[T]], Awaitable[None] | None] | None = None,
    ) -> RestResponse[T]:
        """Async variant of :py:meth:`execute_typed`."""
        raw = await self.execute_async(request)
        typed = self._deserialize(request, raw, response_type)
        if callback is not None:
            result = callback(typed)
            if asyncio.iscoroutine(result):
                await result
        return typed

    # ------------------------------------------------------------------
    # Internal helpers
    # ------------------------------------------------------------------
    def _authenticate_if_needed(self, request: RestRequest) -> None:
        if self.authenticator is not None:
            self.authenticator.authenticate(self, request)

    def _merge_default_parameters(self, request: RestRequest) -> RestRequest:
        """Apply any default parameters that the request hasn't overridden."""
        for default in self.default_parameters:
            if any(
                p.name == default.name and p.type == default.type
                for p in request.parameters
            ):
                continue
            request.add_parameter(default)
        return request

    def _effective_timeout(self, request: RestRequest) -> float | None:
        timeout = request.timeout or self.timeout or 0
        return timeout / 1000.0 if timeout else None

    def _build_payload(
        self, request: RestRequest
    ) -> tuple[dict[str, str], dict[str, str], dict[str, Any] | None, list[tuple[str, tuple]] | None]:
        headers = {
            p.name: str(p.value)
            for p in request.parameters_of(ParameterType.HTTP_HEADER)
        }
        if has_value(self.user_agent) and "User-Agent" not in headers:
            headers["User-Agent"] = self.user_agent

        cookies = {
            p.name: str(p.value)
            for p in request.parameters_of(ParameterType.COOKIE)
        }
        cookies.update(self.cookie_container)

        body_param = request.first_parameter_of(ParameterType.REQUEST_BODY)
        files: list[tuple[str, tuple]] = []
        for f in request.files:
            payload = f.read()
            files.append(
                (
                    f.name,
                    (f.file_name, payload, f.content_type or "application/octet-stream"),
                )
            )

        data: dict[str, Any] | None = None
        if files:
            form = {
                p.name: str(p.value)
                for p in request.parameters_of(ParameterType.GET_OR_POST)
            }
            data = {"form": form}
        elif body_param is not None:
            headers["Content-Type"] = body_param.name
            data = {"data": body_param.value}
        elif request.method in (Method.POST, Method.PUT, Method.PATCH):
            form_pairs = [
                (p.name, str(p.value))
                for p in request.parameters_of(ParameterType.GET_OR_POST)
            ]
            if form_pairs:
                data = {"form": dict(form_pairs)}
        return headers, cookies, data, files or None

    def _dispatch(self, request: RestRequest) -> RestResponse:
        merged_request = self._merge_default_parameters(request)
        url = self.build_uri(merged_request)
        method = merged_request.method.value
        headers, cookies, data, files = self._build_payload(merged_request)
        timeout = self._effective_timeout(merged_request)

        send_kwargs: Dict[str, Any] = {
            "headers": headers,
            "cookies": cookies,
            "timeout": timeout,
            "allow_redirects": self.follow_redirects,
        }
        if self.proxy:
            send_kwargs["proxies"] = self.proxy
        if files:
            send_kwargs["files"] = files
            if data and "form" in data:
                send_kwargs["data"] = data["form"]
        elif data is not None:
            if "data" in data:
                send_kwargs["data"] = data["data"]
            elif "form" in data:
                send_kwargs["data"] = data["form"]
        if merged_request.credentials is not None:
            send_kwargs["auth"] = merged_request.credentials

        http_response = self._session.request(method, url, **send_kwargs)
        response = RestResponse[Any]()
        self._populate_from_requests(response, http_response, merged_request)
        return response

    def _populate_from_requests(
        self,
        response: RestResponse,
        http_response: "requests.Response",
        request: RestRequest,
    ) -> None:
        response.raw_bytes = http_response.content
        response.content = http_response.text
        response.status_code = http_response.status_code
        response.status_description = http_response.reason
        response.response_uri = http_response.url
        response.content_type = http_response.headers.get("Content-Type")
        response.content_encoding = http_response.headers.get("Content-Encoding")
        content_length = http_response.headers.get("Content-Length")
        if content_length is not None:
            try:
                response.content_length = int(content_length)
            except ValueError:
                response.content_length = None
        response.server = http_response.headers.get("Server")
        response.response_status = ResponseStatus.COMPLETED

        for name, value in http_response.headers.items():
            response.headers.append(
                Parameter(name=name, value=value, type=ParameterType.HTTP_HEADER)
            )

        for cookie in http_response.cookies:
            response.cookies.append(
                RestResponseCookie(
                    name=cookie.name,
                    value=cookie.value,
                    domain=cookie.domain,
                    path=cookie.path,
                    secure=cookie.secure,
                    expires=cookie.expires,
                )
            )

        response.request = request

    def _populate_from_httpx(
        self,
        response: RestResponse,
        http_response,
        request: RestRequest,
    ) -> None:
        response.raw_bytes = http_response.content
        response.content = http_response.text
        response.status_code = http_response.status_code
        response.status_description = http_response.reason_phrase
        response.response_uri = str(http_response.url)
        response.content_type = http_response.headers.get("content-type")
        response.content_encoding = http_response.headers.get("content-encoding")
        content_length = http_response.headers.get("content-length")
        if content_length is not None:
            try:
                response.content_length = int(content_length)
            except ValueError:
                response.content_length = None
        response.server = http_response.headers.get("server")
        response.response_status = ResponseStatus.COMPLETED

        for name, value in http_response.headers.items():
            response.headers.append(
                Parameter(name=name, value=value, type=ParameterType.HTTP_HEADER)
            )
        for cookie in http_response.cookies.jar:
            response.cookies.append(
                RestResponseCookie(
                    name=cookie.name,
                    value=cookie.value,
                    domain=cookie.domain,
                    path=cookie.path,
                    secure=cookie.secure,
                    expires=cookie.expires,
                )
            )
        response.request = request

    def _deserialize(
        self,
        request: RestRequest,
        raw: RestResponse,
        response_type: Type[T],
    ) -> RestResponse[T]:
        request.on_before_deserialization(raw)
        handler = self.get_handler(raw.content_type)
        if handler is None:
            return raw  # type: ignore[return-value]
        handler.root_element = request.root_element
        handler.date_format = request.date_format
        handler.namespace = request.xml_namespace
        try:
            raw.data = handler.deserialize(raw, response_type)
        except Exception as exc:  # noqa: BLE001
            raw.response_status = ResponseStatus.ERROR
            raw.error_message = str(exc)
            raw.error_exception = exc
        return raw  # type: ignore[return-value]
