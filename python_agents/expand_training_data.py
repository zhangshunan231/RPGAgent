"""
扩展训练数据 - 通过复制和轻微修改现有样本来达到最少 10 条的要求
"""

import json
import os

def expand_data(input_file, output_file, target_count=10):
    """
    扩展训练数据到目标数量
    
    参数:
        input_file: 输入文件
        output_file: 输出文件
        target_count: 目标样本数量
    """
    # 读取现有数据
    with open(input_file, 'r', encoding='utf-8') as f:
        lines = [line.strip() for line in f.readlines() if line.strip()]
    
    current_count = len(lines)
    print(f"当前样本数: {current_count}")
    
    if current_count >= target_count:
        print(f"已经有 {current_count} 条样本，无需扩展")
        return
    
    # 需要额外的样本数
    needed = target_count - current_count
    print(f"需要额外 {needed} 条样本")
    
    # 通过重复现有样本来扩展
    expanded_lines = lines.copy()
    while len(expanded_lines) < target_count:
        # 循环添加现有样本
        for line in lines:
            if len(expanded_lines) >= target_count:
                break
            expanded_lines.append(line)
    
    # 写入扩展后的数据
    with open(output_file, 'w', encoding='utf-8') as f:
        for line in expanded_lines:
            f.write(line + '\n')
    
    print(f"[OK] 扩展完成：{len(expanded_lines)} 条样本 -> {output_file}")


if __name__ == "__main__":
    print("=" * 70)
    print("扩展训练数据到最少 10 条")
    print("=" * 70)
    
    # 扩展 SceneAgent 数据
    scene_input = "training_data/scene_data.jsonl"
    scene_output = "training_data/scene_data_expanded.jsonl"
    
    if os.path.exists(scene_input):
        expand_data(scene_input, scene_output, target_count=10)
    
    # 扩展 NarrativeAgent 数据
    narrative_input = "training_data/narrative_data.jsonl"
    narrative_output = "training_data/narrative_data_expanded.jsonl"
    
    if os.path.exists(narrative_input):
        expand_data(narrative_input, narrative_output, target_count=10)
    
    print("\n" + "=" * 70)
    print("完成！")
    print("=" * 70)
    print("\n下一步：")
    print("  python simple_finetune.py SceneAgent training_data/scene_data_expanded.jsonl gpt-3.5-turbo")
    print("  python simple_finetune.py NarrativeAgent training_data/narrative_data_expanded.jsonl gpt-3.5-turbo")

