"""Status code integration tests adapted from
``RestSharp.IntegrationTests/StatusCodeTests.cs``.

Uses :mod:`responses` to simulate HTTP responses.
"""

from __future__ import annotations

import responses

from restsharp_py import RestClient, RestRequest


@responses.activate
def test_handles_200_ok() -> None:
    responses.add(responses.GET, "http://example.com/ok", body="hi", status=200)
    response = RestClient("http://example.com").execute(RestRequest("ok"))
    assert response.status_code == 200
    assert response.content == "hi"


@responses.activate
def test_handles_404_not_found() -> None:
    responses.add(responses.GET, "http://example.com/missing", status=404)
    response = RestClient("http://example.com").execute(RestRequest("missing"))
    assert response.status_code == 404


@responses.activate
def test_handles_500_server_error() -> None:
    responses.add(
        responses.GET,
        "http://example.com/boom",
        body="server explosion",
        status=500,
    )
    response = RestClient("http://example.com").execute(RestRequest("boom"))
    assert response.status_code == 500
    assert response.content == "server explosion"


@responses.activate
def test_handles_redirects_when_follow_redirects_enabled() -> None:
    responses.add(
        responses.GET,
        "http://example.com/first",
        status=302,
        headers={"Location": "http://example.com/final"},
    )
    responses.add(responses.GET, "http://example.com/final", body="done", status=200)

    client = RestClient("http://example.com")
    client.follow_redirects = True
    response = client.execute(RestRequest("first"))

    assert response.status_code == 200
    assert response.content == "done"


@responses.activate
def test_records_response_headers() -> None:
    responses.add(
        responses.GET,
        "http://example.com/headers",
        body="ok",
        status=200,
        headers={"X-Custom": "value", "Content-Type": "text/plain"},
    )

    response = RestClient("http://example.com").execute(RestRequest("headers"))
    assert response.header("X-Custom") == "value"
    assert response.content_type and response.content_type.startswith("text/plain")
