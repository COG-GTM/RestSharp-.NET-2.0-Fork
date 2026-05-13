"""OAuth 2 authenticators ported from ``OAuth2Authenticator.cs``."""

from __future__ import annotations

from ..enums import ParameterType


class OAuth2Authenticator:
    """Base class for OAuth 2 authenticators."""

    def __init__(self, access_token: str) -> None:
        self._access_token = access_token

    @property
    def access_token(self) -> str:
        """The configured access token."""
        return self._access_token

    def authenticate(self, client, request) -> None:  # pragma: no cover - abstract
        raise NotImplementedError


class OAuth2UriQueryParameterAuthenticator(OAuth2Authenticator):
    """OAuth 2 authenticator that supplies ``oauth_token`` as a query parameter."""

    def authenticate(self, client, request) -> None:
        request.add_parameter(
            "oauth_token", self.access_token, ParameterType.GET_OR_POST
        )


class OAuth2AuthorizationRequestHeaderAuthenticator(OAuth2Authenticator):
    """OAuth 2 authenticator that supplies the token as an ``Authorization`` header."""

    def __init__(self, access_token: str, token_type: str = "OAuth") -> None:
        super().__init__(access_token)
        self._authorization_value = f"{token_type} {access_token}"

    def authenticate(self, client, request) -> None:
        for parameter in request.parameters:
            if (
                parameter.type == ParameterType.HTTP_HEADER
                and parameter.name.lower() == "authorization"
            ):
                return
        request.add_parameter(
            "Authorization", self._authorization_value, ParameterType.HTTP_HEADER
        )
