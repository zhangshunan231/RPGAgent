from agent_framework import run_multi_agent_process
from llm_config import LLM_CONFIG
print("LLM_CONFIG:", LLM_CONFIG)
if __name__ == "__main__":
    user_input = input("请输入你的RPG故事设想：")
    try:
        result = run_multi_agent_process(user_input)
        print("\n=== 故事扩展 ===")
        print(result["story"])
        print("\n=== 场景参数 ===")
        print(result["scene"])
        print("\n=== 机制设计 ===")
        print(result["mechanics"])
        print("\n=== 生成代码 ===")
        print(result["code"])
    except Exception as e:
        import traceback
        print("[ERROR] 发生异常：", e)
        traceback.print_exc() 