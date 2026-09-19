"""Crypto-only payment boundary for Free/Pro entitlements.

Payment provider integrations are adapters. No private keys or wallet secrets live here.
"""
from typing import Literal
from pydantic import BaseModel, ConfigDict, Field

CryptoAsset = Literal["BTC","ETH","USDC","USDT"]

class PaymentIntent(BaseModel):
    model_config = ConfigDict(extra="forbid")
    id: str
    user_id: str
    plan_id: Literal["free","pro"]
    asset: CryptoAsset
    amount_atomic: int = Field(ge=0)
    checkout_reference: str
    idempotency_key: str = Field(min_length=8, max_length=256)

class PaymentSettlement(BaseModel):
    model_config = ConfigDict(extra="forbid")
    intent_id: str
    status: Literal["pending","confirmed","failed","expired","reconciled"]
    provider_reference: str | None = None
    confirmations: int = Field(default=0, ge=0)
    settled_at: int | None = Field(default=None, gt=0)
    audit_reference: str | None = None
