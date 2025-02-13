from collections import defaultdict
from datetime import datetime
from typing import List

from tqdm import tqdm

from deepnotes.llm.llm_wrapper import get_llm_model
from deepnotes.models.analyzer_models import (
    ConsolidationAnalysisResult,
    Entity,
    Relationship,
)
from deepnotes.storage.graph_storage import get_graph_storage


class KnowledgeProcessor:
    def __init__(self):
        self.graph_storage = get_graph_storage()
        self.llm = get_llm_model()
        self._base_graph = self.graph_storage.get_knowledge_graph()
        self.last_update = datetime.utcnow()

    def get_knowledge_graph(self):
        return self._base_graph

    def merge_analysis(self, new_results: List[ConsolidationAnalysisResult]):
        """Merge new analysis results with incremental update logic"""
        # Track changes for optimization
        changes = {
            "entities_added": 0,
            "entities_updated": 0,
            "relationships_added": 0,
            "relationships_updated": 0,
        }

        for result in tqdm(new_results, desc="Merging knowledge"):
            if result.knowledge_graph:
                changes["entities_added"], changes["entities_updated"] = (
                    self._merge_entities(result.knowledge_graph.entities)
                )
                changes["relationships_added"], changes["relationships_updated"] = (
                    self._merge_relationships(result.knowledge_graph.relationships)
                )

        # Optimize if significant changes
        if sum(changes.values()) > 100:  # Threshold for optimization
            self._optimize_graph()

        self.last_update = datetime.utcnow()

    def _merge_entities(self, new_entities: List[Entity]):
        """Entity merging with conflict resolution"""
        existing_ids = {e.id for e in self._base_graph.entities}
        entities_added = 0
        entities_updated = 0
        for entity in tqdm(new_entities, desc="Merging entities"):
            if entity.id in existing_ids:
                self._update_entity(entity)
                entities_updated += 1
            else:
                self.graph_storage.merge_entity(entity)
                entities_added += 1
        # Refresh base graph after merging
        self._base_graph = self.graph_storage.get_knowledge_graph()
        return entities_added, entities_updated

    def _merge_relationships(self, new_relationships: List[Relationship]):
        """Relationship merging with structural validation"""
        existing_ids = {r.id for r in self._base_graph.relationships}
        relationships_added = 0
        relationships_updated = 0
        for relationship in tqdm(new_relationships, desc="Updating relationships"):
            if relationship.id in existing_ids:
                self._update_relationship(relationship)
                relationships_updated += 1
            elif self._validate_relationship(relationship):
                self.graph_storage.merge_relationship(relationship)
                relationships_added += 1
        # Refresh base graph after merging
        self._base_graph = self.graph_storage.get_knowledge_graph()
        return relationships_added, relationships_updated

    def _update_entity(self, new_entity: Entity):
        """LLM-assisted entity update with version tracking"""
        existing = next(e for e in self._base_graph.entities if e.id == new_entity.id)

        # Simple merge for non-conflicting attributes
        if existing.name == new_entity.name and existing.type == new_entity.type:
            existing.attributes.update(new_entity.attributes)
            existing.metadata.update(new_entity.metadata)
            return

        # LLM-assisted conflict resolution
        merged_entity = self._resolve_entity_conflict(existing, new_entity)
        self.graph_storage.merge_entity(merged_entity)

    def _resolve_entity_conflict(self, existing: Entity, new: Entity) -> Entity:
        """LLM-assisted entity conflict resolution with graph context"""
        prompt = f"""Resolve entity conflict in knowledge graph:
        Existing Entity: {existing.model_dump_json()}
        New Entity: {new.model_dump_json()}
        Connected Relationships:
        {self._get_entity_connections(existing.id)}
        Return merged JSON using this schema: {Entity.model_json_schema()}"""

        return self.llm.generate(prompt, response_model=Entity).model_instance

    def _get_entity_connections(self, entity_id: str) -> str:
        """Get relationships for conflict resolution context"""
        return "\n".join(
            f"- {rel.id} ({rel.type})"
            for rel in self._base_graph.relationships
            if rel.source == entity_id or rel.target == entity_id
        )

    def _update_relationship(self, new_relationship: Relationship):
        """Enhanced relationship update with proper conflict resolution"""
        existing = next(
            (r for r in self._base_graph.relationships if r.id == new_relationship.id),
            None,
        )

        if not existing:
            self.graph_storage.merge_relationship(new_relationship)
            return

        # Resolve conflict using LLM if structural changes
        if (
            existing.source != new_relationship.source
            or existing.target != new_relationship.target
            or existing.type != new_relationship.type
        ):
            merged = self._resolve_relationship_conflict(existing, new_relationship)
            self.graph_storage.merge_relationship(merged)
        else:
            # Simple attribute merge
            self.graph_storage.merge_relationship(new_relationship)

    def _resolve_relationship_conflict(
        self, existing: Relationship, new: Relationship
    ) -> Relationship:
        """LLM-assisted relationship conflict resolution"""
        prompt = f"""Resolve relationship conflict (DON'T include 'id' field):
        Existing: {existing.model_dump(exclude={"id"})}
        New: {new.model_dump(exclude={"id"})}
        Connected Entities:
        - Source: {self.graph_storage.get_entity(existing.source).name if self.graph_storage.get_entity(existing.source) else "Missing"}
        - Target: {self.graph_storage.get_entity(existing.target).name if self.graph_storage.get_entity(existing.target) else "Missing"}
        Return merged JSON using this schema: {Relationship.model_json_schema()}"""

        merged = self.llm.generate(prompt, response_model=Relationship).model_instance
        # Recompute ID after merge
        merged.id = f"{merged.source}__{merged.type}__{merged.target}"
        return merged

    def _resolve_conflicts(self):
        """Resolve inter-entity conflicts and semantic duplicates"""
        entity_groups = defaultdict(list)
        for entity in self._base_graph.entities:
            key = (entity.name.lower(), entity.type)
            entity_groups[key].append(entity)

        for group in tqdm(
            entity_groups.values(), desc="Resolving conflicts", total=len(entity_groups)
        ):
            if len(group) > 1:
                merged_entity = self._merge_entity_group(group)
                for entity in group:
                    self.graph_storage.merge_entity(entity)

    def _prune_orphans(self):
        """Remove orphaned entities and invalid relationships"""
        entity_ids = {e.id for e in self._base_graph.entities}

        # Clean up relationships first
        valid_relationships = []
        for rel in tqdm(self._base_graph.relationships, desc="Pruning relationships"):
            if rel.source in entity_ids and rel.target in entity_ids:
                valid_relationships.append(rel)
        self._base_graph.relationships = valid_relationships

        # Remove entities without connections
        connected_entities = set()
        for rel in tqdm(valid_relationships, desc="Checking connections"):
            connected_entities.add(rel.source)
            connected_entities.add(rel.target)

        self._base_graph.entities = [
            e
            for e in self._base_graph.entities
            if e.id in connected_entities or e.metadata.get("keep_always")
        ]

    def _validate_relationship(self, relationship: Relationship) -> bool:
        """Ensure relationship endpoints exist in knowledge base"""
        entity_ids = {e.id for e in self._base_graph.entities}
        return relationship.source in entity_ids and relationship.target in entity_ids

    def _merge_entity_group(self, entities: List[Entity]) -> Entity | None:
        """Merge multiple conflicting entities using LLM"""
        prompt = f"""Merge these duplicate entities into one authoritative version:
        {[e.model_dump_json() for e in entities]}
        Return merged JSON using this schema: {Entity.model_json_schema()}"""

        return self.llm.generate(
            prompt,
            response_model=Entity,
        ).model_instance

    def _optimize_graph(self):
        """Perform graph optimizations"""
        self._resolve_conflicts()
        self._prune_orphans()
        self._compact_storage()

    def _compact_storage(self):
        """Compact storage to reclaim space and optimize indices"""
        if hasattr(self.graph_storage, "compact"):
            self.graph_storage.compact()
