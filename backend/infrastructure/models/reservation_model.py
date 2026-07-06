from sqlalchemy import Column, Index, Integer, String, ForeignKey

from infrastructure.database import Base


class ReservationModel(Base):
    __tablename__ = "reservations"

    id = Column(String, primary_key=True)
    user_id = Column(String, nullable=False)
    field_id = Column(Integer, ForeignKey("fields.id"), nullable=False)
    date = Column(String, nullable=False)
    start_time = Column(String, nullable=False)
    end_time = Column(String, nullable=False)
    status = Column(String, nullable=False, default="active")
    created_at = Column(String, nullable=False)
    cancelled_at = Column(String, nullable=True)

    __table_args__ = (
        Index("idx_reservations_field_date", "field_id", "date", "status"),
    )
