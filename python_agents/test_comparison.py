#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""
对比测试：简化提示词前后的效果
"""

import sys
import os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from narrative_agent import create_narrative_agent
import json

def test_multiple_scenarios():
    print("=" * 70)
    print("简化提示词后的 NarrativeAgent 测试")
    print("=" * 70)
    
    agent = create_narrative_agent()
    
    test_cases = [
        "一个年轻的法师要寻找失落的魔法书",
        "勇士要击败占领城堡的黑暗领主",
        "探险家在神秘洞穴中寻找古代宝藏",
    ]
    
    for i, test_input in enumerate(test_cases, 1):
        print(f"\n{'='*70}")
        print(f"测试 {i}/3: {test_input}")
        print("="*70)
        
        try:
            response = agent.generate_reply(
                messages=[{"role": "user", "content": test_input}]
            )
            
            # 尝试解析JSON验证格式
            try:
                data = json.loads(response)
                
                # 验证关键字段
                assert "story" in data, "缺少 story 字段"
                assert "steps" in data, "缺少 steps 字段"
                assert len(data["steps"]) > 0, "steps 不能为空"
                
                # 验证第一个step包含主角
                step1 = data["steps"][0]
                assert step1["step"] == 1, "第一个step应该是1"
                
                # 验证location是数字
                for step in data["steps"]:
                    assert isinstance(step["location"], int), f"location应该是数字，但是 {type(step['location'])}"
                    assert 0 <= step["location"] <= 4, f"location应该在0-4之间，但是 {step['location']}"
                
                print(f"\n[SUCCESS] 格式验证通过！")
                print(f"- 故事背景: {data['story'][:50]}...")
                print(f"- 步骤数量: {len(data['steps'])}")
                print(f"- 第一步: {step1['title']}")
                
            except json.JSONDecodeError as e:
                print(f"\n[ERROR] JSON格式错误: {e}")
                print(f"响应内容: {response}")
            except AssertionError as e:
                print(f"\n[ERROR] 格式验证失败: {e}")
                
        except Exception as e:
            print(f"\n[ERROR] 测试失败: {e}")
            import traceback
            traceback.print_exc()
    
    print(f"\n{'='*70}")
    print("[完成] 所有测试完成")
    print("="*70)

if __name__ == "__main__":
    test_multiple_scenarios()

