"""Transactional-outbox relay from PostgreSQL to NATS JetStream."""

import json
from datetime import UTC, datetime

from sqlalchemy import select

from cfip.infrastructure.db.models import OutboxEventModel
from cfip.infrastructure.db.session import session_factory
from cfip.infrastructure.messaging.jetstream import JetStreamPublisher


async def publish_pending_once(publisher: JetStreamPublisher, *, batch_size: int = 50) -> int:
    """Claim a bounded batch, publish with message-id deduplication, then mark success."""
    async with session_factory() as session:
        async with session.begin():
            result = await session.execute(
                select(OutboxEventModel)
                .where(OutboxEventModel.published_at.is_(None))
                .order_by(OutboxEventModel.created_at)
                .with_for_update(skip_locked=True)
                .limit(batch_size)
            )
            events = list(result.scalars().all())
            for event in events:
                event.attempts += 1
        published = 0
        for event in events:
            try:
                await publisher.publish(
                    event.subject,
                    json.dumps(event.payload, separators=(",", ":")).encode("utf-8"),
                    message_id=str(event.id),
                )
            except Exception as exc:
                async with session.begin():
                    event.last_error = str(exc)[:2000]
            else:
                async with session.begin():
                    event.published_at = datetime.now(UTC)
                published += 1
        return published
