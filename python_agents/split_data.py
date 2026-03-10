"""
数据分离脚本
将混合的训练数据按 Agent 类型分离
"""

import json
import os

def split_training_data(input_file='training_data/merged_training_data.jsonl'):
    """
    将混合的训练数据分离为不同 Agent 的数据
    
    参数:
        input_file: 输入文件路径（合并后的 JSONL 文件）
    """
    if not os.path.exists(input_file):
        print(f"❌ 错误：文件不存在 '{input_file}'")
        print("请先运行数据收集和合并：")
        print("  python -c \"from data_collector import get_collector; get_collector().merge_all_sessions()\"")
        return
    
    print(f"📖 读取数据: {input_file}")
    
    with open(input_file, 'r', encoding='utf-8') as f:
        lines = f.readlines()
    
    print(f"✅ 读取完成，共 {len(lines)} 条数据")
    
    # 分类存储
    scene_data = []
    narrative_data = []
    mechanism_data = []
    codegen_data = []
    other_data = []
    
    for i, line in enumerate(lines):
        try:
            data = json.loads(line)
            
            # 检查格式
            if 'messages' not in data or len(data['messages']) < 3:
                print(f"⚠️ 跳过第 {i+1} 条：格式不正确")
                continue
            
            # 获取 system message
            system_msg = data['messages'][0]['content']
            
            # 分类
            if '场景Agent' in system_msg or 'SceneAgent' in system_msg:
                scene_data.append(line)
            elif '叙事Agent' in system_msg or 'NarrativeAgent' in system_msg:
                narrative_data.append(line)
            elif '机制Agent' in system_msg or 'MechanismAgent' in system_msg:
                mechanism_data.append(line)
            elif 'CodeGen' in system_msg or '代码生成' in system_msg:
                codegen_data.append(line)
            else:
                other_data.append(line)
        
        except Exception as e:
            print(f"⚠️ 跳过第 {i+1} 条：解析失败 ({e})")
            continue
    
    # 保存分离后的数据
    output_dir = 'training_data'
    os.makedirs(output_dir, exist_ok=True)
    
    print("\n" + "=" * 60)
    print("分离结果：")
    print("=" * 60)
    
    if scene_data:
        output_file = os.path.join(output_dir, 'scene_data.jsonl')
        with open(output_file, 'w', encoding='utf-8') as f:
            f.writelines(scene_data)
        print(f"✅ SceneAgent: {len(scene_data)} 条 → {output_file}")
    else:
        print(f"⚠️ SceneAgent: 0 条（未收集到数据）")
    
    if narrative_data:
        output_file = os.path.join(output_dir, 'narrative_data.jsonl')
        with open(output_file, 'w', encoding='utf-8') as f:
            f.writelines(narrative_data)
        print(f"✅ NarrativeAgent: {len(narrative_data)} 条 → {output_file}")
    else:
        print(f"⚠️ NarrativeAgent: 0 条（未收集到数据）")
    
    if mechanism_data:
        output_file = os.path.join(output_dir, 'mechanism_data.jsonl')
        with open(output_file, 'w', encoding='utf-8') as f:
            f.writelines(mechanism_data)
        print(f"✅ MechanismAgent: {len(mechanism_data)} 条 → {output_file}")
    
    if codegen_data:
        output_file = os.path.join(output_dir, 'codegen_data.jsonl')
        with open(output_file, 'w', encoding='utf-8') as f:
            f.writelines(codegen_data)
        print(f"✅ CodeGenAgent: {len(codegen_data)} 条 → {output_file}")
    
    if other_data:
        output_file = os.path.join(output_dir, 'other_data.jsonl')
        with open(output_file, 'w', encoding='utf-8') as f:
            f.writelines(other_data)
        print(f"⚠️ 其他: {len(other_data)} 条 → {output_file}")
    
    print("=" * 60)
    
    # 提示下一步
    print("\n📋 下一步：")
    if scene_data:
        print(f"  python simple_finetune.py SceneAgent training_data/scene_data.jsonl")
    if narrative_data:
        print(f"  python simple_finetune.py NarrativeAgent training_data/narrative_data.jsonl")
    
    # 警告
    if len(scene_data) < 50:
        print(f"\n⚠️ 警告：SceneAgent 数据量不足 ({len(scene_data)}/50)，建议收集更多数据")
    if len(narrative_data) < 50:
        print(f"⚠️ 警告：NarrativeAgent 数据量不足 ({len(narrative_data)}/50)，建议收集更多数据")


if __name__ == "__main__":
    split_training_data()

