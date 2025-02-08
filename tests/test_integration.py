from typing import cast

from deepnotes.engine import DeepNotesEngine
from deepnotes.models import IntermediateDataModel


def test_full_pipeline(test_db, sample_data: IntermediateDataModel):
    # Initialize engine with test config
    engine = DeepNotesEngine("tests/test_config.yml")
    engine.data_model = sample_data
    engine.initialize_processors()

    # Run pipeline
    result = engine.run_pipeline()

    # Verify outputs
    data_model = cast(IntermediateDataModel, engine.data_model)
    assert len(data_model.entities) >= 2
    assert any(e.attributes["name"] == "Test Entity 1" for e in data_model.entities)
    assert len(data_model.notes) > 0

    # Check report structure
    assert "technical_report" in result
    assert "executive_summary" in result
    assert "process_audit" in result

    # Verify iteration history
    assert len(data_model.iteration_history) <= 3
    assert data_model.iteration_history[-1]["analysis"] is not None


def test_note_evolution(test_db):
    engine = DeepNotesEngine("tests/test_config.yml")
    engine.initialize_processors()
    engine.run_pipeline()

    # Verify note improvement across iterations
    notes = engine.data_model.notes
    for i in range(1, len(notes)):
        assert len(notes[i]["summary"].get("key_findings", [])) >= len(
            notes[i - 1]["summary"].get("key_findings", [])
        )
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
