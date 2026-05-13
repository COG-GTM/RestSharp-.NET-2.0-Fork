"""XML deserializer with namespace flattening and fuzzy name matching.

Ports ``RestSharp/Deserializers/XmlDeserializer.cs`` using
``xml.etree.ElementTree``. By default, namespace prefixes are
stripped from element/attribute names when ``namespace`` is not
specified.
"""

from __future__ import annotations

import dataclasses
import re
import typing
from datetime import datetime
from decimal import Decimal
from enum import Enum
from typing import Any, Dict, List, Type, get_args, get_origin, get_type_hints
from xml.etree.ElementTree import Element, fromstring

from ..extensions import get_name_variants, parse_json_date
from .json_deserializer import _resolve_string_annotation


_NAMESPACE_RE = re.compile(r"^\{[^}]+\}")


def _strip_namespace(tag: str) -> str:
    return _NAMESPACE_RE.sub("", tag)


class XmlDeserializer:
    """Default XML deserializer."""

    def __init__(self) -> None:
        self.root_element: str | None = None
        self.namespace: str | None = None
        self.date_format: str | None = None

    def deserialize(self, response, target_type: Type[Any]) -> Any:
        if response is None or not getattr(response, "content", None):
            return self._default_instance(target_type)

        text = response.content
        if isinstance(text, bytes):
            text = text.decode("utf-8")

        root = fromstring(text)
        if self.root_element:
            found = self._find_by_name(root, self.root_element)
            if found is not None:
                root = found
        return self._build(target_type, root)

    def _default_instance(self, target_type: Type[Any]) -> Any:
        if target_type in (list, List) or get_origin(target_type) is list:
            return []
        if target_type in (dict, Dict) or get_origin(target_type) is dict:
            return {}
        try:
            return target_type()
        except TypeError:
            if dataclasses.is_dataclass(target_type):
                kwargs = {}
                for field in dataclasses.fields(target_type):
                    if field.default is not dataclasses.MISSING:
                        kwargs[field.name] = field.default
                    elif field.default_factory is not dataclasses.MISSING:  # type: ignore[misc]
                        kwargs[field.name] = field.default_factory()  # type: ignore[misc]
                    else:
                        kwargs[field.name] = None
                return target_type(**kwargs)
            raise

    def _matches(self, element_or_attr_name: str, name: str) -> bool:
        stripped = _strip_namespace(element_or_attr_name)
        for variant in get_name_variants(name):
            if stripped == variant:
                return True
        return False

    def _find_by_name(self, element: Element, name: str) -> Element | None:
        for child in element:
            if self._matches(child.tag, name):
                return child
        return None

    def _find_attr(self, element: Element, name: str) -> str | None:
        for attr_name, attr_value in element.attrib.items():
            if self._matches(attr_name, name):
                return attr_value
        return None

    def _target_attributes(self, target: Any) -> Dict[str, Type[Any]]:
        try:
            hints = get_type_hints(type(target))
        except Exception:
            hints = {}
        if dataclasses.is_dataclass(target) and not isinstance(target, type):
            return {f.name: hints.get(f.name, f.type) for f in dataclasses.fields(target)}
        if hints:
            return {n: t for n, t in hints.items() if not n.startswith("_")}
        return {n: type(v) for n, v in vars(target).items() if not n.startswith("_")}

    def _build(self, target_type: Type[Any], element: Element) -> Any:
        origin = get_origin(target_type)
        args = get_args(target_type)

        if origin is typing.Union:
            non_none = [a for a in args if a is not type(None)]  # noqa: E721
            if len(non_none) == 1:
                target_type = non_none[0]
                origin = get_origin(target_type)
                args = get_args(target_type)

        if origin is list or target_type in (list, List):
            inner = args[0] if args else None
            return [self._build(inner, child) for child in element]
        if origin is dict or target_type in (dict, Dict):
            result: Dict[str, Any] = {}
            for child in element:
                result[_strip_namespace(child.tag)] = (
                    child.text if not list(child) else self._build(dict, child)
                )
            return result

        if isinstance(target_type, type) and issubclass(target_type, (str, int, float, bool, Decimal)):
            return self._convert(element.text, target_type)
        if isinstance(target_type, type) and issubclass(target_type, datetime):
            return self._convert(element.text, target_type)

        if target_type is None:
            return element.text

        target = self._default_instance(target_type)
        for attr_name, attr_type in self._target_attributes(target).items():
            raw_value: Any = None
            attr_value = self._find_attr(element, attr_name)
            if attr_value is not None:
                raw_value = attr_value
            else:
                child = self._find_by_name(element, attr_name)
                if child is not None:
                    if isinstance(attr_type, type) and not list(child):
                        raw_value = child.text
                    elif get_origin(attr_type) is list:
                        inner_args = get_args(attr_type)
                        inner = inner_args[0] if inner_args else None
                        raw_value = [self._build(inner, sub) for sub in child]
                    else:
                        raw_value = self._build(attr_type, child)
            if raw_value is None:
                continue
            converted = self._convert(raw_value, attr_type)
            setattr(target, attr_name, converted)
        return target

    def _convert(self, value: Any, target_type: Type[Any]) -> Any:
        if target_type is None or target_type is Any or value is None:
            return value

        origin = get_origin(target_type)
        args = get_args(target_type)
        if origin is typing.Union:
            non_none = [a for a in args if a is not type(None)]  # noqa: E721
            if len(non_none) == 1:
                target_type = non_none[0]

        if isinstance(target_type, str):
            target_type = _resolve_string_annotation(target_type)
            if isinstance(target_type, str):
                return value

        try:
            if isinstance(target_type, type):
                if issubclass(target_type, bool):
                    if isinstance(value, str):
                        return value.strip().lower() in ("true", "1", "yes")
                    return bool(value)
                if issubclass(target_type, int):
                    return int(value)
                if issubclass(target_type, float):
                    return float(value)
                if issubclass(target_type, Decimal):
                    return Decimal(str(value))
                if issubclass(target_type, str):
                    return str(value)
                if issubclass(target_type, Enum):
                    text = str(value).lower()
                    for member in target_type:
                        if member.name.lower() == text or str(member.value).lower() == text:
                            return member
                    return value
                if issubclass(target_type, datetime):
                    if self.date_format:
                        return datetime.strptime(str(value), self.date_format)
                    return parse_json_date(str(value))
        except TypeError:
            pass
        return value
