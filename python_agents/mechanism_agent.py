import autogen
from llm_config import LLM_CONFIG
import sys

def create_mechanism_agent():
    print("[机制Agent] 开始创建MechanismAgent...")
    system_message = (
        "你是RPG机制设计专家。请根据输入的资产信息（每一类item和character），为每一类分别设计适合的RPG游戏机制，输出结构化建议。"
        "输出要求：\n"
        "- 输出的JSON数组中每个元素的name和type必须与输入资产列表完全一致，不允许出现未在资产列表中的内容。\n"
        "- 只为输入资产列表中的每个资产生成机制，资产之外的内容一律不要生成，哪怕是常见RPG物品也不要补充。\n"
        "- 针对每一类item和角色，单独给出机制建议。\n"
        "- 机制建议应包括：作用、交互方式、成长/升级、与其它系统的关系等。\n"
        "- 输出为JSON数组，每个元素包含type、name、mechanism字段。\n"
        "- 只输出JSON，不要多余解释。"
    )
    agent = autogen.AssistantAgent(
        name="MechanismAgent",
        llm_config=LLM_CONFIG,
        system_message=system_message
    )
    def debug_generate_reply(messages=None, sender=None, **kwargs):
        print("[机制Agent] 传递给LLM的内容:")
        if messages:
            for m in messages:
                print(f"role: {m.get('role')}, content: {m.get('content')}")
        return agent.__class__.generate_reply(agent, messages=messages, sender=sender, **kwargs)
    agent.generate_reply = debug_generate_reply
    print("[机制Agent] MechanismAgent创建完成")
    return agent 