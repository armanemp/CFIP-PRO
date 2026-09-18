# CFIP-PRO — OSS Integration Register

Status: active engineering policy
Date: 2026-09-18

## Selection rule

CFIP-PRO uses mature OSS where it removes substantial bespoke implementation without compromising the platform's commercial model, Python 3.14 baseline, causal market semantics, or architectural independence.

OSS is an implementation component, not the CFIP decision authority. CFIP owns the canonical analysis contract, evidence model, confluence gates, governance and product semantics.

## Integrated

### TA-Lib

- Package: TA-Lib==0.8.0
- Role: classical OHLCV indicators in the backend analysis engine.
- Used for: EMA, RSI, ATR, ADX/+DI/-DI, MACD and Bollinger Bands.
- Reason: removes duplicated numerical indicator code while retaining deterministic, testable outputs.
- Python baseline: compatible with the project's Python 3.14 target.
- CFIP-specific logic remains outside TA-Lib: FVG lifecycle, liquidity, structure, displacement, premium/discount and confluence.

## Evaluated but deliberately not embedded

### VectorBT

VectorBT is technically attractive for large-scale research/backtesting and its current community release supports Python 3.14. However, its community edition is distributed under Apache 2.0 with Commons Clause rather than a conventional permissive commercial license. CFIP therefore does not make VectorBT a runtime dependency or product-core component.

It remains an evaluated research option. A future internal research environment can use it only after explicit licensing review and isolation from the distributable product.

### Backtesting.py

Backtesting.py is lightweight and actively maintained enough for experimentation, but its current project license is AGPL-3.0. It is therefore not embedded in the CFIP product runtime.

### Backtrader

Backtrader is mature and feature-rich, but its GPLv3+ license and older architecture make it unsuitable as a product-core dependency for the current CFIP direction.

## Engineering consequence

The backtest/replay boundary will remain CFIP-owned until a permissively licensed, Python 3.14-compatible engine can be integrated without creating a licensing or semantic constraint. The API boundary will be designed so an external research adapter can be added later without changing terminal or signal contracts.

## Current dependency policy

- Prefer permissive licenses suitable for a commercial product.
- Prefer current Python 3.14 support.
- Prefer stable releases over development branches.
- Do not add an OSS package solely because it has more features.
- Do not duplicate a mature numerical implementation inside CFIP.
- Keep CFIP-specific market-structure semantics explicit and auditable.
