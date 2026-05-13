"""Tests for the :class:`restsharp_py.RestRequest` container."""

from __future__ import annotations

from dataclasses import dataclass

from restsharp_py import (
    DataFormat,
    FileParameter,
    Method,
    Parameter,
    ParameterType,
    RestRequest,
)


def test_default_constructor_uses_get_method() -> None:
    request = RestRequest()
    assert request.method == Method.GET
    assert request.resource == ""
    assert request.parameters == []
    assert request.files == []


def test_resource_constructor_keeps_resource() -> None:
    request = RestRequest("resource/path")
    assert request.resource == "resource/path"


def test_add_parameter_with_name_and_value() -> None:
    request = RestRequest("test")
    request.add_parameter("foo", "bar")
    assert len(request.parameters) == 1
    assert request.parameters[0] == Parameter(
        name="foo", value="bar", type=ParameterType.GET_OR_POST
    )


def test_add_parameter_accepts_parameter_instance() -> None:
    request = RestRequest("test")
    parameter = Parameter(name="foo", value=1, type=ParameterType.URL_SEGMENT)
    request.add_parameter(parameter)
    assert request.parameters == [parameter]


def test_add_header_sets_parameter_type() -> None:
    request = RestRequest("test").add_header("X-Token", "abc")
    assert request.parameters[0].type == ParameterType.HTTP_HEADER


def test_add_cookie_sets_parameter_type() -> None:
    request = RestRequest("test").add_cookie("sid", "abc")
    assert request.parameters[0].type == ParameterType.COOKIE


def test_add_url_segment_sets_parameter_type() -> None:
    request = RestRequest("test").add_url_segment("id", "42")
    assert request.parameters[0].type == ParameterType.URL_SEGMENT


def test_add_body_with_json_format_serializes_object() -> None:
    @dataclass
    class Payload:
        name: str
        value: int

    request = RestRequest("test", Method.POST, request_format=DataFormat.JSON)
    request.add_body(Payload(name="foo", value=1))

    body = request.first_parameter_of(ParameterType.REQUEST_BODY)
    assert body is not None
    assert body.name == "application/json"
    assert '"name": "foo"' in body.value
    assert '"value": 1' in body.value


def test_add_body_with_xml_format_serializes_object() -> None:
    @dataclass
    class Payload:
        name: str = ""

    request = RestRequest("test", Method.POST, request_format=DataFormat.XML)
    request.add_body(Payload(name="foo"))

    body = request.first_parameter_of(ParameterType.REQUEST_BODY)
    assert body is not None
    assert body.name == "text/xml"
    assert "<name>foo</name>" in body.value


def test_add_object_adds_each_property_as_parameter() -> None:
    @dataclass
    class Payload:
        name: str = "foo"
        value: int = 1

    request = RestRequest("test").add_object(Payload())
    names = {p.name for p in request.parameters}
    assert names == {"name", "value"}


def test_add_file_with_bytes_attaches_file_parameter() -> None:
    request = RestRequest("upload").add_file(
        "file", b"hello", file_name="hello.txt", content_type="text/plain"
    )
    assert len(request.files) == 1
    file_param = request.files[0]
    assert isinstance(file_param, FileParameter)
    assert file_param.read() == b"hello"
    assert file_param.file_name == "hello.txt"


def test_increase_num_attempts_increments_counter() -> None:
    request = RestRequest("test")
    assert request.attempts == 0
    request.increase_num_attempts()
    request.increase_num_attempts()
    assert request.attempts == 2
