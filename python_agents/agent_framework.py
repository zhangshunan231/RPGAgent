"""
多智能体RPG开发辅助框架（基于Microsoft Autogen）
"""

import autogen
import os

# # 你需要在环境变量中设置OPENAI_API_KEY，或在此处直接填写
# OPENAI_API_KEY = os.environ.get("OPENAI_API_KEY", "sk-3ce52640cf5249959f295575110e281b")

# llm_config = {
#     "config_list": [
#         {
#             "model": "deepseek-chat",
#             "api_key": OPENAI_API_KEY,
#             "base_url": "https://api.deepseek.com",
#         }
#     ],
#     "temperature": 0.7,
# }

from narrative_agent import create_narrative_agent
from scene_agent import create_scene_agent
from mechanism_agent import create_mechanism_agent
from codegen_agent import create_codegen_agent

# 多Agent协作流程

def run_multi_agent_process(user_input: str):
    narrative_agent = create_narrative_agent()
    scene_agent = create_scene_agent()
    mechanism_agent = create_mechanism_agent()
    codegen_agent = create_codegen_agent()

    narrative_response = narrative_agent.generate_reply(
        messages=[{"role": "user", "content": user_input}]
    )
    print("[DEBUG] NarrativeAgent返回:", narrative_response)
    if isinstance(narrative_response, dict):
        story_data = narrative_response.get("content", str(narrative_response))
    elif isinstance(narrative_response, str):
        story_data = narrative_response
    else:
        story_data = ""

    scene_response = scene_agent.generate_reply(
        messages=[{"role": "user", "content": story_data}]
    )
    print("[DEBUG] SceneAgent返回:", scene_response)
    if isinstance(scene_response, dict):
        scene_data = scene_response.get("content", str(scene_response))
    elif isinstance(scene_response, str):
        scene_data = scene_response
    else:
        scene_data = ""

    mechanism_response = mechanism_agent.generate_reply(
        messages=[{"role": "user", "content": f"故事和场景参数如下：\n{story_data}\n{scene_data}"}]
    )
    print("[DEBUG] MechanismAgent返回:", mechanism_response)
    if isinstance(mechanism_response, dict):
        mechanics_data = mechanism_response.get("content", str(mechanism_response))
    elif isinstance(mechanism_response, str):
        mechanics_data = mechanism_response
    else:
        mechanics_data = ""

    code_response = codegen_agent.generate_reply(
        messages=[{"role": "user", "content": mechanics_data}]
    )
    print("[DEBUG] CodeGenAgent返回: [代码已生成]")
    if isinstance(code_response, dict):
        code_data = code_response.get("content", str(code_response))
    elif isinstance(code_response, str):
        code_data = code_response
    else:
        code_data = ""

    return {
        "story": story_data,
        "scene": scene_data,
        "mechanics": mechanics_data,
        "code": code_data
    } 