"""Async execution tests adapted from
``RestSharp.IntegrationTests/AsyncTests.cs``.

Uses ``httpx.MockTransport`` to stub responses.
"""

from __future__ import annotations

from typing import Any

import httpx
import pytest

from restsharp_py import RestClient, RestRequest


class _StubTransport(httpx.AsyncBaseTransport):
    def __init__(self, responder):
        self._responder = responder

    async def handle_async_request(self, request):
        return self._responder(request)


@pytest.mark.asyncio
async def test_execute_async_returns_response(monkeypatch) -> None:
    def responder(request: httpx.Request) -> httpx.Response:
        assert request.method == "GET"
        return httpx.Response(200, text="async ok")

    real_async_client = httpx.AsyncClient

    def _factory(*args, **kwargs):
        kwargs["transport"] = _StubTransport(responder)
        return real_async_client(*args, **kwargs)

    monkeypatch.setattr(httpx, "AsyncClient", _factory)

    client = RestClient("http://example.com")
    response = await client.execute_async(RestRequest("test"))

    assert response.status_code == 200
    assert response.content == "async ok"


@pytest.mark.asyncio
async def test_execute_async_invokes_callback(monkeypatch) -> None:
    def responder(request: httpx.Request) -> httpx.Response:
        return httpx.Response(200, text="payload")

    real_async_client = httpx.AsyncClient

    def _factory(*args, **kwargs):
        kwargs["transport"] = _StubTransport(responder)
        return real_async_client(*args, **kwargs)

    monkeypatch.setattr(httpx, "AsyncClient", _factory)

    callbacks: list[Any] = []

    async def callback(response) -> None:
        callbacks.append(response.content)

    client = RestClient("http://example.com")
    await client.execute_async(RestRequest("test"), callback=callback)
    assert callbacks == ["payload"]
