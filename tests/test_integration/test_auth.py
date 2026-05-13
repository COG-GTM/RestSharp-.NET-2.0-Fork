"""Integration tests for authenticators adapted from
``RestSharp.IntegrationTests/AuthenticationTests.cs``.

Uses :mod:`responses` to mock the HTTP server.
"""

from __future__ import annotations

import base64

import responses

from restsharp_py import (
    HttpBasicAuthenticator,
    OAuth2AuthorizationRequestHeaderAuthenticator,
    OAuth2UriQueryParameterAuthenticator,
    ParameterType,
    RestClient,
    RestRequest,
    SimpleAuthenticator,
)


@responses.activate
def test_can_authenticate_with_basic_http_auth() -> None:
    captured = {}

    def echo(request):
        header = request.headers.get("Authorization", "")
        captured["header"] = header
        token = header.split(" ", 1)[1]
        decoded = base64.b64decode(token).decode("ascii")
        return (200, {}, "|".join(decoded.split(":")))

    responses.add_callback(
        responses.GET, "http://example.com/test", callback=echo, content_type="text/plain"
    )

    client = RestClient("http://example.com")
    client.authenticator = HttpBasicAuthenticator("testuser", "testpassword")
    response = client.execute(RestRequest("test"))

    assert response.status_code == 200
    assert response.content == "testuser|testpassword"
    assert captured["header"].startswith("Basic ")


@responses.activate
def test_simple_authenticator_adds_credentials_as_query_params() -> None:
    responses.add(
        responses.GET,
        "http://example.com/test",
        body="ok",
        status=200,
        match=[responses.matchers.query_param_matcher({"user": "alice", "pass": "s3cret"})],
    )

    client = RestClient("http://example.com")
    client.authenticator = SimpleAuthenticator("user", "alice", "pass", "s3cret")
    response = client.execute(RestRequest("test"))

    assert response.status_code == 200


@responses.activate
def test_oauth2_authorization_header_authenticator() -> None:
    captured = {}

    def callback(request):
        captured["header"] = request.headers.get("Authorization")
        return (200, {}, "ok")

    responses.add_callback(
        responses.GET, "http://example.com/test", callback=callback
    )

    client = RestClient("http://example.com")
    client.authenticator = OAuth2AuthorizationRequestHeaderAuthenticator("ACCESS_TOKEN")
    client.execute(RestRequest("test"))

    assert captured["header"] == "OAuth ACCESS_TOKEN"


@responses.activate
def test_oauth2_uri_query_authenticator() -> None:
    responses.add(
        responses.GET,
        "http://example.com/test",
        body="ok",
        status=200,
        match=[responses.matchers.query_param_matcher({"oauth_token": "ACCESS_TOKEN"})],
    )

    client = RestClient("http://example.com")
    client.authenticator = OAuth2UriQueryParameterAuthenticator("ACCESS_TOKEN")
    response = client.execute(RestRequest("test"))

    assert response.status_code == 200


def test_basic_authenticator_skips_when_header_present() -> None:
    request = RestRequest("test")
    request.add_parameter("Authorization", "Bearer abc", ParameterType.HTTP_HEADER)
    authenticator = HttpBasicAuthenticator("user", "pass")
    authenticator.authenticate(None, request)

    auth_headers = [
        p for p in request.parameters
        if p.type == ParameterType.HTTP_HEADER and p.name == "Authorization"
    ]
    assert len(auth_headers) == 1
    assert auth_headers[0].value == "Bearer abc"
