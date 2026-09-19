"""Provider-neutral contracts and capability catalog.

Catalog entries describe adapter targets; they are not claims of a live credentialed
integration. Every adapter must normalize into CFIP contracts.
"""
from typing import Literal
from pydantic import BaseModel, ConfigDict, Field

ProviderKind = Literal["market-data","broker","execution","news","fundamentals","ai","research"]
ProviderStatus = Literal["catalog","adapter","verified"]

class ProviderDescriptor(BaseModel):
    model_config = ConfigDict(extra="forbid")
    id: str = Field(min_length=1, max_length=80)
    name: str = Field(min_length=1, max_length=120)
    kind: ProviderKind
    status: ProviderStatus = "catalog"
    capabilities: tuple[str, ...] = ()
    credential_required: bool = True
    notes: str = ""

PROVIDER_CATALOG: tuple[ProviderDescriptor, ...] = (
    ProviderDescriptor(id="eodhd",name="EODHD",kind="market-data",capabilities=("forex","historical","fundamentals")),
    ProviderDescriptor(id="twelve-data",name="Twelve Data",kind="market-data",capabilities=("forex","historical","realtime")),
    ProviderDescriptor(id="finnhub",name="Finnhub",kind="market-data",capabilities=("forex","historical","realtime","news")),
    ProviderDescriptor(id="polygon",name="Polygon",kind="market-data",capabilities=("forex","historical","realtime")),
    ProviderDescriptor(id="alphavantage",name="Alpha Vantage",kind="market-data",capabilities=("forex","historical","fundamentals")),
    ProviderDescriptor(id="dukascopy",name="Dukascopy",kind="market-data",capabilities=("forex","historical","ticks")),
    ProviderDescriptor(id="truefx",name="TrueFX",kind="market-data",capabilities=("forex","ticks")),
    ProviderDescriptor(id="ccxt",name="CCXT",kind="market-data",status="adapter",capabilities=("crypto","ohlcv","orderbook","forex")),
    ProviderDescriptor(id="oanda",name="OANDA",kind="broker",capabilities=("forex","pricing","orders","accounts")),
    ProviderDescriptor(id="fxcm",name="FXCM",kind="broker",capabilities=("forex","pricing","orders","accounts")),
    ProviderDescriptor(id="ig",name="IG",kind="broker",capabilities=("forex","pricing","orders","accounts")),
    ProviderDescriptor(id="saxo",name="Saxo",kind="broker",capabilities=("forex","pricing","orders","accounts")),
    ProviderDescriptor(id="interactive-brokers",name="Interactive Brokers",kind="broker",capabilities=("forex","pricing","orders","accounts","market-depth")),
    ProviderDescriptor(id="ctrader",name="cTrader Open API",kind="broker",capabilities=("forex","pricing","orders","accounts","market-depth")),
    ProviderDescriptor(id="metatrader5",name="MetaTrader 5",kind="broker",capabilities=("forex","pricing","orders","accounts")),
    ProviderDescriptor(id="openai",name="OpenAI",kind="ai",capabilities=("reasoning","structured-output","embeddings")),
    ProviderDescriptor(id="anthropic",name="Anthropic",kind="ai",capabilities=("reasoning","structured-output")),
    ProviderDescriptor(id="gemini",name="Google Gemini",kind="ai",capabilities=("reasoning","structured-output","multimodal")),
    ProviderDescriptor(id="deepseek",name="DeepSeek",kind="ai",capabilities=("reasoning","structured-output")),
    ProviderDescriptor(id="groq",name="Groq",kind="ai",capabilities=("inference","structured-output")),
    ProviderDescriptor(id="openrouter",name="OpenRouter",kind="ai",capabilities=("multi-model-routing","structured-output")),
    ProviderDescriptor(id="ollama",name="Ollama",kind="ai",capabilities=("local-inference","embeddings")),
    ProviderDescriptor(id="lm-studio",name="LM Studio",kind="ai",capabilities=("local-inference","openai-compatible")),
    ProviderDescriptor(id="github",name="GitHub",kind="research",capabilities=("repositories","issues","pull-requests","code-review")),
    ProviderDescriptor(id="tavily",name="Tavily",kind="research",capabilities=("web-search","research")),
    ProviderDescriptor(id="exa",name="Exa",kind="research",capabilities=("web-search","research")),
    ProviderDescriptor(id="gdelt",name="GDELT",kind="news",capabilities=("news","events","research")),
)
