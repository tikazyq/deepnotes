"""
DeepNotes 主入口模块，负责初始化核心组件和启动处理流程
"""

from typing import Any, Optional, Dict, List
from .config import load_config, AppConfig
from .data_processing import DataProcessorFactory
from .iterative.optimization import IterationManager
from .models import IntermediateDataModel
from .note_generation import NoteGenerator
from .reporting.generators import ReportGenerator
from .data_processing.base_processor import BaseDataProcessor
from .iterative.hypothesis import HypothesisManager


class DeepNotesEngine:
    def __init__(self, config_path: str = "config.yml") -> None:
        self.config: AppConfig = load_config(config_path)
        self.processors: List[BaseDataProcessor] = []
        self.data_model: IntermediateDataModel = IntermediateDataModel()
        self.iteration_manager: IterationManager = IterationManager(
            max_iterations=self.config.max_iterations
        )
        self.report_generator: ReportGenerator = ReportGenerator()
        self.note_generator: NoteGenerator = NoteGenerator(
            llm_config=self.config.llm, template_dir="templates/notes"
        )

    def initialize_processors(self) -> None:
        for source in self.config.data_sources:
            processor: BaseDataProcessor = DataProcessorFactory.create_processor(source)
            self.processors.append(processor)

    def run_pipeline(self) -> Dict[str, Any]:
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

    def _create_iteration_state(self, iteration_num: int) -> Dict[str, Any]:
        return {
            "iteration": iteration_num,
            "data_snapshot": self.data_model.model_dump(),
            "hypotheses": self.iteration_manager.current_hypotheses,
            "notes": self.data_model.notes[-1] if self.data_model.notes else None,
        }

    def _analyze_notes(self, new_notes: Dict[str, Any]) -> Dict[str, Any]:
        """使用HypothesisManager分析笔记并生成改进建议"""
        hypothesis_manager = HypothesisManager(
            llm_config=self.config.llm,
        )
        
        # 提取并验证假设
        hypotheses = hypothesis_manager.extract_hypotheses(new_notes)
        validated = hypothesis_manager.validate_hypotheses(
            hypotheses=hypotheses,
            raw_data=self.data_model.raw_data,
            previous_notes=self.data_model.notes
        )
        
        # 生成置信度评分和进化建议
        confidence_scores = hypothesis_manager.calculate_confidence_scores(validated)
        evolution_plan = hypothesis_manager.generate_evolution_plan(
            current_hypotheses=validated,
            previous_hypotheses=self.iteration_manager.current_hypotheses
        )
        
        # 更新迭代管理器中的假设状态
        self.iteration_manager.update_hypotheses(
            new_hypotheses=validated,
            confidence_scores=confidence_scores
        )
        
        return {
            "key_insights": [h.summary() for h in validated if h.confidence > 0.7],
            "confidence_scores": confidence_scores,
            "validation_results": {
                "passed": [h.id for h in validated if h.is_valid],
                "failed": [h.id for h in validated if not h.is_valid]
            },
            "evolution_recommendations": evolution_plan
        }

    def _update_data_model(
        self, 
        new_notes: Dict[str, Any], 
        analysis: Dict[str, Any], 
        iteration: int
    ) -> None:
        self.data_model.notes.append(new_notes)
        self.data_model.iteration_history.append(
            {
                "iteration": iteration,
                "notes_version": len(self.data_model.notes) - 1,
                "analysis": analysis,
            }
        )

    def _collect_data(self) -> None:
        """收集并预处理来自所有数据源的数据"""
        collected_raw_data = []
        collected_metadata = []

        for processor in self.processors:
            try:
                # 处理数据并获取结构化结果
                processed = processor.process()

                # 收集原始数据用于合并
                if 'sample_data' in processed and 'sample_records' in processed['sample_data']:
                    collected_raw_data.extend(processed['sample_data']['sample_records'])

                # 收集元数据
                if 'metadata' in processed:
                    collected_metadata.append(processed['metadata'])

            except Exception as e:
                print(f"Error processing data with {type(processor).__name__}: {str(e)}")

        # 合并原始数据到数据模型
        if collected_raw_data:
            self._merge_data(collected_raw_data)

        # 存储元数据
        self.data_model.metadata.extend(collected_metadata)

    def _generate_final_output(self) -> Dict[str, Any]:
        """生成最终输出报告和整理后的笔记"""
        final_report = self.report_generator.generate_json_report(
            self.data_model,
        )
        return {
            "report": final_report,
            "structured_notes": self.data_model.notes[-1],
            "processing_stats": {
                "total_iterations": len(self.data_model.iteration_history),
                "total_notes": len(self.data_model.notes),
                "data_points": len(self.data_model.raw_data)
            }
        }

    def _check_convergence(self, analysis: Dict[str, Any]) -> bool:
        # Implement actual convergence logic
        if len(self.data_model.notes) < 2:
            return False

        last_two = self.data_model.notes[-2:]
        return self._calculate_similarity(last_two[0], last_two[1]) > 0.9

    def _calculate_similarity(self, note1: Any, note2: Any) -> float:
        """使用Jaccard相似度计算两个笔记之间的相似度"""
        # 提取笔记文本内容
        text1 = note1.get('content', '') if isinstance(note1, dict) else str(note1)
        text2 = note2.get('content', '') if isinstance(note2, dict) else str(note2)

        if not text1 or not text2:
            return 0.0

        # 将笔记内容转换为词集合
        set1 = set(text1.lower().split())
        set2 = set(text2.lower().split())

        intersection = len(set1 & set2)
        union = len(set1 | set2)

        return intersection / union if union != 0 else 0.0

    def _merge_data(self, new_data: List[Dict[str, Any]]) -> None:
        """将新数据合并到数据模型中，处理冲突"""
        # 添加类型检查确保数据格式正确
        if not isinstance(new_data, list):
            raise ValueError("New data must be a list of records")

        # 增强ID处理逻辑
        existing_ids = {item.get('id') for item in self.data_model.raw_data if item.get('id')}

        for item in new_data:
            if not isinstance(item, dict):
                continue

            item_id = item.get('id')
            if item_id and item_id in existing_ids:
                # 使用生成器表达式提高查找效率
                existing = next(
                    (idx for idx, existing in enumerate(self.data_model.raw_data)
                     if existing.get('id') == item_id),
                    None
                )
                if existing is not None:
                    self.data_model.raw_data[existing] = item
            else:
                self.data_model.raw_data.append(item)


if __name__ == "__main__":
    import sys

    config_file = sys.argv[1] if len(sys.argv) > 1 else "config.yml"
    engine = DeepNotesEngine(config_file)
    engine.initialize_processors()
    result = engine.run_pipeline()
    print("Processing completed. Final report:", result)
