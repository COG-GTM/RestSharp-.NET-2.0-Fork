"""Simple key/value authenticator ported from ``SimpleAuthenticator.cs``."""

from __future__ import annotations


class SimpleAuthenticator:
    """Adds two GetOrPost parameters (username and password) to every request."""

    def __init__(
        self,
        username_key: str,
        username: str,
        password_key: str,
        password: str,
    ) -> None:
        self._username_key = username_key
        self._username = username
        self._password_key = password_key
        self._password = password

    def authenticate(self, client, request) -> None:
        """Append the credentials as ``GET/POST`` parameters."""
        request.add_parameter(self._username_key, self._username)
        request.add_parameter(self._password_key, self._password)
