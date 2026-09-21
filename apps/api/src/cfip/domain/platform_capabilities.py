"""Canonical CFIP capability registry.

The registry is descriptive metadata only. Runtime implementations remain behind
domain/application contracts so the UI, admin plane and intelligence layer do not
duplicate feature truth.
"""
from typing import Literal
from pydantic import BaseModel, ConfigDict, Field

CapabilityKind = Literal["terminal","market","analysis","trading","intelligence","research","platform","admin"]
CapabilityMaturity = Literal["native","integrated","contract","adapter","planned"]

class PlatformCapability(BaseModel):
    model_config = ConfigDict(extra="forbid")
    id: str = Field(min_length=3, max_length=120)
    kind: CapabilityKind
    maturity: CapabilityMaturity
    contract: str = Field(min_length=3, max_length=160)
    dependencies: tuple[str, ...] = ()
    configurable: bool = True
    evidence_required: bool = False

PLATFORM_CAPABILITIES: tuple[PlatformCapability, ...] = (
    PlatformCapability(id="terminal.chart", kind="terminal", maturity="native", contract="terminal.chart.v1"),
    PlatformCapability(id="terminal.indicators", kind="terminal", maturity="native", contract="terminal.indicator.v1"),
    PlatformCapability(id="terminal.drawings", kind="terminal", maturity="native", contract="terminal.object.v1"),
    PlatformCapability(id="terminal.replay", kind="terminal", maturity="native", contract="market.replay.v1"),
    PlatformCapability(id="terminal.alerts", kind="terminal", maturity="contract", contract="notification.alert.v1"),
    PlatformCapability(id="market.normalized-data", kind="market", maturity="integrated", contract="market.observation.v1"),
    PlatformCapability(id="market.providers", kind="market", maturity="integrated", contract="provider.catalog.v1"),
    PlatformCapability(id="market.failover", kind="market", maturity="integrated", contract="provider.health.v1"),
    PlatformCapability(id="analysis.unified", kind="analysis", maturity="integrated", contract="analysis.unified.v1", evidence_required=True),
    PlatformCapability(id="analysis.structure", kind="analysis", maturity="native", contract="analysis.structure.v1"),
    PlatformCapability(id="analysis.fvg", kind="analysis", maturity="native", contract="analysis.fvg.v1"),
    PlatformCapability(id="analysis.order-blocks", kind="analysis", maturity="native", contract="analysis.order-block.v1"),
    PlatformCapability(id="analysis.mtf", kind="analysis", maturity="contract", contract="analysis.mtf.v1"),
    PlatformCapability(id="analysis.liquidity", kind="analysis", maturity="native", contract="analysis.liquidity.v1"),
    PlatformCapability(id="trading.risk", kind="trading", maturity="integrated", contract="risk.target.v1"),
    PlatformCapability(id="trading.outcomes", kind="trading", maturity="integrated", contract="signal.outcome.v1", evidence_required=True),
    PlatformCapability(id="trading.journal", kind="trading", maturity="contract", contract="trading.journal.v1"),
    PlatformCapability(id="intelligence.core", kind="intelligence", maturity="contract", contract="intelligence.core.v1", evidence_required=True),
    PlatformCapability(id="intelligence.agent-runtime", kind="intelligence", maturity="integrated", contract="intelligence.agent.v1", dependencies=("pydantic-ai",), evidence_required=True),
    PlatformCapability(id="intelligence.tools", kind="intelligence", maturity="contract", contract="intelligence.tool.v1", evidence_required=True),
    PlatformCapability(id="intelligence.models", kind="intelligence", maturity="contract", contract="intelligence.model.v1", evidence_required=True),
    PlatformCapability(id="intelligence.memory", kind="intelligence", maturity="contract", contract="intelligence.memory.v1", evidence_required=True),
    PlatformCapability(id="intelligence.research", kind="intelligence", maturity="contract", contract="intelligence.research.v1", evidence_required=True),
    PlatformCapability(id="intelligence.learning", kind="intelligence", maturity="integrated", contract="learning.feedback.v1", evidence_required=True),
    PlatformCapability(id="intelligence.self-development", kind="intelligence", maturity="contract", contract="evolution.change.v1", evidence_required=True),
    PlatformCapability(id="intelligence.self-healing", kind="intelligence", maturity="integrated", contract="health.remediation.v1", evidence_required=True),
    PlatformCapability(id="research.fabric", kind="research", maturity="contract", contract="research.evidence.v1", evidence_required=True),
    PlatformCapability(id="research.oss-adapters", kind="research", maturity="contract", contract="oss.adapter.v1"),
    PlatformCapability(id="platform.git-governance", kind="platform", maturity="integrated", contract="git.governance.v1", evidence_required=True),
    PlatformCapability(id="platform.i18n", kind="platform", maturity="native", contract="experience.locale.v1"),
    PlatformCapability(id="admin.control-plane", kind="admin", maturity="integrated", contract="control-plane.v1"),
)

def capabilities(kind: CapabilityKind | None = None) -> tuple[PlatformCapability, ...]:
    if kind is None:
        return PLATFORM_CAPABILITIES
    return tuple(item for item in PLATFORM_CAPABILITIES if item.kind == kind)
