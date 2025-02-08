from pydantic import BaseModel

class AgentMessage(BaseModel):
    """Standardized agent communication protocol"""
    sender: str
    receiver: str
    payload: dict
    context: dict
    message_type: str  # e.g., "data_update", "hypothesis", "validation"
    
class WorkflowState(BaseModel):
    """Shared state representation"""
    current_data: IntermediateDataModel
    hypotheses: List[Hypothesis]
    iteration: int
    validation_metrics: dict