"""
DeepNotes 主入口模块，负责初始化核心组件和启动处理流程
"""

from .config import load_config
from .data_processing import DataProcessorFactory
from .iterative.optimization import IterationManager
from .models import IntermediateDataModel
from .note_generation import NoteGenerator
from .reporting.generators import ReportGenerator


class DeepNotesEngine:
    def __init__(self, config_path="config.yml"):
        self.config = load_config(config_path)
        self.processors = []
        self.data_model = IntermediateDataModel()
        self.iteration_manager = IterationManager(
            max_iterations=self.config.max_iterations
        )
        self.report_generator = ReportGenerator()
        self.note_generator = NoteGenerator(
            llm_config=self.config.llm, template_dir="templates/notes"
        )

    def initialize_processors(self):
        for source in self.config.data_sources:
            processor = DataProcessorFactory.create_processor(source)
            self.processors.append(processor)

    def run_pipeline(self):
        # Data collection and initial processing
        self._collect_data()

        # Iterative refinement loop
        for iteration in range(self.config.max_iterations):
            current_state = self._create_iteration_state(iteration)

            # Generate/update notes
            new_notes = self.note_generator.generate(
                self.data_model,
                previous_notes=current_state.get("notes"),
                context=current_state,
            )

            # Analyze and refine
            analysis = self._analyze_notes(new_notes)
            self._update_data_model(new_notes, analysis, iteration)

            # Check convergence
            if self._check_convergence(analysis):
                break

        # Final reporting
        return self._generate_final_output()

    def _create_iteration_state(self, iteration_num):
        return {
            "iteration": iteration_num,
            "data_snapshot": self.data_model.dict(),
            "hypotheses": self.iteration_manager.current_hypotheses,
            "notes": self.data_model.notes[-1] if self.data_model.notes else None,
        }

    def _analyze_notes(self, new_notes):
        # Implement analysis logic using HypothesisManager
        return {"key_insights": [], "confidence_scores": {}, "validation_results": {}}

    def _update_data_model(self, new_notes, analysis, iteration):
        self.data_model.notes.append(new_notes)
        self.data_model.iteration_history.append(
            {
                "iteration": iteration,
                "notes_version": len(self.data_model.notes) - 1,
                "analysis": analysis,
            }
        )

    def _collect_data(self):
        # Data collection implementation
        pass

    def _generate_final_output(self):
        # Final reporting implementation
        pass

    def _check_convergence(self, analysis):
        # Implement actual convergence logic
        if len(self.data_model.notes) < 2:
            return False

        last_two = self.data_model.notes[-2:]
        return self._calculate_similarity(last_two[0], last_two[1]) > 0.9

    def _calculate_similarity(self, note1, note2):
        # Implement similarity scoring
        return 0.85  # Example value

    def _merge_data(self, new_data):
        # Data fusion implementation
        pass


if __name__ == "__main__":
    import sys

    config_file = sys.argv[1] if len(sys.argv) > 1 else "config.yml"
    engine = DeepNotesEngine(config_file)
    engine.initialize_processors()
    result = engine.run_pipeline()
    print("Processing completed. Final report:", result)
