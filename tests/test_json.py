"""JSON deserializer tests adapted from ``RestSharp.Tests/JsonTests.cs``."""

from __future__ import annotations

import json
from dataclasses import dataclass, field
from datetime import datetime
from typing import List, Optional

from restsharp_py import JsonDeserializer, RestResponse


@dataclass
class PersonForJson:
    name: str = ""
    age: int = 0
    is_cool: bool = False
    start_date: Optional[datetime] = None


@dataclass
class NullableValues:
    id: Optional[int] = None
    start_date: Optional[datetime] = None
    unique_id: Optional[str] = None


@dataclass
class Group:
    name: str = ""
    count: int = 0


@dataclass
class VenuesResponse:
    groups: List[Group] = field(default_factory=list)


def _response(content: str) -> RestResponse:
    return RestResponse(content=content)


def test_deserialize_basic_object() -> None:
    payload = json.dumps({"name": "Foo", "age": 30, "is_cool": True})
    deserializer = JsonDeserializer()
    result = deserializer.deserialize(_response(payload), PersonForJson)
    assert result.name == "Foo"
    assert result.age == 30
    assert result.is_cool is True


def test_deserialize_with_root_element() -> None:
    payload = json.dumps({"response": {"groups": [{"name": "A", "count": 1}]}})
    deserializer = JsonDeserializer()
    deserializer.root_element = "response"
    result = deserializer.deserialize(_response(payload), VenuesResponse)
    assert len(result.groups) == 1
    assert result.groups[0].name == "A"


def test_deserialize_list_of_strings_with_root() -> None:
    payload = json.dumps({"users": ["johnsheehan", "jagregory"]})
    deserializer = JsonDeserializer()
    deserializer.root_element = "users"
    result = deserializer.deserialize(_response(payload), List[str])
    assert result == ["johnsheehan", "jagregory"]


def test_deserialize_nullable_values_with_null() -> None:
    payload = json.dumps({"id": None, "start_date": None, "unique_id": None})
    result = JsonDeserializer().deserialize(_response(payload), NullableValues)
    assert result.id is None
    assert result.start_date is None
    assert result.unique_id is None


def test_deserialize_nullable_values_with_data() -> None:
    payload = json.dumps(
        {
            "id": 123,
            "start_date": "2010-02-21T09:35:00",
            "unique_id": "AC1FC4BC-087A-4242-B8EE-C53EBE9887A5",
        }
    )
    result = JsonDeserializer().deserialize(_response(payload), NullableValues)
    assert result.id == 123
    assert result.start_date == datetime(2010, 2, 21, 9, 35, 0)
    assert result.unique_id == "AC1FC4BC-087A-4242-B8EE-C53EBE9887A5"


def test_deserialize_custom_formatted_date() -> None:
    payload = json.dumps({"start_date": "08 2010 Feb, 11:11 11 AM"})
    deserializer = JsonDeserializer()
    deserializer.date_format = "%d %Y %b, %I:%M %S %p"
    result = deserializer.deserialize(_response(payload), PersonForJson)
    assert result.start_date == datetime(2010, 2, 8, 11, 11, 11)


def test_fuzzy_name_matching_pascal_to_snake() -> None:
    @dataclass
    class Product:
        product_id: Optional[int] = None
        name: Optional[str] = None

    payload = json.dumps({"ProductId": 42, "Name": "Widget"})
    result = JsonDeserializer().deserialize(_response(payload), Product)
    assert result.product_id == 42
    assert result.name == "Widget"


def test_empty_content_returns_default_instance() -> None:
    result = JsonDeserializer().deserialize(_response(""), PersonForJson)
    assert isinstance(result, PersonForJson)
    assert result.name == ""
