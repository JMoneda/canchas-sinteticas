from typing import List

from sqlalchemy.orm import Session

from domain.entities.field import Field
from domain.repositories.field_repository import FieldRepository
from infrastructure.models.field_model import FieldModel


class SQLiteFieldRepository(FieldRepository):
    def __init__(self, session: Session):
        self._session = session

    def get_all(self) -> List[Field]:
        return [Field(id=m.id, name=m.name) for m in self._session.query(FieldModel).all()]
