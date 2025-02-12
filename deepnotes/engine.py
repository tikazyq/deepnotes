from deepnotes.processors.document_processor import DocumentProcessor
from deepnotes.processors.fusion_processor import FusionProcessor


class DeepNotesEngine:
    def __init__(self):
        self.document_processor = DocumentProcessor()
        self.fusion_processor = FusionProcessor()

    def run(self, target_path: str):
        return self.document_processor.process(target_path)


if __name__ == "__main__":
    engine = DeepNotesEngine()
    result = engine.run(
        "/home/marvin/projects/tikazyq/deepnotes/data/academic/2410.04415v1.pdf"
    )
    print(result.model_dump_json(indent=2))
