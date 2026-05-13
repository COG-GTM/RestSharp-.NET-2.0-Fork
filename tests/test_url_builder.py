"""Ported from ``RestSharp.Tests/UrlBuilderTests.cs``."""

from restsharp_py import Method, RestClient, RestRequest


def test_get_with_leading_slash() -> None:
    request = RestRequest("/resource")
    client = RestClient("http://example.com")
    assert client.build_uri(request) == "http://example.com/resource"


def test_post_with_leading_slash() -> None:
    request = RestRequest("/resource", Method.POST)
    client = RestClient("http://example.com")
    assert client.build_uri(request) == "http://example.com/resource"


def test_get_with_leading_slash_and_baseurl_trailing_slash() -> None:
    request = RestRequest("/resource")
    request.add_parameter("foo", "bar")
    client = RestClient("http://example.com/")
    assert client.build_uri(request) == "http://example.com/resource?foo=bar"


def test_post_with_leading_slash_and_baseurl_trailing_slash() -> None:
    request = RestRequest("/resource", Method.POST)
    client = RestClient("http://example.com/")
    assert client.build_uri(request) == "http://example.com/resource"


def test_get_with_resource_containing_slashes() -> None:
    request = RestRequest("resource/foo")
    client = RestClient("http://example.com")
    assert client.build_uri(request) == "http://example.com/resource/foo"


def test_post_with_resource_containing_slashes() -> None:
    request = RestRequest("resource/foo", Method.POST)
    client = RestClient("http://example.com")
    assert client.build_uri(request) == "http://example.com/resource/foo"


def test_get_with_resource_containing_tokens() -> None:
    request = RestRequest("resource/{foo}")
    request.add_url_segment("foo", "bar")
    client = RestClient("http://example.com")
    assert client.build_uri(request) == "http://example.com/resource/bar"


def test_post_with_resource_containing_tokens() -> None:
    request = RestRequest("resource/{foo}", Method.POST)
    request.add_url_segment("foo", "bar")
    client = RestClient("http://example.com")
    assert client.build_uri(request) == "http://example.com/resource/bar"


def test_get_with_empty_request() -> None:
    request = RestRequest()
    client = RestClient("http://example.com/resource")
    assert client.build_uri(request) == "http://example.com/resource"


def test_get_with_empty_request_and_bare_hostname() -> None:
    request = RestRequest()
    client = RestClient("http://example.com")
    assert client.build_uri(request) == "http://example.com/"
