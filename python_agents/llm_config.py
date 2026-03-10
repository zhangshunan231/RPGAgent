# llm_config.py
# Qwen (DashScope) OpenAI-compatible API config
# Env: DASHSCOPE_API_KEY

import os

DASHSCOPE_API_KEY = os.getenv("DASHSCOPE_API_KEY", "")
DASHSCOPE_BASE_URL = "https://dashscope.aliyuncs.com/compatible-mode/v1"

# Default config for all agents
LLM_CONFIG = {
    "config_list": [
        {
            "model": "qwen-plus",
            "api_key": DASHSCOPE_API_KEY,
            "base_url": DASHSCOPE_BASE_URL,
        }
    ],
    "temperature": 0.7,
    "seed": None,
}

# NarrativeAgent uses the same Qwen config (no finetune)
NARRATIVE_FINETUNED_CONFIG = LLM_CONFIG
