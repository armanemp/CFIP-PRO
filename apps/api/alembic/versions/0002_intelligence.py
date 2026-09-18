"""Persist governed intelligence artifacts and immutable audit events."""

from collections.abc import Sequence

import sqlalchemy as sa
from alembic import op
from sqlalchemy.dialects import postgresql

revision: str = "0002_intelligence"
down_revision: str | Sequence[str] | None = "0001_market_data"
branch_labels: str | Sequence[str] | None = None
depends_on: str | Sequence[str] | None = None


def upgrade() -> None:
    op.create_table(
        "intelligence_evidence",
        sa.Column("id", sa.String(length=128), nullable=False),
        sa.Column("kind", sa.String(length=32), nullable=False),
        sa.Column("source", sa.String(length=256), nullable=False),
        sa.Column("observed_at", sa.BigInteger(), nullable=False),
        sa.Column("content_hash", sa.String(length=128), nullable=False),
        sa.Column("freshness_seconds", sa.Integer(), nullable=False),
        sa.Column("confidence", sa.Numeric(6, 5), nullable=False),
        sa.Column("facts", postgresql.JSONB(astext_type=sa.Text()), nullable=False),
        sa.Column("created_at", sa.DateTime(timezone=True), server_default=sa.func.now(), nullable=False),
        sa.PrimaryKeyConstraint("id"),
    )

    op.create_table(
        "learning_records",
        sa.Column("id", sa.String(length=128), nullable=False),
        sa.Column("learning_type", sa.String(length=32), nullable=False),
        sa.Column("created_at_epoch", sa.BigInteger(), nullable=False),
        sa.Column("subject", sa.String(length=256), nullable=False),
        sa.Column("evidence_ids", postgresql.JSONB(astext_type=sa.Text()), nullable=False),
        sa.Column("lesson", sa.String(length=4000), nullable=False),
        sa.Column("confidence", sa.Numeric(6, 5), nullable=False),
        sa.Column("status", sa.String(length=32), nullable=False),
        sa.Column("validation_count", sa.Integer(), nullable=False, server_default="0"),
        sa.Column("created_at", sa.DateTime(timezone=True), server_default=sa.func.now(), nullable=False),
        sa.PrimaryKeyConstraint("id"),
    )
    op.create_index("ix_learning_records_status", "learning_records", ["status"])

    op.create_table(
        "intelligence_proposals",
        sa.Column("id", sa.String(length=128), nullable=False),
        sa.Column("kind", sa.String(length=32), nullable=False),
        sa.Column("title", sa.String(length=256), nullable=False),
        sa.Column("rationale", sa.String(length=4000), nullable=False),
        sa.Column("evidence_ids", postgresql.JSONB(astext_type=sa.Text()), nullable=False),
        sa.Column("risk_level", sa.String(length=16), nullable=False),
        sa.Column("requires_approval", sa.Boolean(), nullable=False),
        sa.Column("status", sa.String(length=32), nullable=False),
        sa.Column("created_at", sa.DateTime(timezone=True), server_default=sa.func.now(), nullable=False),
        sa.PrimaryKeyConstraint("id"),
    )
    op.create_index("ix_intelligence_proposals_status", "intelligence_proposals", ["status"])

    op.create_table(
        "intelligence_feedback",
        sa.Column("id", postgresql.UUID(as_uuid=True), nullable=False),
        sa.Column("learning_id", sa.String(length=128), nullable=False),
        sa.Column("accepted", sa.Boolean(), nullable=False),
        sa.Column("reviewer", sa.String(length=128), nullable=False),
        sa.Column("reason", sa.String(length=2000), nullable=False),
        sa.Column("observed_at", sa.BigInteger(), nullable=False),
        sa.Column("created_at", sa.DateTime(timezone=True), server_default=sa.func.now(), nullable=False),
        sa.ForeignKeyConstraint(["learning_id"], ["learning_records.id"], ondelete="RESTRICT"),
        sa.PrimaryKeyConstraint("id"),
    )
    op.create_index("ix_intelligence_feedback_learning_id", "intelligence_feedback", ["learning_id"])

    op.create_table(
        "intelligence_audit_events",
        sa.Column("id", postgresql.UUID(as_uuid=True), nullable=False),
        sa.Column("event_key", sa.String(length=256), nullable=False),
        sa.Column("event_type", sa.String(length=128), nullable=False),
        sa.Column("aggregate_type", sa.String(length=64), nullable=False),
        sa.Column("aggregate_id", sa.String(length=160), nullable=False),
        sa.Column("payload", postgresql.JSONB(astext_type=sa.Text()), nullable=False),
        sa.Column("occurred_at", sa.BigInteger(), nullable=False),
        sa.Column("created_at", sa.DateTime(timezone=True), server_default=sa.func.now(), nullable=False),
        sa.PrimaryKeyConstraint("id"),
        sa.UniqueConstraint("event_key", name="uq_intelligence_audit_event_key"),
    )
    op.create_index(
        "ix_intelligence_audit_events_occurred_at",
        "intelligence_audit_events",
        ["occurred_at"],
    )


def downgrade() -> None:
    op.drop_index("ix_intelligence_audit_events_occurred_at", table_name="intelligence_audit_events")
    op.drop_table("intelligence_audit_events")
    op.drop_index("ix_intelligence_feedback_learning_id", table_name="intelligence_feedback")
    op.drop_table("intelligence_feedback")
    op.drop_index("ix_intelligence_proposals_status", table_name="intelligence_proposals")
    op.drop_table("intelligence_proposals")
    op.drop_index("ix_learning_records_status", table_name="learning_records")
    op.drop_table("learning_records")
    op.drop_table("intelligence_evidence")
