from datetime import datetime

import pytest

from deepnotes.loaders.web_document_loader import WebDocumentLoader
from deepnotes.models.loader_models import DocumentLoadedData
from deepnotes.models.source_models import SourceConfig, SourceType


@pytest.fixture
def mock_loader_config():
    return SourceConfig(
        type=SourceType.WEB_DOCUMENT,
        name="test_loader",
        connection={
            "start_urls": ["https://example.com/docs"],
            "url_pattern": "https://example.com/docs"
        },
        options={
            "chunk_size": 1000,
            "chunk_overlap": 100,
            "concurrency": 2,
            "retries": 1
        }
    )

@pytest.fixture
def mock_simple_html():
    return """
    <html>
        <head><title>Test Page</title></head>
        <body>
            <h1>Main Content</h1>
            <article>
                <p>This is a test document content that should be extracted.</p>
                <p>Another paragraph with meaningful information.</p>
            </article>
        </body>
    </html>
    """

def test_web_loader_process_basic(mock_loader_config, mock_simple_html, httpx_mock):
    # Allow multiple matches for the same URL
    httpx_mock.add_response(
        url="https://example.com/docs",
        method="GET",
        text=mock_simple_html,
        status_code=200,
        headers={"Content-Type": "text/html"},
        is_reusable=True
    )

    loader = WebDocumentLoader(mock_loader_config)
    result = loader.process()

    # Basic type checks
    assert isinstance(result, DocumentLoadedData)
    assert result.source_type == SourceType.WEB_DOCUMENT
    assert len(result.documents) == 1

    # Check document structure
    doc = result.documents[0]
    assert doc.metadata.file_path == "https://example.com/docs"
    assert doc.metadata.file_type == "web_document"
    assert len(doc.chunks) > 0

    # Check content extraction
    content = doc.raw_content.lower()
    assert "test document content" in content
    assert "meaningful information" in content

    # Check global metadata
    assert result.global_metadata["source_path"] == "https://example.com/docs"
    assert result.global_metadata["total_chunks"] == len(doc.chunks)

@pytest.mark.httpx_mock(assert_all_requests_were_expected=False)
def test_web_loader_crawling(mock_loader_config, mock_simple_html, httpx_mock):
    # Mock responses with linked pages
    main_page = mock_simple_html.replace(
        "</article>",
        '<a href="/docs/page1">Page1</a><a href="https://external.com">External</a></article>'
    )

    # Mock main page with reusable response
    httpx_mock.add_response(
        url="https://example.com/docs",
        method="GET",
        text=main_page,
        status_code=200,
        is_reusable=True  # Allow reuse
    )
    # Mock subpage with reusable response
    httpx_mock.add_response(
        url="https://example.com/docs/page1",
        method="GET",
        text="<html><body><h1>Page 1 Content</h1></body></html>",
        status_code=200,
        is_reusable=True  # Allow reuse
    )

    loader = WebDocumentLoader(mock_loader_config)
    result = loader.process()

    # Should process both start URL and valid subpage
    urls = {doc.metadata.file_path for doc in result.documents}
    assert len(urls) == 2
    assert "https://example.com/docs" in urls
    assert "https://example.com/docs/page1" in urls
    assert "https://external.com" not in urls  # Should be filtered by url_pattern

@pytest.mark.httpx_mock(assert_all_requests_were_expected=False)
def test_web_loader_error_handling(mock_loader_config, httpx_mock):
    httpx_mock.add_response(
        url="https://example.com/docs",
        method="GET",
        status_code=404,
        is_reusable=True
    )

    loader = WebDocumentLoader(mock_loader_config)
    result = loader.process()

    # Should return empty result when no pages could be loaded
    assert len(result.documents) == 0
    assert result.global_metadata["total_chunks"] == 0

@pytest.mark.httpx_mock(assert_all_requests_were_expected=False)
def test_web_loader_metadata(mock_loader_config, mock_simple_html, httpx_mock):
    httpx_mock.add_response(
        url="https://example.com/docs",
        method="GET",
        text=mock_simple_html,
        status_code=200,
        headers={"Content-Type": "text/html"},
        is_reusable=True
    )

    loader = WebDocumentLoader(mock_loader_config)
    result = loader.process()
    doc = result.documents[0]

    # Check metadata fields
    assert doc.metadata.file_size > 0
    created_at = float(doc.metadata.created_at)
    assert created_at <= datetime.now().timestamp()
    assert created_at >= datetime(2024, 1, 1).timestamp()
