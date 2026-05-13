"""NTLM authenticator ported from ``NtlmAuthenticator.cs``.

The .NET implementation simply set ``CredentialCache.DefaultCredentials``
on the request. The Python equivalent attaches an ``HttpNtlmAuth`` instance
(from ``requests_ntlm``) on the request so the client can hand it to
``requests``. ``requests_ntlm`` is an optional dependency: this module
imports it lazily so importing :mod:`restsharp_py.authenticators` does not
require ``requests_ntlm`` to be installed.
"""

from __future__ import annotations


class NtlmAuthenticator:
    """Tries to authenticate with NTLM credentials."""

    def __init__(self, username: str | None = None, password: str | None = None) -> None:
        self._username = username
        self._password = password

    def authenticate(self, client, request) -> None:
        """Attach an ``HttpNtlmAuth`` instance to the request credentials."""
        try:
            from requests_ntlm import HttpNtlmAuth
        except ImportError as exc:  # pragma: no cover - exercised only when extra missing
            raise ImportError(
                "NtlmAuthenticator requires the `requests_ntlm` package."
            ) from exc

        if self._username is None or self._password is None:
            # Fall back to default credentials when supported by the OS.
            try:
                from requests_negotiate_sspi import HttpNegotiateAuth  # type: ignore

                request.credentials = HttpNegotiateAuth()
                return
            except ImportError:  # pragma: no cover - non-Windows hosts
                raise ValueError(
                    "NTLM default credentials require `requests_negotiate_sspi`."
                )
        request.credentials = HttpNtlmAuth(self._username, self._password)
