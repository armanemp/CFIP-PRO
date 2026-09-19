from cfip.application.event_bus import EventBus
from cfip.application.paper_oms import PaperOMS
from cfip.application.runtime_components import EventBusComponent, IntelligenceRuntimeComponent, MarketRuntimeComponent, ProviderRegistryComponent, TerminalRuntimeComponent
from cfip.application.runtime_supervisor import RuntimeSupervisor
from cfip.domain.execution_contracts import OrderRequest

import pytest

@pytest.mark.asyncio
async def test_runtime_starts_dependency_ordered_components():
    bus = EventBus()
    supervisor = RuntimeSupervisor((
        EventBusComponent(bus), ProviderRegistryComponent(), IntelligenceRuntimeComponent(),
        MarketRuntimeComponent(), TerminalRuntimeComponent(),
    ))
    snapshot = await supervisor.start()
    assert snapshot.status == "ready"
    assert {item.component for item in snapshot.components} == {"event-bus","provider-registry","elyrava-intelligence","market-data","terminal"}
    await supervisor.stop()
    assert supervisor.snapshot().status == "stopped"

@pytest.mark.asyncio
async def test_event_bus_round_trip():
    bus = EventBus()
    stream = bus.subscribe("market.quote")
    waiter = stream.__anext__()
    await bus.publish("market.quote", {"symbol":"EUR/USD","last":1.1})
    assert await waiter == {"symbol":"EUR/USD","last":1.1}
    await stream.aclose()

def test_paper_oms_is_idempotent_and_rejects_live_mode():
    oms = PaperOMS()
    request = OrderRequest(client_order_id="x1",provider_id="paper",symbol="EUR/USD",side="buy",order_type="market",quantity=1000,correlation_id="c1")
    first = oms.submit(request)
    second = oms.submit(request)
    assert first == second and first.accepted
    live = request.model_copy(update={"client_order_id":"x2","mode":"live"})
    assert not oms.submit(live).accepted
