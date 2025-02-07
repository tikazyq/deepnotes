from typing import Dict

from jinja2 import Environment, FileSystemLoader


class ReportGenerator:
    def __init__(self, template_dir="templates"):
        self.env = Environment(loader=FileSystemLoader(template_dir))

    def generate_json_report(self, data_model) -> Dict:
        """Generate machine-readable JSON report"""
        return {
            "metadata": data_model.metadata,
            "summary": data_model.validation_summary,
            "entities_count": len(data_model.entities),
            "relationships_count": len(data_model.relationships),
        }

    def generate_markdown_report(self, data_model) -> str:
        """Generate human-readable markdown report"""
        template = self.env.get_template("report.md.j2")
        return template.render(
            model=data_model, timestamp=data_model.timestamp.isoformat()
        )

    def generate_combined_output(self, data_model):
        return {
            "technical_report": self._generate_technical_report(data_model),
            "executive_summary": self._generate_executive_summary(data_model),
            "process_audit": self._generate_process_audit(data_model),
        }

    def _generate_process_audit(self, data_model):
        template = self.env.get_template("process_audit.md.j2")
        return template.render(
            iterations=data_model.iteration_history,
            note_versions=len(data_model.notes),
            final_confidence=data_model.validation_summary.get("confidence", 0),
        )
