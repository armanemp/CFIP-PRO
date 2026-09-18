"""Persist signal lifecycle, outcome, calibration and drift artifacts."""

from collections.abc import Sequence

import sqlalchemy as sa
from alembic import op
from sqlalchemy.dialects import postgresql

revision: str = "0003_outcomes"
down_revision: str | Sequence[str] | None = "0002_intelligence"
branch_labels: str | Sequence[str] | None = None
depends_on: str | Sequence[str] | None = None


def upgrade() -> None:
    op.create_table(
        "signal_lifecycles",
        sa.Column("id", postgresql.UUID(as_uuid=True), nullable=False),
        sa.Column("signal_id", sa.String(length=160), nullable=False),
        sa.Column("symbol", sa.String(length=64), nullable=False),
        sa.Column("timeframe", sa.String(length=16), nullable=False),
        sa.Column("direction", sa.String(length=8), nullable=False),
        sa.Column("decision_time", sa.BigInteger(), nullable=False),
        sa.Column("state", sa.String(length=16), nullable=False),
        sa.Column("emitted_at", sa.BigInteger(), nullable=True),
        sa.Column("triggered_at", sa.BigInteger(), nullable=True),
        sa.Column("closed_at", sa.BigInteger(), nullable=True),
        sa.Column("expires_at", sa.BigInteger(), nullable=True),
        sa.Column("analysis_id", sa.String(length=128), nullable=True),
        sa.Column("entry", sa.Numeric(38, 18), nullable=True),
        sa.Column("stop", sa.Numeric(38, 18), nullable=True),
        sa.Column("tp1", sa.Numeric(38, 18), nullable=True),
        sa.Column("tp2", sa.Numeric(38, 18), nullable=True),
        sa.Column("tp3", sa.Numeric(38, 18), nullable=True),
        sa.Column("evidence_ids", postgresql.JSONB(astext_type=sa.Text()), nullable=False),
        sa.Column(
            "created_at",
            sa.DateTime(timezone=True),
            server_default=sa.func.now(),
            nullable=False,
        ),
        sa.PrimaryKeyConstraint("id"),
        sa.UniqueConstraint("signal_id", name="uq_signal_lifecycles_signal_id"),
    )
    op.create_index(
        "ix_signal_lifecycles_symbol_timeframe_decision",
        "signal_lifecycles",
        ["symbol", "timeframe", "decision_time"],
    )
    op.create_index("ix_signal_lifecycles_state", "signal_lifecycles", ["state"])

    op.create_table(
        "signal_outcome_events",
        sa.Column("id", postgresql.UUID(as_uuid=True), nullable=False),
        sa.Column("event_key", sa.String(length=256), nullable=False),
        sa.Column("signal_id", sa.String(length=160), nullable=False),
        sa.Column("event_type", sa.String(length=16), nullable=False),
        sa.Column("event_time", sa.BigInteger(), nullable=False),
        sa.Column("price", sa.Numeric(38, 18), nullable=True),
        sa.Column(
            "payload",
            postgresql.JSONB(astext_type=sa.Text()),
            nullable=False,
        ),
        sa.Column(
            "created_at",
            sa.DateTime(timezone=True),
            server_default=sa.func.now(),
            nullable=False,
        ),
        sa.PrimaryKeyConstraint("id"),
        sa.UniqueConstraint(
            "event_key", name="uq_signal_outcome_events_event_key"
        ),
    )
    op.create_index(
        "ix_signal_outcome_events_signal_time",
        "signal_outcome_events",
        ["signal_id", "event_time"],
    )

    op.create_table(
        "signal_outcomes",
        sa.Column("id", postgresql.UUID(as_uuid=True), nullable=False),
        sa.Column("signal_id", sa.String(length=160), nullable=False),
        sa.Column("label", sa.String(length=16), nullable=False),
        sa.Column("event_type", sa.String(length=16), nullable=False),
        sa.Column("decision_time", sa.BigInteger(), nullable=False),
        sa.Column("evaluation_time", sa.BigInteger(), nullable=False),
        sa.Column("entry", sa.Numeric(38, 18), nullable=False),
        sa.Column("stop", sa.Numeric(38, 18), nullable=True),
        sa.Column("tp1", sa.Numeric(38, 18), nullable=True),
        sa.Column("tp2", sa.Numeric(38, 18), nullable=True),
        sa.Column("tp3", sa.Numeric(38, 18), nullable=True),
        sa.Column("mfe_r", sa.Numeric(20, 10), nullable=True),
        sa.Column("mae_r", sa.Numeric(20, 10), nullable=True),
        sa.Column("realized_r", sa.Numeric(20, 10), nullable=True),
        sa.Column(
            "attribution",
            postgresql.JSONB(astext_type=sa.Text()),
            nullable=False,
        ),
        sa.Column(
            "evidence_ids",
            postgresql.JSONB(astext_type=sa.Text()),
            nullable=False,
        ),
        sa.Column(
            "created_at",
            sa.DateTime(timezone=True),
            server_default=sa.func.now(),
            nullable=False,
        ),
        sa.PrimaryKeyConstraint("id"),
        sa.UniqueConstraint("signal_id", name="uq_signal_outcomes_signal_id"),
    )
    op.create_index(
        "ix_signal_outcomes_label_evaluation_time",
        "signal_outcomes",
        ["label", "evaluation_time"],
    )

    op.create_table(
        "calibration_reports",
        sa.Column("id", postgresql.UUID(as_uuid=True), nullable=False),
        sa.Column("as_of", sa.BigInteger(), nullable=False),
        sa.Column("sample_count", sa.Integer(), nullable=False),
        sa.Column("brier_score", sa.Numeric(20, 10), nullable=True),
        sa.Column("log_loss", sa.Numeric(20, 10), nullable=True),
        sa.Column(
            "expected_calibration_error",
            sa.Numeric(20, 10),
            nullable=True,
        ),
        sa.Column(
            "bins",
            postgresql.JSONB(astext_type=sa.Text()),
            nullable=False,
        ),
        sa.Column(
            "created_at",
            sa.DateTime(timezone=True),
            server_default=sa.func.now(),
            nullable=False,
        ),
        sa.PrimaryKeyConstraint("id"),
    )
    op.create_index(
        "ix_calibration_reports_as_of",
        "calibration_reports",
        ["as_of"],
    )

    op.create_table(
        "drift_reports",
        sa.Column("id", postgresql.UUID(as_uuid=True), nullable=False),
        sa.Column("as_of", sa.BigInteger(), nullable=False),
        sa.Column("baseline_count", sa.Integer(), nullable=False),
        sa.Column("current_count", sa.Integer(), nullable=False),
        sa.Column("minimum_sample_count", sa.Integer(), nullable=False),
        sa.Column("score", sa.Numeric(20, 10), nullable=True),
        sa.Column("detected", sa.Boolean(), nullable=False),
        sa.Column("reason", sa.String(length=256), nullable=False),
        sa.Column(
            "created_at",
            sa.DateTime(timezone=True),
            server_default=sa.func.now(),
            nullable=False,
        ),
        sa.PrimaryKeyConstraint("id"),
    )
    op.create_index("ix_drift_reports_as_of", "drift_reports", ["as_of"])


def downgrade() -> None:
    op.drop_index("ix_drift_reports_as_of", table_name="drift_reports")
    op.drop_table("drift_reports")
    op.drop_index("ix_calibration_reports_as_of", table_name="calibration_reports")
    op.drop_table("calibration_reports")
    op.drop_index(
        "ix_signal_outcomes_label_evaluation_time",
        table_name="signal_outcomes",
    )
    op.drop_table("signal_outcomes")
    op.drop_index(
        "ix_signal_outcome_events_signal_time",
        table_name="signal_outcome_events",
    )
    op.drop_table("signal_outcome_events")
    op.drop_index(
        "ix_signal_lifecycles_state",
        table_name="signal_lifecycles",
    )
    op.drop_index(
        "ix_signal_lifecycles_symbol_timeframe_decision",
        table_name="signal_lifecycles",
    )
    op.drop_table("signal_lifecycles")
