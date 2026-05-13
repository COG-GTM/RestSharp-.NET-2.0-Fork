"""Utility helpers ported from RestSharp/Extensions/.

Includes string/case helpers used by the JSON/XML deserializers
(name variant matching), URL encoding helpers, and reflection
helpers used by ``RestRequest.add_object``.
"""

from __future__ import annotations

import re
from datetime import datetime, timezone
from typing import Any, Iterable, Iterator, List
from urllib.parse import quote, unquote


def has_value(value: str | None) -> bool:
    """Return True if ``value`` is not None and not empty."""
    return value is not None and value != ""


def url_encode(value: str) -> str:
    """Percent-encode a string for use in URLs.

    Mirrors RestSharp's reliance on ``Uri.EscapeDataString``.
    """
    if value is None:
        return ""
    return quote(str(value), safe="")


def url_decode(value: str) -> str:
    """Percent-decode a URL-encoded string."""
    if value is None:
        return ""
    return unquote(value)


def remove_underscores_and_dashes(input_str: str) -> str:
    """Remove underscores and dashes from ``input_str``."""
    return input_str.replace("_", "").replace("-", "")


def remove_surrounding_quotes(value: str) -> str:
    """Remove leading and trailing double quotes if both are present."""
    if value.startswith('"') and value.endswith('"'):
        return value[1:-1]
    return value


def is_upper_case(value: str) -> bool:
    """Return True if the input is composed entirely of uppercase letters."""
    return bool(re.fullmatch(r"[A-Z]+", value))


def add_underscores(pascal_cased: str) -> str:
    """Add underscores between word boundaries of a PascalCase string."""
    s = re.sub(r"([A-Z]+)([A-Z][a-z])", r"\1_\2", pascal_cased)
    s = re.sub(r"([a-z\d])([A-Z])", r"\1_\2", s)
    return re.sub(r"[-\s]", "_", s)


def add_dashes(pascal_cased: str) -> str:
    """Add dashes between word boundaries of a PascalCase string."""
    s = re.sub(r"([A-Z]+)([A-Z][a-z])", r"\1-\2", pascal_cased)
    s = re.sub(r"([a-z\d])([A-Z])", r"\1-\2", s)
    return re.sub(r"[\s]", "-", s)


def make_initial_lower_case(word: str) -> str:
    """Lowercase the first character of ``word``."""
    if not word:
        return word
    return word[0].lower() + word[1:]


def to_pascal_case(text: str, remove_underscores_flag: bool = True) -> str:
    """Convert a string like ``product_id`` to ``ProductId``."""
    if not text:
        return text

    text = text.replace("_", " ")
    join_str = "" if remove_underscores_flag else "_"
    words = text.split(" ")
    if len(words) > 1 or is_upper_case(words[0]):
        result = []
        for word in words:
            if not word:
                continue
            rest = word[1:]
            if is_upper_case(rest):
                rest = rest.lower()
            result.append(word[0].upper() + rest)
        return join_str.join(result)
    return words[0][0].upper() + words[0][1:]


def to_camel_case(text: str) -> str:
    """Convert a string to camelCase using :func:`to_pascal_case`."""
    return make_initial_lower_case(to_pascal_case(text))


def get_name_variants(name: str) -> Iterator[str]:
    """Yield common case variants of ``name`` used for fuzzy matching."""
    if not name:
        return
    seen: set[str] = set()

    def emit(value: str) -> Iterator[str]:
        if value and value not in seen:
            seen.add(value)
            yield value

    yield from emit(name)
    yield from emit(to_pascal_case(name))
    yield from emit(to_camel_case(name))
    yield from emit(name.lower())
    yield from emit(name.upper())
    yield from emit(add_underscores(name))
    yield from emit(add_underscores(name).lower())
    yield from emit(add_dashes(name))
    yield from emit(add_dashes(name).lower())


_JSON_DATE_FORMATS: List[str] = [
    "%Y-%m-%d %H:%M:%SZ",
    "%Y-%m-%dT%H:%M:%S",
    "%Y-%m-%dT%H:%M:%SZ",
    "%Y-%m-%d %H:%M:%S",
    "%Y-%m-%dT%H:%M:%S%z",
    "%m/%d/%Y %I:%M:%S %p",
]


def parse_json_date(raw: str) -> datetime:
    """Parse common JSON date formats into a ``datetime``.

    Supports raw Unix timestamps, ``/Date(...)`` Microsoft format,
    ``new Date(...)`` literals, and a few ISO-8601 variants.
    Returns ``datetime.min`` on failure (parallels .NET ``default(DateTime)``).
    """
    if raw is None:
        return datetime.min
    cleaned = raw.replace("\n", "").replace("\r", "")
    cleaned = remove_surrounding_quotes(cleaned)

    try:
        unix = int(cleaned)
    except ValueError:
        unix = None
    if unix is not None:
        epoch = datetime(1970, 1, 1, tzinfo=timezone.utc)
        return epoch.fromtimestamp(unix, tz=timezone.utc)

    match = re.search(r"/Date\((-?\d+)(-|\+)?([0-9]{4})?\)/", cleaned)
    if match:
        ms = int(match.group(1))
        epoch = datetime(1970, 1, 1, tzinfo=timezone.utc)
        dt = epoch.fromtimestamp(ms / 1000.0, tz=timezone.utc)
        if match.group(3):
            mod = datetime.strptime(match.group(3), "%H%M").time()
            offset = mod.hour * 3600 + mod.minute * 60
            if match.group(2) == "+":
                dt = dt.fromtimestamp(dt.timestamp() + offset, tz=timezone.utc)
            else:
                dt = dt.fromtimestamp(dt.timestamp() - offset, tz=timezone.utc)
        return dt

    new_date_match = re.search(r"new\s*Date\((-?\d+)\)", cleaned)
    if new_date_match:
        ms = int(new_date_match.group(1))
        epoch = datetime(1970, 1, 1, tzinfo=timezone.utc)
        return epoch.fromtimestamp(ms / 1000.0, tz=timezone.utc)

    for fmt in _JSON_DATE_FORMATS:
        try:
            return datetime.strptime(cleaned, fmt)
        except ValueError:
            continue
    return datetime.min


def get_public_properties(obj: Any) -> Iterable[tuple[str, Any]]:
    """Yield ``(name, value)`` pairs for the public attributes of ``obj``.

    Used by :py:meth:`RestRequest.add_object`. Honors ``__dict__`` and
    ``__slots__``; dataclasses work transparently via ``__dict__``.
    """
    if hasattr(obj, "__dataclass_fields__"):
        for name in obj.__dataclass_fields__:
            yield name, getattr(obj, name)
        return

    if hasattr(obj, "__dict__"):
        for name, value in obj.__dict__.items():
            if not name.startswith("_"):
                yield name, value
        return

    slots = getattr(obj, "__slots__", None)
    if slots:
        for name in slots:
            if not name.startswith("_"):
                yield name, getattr(obj, name, None)
