import sys
import time

import autogen

from llm_config import LLM_CONFIG


def create_narrative_agent():
    print("[DEBUG] Python:", sys.executable)
    print("[DEBUG] autogen:", getattr(autogen, "__version__", "unknown"))

    try:
        import openai  # noqa: F401

        print("[DEBUG] openai:", getattr(openai, "__version__", "unknown"))
    except Exception:
        print("[DEBUG] openai: not available")

    print("[DEBUG] Using Qwen (DashScope) config:", LLM_CONFIG)
    print("[DEBUG] Creating NarrativeAgent...")
    start = time.time()

    agent = autogen.AssistantAgent(
        name="NarrativeAgent",
        llm_config=LLM_CONFIG,
        system_message=(
            "You are a narrative agent. Expand the user's RPG story idea into a complete story and a step breakdown.\n"
            "\n"
            "Rules:\n"
            "- Only step 1 can include the MainCharacter (hero).\n"
            "- Steps 2+ should not include the MainCharacter; use NPCs/enemies instead.\n"
            "- Use 4-5 steps.\n"
            "\n"
            "Return ONLY valid JSON with this exact schema:\n"
            "{\n"
            "  \"story\": \"...\",\n"
            "  \"steps\": [\n"
            "    {\n"
            "      \"step\": 1,\n"
            "      \"title\": \"...\",\n"
            "      \"location\": 0,\n"
            "      \"objective\": \"...\",\n"
            "      \"key_characters\": [\"...\"],\n"
            "      \"main_dialogues\": [{\"character\": \"...\", \"dialogue\": \"...\"}],\n"
            "      \"key_items\": [\"...\"]\n"
            "    }\n"
            "  ]\n"
            "}\n"
            "\n"
            "location must be an integer: 0=Village, 1=Forest, 2=Grassland, 3=Castle, 4=Cave.\n"
        ),
    )

    print("[DEBUG] NarrativeAgent created in", time.time() - start, "s")
    return agent
