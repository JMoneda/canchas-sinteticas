from abc import ABC, abstractmethod
from typing import List

from domain.entities.field import Field


class FieldRepository(ABC):
    @abstractmethod
    def get_all(self) -> List[Field]:
        ...
