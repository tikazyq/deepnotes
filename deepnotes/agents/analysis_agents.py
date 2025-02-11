from autogen_agentchat.agents import AssistantAgent
from autogen_agentchat.base import TaskResult
from autogen_agentchat.conditions import TextMentionTermination
from autogen_agentchat.teams import RoundRobinGroupChat
from autogen_ext.agents.file_surfer import FileSurfer

from deepnotes.llm.client import get_model_client

DEFAULT_SYSTEM_MESSAGE = """An agent that can analyze text data.

Please be aware that when you want to list or read a file, you need to ask a FileSurfer (an agent in the team) to do it.
"""


class AnalysisAgent(AssistantAgent):
    def __init__(self, system_message=None):
        super().__init__(
            name="AnalysisAgent",
            model_client=get_model_client(),
            system_message=system_message or "",
        )


async def run():
    analysis_agent = AnalysisAgent()
    document_processor_agent = FileSurfer("FileSurfer", get_model_client())
    text_termination = TextMentionTermination("APPROVE")
    team = RoundRobinGroupChat(
        participants=[analysis_agent, document_processor_agent],
        termination_condition=text_termination,
    )
    async for message in team.run_stream(
        task="What are the main topics of document files at /home/marvin/projects/tikazyq/deepnotes/data/academic"
    ):
        if isinstance(message, TaskResult):
            print("Stop Reason:", message.stop_reason)
        else:
            print("---")
            print(message.source + ":")
            print(message.content)


if __name__ == "__main__":
    import asyncio

    asyncio.run(run())
