# RestSharp Python Port (`restsharp_py`)

A faithful port of the .NET RestSharp 2.0 library to Python, built on top of
[`requests`](https://pypi.org/project/requests/) (for synchronous calls) and
[`httpx`](https://pypi.org/project/httpx/) (for the async path). The public
surface mirrors the original `RestClient` / `RestRequest` / `RestResponse`
pattern while using Pythonic idioms (dataclasses, enums, type hints, protocols).

## Installation

```bash
pip install -r requirements.txt
# or, with the optional XML extras:
pip install ".[xml,dev]"
```

## Quick start

```python
from restsharp_py import HttpBasicAuthenticator, Method, RestClient, RestRequest

client = RestClient("http://example.com")
client.authenticator = HttpBasicAuthenticator("user", "pass")

request = RestRequest("resource/{id}", Method.POST)
request.add_url_segment("id", "123")
request.add_parameter("name", "value")
request.add_header("X-Custom", "header")

response = client.execute(request)
print(response.status_code, response.content)
```

### Typed responses

```python
from dataclasses import dataclass
from restsharp_py import RestClient, RestRequest

@dataclass
class Pet:
    name: str = ""
    age: int = 0

client = RestClient("http://example.com")
typed = client.execute_typed(RestRequest("pets/1"), Pet)
print(typed.data)  # Pet(name=..., age=...)
```

### Async dispatch

```python
import asyncio
from restsharp_py import RestClient, RestRequest

async def main() -> None:
    client = RestClient("http://example.com")
    response = await client.execute_async(RestRequest("ping"))
    print(response.content)

asyncio.run(main())
```

## Module layout

```
restsharp_py/
    __init__.py            # public re-exports
    enums.py               # ParameterType, DataFormat, Method, ResponseStatus, DateFormat
    request.py             # RestRequest, Parameter, FileParameter
    response.py            # RestResponse, RestResponseCookie
    client.py              # RestClient (sync + async)
    extensions.py          # URL/case helpers used by deserializers (fuzzy name matching)
    serializers/
        __init__.py        # ISerializer protocol
        json_serializer.py
        xml_serializer.py  # supports SerializeAs decorator metadata
    deserializers/
        __init__.py        # IDeserializer protocol
        json_deserializer.py
        xml_deserializer.py
    authenticators/
        __init__.py        # IAuthenticator protocol
        basic.py           # HttpBasicAuthenticator
        oauth1.py          # OAuth1Authenticator (requests_oauthlib)
        oauth2.py          # OAuth2Authenticator + Header/Query variants
        ntlm.py            # NtlmAuthenticator (requests_ntlm)
        simple.py          # SimpleAuthenticator
```

## Running tests

```bash
pip install -r requirements.txt
python -m pytest tests/
```

The integration tests (`tests/test_integration/`) use
[`responses`](https://pypi.org/project/responses/) and
[`httpx.MockTransport`](https://www.python-httpx.org/advanced/transports/) to
mock the HTTP layer; no network is required.
