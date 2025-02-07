import sqlite3
from pathlib import Path

import pytest


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
    conn.close()

    yield
    db_path.unlink()
