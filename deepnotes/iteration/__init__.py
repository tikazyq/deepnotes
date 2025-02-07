"""
迭代管理模块，负责控制优化流程和评估指标
"""

from dataclasses import dataclass
from typing import Any, Dict, List


@dataclass
class IterationContext:
    iteration_num: int
    previous_metrics: Dict[str, float]


class IterationManager:
    def __init__(self, max_iterations: int = 3, evaluation_metrics: List[str] = None):
        self.max_iterations = max_iterations
        self.current_iteration = 0
        self.metrics = evaluation_metrics or []
        self.should_terminate = False

    def __iter__(self):
        while (
            self.current_iteration < self.max_iterations and not self.should_terminate
        ):
            self.current_iteration += 1
            yield IterationContext(
                iteration_num=self.current_iteration,
                previous_metrics=getattr(self, "last_metrics", {}),
            )

    def evaluate(self, current_notes: Any):
        """
        执行笔记质量评估，更新迭代状态
        """
        # TODO: 实现具体评估逻辑
        self.last_metrics = {metric: 0.8 for metric in self.metrics}

        # 简单终止条件示例
        if self.current_iteration >= 2 and all(
            v > 0.7 for v in self.last_metrics.values()
        ):
            self.should_terminate = True
