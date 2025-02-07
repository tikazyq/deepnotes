"""
笔记生成模块，包含 LLM 交互和结构化输出处理
"""

from datetime import datetime
from typing import Any, Dict, List, Optional

from pydantic import BaseModel

from ..config import LLMConfig
from ..llm import LLMWrapper
from ..models import IntermediateDataModel


class NoteGenerator:
    def __init__(self, llm_config: LLMConfig, template_dir: str = "templates"):
        self.llm = LLMWrapper(llm_config)
        self.template_dir = template_dir

    def generate(
        self,
        data_model: IntermediateDataModel,
        previous_notes: Optional[Dict[str, Any]] = None,
        context: Optional[Dict[str, Any]] = None
    ) -> Dict[str, Any]:
        context = context or {}
        prompt = self._build_prompt(
            data_model,
            previous_notes,
            context.get("hypotheses", []),
            context.get("iteration", 0),
        )
        raw_output = self.llm.generate(prompt)
        return self._parse_output(raw_output, data_model)

    def _build_prompt(
        self, data_model: BaseModel, previous_notes, hypotheses, iteration
    ):
        return f"""
        Analyze the following data and generate structured notes:

        Current Data State:
        {data_model.model_dump_json(indent=2)[:2000]}...

        Previous Notes Version:
        {previous_notes["summary"].content[:1000] if previous_notes else "None"}

        Active Hypotheses:
        {hypotheses}

        Iteration Context: {iteration}

        Output format: JSON with sections for key_findings, relationships, and uncertainties
        """

    def _parse_output(self, raw_output: Any, data_model: IntermediateDataModel) -> Dict[str, Any]:
        # TODO: Implement actual parsing logic
        return {
            "summary": raw_output,
            "entities": self._extract_entities(raw_output.content),
            "timestamp": datetime.now(),
        }

    def _extract_entities(self, content: str) -> list:
        """Improved entity extraction from JSON content"""
        try:
            import json
            data = json.loads(content)
            # Look for actual entities structure in the response
            return data.get('entities', []) or data.get('key_findings', [])[:3]
        except json.JSONDecodeError:
            # Fallback to simple text parsing
            return [line.strip() for line in content.split('\n')
                   if 'entity' in line.lower()][:3]
        except Exception:
            return []
