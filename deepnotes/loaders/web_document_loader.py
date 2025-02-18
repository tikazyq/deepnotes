import time
from concurrent.futures import ThreadPoolExecutor, as_completed
from datetime import datetime
from threading import Lock
from typing import List, Optional
from urllib.parse import urljoin

import httpx
from boilerpy3 import extractors
from bs4 import BeautifulSoup
from llama_index.core.node_parser import TokenTextSplitter
from readability import Document
from tqdm import tqdm
from trafilatura import extract

from deepnotes.loaders.base_loader import BaseLoader
from deepnotes.models.loader_models import (
    DocumentChunk,
    DocumentLoadedData,
    FileMetadata,
    ProcessedDocument,
)
from deepnotes.models.source_models import SourceConfig, SourceType


class WebDocumentLoader(BaseLoader):
    def __init__(self, config: SourceConfig):
        super().__init__(config)
        self.visited_urls = set()
        self.visited_lock = Lock()
        self.splitter = TokenTextSplitter(
            chunk_size=self.config.options.get("chunk_size", 2000),
            chunk_overlap=self.config.options.get("chunk_overlap", 200),
        )
        self.timeout = self.config.options.get("timeout", 10)
        self.concurrency = self.config.options.get("concurrency", 10)
        self.retries = self.config.options.get("retries", 3)
        self.backoff_factor = self.config.options.get("backoff_factor", 1)

    @classmethod
    def get_source_type(cls) -> SourceType:
        return SourceType.WEB_DOCUMENT

    def validate_config(self) -> bool:
        return "start_urls" in self.config.connection and "url_pattern" in self.config.connection

    def _is_valid_url(self, url: str) -> bool:
        return url.startswith(self.config.connection["url_pattern"])

    @staticmethod
    def _extract_main_content(html: str) -> str:
        content = ""

        # Try Trafilatura first
        try:
            content = extract(html) or ""
            if content.strip():
                return content
        except:
            pass

        # Try Readability-lxml as second option
        try:
            doc = Document(html)
            content = doc.summary() or ""
            if content.strip():
                return BeautifulSoup(content, 'html.parser').get_text(separator='\n', strip=True)
        except:
            pass

        # Try boilerpy3 as third option
        try:
            extractor = extractors.ArticleExtractor()
            doc = extractor.get_content(html)
            return doc or ""
        except:
            pass

        # Final fallback to BeautifulSoup with improved logic
        try:
            soup = BeautifulSoup(html, 'html.parser')
            # Remove non-content elements
            for element in soup(['script', 'style', 'nav', 'footer', 'header', 'aside']):
                element.decompose()

            # Try common content containers in priority order
            selectors = [
                'article', 'main',
                '.main-content', '.article-body',
                '#content', '#main',
                'div[role="main"]',
                'div.content', 'div.post-content'
            ]

            for selector in selectors:
                elements = soup.select(selector)
                if elements:
                    text = '\n'.join(e.get_text(separator='\n', strip=True) for e in elements)
                    if len(text) > 200:  # Minimum content length threshold
                        return text

            # Fallback to body if no selectors matched
            body = soup.body
            if body:
                return body.get_text(separator='\n', strip=True)
        except Exception as e:
            print(f"Error in fallback extraction: {str(e)}")

        return content

    def _fetch_url(self, url: str) -> Optional[httpx.Response]:
        """Helper method with retry logic"""
        client = httpx.Client(
            headers={'User-Agent': 'Mozilla/5.0 (compatible; DeepNotes/1.0)'},
            timeout=httpx.Timeout(self.timeout),
            follow_redirects=True
        )

        for attempt in range(self.retries + 1):
            try:
                response = client.get(url)
                if response.status_code == 200:
                    return response
                # Retry on server errors
                if 500 <= response.status_code < 600:
                    raise httpx.HTTPStatusError(
                        f"Server error: {response.status_code}",
                        request=response.request,
                        response=response
                    )
            except (httpx.RequestError, httpx.HTTPStatusError) as e:
                if attempt < self.retries:
                    sleep_time = self.backoff_factor * (2 ** attempt)
                    print(f"Retrying {url} in {sleep_time}s (attempt {attempt + 1}/{self.retries})")
                    time.sleep(sleep_time)
                else:
                    print(f"Failed to fetch {url} after {self.retries} attempts: {str(e)}")
        return None

    def _crawl(self, start_urls: List[str]) -> List[str]:
        with ThreadPoolExecutor(max_workers=self.concurrency) as executor:
            futures = {}
            url_queue = set(start_urls)
            progress_bar = tqdm(desc="Discovering URLs", unit="urls")

            while url_queue:
                batch = list(url_queue)
                url_queue.clear()

                for url in batch:
                    if url not in self.visited_urls:
                        with self.visited_lock:
                            self.visited_urls.add(url)
                        futures[executor.submit(self._process_url, url)] = url
                        progress_bar.update(1)

                for future in as_completed(futures):
                    url = futures.pop(future)
                    try:
                        new_urls = future.result()
                        url_queue.update(new_urls)
                    except Exception as e:
                        print(f"Error processing {url}: {str(e)}")

            progress_bar.close()
            return list(self.visited_urls)

    def _process_url(self, url: str) -> List[str]:
        try:
            response = self._fetch_url(url)
            if response:
                soup = BeautifulSoup(response.text, 'html.parser')
                new_urls = []
                for link in soup.find_all('a', href=True):
                    absolute_url = urljoin(url, link['href'])
                    clean_url = absolute_url.split('#')[0].split('?')[0]
                    if self._is_valid_url(clean_url):
                        with self.visited_lock:
                            if clean_url not in self.visited_urls:
                                new_urls.append(clean_url)
                return new_urls
        except Exception as e:
            print(f"Error crawling {url}: {str(e)}")
        return []

    def process(self) -> DocumentLoadedData:
        start_urls = self.config.connection["start_urls"]
        if not isinstance(start_urls, list):
            start_urls = [start_urls]

        processed_docs = []

        # First show crawling progress
        urls = self._crawl(start_urls)
        print(f"Found {len(urls)} URLs to process")

        def process_single_url(url: str) -> Optional[ProcessedDocument]:
            try:
                response = self._fetch_url(url)
                if response:
                    content = self._extract_main_content(response.text)
                    if content:
                        chunks = self.splitter.split_text(content)
                        return ProcessedDocument(
                            metadata=FileMetadata(
                                file_path=url,
                                file_size=len(content),
                                file_type="web_document",
                                created_at=str(datetime.now().timestamp())
                            ),
                            chunks=[DocumentChunk(text=chunk, index=idx) for idx, chunk in enumerate(chunks)],
                            raw_content=content
                        )
            except Exception as e:
                print(f"Error processing {url}: {str(e)}")
            return None

        with ThreadPoolExecutor(max_workers=self.concurrency) as executor:
            futures = [executor.submit(process_single_url, url) for url in urls]

            # Create progress bar for processing
            with tqdm(total=len(futures), desc="Processing web documents", unit="docs") as pbar:
                for future in as_completed(futures):
                    result = future.result()
                    if result:
                        processed_docs.append(result)
                    pbar.update(1)

        return DocumentLoadedData(
            source_type=SourceType.WEB_DOCUMENT,
            global_metadata={
                "source_path": self.config.connection["url_pattern"],
                "file_types": ["web_document"],
                "total_chunks": sum(len(doc.chunks) for doc in processed_docs)
            },
            documents=processed_docs
        )
