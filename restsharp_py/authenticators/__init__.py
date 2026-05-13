"""Authenticators ported from ``RestSharp/Authenticators/``."""

from __future__ import annotations

from typing import Protocol, runtime_checkable


@runtime_checkable
class IAuthenticator(Protocol):
    """Protocol mirroring RestSharp's ``IAuthenticator`` interface."""

    def authenticate(self, client, request) -> None:
        """Modify ``request`` so subsequent dispatch is authenticated."""
        ...


from .basic import HttpBasicAuthenticator  # noqa: E402
from .ntlm import NtlmAuthenticator  # noqa: E402
from .oauth1 import OAuth1Authenticator  # noqa: E402
from .oauth2 import (  # noqa: E402
    OAuth2Authenticator,
    OAuth2AuthorizationRequestHeaderAuthenticator,
    OAuth2UriQueryParameterAuthenticator,
)
from .simple import SimpleAuthenticator  # noqa: E402

__all__ = [
    "IAuthenticator",
    "HttpBasicAuthenticator",
    "NtlmAuthenticator",
    "OAuth1Authenticator",
    "OAuth2Authenticator",
    "OAuth2AuthorizationRequestHeaderAuthenticator",
    "OAuth2UriQueryParameterAuthenticator",
    "SimpleAuthenticator",
]
