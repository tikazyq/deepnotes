import os

from deepnotes.processors.document_processor import DocumentProcessor
from deepnotes.processors.knowledge_processor import KnowledgeProcessor


class DeepNotesEngine:
    def __init__(self):
        self.document_processor = DocumentProcessor()
        self.knowledge_processor = KnowledgeProcessor()

    def run(self, target_path: str):
        processed_results = self.document_processor.process(target_path)
        for result in processed_results:
            print(result.model_dump_json(indent=2))
        return self.knowledge_processor.merge_analysis(processed_results)



if __name__ == "__main__":
    engine = DeepNotesEngine()
    knowledge_base = engine.run(
        os.getenv("DEEPNOTES_TARGET_PATH"),
    )
    print(knowledge_base.model_dump_json(indent=2))
