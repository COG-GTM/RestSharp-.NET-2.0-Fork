"""XML deserializer tests adapted from ``RestSharp.Tests/XmlTests.cs``."""

from __future__ import annotations

from dataclasses import dataclass, field
from typing import List, Optional

from restsharp_py import RestResponse, XmlDeserializer


@dataclass
class Item:
    name: str = ""
    value: int = 0


@dataclass
class Person:
    name: str = ""
    age: int = 0
    is_cool: bool = False
    items: List[Item] = field(default_factory=list)


def _response(content: str) -> RestResponse:
    return RestResponse(content=content)


def test_deserialize_basic_object() -> None:
    payload = (
        "<Person><Name>Foo</Name><Age>30</Age><IsCool>true</IsCool>"
        "<Items><Item><Name>One</Name><Value>1</Value></Item></Items></Person>"
    )
    result = XmlDeserializer().deserialize(_response(payload), Person)
    assert result.name == "Foo"
    assert result.age == 30
    assert result.is_cool is True
    assert result.items == [Item(name="One", value=1)]


def test_deserialize_with_root_element() -> None:
    payload = (
        "<Response><Person><Name>Foo</Name><Age>30</Age></Person></Response>"
    )
    deserializer = XmlDeserializer()
    deserializer.root_element = "Person"
    result = deserializer.deserialize(_response(payload), Person)
    assert result.name == "Foo"
    assert result.age == 30


def test_deserialize_namespaced_xml_flattens_when_unset() -> None:
    payload = (
        '<ns:Person xmlns:ns="http://example.com">'
        "<ns:Name>Foo</ns:Name><ns:Age>30</ns:Age>"
        "</ns:Person>"
    )
    result = XmlDeserializer().deserialize(_response(payload), Person)
    assert result.name == "Foo"
    assert result.age == 30


def test_deserialize_attributes_use_fuzzy_matching() -> None:
    @dataclass
    class Product:
        product_id: Optional[int] = None
        name: Optional[str] = None

    payload = '<Product ProductId="42"><Name>Widget</Name></Product>'
    result = XmlDeserializer().deserialize(_response(payload), Product)
    assert result.product_id == 42
    assert result.name == "Widget"


def test_empty_content_returns_default_instance() -> None:
    result = XmlDeserializer().deserialize(_response(""), Person)
    assert isinstance(result, Person)
    assert result.name == ""
