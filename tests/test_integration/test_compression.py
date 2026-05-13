"""Decompression integration tests.

Adapted from ``RestSharp.IntegrationTests/CompressionTests.cs``. The
``requests`` library handles ``gzip``/``deflate`` decompression
transparently, so we just verify that the client surfaces the
decoded body.
"""

from __future__ import annotations

import gzip
import zlib

import responses

from restsharp_py import RestClient, RestRequest


@responses.activate
def test_gzip_response_is_decompressed() -> None:
    payload = b"hello world"
    compressed = gzip.compress(payload)
    responses.add(
        responses.GET,
        "http://example.com/gzip",
        body=compressed,
        status=200,
        headers={"Content-Encoding": "gzip"},
        content_type="text/plain",
    )

    response = RestClient("http://example.com").execute(RestRequest("gzip"))
    assert response.status_code == 200
    assert response.content == "hello world"


@responses.activate
def test_deflate_response_is_decompressed() -> None:
    payload = b"hello deflate"
    compressed = zlib.compress(payload)
    responses.add(
        responses.GET,
        "http://example.com/deflate",
        body=compressed,
        status=200,
        headers={"Content-Encoding": "deflate"},
        content_type="text/plain",
    )

    response = RestClient("http://example.com").execute(RestRequest("deflate"))
    assert response.status_code == 200
    assert response.content == "hello deflate"
