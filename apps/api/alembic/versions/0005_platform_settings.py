"""add persistent platform settings

Revision ID: 0005_platform_settings
Revises: 0004_feedback_idempotency
"""
from alembic import op
import sqlalchemy as sa

revision = "0005_platform_settings"
down_revision = "0004_feedback_idempotency"
branch_labels = None
depends_on = None

def upgrade() -> None:
    op.create_table(
        "platform_settings",
        sa.Column("key", sa.String(length=128), nullable=False),
        sa.Column("value", sa.String(length=512), nullable=False),
        sa.Column("updated_at", sa.DateTime(timezone=True), server_default=sa.func.now(), nullable=False),
        sa.PrimaryKeyConstraint("key"),
    )

def downgrade() -> None:
    op.drop_table("platform_settings")
