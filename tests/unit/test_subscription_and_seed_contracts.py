from cfip.domain.seed_contracts import DEFAULT_SEEDS
from cfip.domain.subscription_contracts import PLANS


def test_exactly_two_plans() -> None:
    assert {plan.id for plan in PLANS} == {"free", "pro"}


def test_seed_manifest_never_contains_raw_secrets() -> None:
    for identity in DEFAULT_SEEDS.identities:
        assert identity.secret_ref.startswith("secrets/")
        assert "secret" not in identity.display_name.lower()
