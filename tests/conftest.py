import sqlite3
from pathlib import Path

import pytest
from deepnotes.models import DataEntity, Relationship, IntermediateDataModel


@pytest.fixture(scope="module")
def test_db():
    db_path = Path("test.db")
    if db_path.exists():
        db_path.unlink()

    conn = sqlite3.connect("test.db")
    cursor = conn.cursor()

    # Create test tables
    cursor.execute("""
        CREATE TABLE employees (
            id INTEGER PRIMARY KEY,
            name TEXT,
            department TEXT,
            salary REAL
        )
    """)

    cursor.execute("""
        CREATE TABLE departments (
            id INTEGER PRIMARY KEY,
            name TEXT,
            budget REAL
        )
    """)

    # Insert test data
    cursor.executemany(
        "INSERT INTO employees VALUES (?, ?, ?, ?)",
        [
            (1, "Alice", "Engineering", 85000),
            (2, "Bob", "Marketing", 75000),
            (3, "Charlie", "Engineering", 90000),
        ],
    )

    cursor.executemany(
        "INSERT INTO departments VALUES (?, ?, ?)",
        [(1, "Engineering", 1000000), (2, "Marketing", 500000)],
    )

    conn.commit()
    yield conn  # Keep connection open during tests
    conn.close()
    db_path.unlink()


@pytest.fixture
def sample_data():
    return IntermediateDataModel(
        entities=[
            DataEntity(id="ent-1", type="test", attributes={"name": "Test Entity 1"}, source="test"),
            DataEntity(id="ent-2", type="test", attributes={"name": "Test Entity 2"}, source="test")
        ],
        relationships=[
            Relationship(source="ent-1", target="ent-2", type="related")  # Proper relationship
        ],
        metadata=[{"source": "test"}],
        notes=[],
        iteration_history=[]
    )
