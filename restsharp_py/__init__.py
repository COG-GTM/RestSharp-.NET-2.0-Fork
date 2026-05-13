"""Python port of RestSharp.

Public re-exports mirror the surface area used in the .NET library:
``RestClient``, ``RestRequest``, ``RestResponse``, plus enums and
serializer/deserializer factories.
"""

from __future__ import annotations

from .authenticators import (
    HttpBasicAuthenticator,
    IAuthenticator,
    NtlmAuthenticator,
    OAuth1Authenticator,
    OAuth2AuthorizationRequestHeaderAuthenticator,
    OAuth2Authenticator,
    OAuth2UriQueryParameterAuthenticator,
    SimpleAuthenticator,
)
from .client import RestClient
from .deserializers import IDeserializer, JsonDeserializer, XmlDeserializer
from .enums import DataFormat, DateFormat, Method, ParameterType, ResponseStatus
from .request import FileParameter, Parameter, RestRequest
from .response import RestResponse, RestResponseCookie
from .serializers import ISerializer, JsonSerializer, SerializeAs, XmlSerializer

__all__ = [
    "DataFormat",
    "DateFormat",
    "FileParameter",
    "HttpBasicAuthenticator",
    "IAuthenticator",
    "IDeserializer",
    "ISerializer",
    "JsonDeserializer",
    "JsonSerializer",
    "Method",
    "NtlmAuthenticator",
    "OAuth1Authenticator",
    "OAuth2AuthorizationRequestHeaderAuthenticator",
    "OAuth2Authenticator",
    "OAuth2UriQueryParameterAuthenticator",
    "Parameter",
    "ParameterType",
    "ResponseStatus",
    "RestClient",
    "RestRequest",
    "RestResponse",
    "RestResponseCookie",
    "SerializeAs",
    "SimpleAuthenticator",
    "XmlDeserializer",
    "XmlSerializer",
]
