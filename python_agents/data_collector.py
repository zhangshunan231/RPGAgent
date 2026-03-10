"""
数据收集脚本 - 用于 fine-tuning
自动记录 agent 的输入输出，保存为 DeepSeek fine-tuning 格式
"""

import json
import os
from datetime import datetime

class DataCollector:
    def __init__(self, output_dir="training_data"):
        self.output_dir = output_dir
        os.makedirs(output_dir, exist_ok=True)
        self.session_file = os.path.join(
            output_dir, 
            f"session_{datetime.now().strftime('%Y%m%d_%H%M%S')}.jsonl"
        )
    
    def log_interaction(self, agent_name, system_message, user_input, assistant_output):
        """
        记录一次交互
        
        参数:
            agent_name: Agent名称 (NarrativeAgent, SceneAgent等)
            system_message: 系统提示词
            user_input: 用户输入
            assistant_output: Agent输出
        """
        # DeepSeek fine-tuning 格式
        training_sample = {
            "messages": [
                {"role": "system", "content": system_message},
                {"role": "user", "content": user_input},
                {"role": "assistant", "content": assistant_output}
            ]
        }
        
        # 追加到 JSONL 文件
        with open(self.session_file, 'a', encoding='utf-8') as f:
            f.write(json.dumps(training_sample, ensure_ascii=False) + '\n')
        
        print(f"[DataCollector] 已记录 {agent_name} 的交互数据")
    
    def merge_all_sessions(self, output_file="merged_training_data.jsonl"):
        """
        合并所有 session 文件为一个训练文件
        """
        output_path = os.path.join(self.output_dir, output_file)
        all_samples = []
        
        for filename in os.listdir(self.output_dir):
            if filename.startswith("session_") and filename.endswith(".jsonl"):
                filepath = os.path.join(self.output_dir, filename)
                with open(filepath, 'r', encoding='utf-8') as f:
                    for line in f:
                        all_samples.append(json.loads(line))
        
        # 写入合并文件
        with open(output_path, 'w', encoding='utf-8') as f:
            for sample in all_samples:
                f.write(json.dumps(sample, ensure_ascii=False) + '\n')
        
        print(f"[DataCollector] 已合并 {len(all_samples)} 条样本到 {output_path}")
        return output_path
    
    def validate_data(self, min_samples=10):
        """
        验证数据质量
        """
        try:
            with open(self.session_file, 'r', encoding='utf-8') as f:
                lines = f.readlines()
                count = len(lines)
                
                if count < min_samples:
                    print(f"[警告] 样本数量不足：{count}/{min_samples}")
                    return False
                
                # 检查格式
                for i, line in enumerate(lines[:3]):  # 检查前3条
                    sample = json.loads(line)
                    if "messages" not in sample:
                        print(f"[错误] 第{i+1}条样本格式错误")
                        return False
                
                print(f"[DataCollector] 数据验证通过：{count} 条样本")
                return True
        except Exception as e:
            print(f"[错误] 数据验证失败: {e}")
            return False


# 全局实例
_collector = None

def get_collector():
    """获取全局 DataCollector 实例"""
    global _collector
    if _collector is None:
        _collector = DataCollector()
    return _collector

