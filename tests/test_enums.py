"""Tests for ported enums."""

from restsharp_py import DataFormat, DateFormat, Method, ParameterType, ResponseStatus


def test_method_members() -> None:
    assert Method.GET.value == "GET"
    assert Method.POST.value == "POST"
    assert Method.PATCH.value == "PATCH"
    assert {m.value for m in Method} == {
        "GET",
        "POST",
        "PUT",
        "DELETE",
        "HEAD",
        "OPTIONS",
        "PATCH",
    }


def test_parameter_type_members() -> None:
    assert {p.value for p in ParameterType} == {
        "Cookie",
        "GetOrPost",
        "UrlSegment",
        "HttpHeader",
        "RequestBody",
    }


def test_data_format_members() -> None:
    assert {d.value for d in DataFormat} == {"Json", "Xml"}


def test_response_status_members() -> None:
    assert {s.value for s in ResponseStatus} == {
        "None",
        "Completed",
        "Error",
        "TimedOut",
        "Aborted",
    }


def test_date_format_constants() -> None:
    assert DateFormat.ISO_8601 == "%Y-%m-%dT%H:%M:%S"
    assert DateFormat.ROUND_TRIP == "%Y-%m-%d %H:%M:%SZ"
