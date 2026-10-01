"""Add idempotency constraint for intelligence feedback."""

from collections.abc import Sequence

from alembic import op

revision: str = "0004_feedback_idempotency"
down_revision: str | Sequence[str] | None = "0003_outcome_analytics"
branch_labels: str | Sequence[str] | None = None
depends_on: str | Sequence[str] | None = None


def upgrade() -> None:
    op.create_unique_constraint(
        "uq_intelligence_feedback_idempotency",
        "intelligence_feedback",
        ["learning_id", "reviewer", "observed_at"],
    )


def downgrade() -> None:
    op.drop_constraint(
        "uq_intelligence_feedback_idempotency",
        "intelligence_feedback",
        type_="unique",
    )
