"""JSON deserializer with fuzzy name matching.

Ports ``RestSharp/Deserializers/JsonDeserializer.cs``. Supports
mapping JSON keys to Python attribute names using a series of
case/underscore variants (``product_id`` ↔ ``ProductId``).
"""

from __future__ import annotations

import dataclasses
import json
import typing
from datetime import datetime
from decimal import Decimal
from enum import Enum
from typing import Any, Dict, List, Type, get_args, get_origin, get_type_hints

from ..extensions import get_name_variants, parse_json_date


_BUILTIN_TYPES: Dict[str, Type[Any]] = {
    "int": int,
    "float": float,
    "str": str,
    "bool": bool,
    "bytes": bytes,
    "Decimal": Decimal,
    "datetime": datetime,
}


def _resolve_string_annotation(value: str) -> Any:
    """Best-effort resolution of a stringified type annotation."""
    return _BUILTIN_TYPES.get(value, value)


class JsonDeserializer:
    """Default JSON deserializer."""

    def __init__(self) -> None:
        self.root_element: str | None = None
        self.namespace: str | None = None
        self.date_format: str | None = None

    def deserialize(self, response, target_type: Type[Any]) -> Any:
        """Deserialize ``response.content`` into ``target_type``."""
        if response is None or not getattr(response, "content", None):
            return self._default_instance(target_type)

        text = response.content
        if isinstance(text, bytes):
            text = text.decode("utf-8")
        parsed = json.loads(text)

        root = self._find_root(parsed)

        if target_type in (dict, Dict) or get_origin(target_type) is dict:
            return self._build_dict(target_type, root)
        if target_type in (list, List) or get_origin(target_type) is list:
            iterable = root if isinstance(root, list) else [root]
            return self._build_list(target_type, iterable)

        target = self._default_instance(target_type)
        if isinstance(target, list):
            iterable = root if isinstance(root, list) else [root]
            return self._build_list(target_type, iterable)
        if isinstance(target, dict):
            return self._build_dict(target_type, root)

        self._map(target, root)
        return target

    def _find_root(self, content: Any) -> Any:
        if self.root_element and isinstance(content, dict):
            return content.get(self.root_element, content)
        return content

    def _default_instance(self, target_type: Type[Any]) -> Any:
        if target_type in (list, List) or get_origin(target_type) is list:
            return []
        if target_type in (dict, Dict) or get_origin(target_type) is dict:
            return {}
        try:
            return target_type()
        except TypeError:
            # dataclasses with required positional args
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

    def _target_attributes(self, target: Any) -> Dict[str, Type[Any]]:
        # Always prefer resolved type hints so that ``from __future__ import annotations``
        # in user code doesn't leave us with stringified types.
        try:
            hints = get_type_hints(type(target))
        except Exception:
            hints = {}
        if dataclasses.is_dataclass(target) and not isinstance(target, type):
            return {f.name: hints.get(f.name, f.type) for f in dataclasses.fields(target)}
        if hints:
            return {n: t for n, t in hints.items() if not n.startswith("_")}
        return {n: type(v) for n, v in vars(target).items() if not n.startswith("_")}

    def _map(self, target: Any, data: Any) -> None:
        if not isinstance(data, dict):
            return
        for attr_name, attr_type in self._target_attributes(target).items():
            value = self._find_value(data, attr_name)
            if value is None:
                continue
            converted = self._convert(value, attr_type)
            setattr(target, attr_name, converted)

    def _find_value(self, data: Dict[str, Any], name: str) -> Any:
        for variant in get_name_variants(name):
            if variant in data and data[variant] is not None:
                return data[variant]
        return None

    def _convert(self, value: Any, target_type: Type[Any]) -> Any:
        if target_type is None or target_type is Any or value is None:
            return value

        # Unwrap Optional[X] / Union[X, None] / strings via typing
        origin = get_origin(target_type)
        args = get_args(target_type)
        if origin is typing.Union:
            non_none = [a for a in args if a is not type(None)]  # noqa: E721
            if len(non_none) == 1:
                target_type = non_none[0]
                origin = get_origin(target_type)
                args = get_args(target_type)

        if isinstance(target_type, str):
            target_type = _resolve_string_annotation(target_type)
            if isinstance(target_type, str):
                return value
            origin = get_origin(target_type)
            args = get_args(target_type)

        if origin is list and args:
            inner = args[0]
            return [self._convert(item, inner) for item in value or []]
        if origin is dict and len(args) == 2:
            _, val_type = args
            return {k: self._convert(v, val_type) for k, v in (value or {}).items()}

        try:
            if isinstance(target_type, type):
                if issubclass(target_type, bool):
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
                    return self._find_enum_value(target_type, value)
                if issubclass(target_type, datetime):
                    if self.date_format:
                        return datetime.strptime(str(value), self.date_format)
                    return parse_json_date(str(value))
        except TypeError:
            pass

        # Nested object
        if isinstance(target_type, type) and isinstance(value, dict):
            instance = self._default_instance(target_type)
            self._map(instance, value)
            return instance
        return value

    @staticmethod
    def _find_enum_value(enum_type: Type[Enum], value: Any) -> Enum | Any:
        text = str(value).lower()
        for member in enum_type:
            if member.name.lower() == text or str(member.value).lower() == text:
                return member
        return value

    def _build_list(self, target_type: Type[Any], items: Any) -> List[Any]:
        args = get_args(target_type)
        inner = args[0] if args else None
        if inner is None or not isinstance(inner, type):
            return list(items or [])
        return [self._convert(item, inner) for item in items or []]

    def _build_dict(self, target_type: Type[Any], items: Any) -> Dict[Any, Any]:
        args = get_args(target_type)
        if not args or len(args) != 2:
            return dict(items or {})
        _, value_type = args
        return {k: self._convert(v, value_type) for k, v in (items or {}).items()}
