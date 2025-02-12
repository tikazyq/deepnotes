from deepnotes.llm.llm_wrapper import get_llm_model


class FusionProcessor:
    def __init__(self):
        """
        Initializes the second-layer document fusion processor.
        """
        self.llm_model = get_llm_model()
