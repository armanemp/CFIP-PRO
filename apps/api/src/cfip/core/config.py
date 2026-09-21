"""Application configuration."""

from functools import lru_cache

from pydantic import Field
from pydantic_settings import BaseSettings, SettingsConfigDict

from cfip.domain.intelligence_identity import DEFAULT_INTELLIGENCE_NAME


class Settings(BaseSettings):
    model_config = SettingsConfigDict(env_file=".env", env_file_encoding="utf-8", extra="ignore")

    app_name: str = Field(default="CFIP-PRO", alias="APP_NAME")
    app_env: str = Field(default="development", alias="APP_ENV")
    app_version: str = Field(default="0.1.0", alias="APP_VERSION")
    api_host: str = Field(default="127.0.0.1", alias="API_HOST")
    api_port: int = Field(default=8000, alias="API_PORT")
    cors_origins: str = Field(default="http://localhost:3000", alias="CORS_ORIGINS")
    trusted_hosts: str = Field(default="localhost,127.0.0.1", alias="TRUSTED_HOSTS")
    security_headers_enabled: bool = Field(default=True, alias="SECURITY_HEADERS_ENABLED")
    hsts_enabled: bool = Field(default=False, alias="HSTS_ENABLED")
    database_url: str = Field(
        default="postgresql+asyncpg://cfip:cfip@127.0.0.1:5432/cfip", alias="DATABASE_URL"
    )
    nats_url: str = Field(default="nats://127.0.0.1:4222", alias="NATS_URL")
    redis_url: str = Field(default="redis://127.0.0.1:6379/0", alias="REDIS_URL")
    intelligence_name: str = Field(default=DEFAULT_INTELLIGENCE_NAME, alias="INTELLIGENCE_NAME")

    @property
    def cors_origin_list(self) -> list[str]:
        return [origin.strip() for origin in self.cors_origins.split(",") if origin.strip()]

    @property
    def trusted_host_list(self) -> list[str]:
        return [host.strip() for host in self.trusted_hosts.split(",") if host.strip()]


@lru_cache(maxsize=1)
def get_settings() -> Settings:
    return Settings()
