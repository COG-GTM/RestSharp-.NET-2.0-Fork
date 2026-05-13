"""Tests for the :class:`restsharp_py.RestClient` plumbing."""

from __future__ import annotations

from dataclasses import dataclass
from typing import Optional

import responses

from restsharp_py import (
    JsonDeserializer,
    Method,
    RestClient,
    RestRequest,
    RestResponse,
    XmlDeserializer,
)


def test_default_handlers_registered() -> None:
    client = RestClient()
    assert isinstance(client.get_handler("application/json"), JsonDeserializer)
    assert isinstance(client.get_handler("application/xml"), XmlDeserializer)
    assert isinstance(client.get_handler("text/json"), JsonDeserializer)
    assert isinstance(client.get_handler("text/xml"), XmlDeserializer)


def test_remove_handler_removes_content_type() -> None:
    client = RestClient()
    client.remove_handler("application/json")
    handler = client.get_handler("application/json")
    # Falls back to the wildcard handler (XML by default)
    assert isinstance(handler, XmlDeserializer)


def test_clear_handlers_removes_all_registered() -> None:
    client = RestClient()
    client.clear_handlers()
    assert client.get_handler("application/json") is None


def test_base_url_strips_trailing_slash() -> None:
    client = RestClient("http://example.com/")
    assert client.base_url == "http://example.com"


@responses.activate
def test_execute_returns_rest_response_with_body() -> None:
    responses.add(
        responses.GET, "http://example.com/test", body="hi", status=200
    )
    response = RestClient("http://example.com").execute(RestRequest("test"))
    assert isinstance(response, RestResponse)
    assert response.status_code == 200
    assert response.content == "hi"
    assert response.raw_bytes == b"hi"


@responses.activate
def test_execute_typed_deserializes_json_body() -> None:
    @dataclass
    class Payload:
        name: Optional[str] = None
        value: Optional[int] = None

    responses.add(
        responses.GET,
        "http://example.com/payload",
        json={"name": "foo", "value": 42},
        status=200,
    )

    response = RestClient("http://example.com").execute_typed(
        RestRequest("payload"), Payload
    )
    assert response.status_code == 200
    assert response.data == Payload(name="foo", value=42)


@responses.activate
def test_execute_post_form_data() -> None:
    captured = {}

    def callback(request):
        captured["body"] = request.body
        captured["content_type"] = request.headers.get("Content-Type", "")
        return (200, {}, "ok")

    responses.add_callback(
        responses.POST, "http://example.com/submit", callback=callback
    )

    request = RestRequest("submit", Method.POST)
    request.add_parameter("name", "foo")
    request.add_parameter("value", "bar")

    response = RestClient("http://example.com").execute(request)

    assert response.status_code == 200
    assert "application/x-www-form-urlencoded" in captured["content_type"]
    assert "name=foo" in captured["body"]
    assert "value=bar" in captured["body"]


@responses.activate
def test_default_parameter_is_applied_to_requests() -> None:
    captured = {}

    def callback(request):
        captured["headers"] = dict(request.headers)
        return (200, {}, "ok")

    responses.add_callback(
        responses.GET, "http://example.com/test", callback=callback
    )

    client = RestClient("http://example.com")
    client.add_default_header("X-API-Key", "secret")
    client.execute(RestRequest("test"))

    assert captured["headers"].get("X-API-Key") == "secret"


@responses.activate
def test_error_response_sets_response_status_error() -> None:
    responses.add(
        responses.GET,
        "http://example.com/oops",
        body=ConnectionError("boom"),
    )
    response = RestClient("http://example.com").execute(RestRequest("oops"))
    assert response.response_status.value == "Error"
    assert response.error_exception is not None


def test_download_data_returns_raw_bytes(monkeypatch) -> None:
    responses_module = __import__("responses")
    with responses_module.RequestsMock() as rsps:
        rsps.add(
            responses_module.GET,
            "http://example.com/binary",
            body=b"\x00\x01\x02",
            status=200,
        )
        data = RestClient("http://example.com").download_data(RestRequest("binary"))
        assert data == b"\x00\x01\x02"
