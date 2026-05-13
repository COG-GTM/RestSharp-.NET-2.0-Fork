"""File upload integration tests adapted from
``RestSharp.IntegrationTests/FileTests.cs``.

Multipart bodies are inspected via :mod:`responses` callbacks.
"""

from __future__ import annotations

import responses

from restsharp_py import Method, RestClient, RestRequest


@responses.activate
def test_uploads_file_as_multipart() -> None:
    captured = {}

    def callback(request):
        captured["body"] = request.body
        captured["content_type"] = request.headers.get("Content-Type")
        return (200, {}, "ok")

    responses.add_callback(
        responses.POST, "http://example.com/upload", callback=callback
    )

    client = RestClient("http://example.com")
    request = RestRequest("upload", Method.POST)
    request.add_file("file", b"hello world", file_name="hello.txt", content_type="text/plain")
    response = client.execute(request)

    assert response.status_code == 200
    assert captured["content_type"].startswith("multipart/form-data")
    raw = captured["body"]
    if isinstance(raw, (bytes, bytearray)):
        raw = bytes(raw)
        assert b"hello world" in raw
        assert b'filename="hello.txt"' in raw
    else:  # pragma: no cover - responses encodes to bytes
        assert "hello world" in raw


@responses.activate
def test_uploads_file_from_disk(tmp_path) -> None:
    path = tmp_path / "letters.txt"
    path.write_bytes(b"abcdef")

    captured = {}

    def callback(request):
        captured["body"] = request.body
        return (200, {}, "ok")

    responses.add_callback(
        responses.POST, "http://example.com/upload", callback=callback
    )

    client = RestClient("http://example.com")
    request = RestRequest("upload", Method.POST)
    request.add_file("file", str(path))
    response = client.execute(request)

    assert response.status_code == 200
    raw = captured["body"]
    if isinstance(raw, (bytes, bytearray)):
        assert b"abcdef" in bytes(raw)
