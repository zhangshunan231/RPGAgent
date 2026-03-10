# llm_config.py
DEEPSEEK_API_KEY = "sk-3ce52640cf5249959f295575110e281b"

LLM_CONFIG = {
    "config_list": [
        {
            "model": "deepseek-chat",
            "api_key": DEEPSEEK_API_KEY,
            "base_url": "https://api.deepseek.com"
        }
    ],
    "temperature": 0.7,
    "seed": None,
} 