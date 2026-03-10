#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""
测试微调后的 NarrativeAgent 模型
"""

import sys
import os

# 添加当前目录到路径
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from narrative_agent import create_narrative_agent

def test_finetuned_narrative():
    print("=" * 70)
    print("测试微调后的 NarrativeAgent")
    print("=" * 70)
    
    # 创建使用微调模型的 NarrativeAgent
    agent = create_narrative_agent()
    
    # 测试用例
    test_input = "一个勇敢的骑士要拯救被困在塔中的公主"
    
    print(f"\n[输入] {test_input}\n")
    print("-" * 70)
    print("[NarrativeAgent 生成中...]\n")
    
    try:
        response = agent.generate_reply(
            messages=[{"role": "user", "content": test_input}]
        )
        
        print("[输出]")
        print(response)
        print("\n" + "=" * 70)
        print("[SUCCESS] 微调模型测试成功！")
        print("=" * 70)
        
    except Exception as e:
        print(f"\n[ERROR] 测试失败: {e}")
        import traceback
        traceback.print_exc()

if __name__ == "__main__":
    test_finetuned_narrative()

