"""OAuth 1.0 authenticator ported from ``OAuth1Authenticator.cs``.

Wraps :class:`requests_oauthlib.OAuth1` and exposes factory methods
that mirror the static factory methods on the .NET class.
"""

from __future__ import annotations

from enum import Enum


class OAuthType(Enum):
    """OAuth request flow being modelled."""

    REQUEST_TOKEN = "RequestToken"
    ACCESS_TOKEN = "AccessToken"
    PROTECTED_RESOURCE = "ProtectedResource"
    CLIENT_AUTHENTICATION = "ClientAuthentication"


class OAuth1Authenticator:
    """OAuth 1.0a authenticator backed by ``requests_oauthlib``."""

    def __init__(
        self,
        consumer_key: str,
        consumer_secret: str,
        token: str | None = None,
        token_secret: str | None = None,
        callback_uri: str | None = None,
        verifier: str | None = None,
        session_handle: str | None = None,
        client_username: str | None = None,
        client_password: str | None = None,
        oauth_type: OAuthType = OAuthType.PROTECTED_RESOURCE,
        realm: str | None = None,
    ) -> None:
        self.consumer_key = consumer_key
        self.consumer_secret = consumer_secret
        self.token = token
        self.token_secret = token_secret
        self.callback_uri = callback_uri
        self.verifier = verifier
        self.session_handle = session_handle
        self.client_username = client_username
        self.client_password = client_password
        self.oauth_type = oauth_type
        self.realm = realm

    # ------------------------------------------------------------------
    # Factories mirroring the upstream static helpers
    # ------------------------------------------------------------------
    @classmethod
    def for_request_token(
        cls,
        consumer_key: str,
        consumer_secret: str,
        callback_uri: str | None = None,
    ) -> "OAuth1Authenticator":
        """Create a request-token authenticator."""
        return cls(
            consumer_key=consumer_key,
            consumer_secret=consumer_secret,
            callback_uri=callback_uri,
            oauth_type=OAuthType.REQUEST_TOKEN,
        )

    @classmethod
    def for_access_token(
        cls,
        consumer_key: str,
        consumer_secret: str,
        token: str,
        token_secret: str,
        verifier: str | None = None,
    ) -> "OAuth1Authenticator":
        """Create an access-token authenticator."""
        return cls(
            consumer_key=consumer_key,
            consumer_secret=consumer_secret,
            token=token,
            token_secret=token_secret,
            verifier=verifier,
            oauth_type=OAuthType.ACCESS_TOKEN,
        )

    @classmethod
    def for_protected_resource(
        cls,
        consumer_key: str,
        consumer_secret: str,
        access_token: str,
        access_token_secret: str,
    ) -> "OAuth1Authenticator":
        """Create an authenticator for accessing protected resources."""
        return cls(
            consumer_key=consumer_key,
            consumer_secret=consumer_secret,
            token=access_token,
            token_secret=access_token_secret,
            oauth_type=OAuthType.PROTECTED_RESOURCE,
        )

    @classmethod
    def for_client_authentication(
        cls,
        consumer_key: str,
        consumer_secret: str,
        username: str,
        password: str,
    ) -> "OAuth1Authenticator":
        """Create an authenticator for xAuth client-credential flows."""
        return cls(
            consumer_key=consumer_key,
            consumer_secret=consumer_secret,
            client_username=username,
            client_password=password,
            oauth_type=OAuthType.CLIENT_AUTHENTICATION,
        )

    # ------------------------------------------------------------------
    # IAuthenticator implementation
    # ------------------------------------------------------------------
    def authenticate(self, client, request) -> None:
        """Attach a configured ``requests_oauthlib.OAuth1`` instance."""
        try:
            from requests_oauthlib import OAuth1
        except ImportError as exc:  # pragma: no cover - exercised only when extra missing
            raise ImportError(
                "OAuth1Authenticator requires the `requests_oauthlib` package."
            ) from exc
        request.credentials = OAuth1(
            client_key=self.consumer_key,
            client_secret=self.consumer_secret,
            resource_owner_key=self.token,
            resource_owner_secret=self.token_secret,
            verifier=self.verifier,
            callback_uri=self.callback_uri,
            realm=self.realm,
        )
