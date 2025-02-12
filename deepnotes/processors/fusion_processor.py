from collections import defaultdict
from typing import List

from deepnotes.llm.llm_wrapper import get_llm_model
from deepnotes.models.models import (
    ConsolidationAnalysisResult,
    Entity,
    KnowledgeGraph,
    Relationship,
)


class FusionProcessor:
    def __init__(self):
        """
        Initializes the second-layer document fusion processor.
        """
        self.llm_model = get_llm_model()
        self.knowledge_base = KnowledgeGraph(entities=[], relationships=[])

    def merge_analysis(self, new_results: List[ConsolidationAnalysisResult]):
        """Merge new analysis results into existing knowledge base"""
        for result in new_results:
            if result.knowledge_graph:
                self._merge_entities(result.knowledge_graph.entities)
                self._merge_relationships(result.knowledge_graph.relationships)

        # Post-merge processing
        self._resolve_conflicts()
        self._prune_orphans()
        return self.knowledge_base

    def _merge_entities(self, new_entities: List[Entity]):
        """Entity merging with conflict resolution"""
        existing_ids = {e.id for e in self.knowledge_base.entities}

        for entity in new_entities:
            if entity.id in existing_ids:
                self._update_entity(entity)
            else:
                self.knowledge_base.entities.append(entity)

    def _update_entity(self, new_entity: Entity):
        """LLM-assisted entity update with version tracking"""
        existing = next(
            e for e in self.knowledge_base.entities if e.id == new_entity.id
        )

        # Simple merge for non-conflicting attributes
        if existing.name == new_entity.name and existing.type == new_entity.type:
            existing.attributes.update(new_entity.attributes)
            existing.metadata.update(new_entity.metadata)
            return

        # LLM-assisted conflict resolution
        prompt = f"""Resolve entity conflict between:
        Existing: {existing.model_dump_json()}
        New: {new_entity.model_dump_json()}
        Return merged JSON using this schema: {Entity.model_json_schema()}"""

        merged_entity = self.llm_model.generate(
            prompt, response_model=Entity
        ).model_instance

        self.knowledge_base.entities.remove(existing)
        self.knowledge_base.entities.append(merged_entity)

    def _merge_relationships(self, new_relationships: List[Relationship]):
        """Relationship merging with structural validation"""
        existing_ids = {r.id for r in self.knowledge_base.relationships}

        for relationship in new_relationships:
            if relationship.id in existing_ids:
                self._update_relationship(relationship)
            elif self._validate_relationship(relationship):
                self.knowledge_base.relationships.append(relationship)

    def _update_relationship(self, new_relationship: Relationship):
        """LLM-assisted relationship update with graph consistency checks"""
        existing = next(
            r for r in self.knowledge_base.relationships if r.id == new_relationship.id
        )

        # Simple merge for non-conflicting attributes
        if (
            existing.source == new_relationship.source
            and existing.target == new_relationship.target
            and existing.type == new_relationship.type
        ):
            existing.attributes.update(new_relationship.attributes)
            existing.metadata.update(new_relationship.metadata)
            return

        # LLM-assisted conflict resolution
        prompt = f"""Resolve relationship conflict between:
        Existing: {existing.model_dump_json()}
        New: {new_relationship.model_dump_json()}
        Return merged JSON using this schema: {Relationship.model_json_schema()}"""

        merged_relationship = self.llm_model.generate(
            prompt, response_model=Relationship
        ).model_instance

        self.knowledge_base.relationships.remove(existing)
        self.knowledge_base.relationships.append(merged_relationship)

    def _resolve_conflicts(self):
        """Resolve inter-entity conflicts and semantic duplicates"""
        entity_groups = defaultdict(list)
        for entity in self.knowledge_base.entities:
            key = (entity.name.lower(), entity.type)
            entity_groups[key].append(entity)

        for group in entity_groups.values():
            if len(group) > 1:
                merged_entity = self._merge_entity_group(group)
                for entity in group:
                    self.knowledge_base.entities.remove(entity)
                self.knowledge_base.entities.append(merged_entity)

    def _prune_orphans(self):
        """Remove orphaned entities and invalid relationships"""
        entity_ids = {e.id for e in self.knowledge_base.entities}

        # Clean up relationships first
        valid_relationships = []
        for rel in self.knowledge_base.relationships:
            if rel.source in entity_ids and rel.target in entity_ids:
                valid_relationships.append(rel)
        self.knowledge_base.relationships = valid_relationships

        # Remove entities without connections
        connected_entities = set()
        for rel in self.knowledge_base.relationships:
            connected_entities.add(rel.source)
            connected_entities.add(rel.target)

        self.knowledge_base.entities = [
            e
            for e in self.knowledge_base.entities
            if e.id in connected_entities or e.metadata.get("keep_always")
        ]

    def _validate_relationship(self, relationship: Relationship) -> bool:
        """Ensure relationship endpoints exist in knowledge base"""
        entity_ids = {e.id for e in self.knowledge_base.entities}
        return relationship.source in entity_ids and relationship.target in entity_ids

    def _merge_entity_group(self, entities: List[Entity]) -> Entity | None:
        """Merge multiple conflicting entities using LLM"""
        prompt = f"""Merge these duplicate entities into one authoritative version:
        {[e.model_dump_json() for e in entities]}
        Return merged JSON using this schema: {Entity.model_json_schema()}"""

        return self.llm_model.generate(prompt, response_model=Entity).model_instance
