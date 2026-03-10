import autogen
from llm_config import LLM_CONFIG
import json
import re
from typing import Dict, List, Any, Optional, Union

def create_codegen_agent():
    system_message = """
    你是Unity RPG游戏的代码生成专家。你的任务是根据每个场景对象的描述、类型和机制需求，为每个对象分配最合适的代码模板和参数。

    你的输出应该是一个JSON对象，包含以下字段：
    - object_mechanisms: 一个字典，key为对象名称，value为分配的机制类型（如\"CharacterController\"、\"Dialogue\"、\"EnemyAI\"、\"Trader\"、\"ItemCollector\"等）
    - dialogue_lines: （可选）如有对话机制，为相关对象提供对话内容数组
    - 其他参数字段...

    可用的机制类型包括：
    - CharacterController: 主角控制器
    - Dialogue: 对话系统
    - EnemyAI: 敌人AI
    - Trader: 商人系统
    - ItemCollector: 道具收集系统（用于Props类型的对象）

    例如：
    {
      "object_mechanisms": {
        "shunan": "CharacterController",
        "nanshu": "Dialogue",
        "enemy1": "EnemyAI",
        "enemy2": "Trader",
        "treasure_chest": "ItemCollector"
      },
      "dialogue_lines": {
        "nanshu": ["你好，勇士！", "欢迎来到村庄。", "祝你好运！"]
      }
    }

    你将收到如下输入：
    - scene_objects: 场景中的对象列表，每个对象包含name、type、displayName、description等
    - input: 机制需求描述

    请根据每个对象的描述和机制需求，智能分配最合适的机制类型和参数，输出如上格式的JSON。
    
    特别注意：
    - 对于type为"Props"或"ItemCollector"的对象，应该分配"ItemCollector"机制
    - 对于type为"MainCharacter"的对象，应该分配"CharacterController"机制
    - 对于type为"NPC"的对象，应该分配"Dialogue"机制
    - 对于type为"Enemy"的对象，应该分配"EnemyAI"机制
    """
    
    agent = autogen.AssistantAgent(
        name="CodeGenAgent",
        llm_config=LLM_CONFIG,
        system_message=system_message
    )
    
    def debug_generate_reply(messages=None, sender=None, **kwargs):
        if messages is None:
            messages = []
            
        print("[CodeGen Agent] 传递给LLM的内容:")
        for m in messages:
            content = m.get('content', '')
            if content and isinstance(content, str) and len(content) > 200:
                truncated = content[0:200]
                print(f"role: {m.get('role')}, content: {truncated}...")
            else:
                print(f"role: {m.get('role')}, content: {content}")
        
        # 处理输入中的场景对象信息
        user_message = ""
        for m in messages:
            if m.get('role') == 'user':
                content = m.get('content')
                if isinstance(content, str):
                    user_message = content
                break
        
        # 解析JSON输入
        try:
            if user_message and user_message.strip().startswith('{'):
                input_data = json.loads(user_message)
                
                # 检查是否包含场景对象信息
                if 'scene_objects' in input_data and isinstance(input_data['scene_objects'], list):
                    scene_objects = input_data['scene_objects']
                    print(f"[CodeGen Agent] 收到场景对象: {len(scene_objects)} 个")
                    
                    # 添加场景对象信息到提示中
                    scene_objects_info = "场景对象信息:\n"
                    for i, obj in enumerate(scene_objects):
                        if isinstance(obj, dict):
                            scene_objects_info += f"{i+1}. 名称: {obj.get('name', 'Unknown')}, 类型: {obj.get('type', 'Unknown')}, 显示名称: {obj.get('displayName', '')}\n"
                    
                    # 修改最后一条用户消息，添加场景对象信息
                    for i, m in enumerate(messages):
                        if m.get('role') == 'user':
                            # 保持原始内容，但添加场景对象信息
                            if isinstance(m['content'], str):
                                messages[i]['content'] += f"\n\n{scene_objects_info}\n\n请根据这些场景对象信息，结合机制需求，为每个对象分配最合适的机制类型和参数，输出object_mechanisms和相关参数。"
                            break
        except Exception as e:
            print(f"[CodeGen Agent] 解析输入时出错: {e}")
        
        response = agent.__class__.generate_reply(agent, messages=messages, sender=sender, **kwargs)
        
        print("[CodeGen Agent] LLM返回:")
        if isinstance(response, str) and len(response) > 200:
            truncated = response[0:200]
            print(f"{truncated}...")
        else:
            print(response)
        
        # 只保留LLM输出的object_mechanisms，不再自动补全
        return response
    
    agent.generate_reply = debug_generate_reply
    return agent 