from pydantic import BaseModel

from ..models import IntermediateDataModel


class Hypothesis(BaseModel):
    description: str
    confidence: float = 0.8
    supporting_evidence: list = []

    @classmethod
    def from_data_model(cls, data_model: IntermediateDataModel):
        """Create hypothesis from data model analysis"""
        return cls(
            description="Generated from data model analysis",
            supporting_evidence=data_model.entities[:3],  # Example evidence
        )

    def apply(self, data_model: IntermediateDataModel) -> IntermediateDataModel:
        """Apply hypothesis to modify data model"""
        # Example implementation
        data_model.metadata["hypothesis_applied"] = self.description
        return data_model
