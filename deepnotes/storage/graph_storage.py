import os
from abc import ABC, abstractmethod
from collections import defaultdict
from pathlib import Path
from typing import Optional

import networkx as nx
from neo4j import GraphDatabase

from deepnotes.config.config import get_config
from deepnotes.models.analyzer_models import Entity, KnowledgeGraph, Relationship


class GraphStorage(ABC):
    @abstractmethod
    def get_entity(self, entity_id: str) -> Optional[Entity]:
        pass

    @abstractmethod
    def merge_entity(self, entity: Entity) -> Entity:
        pass

    @abstractmethod
    def get_relationship(self, rel_id: str) -> Optional[Relationship]:
        pass

    @abstractmethod
    def merge_relationship(self, rel: Relationship) -> Relationship:
        pass

    @abstractmethod
    def find_duplicate_entities(self) -> list[list[Entity]]:
        pass

    @abstractmethod
    def get_knowledge_graph(self) -> KnowledgeGraph:
        pass


class NetworkXStorage(GraphStorage):
    def __init__(self, graph_file: Path):
        self.graph_file = graph_file
        self.graph = nx.MultiDiGraph()

        # Load existing graph if file exists
        if self.graph_file.exists():
            self.graph = nx.readwrite.graphml.read_graphml(self.graph_file)

    def save(self):
        """Persist graph to file using GraphML format"""
        nx.readwrite.graphml.write_graphml(self.graph, self.graph_file)

    def merge_entity(self, entity: Entity) -> Entity:
        existing = self.get_entity(entity.id)
        if existing:
            # Simple attribute merge without resolution
            merged = existing.model_copy(update=entity.model_dump(exclude_unset=True))
            self.graph.nodes[entity.id].update(merged.model_dump())
            return merged
        self.graph.add_node(entity.id, **entity.model_dump())
        return entity

    def merge_relationship(self, rel: Relationship) -> Relationship:
        existing = self.get_relationship(rel.id)
        if existing:
            # Simple attribute merge without resolution
            merged = existing.model_copy(update=rel.model_dump(exclude_unset=True))
            self.graph.edges[rel.id].update(merged.model_dump())
            return merged
        self.graph.add_edge(rel.source, rel.target, key=rel.id, **rel.model_dump())
        return rel

    def get_entity(self, entity_id: str) -> Optional[Entity]:
        if entity_id in self.graph.nodes:
            return Entity(**self.graph.nodes[entity_id])
        return None

    def get_relationship(self, rel_id: str) -> Optional[Relationship]:
        for u, v, key, data in self.graph.edges(keys=True, data=True):
            if key == rel_id:
                return Relationship(**data)
        return None

    def find_duplicate_entities(self) -> list[list[Entity]]:
        name_map = {}
        for node_id in self.graph.nodes:
            entity = self.get_entity(node_id)
            if entity.name:
                name_map.setdefault(entity.name, []).append(entity)
        return [group for group in name_map.values() if len(group) > 1]

    def get_knowledge_graph(self) -> KnowledgeGraph:
        entities = [Entity(**data) for _, data in self.graph.nodes(data=True)]
        relationships = [
            Relationship(**data) for _, _, data in self.graph.edges(data=True)
        ]
        return KnowledgeGraph(entities=entities, relationships=relationships)


class Neo4jStorage(GraphStorage):
    def __init__(self, uri: str, user: str, password: str):
        self.driver = GraphDatabase.driver(uri, auth=(user, password))

    def merge_entity(self, entity: Entity) -> Entity:
        with self.driver.session() as session:
            session.execute_write(
                lambda tx: tx.run(
                    "MERGE (e:Entity {id: $id}) SET e += $props RETURN e",
                    id=entity.id,
                    props=entity.model_dump(exclude_unset=True),
                )
            )
        return entity

    def get_entity(self, entity_id: str) -> Optional[Entity]:
        with self.driver.session() as session:
            result = session.execute_read(
                lambda tx: tx.run(
                    "MATCH (e:Entity {id: $id}) RETURN e", id=entity_id
                ).single()
            )
            return Entity(**result["e"]) if result else None

    def get_relationship(self, rel_id: str) -> Optional[Relationship]:
        with self.driver.session() as session:
            result = session.execute_read(
                lambda tx: tx.run(
                    "MATCH ()-[r {id: $id}]->() RETURN r", id=rel_id
                ).single()
            )
            return Relationship(**result["r"]) if result else None

    def find_duplicate_entities(self) -> list[list[Entity]]:
        with self.driver.session() as session:
            result = session.execute_read(
                lambda tx: tx.run(
                    "MATCH (e:Entity)"
                    "WITH e.name AS name, collect(e) AS group "
                    "WHERE size(group) > 1 "
                    "RETURN group"
                ).data()
            )
            return [[Entity(**node) for node in group["group"]] for group in result]

    def get_knowledge_graph(self) -> KnowledgeGraph:
        with self.driver.session() as session:
            entities = session.execute_read(
                lambda tx: [
                    Entity(**record["e"])
                    for record in tx.run("MATCH (e:Entity) RETURN e").data()
                ]
            )
            relationships = session.execute_read(
                lambda tx: [
                    Relationship(**record["r"])
                    for record in tx.run("MATCH ()-[r]->() RETURN r").data()
                ]
            )
            return KnowledgeGraph(entities=entities, relationships=relationships)

    def merge_relationship(self, rel: Relationship) -> Relationship:
        with self.driver.session() as session:
            session.execute_write(
                lambda tx: tx.run(
                    "MATCH (a:Entity {id: $source}), (b:Entity {id: $target}) "
                    "MERGE (a)-[r:RELATIONSHIP {id: $id}]->(b) "
                    "SET r += $props RETURN r",
                    id=rel.id,
                    source=rel.source,
                    target=rel.target,
                    props=rel.model_dump(exclude_unset=True),
                )
            )
        return rel


class MemoryStorage(GraphStorage):
    """In-memory graph storage using dictionaries"""

    def __init__(self):
        self.entities: dict[str, Entity] = {}
        self.relationships: dict[str, Relationship] = {}

    def get_entity(self, entity_id: str) -> Optional[Entity]:
        return self.entities.get(entity_id)

    def merge_entity(self, entity: Entity) -> Entity:
        if entity.id in self.entities:
            existing = self.entities[entity.id]
            merged = existing.model_copy(update=entity.model_dump(exclude_unset=True))
            self.entities[entity.id] = merged
            return merged
        self.entities[entity.id] = entity
        return entity

    def get_relationship(self, rel_id: str) -> Optional[Relationship]:
        return self.relationships.get(rel_id)

    def merge_relationship(self, rel: Relationship) -> Relationship:
        if rel.id in self.relationships:
            existing = self.relationships[rel.id]
            merged = existing.model_copy(update=rel.model_dump(exclude_unset=True))
            self.relationships[rel.id] = merged
            return merged
        self.relationships[rel.id] = rel
        return rel

    def find_duplicate_entities(self) -> list[list[Entity]]:
        name_map = defaultdict(list)
        for entity in self.entities.values():
            if entity.name:
                name_map[entity.name.lower()].append(entity)
        return [group for group in name_map.values() if len(group) > 1]

    def get_knowledge_graph(self) -> KnowledgeGraph:
        return KnowledgeGraph(
            entities=list(self.entities.values()),
            relationships=list(self.relationships.values()),
        )

    def clear(self):
        """Clear all stored data (for testing)"""
        self.entities.clear()
        self.relationships.clear()


def get_graph_storage() -> GraphStorage:
    """Factory method to create graph storage based on configuration"""
    config = get_config()
    storage: GraphStorage
    storage_type = config["graph"]["storage_type"]
    if storage_type == "memory":
        storage = MemoryStorage()
    elif storage_type == "networkx":
        storage = NetworkXStorage(
            graph_file=Path(config["graph"]["networkx"]["graph_file"])
        )
    elif storage_type == "neo4j":
        storage = Neo4jStorage(
            uri=config["graph"]["neo4j"]["uri"],
            user=os.path.expandvars(config["graph"]["neo4j"]["user"]),
            password=os.path.expandvars(config["graph"]["neo4j"]["password"]),
        )
    else:
        raise ValueError(f"Unsupported graph storage type: {storage_type}")
    return storage
