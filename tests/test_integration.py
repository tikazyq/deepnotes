from deepnotes.main import DeepNotesEngine
from deepnotes.models import IntermediateDataModel


def test_full_pipeline(test_db):
    # Initialize engine with test config
    engine = DeepNotesEngine("tests/test_config.yml")
    engine.initialize_processors()

    # Run pipeline
    result = engine.run_pipeline()

    # Verify outputs
    assert isinstance(engine.data_model, IntermediateDataModel)
    assert len(engine.data_model.entities) > 0
    assert len(engine.data_model.notes) > 0

    # Check report structure
    assert "technical_report" in result
    assert "executive_summary" in result
    assert "process_audit" in result

    # Verify iteration history
    assert len(engine.data_model.iteration_history) <= 3
    assert engine.data_model.iteration_history[-1]["analysis"] is not None


def test_note_evolution(test_db):
    engine = DeepNotesEngine("tests/test_config.yml")
    engine.initialize_processors()
    engine.run_pipeline()

    # Verify note improvement across iterations
    notes = engine.data_model.notes
    for i in range(1, len(notes)):
        assert len(notes[i]["summary"]) > len(notes[i - 1]["summary"])
        assert notes[i]["timestamp"] > notes[i - 1]["timestamp"]


def test_data_processing(test_db):
    engine = DeepNotesEngine("tests/test_config.yml")
    engine.initialize_processors()

    # Test data collection
    engine._collect_data()
    assert len(engine.processors) == 1
    assert engine.processors[0].raw_data is not None

    # Test data transformation
    processed = engine.processors[0].process()
    assert not processed.empty
    assert list(processed.columns) == ["id", "name", "department", "salary"]
