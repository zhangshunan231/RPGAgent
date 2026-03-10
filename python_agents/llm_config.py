# llm_config.py
# 智增增 API 配置（支持 GPT-4 和 Fine-tuning）
# 官网: https://gpt.zhizengzeng.com/#/login
# 从智增增后台获取你的 API Key

ZZZ_API_KEY = "sk-zk2fab5e0e809a55ef570656a102a6e1894691033c50d9e4"  # 请替换为你在智增增后台获取的 key

# 默认配置（用于其他 Agent）
LLM_CONFIG = {
    "config_list": [
        {
            "model": "gpt-4o",  # 可选: gpt-4, gpt-4o, gpt-4-turbo, gpt-3.5-turbo 等
            "api_key": ZZZ_API_KEY,
            "base_url": "https://api.zhizengzeng.com/v1"  # 智增增的 base_url
        }
    ],
    "temperature": 0.7,
    "seed": None,
}

# NarrativeAgent 微调模型配置
NARRATIVE_FINETUNED_CONFIG = {
    "config_list": [
        {
            "model": "ft:gpt-4o-2024-08-06:zzzorg::CZu0egBZ",  # 微调后的模型
            "api_key": ZZZ_API_KEY,
            "base_url": "https://api.zhizengzeng.com/v1"
        }
    ],
    "temperature": 0.7,
    "seed": None,
}

# 备份：DeepSeek 配置（如果需要切换回来）
# DEEPSEEK_API_KEY = "sk-3ce52640cf5249959f295575110e281b"
# LLM_CONFIG = {
#     "config_list": [{"model": "deepseek-chat", "api_key": DEEPSEEK_API_KEY, "base_url": "https://api.deepseek.com"}],
#     "temperature": 0.7,
#     "seed": None,
# } 