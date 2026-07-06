from sqlalchemy.orm import Session

from infrastructure.models.field_model import FieldModel


def seed_fields(session: Session) -> None:
    if session.query(FieldModel).count() == 0:
        session.add_all([
            FieldModel(name="Cancha A"),
            FieldModel(name="Cancha B"),
            FieldModel(name="Cancha C"),
        ])
        session.commit()
