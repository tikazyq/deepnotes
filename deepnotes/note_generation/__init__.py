"""
笔记生成模块，包含 LLM 交互和结构化输出处理
"""

from datetime import datetime

from pydantic import BaseModel

from ..llm import LLMWrapper


class NoteGenerator:
    def __init__(self, llm_config, template_dir="templates"):
        self.llm = LLMWrapper(llm_config)
        self.template_dir = template_dir

    def generate(self, data_model, previous_notes=None, context=None):
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
        {previous_notes["summary"][:1000] if previous_notes else "None"}

        Active Hypotheses:
        {hypotheses}

        Iteration Context: {iteration}

        Output format: JSON with sections for key_findings, relationships, and uncertainties
        """

    def _parse_output(self, raw_output, data_model):
        # TODO: Implement actual parsing logic
        return {
            "summary": raw_output,
            "entities": data_model.entities,
            "timestamp": datetime.now(),
        }
