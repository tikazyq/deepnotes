from setuptools import find_packages, setup

setup(
    name="deepnotes",
    version="0.1.0",
    packages=find_packages(),
    install_requires=[
        "autogen>=0.4.0",
        "pyyaml>=6.0",
        "python-dotenv>=0.19.0",
        "tqdm>=4.0.0",
        "sqlalchemy>=1.4.0",
    ],
    entry_points={
        "console_scripts": [
            "deepnotes=deepnotes.main:run_pipeline",
        ],
    },
    include_package_data=True,
    package_data={
        "deepnotes": ["config.yml"],
    },
    author="Your Name",
    author_email="tikazyq@163.com",
    description="AI-powered knowledge extraction and organization framework",
    long_description=open("README.md").read(),
    long_description_content_type="text/markdown",
    url="https://github.com/tikazyq/deepnotes",
    classifiers=[
        "Programming Language :: Python :: 3",
        "License :: OSI Approved :: MIT License",
        "Operating System :: OS Independent",
    ],
    python_requires=">=3.11",
)
