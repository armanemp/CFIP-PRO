from typing import Literal
from pydantic import BaseModel, ConfigDict, Field

NotificationChannel = Literal["in_app", "browser", "email", "webhook"]
NotificationSeverity = Literal["info", "success", "warning", "critical"]

class Notification(BaseModel):
    model_config = ConfigDict(extra="forbid")
    id: str = Field(min_length=1, max_length=128)
    channel: NotificationChannel
    severity: NotificationSeverity = "info"
    title: str = Field(min_length=1, max_length=200)
    body: str = Field(min_length=1, max_length=4000)
    symbol: str | None = None
    correlation_id: str
    dedupe_key: str | None = None
    read: bool = False
