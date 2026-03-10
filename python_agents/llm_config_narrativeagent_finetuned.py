# NarrativeAgent Fine-tuned 模型配置
# 生成时间: 2025-11-09 15:26:17

ZZZ_API_KEY = "sk-zk2fab5e0e809a55ef570656a102a6e1894691033c50d9e4"

LLM_CONFIG = {
    "config_list": [
        {
            "model": "ft:gpt-4o-2024-08-06:zzzorg::CZttbayq",  # Fine-tuned 模型
            "api_key": ZZZ_API_KEY,
            "base_url": "https://api.zhizengzeng.com/v1"
        }
    ],
    "temperature": 0.7,
    "seed": None,
}

# 使用方法：
# 在 narrativeagent.py 中，将 from llm_config import LLM_CONFIG
# 改为 from llm_config_narrativeagent_finetuned import LLM_CONFIG
