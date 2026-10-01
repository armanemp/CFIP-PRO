from cfip.domain.analysis_contracts import AnalysisEvidence, AnalysisRequest, aggregate_analysis


def _evidence(direction: str, strength: float, identifier: str) -> AnalysisEvidence:
    return AnalysisEvidence(id=identifier, kind="indicator", label=identifier, direction=direction, strength=strength, source="test", observed_at=1)


def test_analysis_aggregates_confluence_without_provider_types() -> None:
    result = aggregate_analysis(AnalysisRequest(symbol="EURUSD", timeframe="1H", evidence=[_evidence("bullish", .8, "ema"), _evidence("bullish", .7, "structure")]))
    assert result.direction == "bullish"
    assert result.confluence == .75
    assert result.final_answer_available is True
    assert "minimum_confluence" in result.passed_gates


def test_analysis_blocks_when_evidence_is_weak() -> None:
    result = aggregate_analysis(AnalysisRequest(symbol="EURUSD", timeframe="1H", evidence=[_evidence("bullish", .2, "ema")], minimum_confluence=.5))
    assert result.final_answer_available is False
    assert result.blocked_reasons == ["minimum_confluence_not_met"]
