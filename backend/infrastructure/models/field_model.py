from sqlalchemy import Column, Integer, String

from infrastructure.database import Base


class FieldModel(Base):
    __tablename__ = "fields"

    id = Column(Integer, primary_key=True, autoincrement=True)
    name = Column(String, nullable=False, unique=True)
