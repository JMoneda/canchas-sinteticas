from sqlalchemy import Column, ForeignKey, Integer, String

from infrastructure.database import Base


class NoShowModel(Base):
    __tablename__ = "no_shows"

    id = Column(Integer, primary_key=True, autoincrement=True)
    reservation_id = Column(String, ForeignKey("reservations.id"), nullable=False)
    user_id = Column(String, nullable=False)
    cancelled_at = Column(String, nullable=False)
