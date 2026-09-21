"""Optional trafilatura research adapter with fail-closed URL policy."""
from __future__ import annotations

import hashlib
import ipaddress
import socket
from datetime import UTC, datetime
from urllib.parse import urlparse

from cfip.domain.research_contracts import ResearchDocument, ResearchRequest


class TrafilaturaUnavailable(RuntimeError):
    pass


def _safe_url(url: str) -> None:
    parsed = urlparse(url)
    if parsed.scheme not in {"http", "https"} or not parsed.hostname:
        raise ValueError("unsupported_research_url")
    host = parsed.hostname.rstrip(".").lower()
    if host in {"localhost", "metadata.google.internal"}:
        raise ValueError("blocked_research_host")
    try:
        infos = socket.getaddrinfo(host, parsed.port or (443 if parsed.scheme == "https" else 80), type=socket.SOCK_STREAM)
    except OSError as exc:
        raise ValueError("research_host_resolution_failed") from exc
    for info in infos:
        address = ipaddress.ip_address(info[4][0])
        if address.is_private or address.is_loopback or address.is_link_local or address.is_reserved:
            raise ValueError("blocked_private_research_address")


def _version(module: object) -> str:
    return str(getattr(module, "__version__", "unknown"))


class TrafilaturaResearchAdapter:
    id = "trafilatura"

    def available(self) -> bool:
        try:
            import trafilatura  # type: ignore
            return trafilatura is not None
        except ImportError:
            return False

    @property
    def version(self) -> str:
        try:
            import trafilatura  # type: ignore
            return _version(trafilatura)
        except ImportError:
            return "unavailable"

    def extract(self, request: ResearchRequest) -> ResearchDocument:
        _safe_url(request.url)
        try:
            import trafilatura  # type: ignore
        except ImportError as exc:
            raise TrafilaturaUnavailable("trafilatura_not_installed") from exc
        downloaded = trafilatura.fetch_url(request.url)
        if not downloaded:
            raise RuntimeError("research_fetch_failed")
        text = trafilatura.extract(downloaded, include_comments=False, include_tables=True)
        if not text:
            raise RuntimeError("research_extraction_failed")
        digest = hashlib.sha256(text.encode("utf-8")).hexdigest()
        return ResearchDocument(
            url=request.url,
            text=text,
            content_digest=digest,
            extractor=self.id,
            extractor_version=self.version,
            fetched_at_epoch=int(datetime.now(UTC).timestamp()),
        )
