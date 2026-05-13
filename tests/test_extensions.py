"""Tests for the string and URL helpers in :mod:`restsharp_py.extensions`."""

from datetime import datetime, timezone

from restsharp_py.extensions import (
    add_dashes,
    add_underscores,
    get_name_variants,
    has_value,
    parse_json_date,
    remove_surrounding_quotes,
    to_camel_case,
    to_pascal_case,
    url_decode,
    url_encode,
)


def test_has_value_true_and_false() -> None:
    assert has_value("abc") is True
    assert has_value("") is False
    assert has_value(None) is False


def test_url_encode_decode_roundtrip() -> None:
    raw = "foo bar/baz?qux=1"
    encoded = url_encode(raw)
    assert encoded == "foo%20bar%2Fbaz%3Fqux%3D1"
    assert url_decode(encoded) == raw


def test_add_underscores_converts_pascal_case() -> None:
    assert add_underscores("ProductId") == "Product_Id"
    assert add_underscores("HTTPResponse") == "HTTP_Response"


def test_add_dashes_converts_pascal_case() -> None:
    assert add_dashes("ProductId") == "Product-Id"
    assert add_dashes("HTTPResponse") == "HTTP-Response"


def test_to_pascal_case_and_camel_case() -> None:
    assert to_pascal_case("product_id") == "ProductId"
    assert to_camel_case("product_id") == "productId"


def test_get_name_variants_includes_common_forms() -> None:
    variants = list(get_name_variants("ProductId"))
    assert "ProductId" in variants
    assert "productId" in variants
    assert "Product_Id" in variants
    assert "product_id" in variants
    assert "Product-Id" in variants
    assert "product-id" in variants


def test_remove_surrounding_quotes_strips_only_paired_quotes() -> None:
    assert remove_surrounding_quotes('"hello"') == "hello"
    assert remove_surrounding_quotes('"hello') == '"hello'


def test_parse_json_date_handles_iso_format() -> None:
    parsed = parse_json_date("2010-02-21T09:35:00")
    assert parsed == datetime(2010, 2, 21, 9, 35, 0)


def test_parse_json_date_handles_microsoft_format() -> None:
    parsed = parse_json_date("/Date(1234567890000)/")
    assert parsed == datetime(2009, 2, 13, 23, 31, 30, tzinfo=timezone.utc)
