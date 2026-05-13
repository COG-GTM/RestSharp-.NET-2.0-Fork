"""Enums ported from RestSharp/Enum.cs."""

from enum import Enum


class ParameterType(Enum):
    """Types of parameters that can be added to requests."""

    COOKIE = "Cookie"
    GET_OR_POST = "GetOrPost"
    URL_SEGMENT = "UrlSegment"
    HTTP_HEADER = "HttpHeader"
    REQUEST_BODY = "RequestBody"


class DataFormat(Enum):
    """Data formats supported for request bodies."""

    JSON = "Json"
    XML = "Xml"


class Method(Enum):
    """HTTP method to use when making requests."""

    GET = "GET"
    POST = "POST"
    PUT = "PUT"
    DELETE = "DELETE"
    HEAD = "HEAD"
    OPTIONS = "OPTIONS"
    PATCH = "PATCH"


class ResponseStatus(Enum):
    """Status for responses."""

    NONE = "None"
    COMPLETED = "Completed"
    ERROR = "Error"
    TIMED_OUT = "TimedOut"
    ABORTED = "Aborted"


class DateFormat:
    """Format strings for commonly-used date formats.

    Python equivalents of the .NET format specifiers used in the
    original RestSharp library.
    """

    # ISO 8601 sortable date/time pattern (.NET "s")
    ISO_8601 = "%Y-%m-%dT%H:%M:%S"
    # Round-trip date/time pattern (.NET "u")
    ROUND_TRIP = "%Y-%m-%d %H:%M:%SZ"
