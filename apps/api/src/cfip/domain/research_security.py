"""Fail-closed research URL policy to reduce SSRF and metadata-service exposure."""
from urllib.parse import urlparse
from pydantic import BaseModel, ConfigDict

class ResearchURLPolicy(BaseModel):
    model_config = ConfigDict(extra="forbid")
    allowed_schemes: tuple[str, ...] = ("https",)
    allow_private_networks: bool = False
    allow_credentials: bool = False
    max_url_length: int = 4096

    def validate(self, url: str) -> str:
        if len(url) > self.max_url_length:
            raise ValueError("research_url_too_long")
        parsed = urlparse(url)
        if parsed.scheme not in self.allowed_schemes or not parsed.hostname:
            raise ValueError("research_url_scheme_or_host_rejected")
        if parsed.username or parsed.password:
            if not self.allow_credentials:
                raise ValueError("research_url_credentials_rejected")
        host = parsed.hostname.lower().rstrip(".")
        if not self.allow_private_networks and host in {"localhost", "metadata.google.internal"}:
            raise ValueError("research_url_private_host_rejected")
        return url
