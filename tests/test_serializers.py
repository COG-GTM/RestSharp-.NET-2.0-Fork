"""Serializer tests adapted from ``RestSharp.Tests/SerializerTests.cs``."""

from __future__ import annotations

import json
from dataclasses import dataclass, field
from datetime import datetime
from decimal import Decimal
from typing import List
from xml.etree.ElementTree import fromstring

from restsharp_py import (
    DateFormat,
    JsonSerializer,
    SerializeAs,
    XmlSerializer,
)


@dataclass
class Item:
    name: str = ""
    value: int = 0


@dataclass
class Person:
    name: str = ""
    age: int = 0
    price: Decimal = Decimal("0")
    start_date: datetime = field(default_factory=lambda: datetime(2009, 12, 18, 10, 2, 23))
    is_cool: bool = False
    items: List[Item] = field(default_factory=list)


def test_xml_serializer_roundtrip_basic_poco() -> None:
    person = Person(
        name="Foo",
        age=50,
        price=Decimal("19.95"),
        start_date=datetime(2009, 12, 18, 10, 2, 23),
        items=[Item(name="One", value=1)],
    )
    serializer = XmlSerializer()
    xml = serializer.serialize(person)

    root = fromstring(xml)
    assert root.tag == "Person"
    assert root.find("name").text == "Foo"
    assert root.find("age").text == "50"
    assert root.find("is_cool").text == "false"
    items = root.find("items")
    assert items is not None
    assert items.find("Item").find("name").text == "One"


def test_xml_serializer_uses_iso_date_format() -> None:
    person = Person(
        name="Foo",
        age=50,
        price=Decimal("19.95"),
        start_date=datetime(2009, 12, 18, 10, 2, 23),
    )
    serializer = XmlSerializer()
    serializer.date_format = DateFormat.ISO_8601
    xml = serializer.serialize(person)
    root = fromstring(xml)
    assert root.find("start_date").text == "2009-12-18T10:02:23"


def test_xml_serializer_supports_root_element_wrapper() -> None:
    person = Person(name="Foo", age=50, price=Decimal("19.95"))
    serializer = XmlSerializer()
    serializer.root_element = "Result"
    xml = serializer.serialize(person)
    root = fromstring(xml)
    assert root.tag == "Result"
    assert root.find("Person") is not None


def test_xml_serializer_uses_namespace() -> None:
    person = Person(name="Foo", age=50, price=Decimal("19.95"))
    serializer = XmlSerializer("http://example.com")
    xml = serializer.serialize(person)
    assert "http://example.com" in xml


@SerializeAs(name="ordered")
@dataclass
class OrderedProperties:
    name: str = "Name"
    age: int = 99
    start_date: datetime = field(default_factory=lambda: datetime(2010, 1, 1))
    _field_options: dict = field(
        default_factory=lambda: {
            "name": {"index": 1},
            "age": {"index": 2},
            "start_date": {"index": 3},
        }
    )


def test_xml_serializer_serialize_as_decorator_changes_root_name() -> None:
    serializer = XmlSerializer()
    xml = serializer.serialize(OrderedProperties())
    root = fromstring(xml)
    assert root.tag == "ordered"


def test_json_serializer_writes_indented_json() -> None:
    serializer = JsonSerializer()
    payload = serializer.serialize({"name": "Foo", "age": 50})
    assert json.loads(payload) == {"name": "Foo", "age": 50}
    # The default serializer should produce indented (multi-line) output.
    assert "\n" in payload


def test_json_serializer_handles_dataclasses() -> None:
    payload = JsonSerializer().serialize(Item(name="One", value=1))
    assert json.loads(payload) == {"name": "One", "value": 1}
