"""HTTP Basic authenticator ported from ``HttpBasicAuthenticator.cs``."""

from __future__ import annotations

import base64

from ..enums import ParameterType


class HttpBasicAuthenticator:
    """Adds an ``Authorization: Basic ...`` header to outgoing requests."""

    def __init__(self, username: str, password: str) -> None:
        self._username = username
        self._password = password

    def authenticate(self, client, request) -> None:
        """Append the basic-auth header unless one is already present."""
        for parameter in request.parameters:
            if (
                parameter.type == ParameterType.HTTP_HEADER
                and parameter.name.lower() == "authorization"
            ):
                return
        token = base64.b64encode(
            f"{self._username}:{self._password}".encode("utf-8")
        ).decode("ascii")
        request.add_parameter(
            "Authorization", f"Basic {token}", ParameterType.HTTP_HEADER
        )

    @property
    def username(self) -> str:
        """The configured username."""
        return self._username

    @property
    def password(self) -> str:
        """The configured password."""
        return self._password
