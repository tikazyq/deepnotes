from autogen_agentchat.messages import TextMessage
from autogen_core import CancellationToken
from autogen_ext.agents.file_surfer import FileSurfer

from deepnotes.llm.client import get_model_client


class DocumentProcessorAgent(FileSurfer):
    def __init__(self):
        super().__init__(
            name="DocumentProcessorAgent",
            model_client=get_model_client(),
        )


async def run():
    agent = DocumentProcessorAgent()
    response = await agent.on_messages(
        [
            TextMessage(
                # content="Hello, can you take a look at the content of these documents located at /home/marvin/projects/tikazyq/deepnotes/data/academic?",
                content="What kind of category is for the documents in /home/marvin/projects/tikazyq/deepnotes/data/academic?",
                source="user",
            )
        ],
        cancellation_token=CancellationToken(),
    )
    print(response.inner_messages)
    print(response.chat_message)


if __name__ == "__main__":
    import asyncio

    asyncio.run(run())
