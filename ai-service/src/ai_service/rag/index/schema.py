"""Azure AI Search index definition (one index per product; tenants separated by filter)."""

from __future__ import annotations

from azure.search.documents.indexes.models import (
    HnswAlgorithmConfiguration,
    HnswParameters,
    SearchableField,
    SearchField,
    SearchIndex,
    SemanticConfiguration,
    SemanticField,
    SemanticPrioritizedFields,
    SemanticSearch,
    SimpleField,
    VectorSearch,
    VectorSearchAlgorithmMetric,
    VectorSearchProfile,
)

from ai_service.settings import Settings

STRING = "Edm.String"
VECTOR_PROFILE = "content-vector-profile"
HNSW_CONFIG = "content-hnsw"

SELECT_FIELDS = [
    "id",
    "document_id",
    "title",
    "heading_path",
    "headings",
    "content",
    "page",
    "source_url",
    "language",
]


def build_index(settings: Settings, name: str | None = None) -> SearchIndex:
    index_name = name or settings.search_index_name
    if not index_name:
        raise RuntimeError("SEARCH_INDEX_NAME is not configured")
    fields: list[SearchField] = [
        SimpleField(name="id", type=STRING, key=True, filterable=True),
        SimpleField(name="tenant_id", type=STRING, filterable=True),
        SimpleField(name="product", type=STRING, filterable=True),
        SimpleField(name="document_id", type=STRING, filterable=True),
        SearchableField(name="title", type=STRING, analyzer_name="standard.lucene"),
        SearchableField(name="heading_path", type=STRING, analyzer_name="standard.lucene"),
        SearchableField(name="headings", collection=True, analyzer_name="standard.lucene"),
        SearchableField(name="summary", type=STRING, analyzer_name="standard.lucene"),
        # Original text (display, semantic ranker, model input).
        SearchableField(name="content", type=STRING, analyzer_name="standard.lucene"),
        # Language-specific analyzers: only the field matching the chunk language is filled.
        SearchableField(name="content_ar", type=STRING, analyzer_name="ar.microsoft"),
        SearchableField(name="content_en", type=STRING, analyzer_name="en.microsoft"),
        # Normalized Arabic search copy (no diacritics/tatweel, unified alef/ya, ASCII digits).
        SearchableField(name="content_search", type=STRING, analyzer_name="standard.lucene"),
        SearchField(
            name="content_vector",
            type="Collection(Edm.Single)",
            searchable=True,
            vector_search_dimensions=settings.embed_dimensions,
            vector_search_profile_name=VECTOR_PROFILE,
        ),
        SimpleField(name="page", type="Edm.Int32", filterable=True),
        SimpleField(name="language", type=STRING, filterable=True, facetable=True),
        SimpleField(name="acl_groups", type="Collection(Edm.String)", filterable=True),
        SimpleField(name="doc_type", type=STRING, filterable=True, facetable=True),
        SimpleField(
            name="effective_date",
            type="Edm.DateTimeOffset",
            filterable=True,
            sortable=True,
        ),
        SimpleField(name="version", type=STRING, filterable=True),
        SimpleField(name="source_url", type=STRING),
        SimpleField(name="chunk_index", type="Edm.Int32", sortable=True),
        SimpleField(name="content_sha256", type=STRING, filterable=True),
    ]
    vector_search = VectorSearch(
        algorithms=[
            HnswAlgorithmConfiguration(
                name=HNSW_CONFIG,
                parameters=HnswParameters(metric=VectorSearchAlgorithmMetric.COSINE),
            )
        ],
        profiles=[VectorSearchProfile(name=VECTOR_PROFILE, algorithm_configuration_name=HNSW_CONFIG)],
    )
    semantic = SemanticSearch(
        default_configuration_name=settings.search_semantic_config,
        configurations=[
            SemanticConfiguration(
                name=settings.search_semantic_config,
                prioritized_fields=SemanticPrioritizedFields(
                    title_field=SemanticField(field_name="title"),
                    content_fields=[SemanticField(field_name="content")],
                    keywords_fields=[SemanticField(field_name="heading_path")],
                ),
            )
        ],
    )
    return SearchIndex(name=index_name, fields=fields, vector_search=vector_search, semantic_search=semantic)
