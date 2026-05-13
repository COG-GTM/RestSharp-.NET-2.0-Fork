"""Default XML serializer.

Mirrors ``RestSharp/Serializers/XmlSerializer.cs`` using
``xml.etree.ElementTree``. Properties annotated with the
:func:`SerializeAs` decorator are serialized as XML attributes
or use custom names.
"""

from __future__ import annotations

import dataclasses
from datetime import date, datetime
from decimal import Decimal
from typing import Any, Dict, Iterable, List, Tuple
from xml.etree.ElementTree import Element, SubElement, tostring


_SERIALIZE_AS = "_restsharp_serialize_as"


def SerializeAs(
    name: str | None = None,
    attribute: bool = False,
    index: int | None = None,
    transform: str | None = None,
):
    """Decorator/metadata factory mimicking ``SerializeAsAttribute``.

    Apply at the class level to override the XML element name or
    transformation. Per-field metadata is read directly from a
    ``_restsharp_serialize_as`` dict attribute on the instance.
    """

    def wrapper(target):
        setattr(
            target,
            _SERIALIZE_AS,
            {
                "name": name,
                "attribute": attribute,
                "index": index,
                "transform": transform,
            },
        )
        return target

    return wrapper


def _transform_name(name: str, transform: str | None) -> str:
    if not transform:
        return name
    if transform == "lower":
        return name.lower()
    if transform == "upper":
        return name.upper()
    if transform == "camel":
        return name[0].lower() + name[1:] if name else name
    return name


def _class_options(obj: Any) -> dict | None:
    return getattr(type(obj), _SERIALIZE_AS, None)


def _iter_properties(obj: Any) -> Iterable[Tuple[str, Any, Dict[str, Any] | None]]:
    """Yield ``(name, value, field_options)`` for each public attribute.

    ``field_options`` may include ``name``, ``attribute``, ``index``,
    ``transform`` keys when supplied via a ``_field_options`` dict
    on the object or via dataclass field metadata.
    """
    overrides: dict[str, dict[str, Any]] = getattr(obj, "_field_options", {}) or {}

    if dataclasses.is_dataclass(obj) and not isinstance(obj, type):
        for field in dataclasses.fields(obj):
            meta = dict(field.metadata or {})
            meta.update(overrides.get(field.name, {}))
            yield field.name, getattr(obj, field.name), meta or None
        return

    iterable = obj.__dict__.items() if hasattr(obj, "__dict__") else []
    for name, value in iterable:
        if name.startswith("_") or name == "_field_options":
            continue
        yield name, value, overrides.get(name)


class XmlSerializer:
    """Default XML serializer used when ``RestRequest.request_format == XML``."""

    def __init__(self, namespace: str | None = None) -> None:
        self.content_type: str = "text/xml"
        self.date_format: str | None = None
        self.root_element: str | None = None
        self.namespace: str | None = namespace

    def serialize(self, obj: Any) -> str:
        """Serialize ``obj`` as XML and return the result as a string."""
        class_opts = _class_options(obj)
        name = type(obj).__name__
        if class_opts and class_opts.get("name"):
            name = class_opts["name"]
        name = _transform_name(name, (class_opts or {}).get("transform"))
        root = self._make_element(name)

        if isinstance(obj, (list, tuple)):
            for item in obj:
                item_opts = _class_options(item) or {}
                item_name = item_opts.get("name") or type(item).__name__
                instance = SubElement(root, item_name)
                self._map(instance, item)
        else:
            self._map(root, obj)

        if self.root_element:
            wrapper = self._make_element(self.root_element)
            wrapper.append(root)
            root = wrapper

        xml_bytes = tostring(root, encoding="utf-8", xml_declaration=True)
        return xml_bytes.decode("utf-8")

    def _make_element(self, name: str) -> Element:
        if self.namespace:
            return Element(f"{{{self.namespace}}}{name}")
        return Element(name)

    def _map(self, root: Element, obj: Any) -> None:
        items: List[Tuple[str, Any, Dict[str, Any] | None]] = list(_iter_properties(obj))
        items.sort(
            key=lambda triple: (
                triple[2]["index"]
                if triple[2] and triple[2].get("index") is not None
                else 1 << 31
            )
        )
        class_opts = _class_options(obj) or {}
        for name, value, opts in items:
            if value is None:
                continue
            opts = opts or {}
            use_attribute = bool(opts.get("attribute"))
            element_name = opts.get("name") or name
            transform = opts.get("transform") or class_opts.get("transform")
            element_name = _transform_name(element_name, transform)

            if isinstance(value, (str, int, float, bool, datetime, date, Decimal)):
                serialized = self._serialize_value(value)
                if use_attribute:
                    root.set(element_name, serialized)
                    continue
                element = self._make_element(element_name)
                element.text = serialized
                root.append(element)
            elif isinstance(value, (list, tuple)):
                element = self._make_element(element_name)
                for item in value:
                    item_name = type(item).__name__
                    item_opts = _class_options(item) or {}
                    if item_opts.get("name"):
                        item_name = item_opts["name"]
                    sub = SubElement(element, item_name)
                    self._map(sub, item)
                root.append(element)
            else:
                element = self._make_element(element_name)
                self._map(element, value)
                root.append(element)

    def _serialize_value(self, value: Any) -> str:
        if isinstance(value, bool):
            return "true" if value else "false"
        if isinstance(value, datetime) and self.date_format:
            return value.strftime(self.date_format)
        return str(value)
