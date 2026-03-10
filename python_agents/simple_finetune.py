"""
智增增 Fine-tuning 超简单脚本
根据智增增官方文档编写，支持新旧版本的 openai 包
"""

import openai
import time
import json
from llm_config import ZZZ_API_KEY

# 智增增配置
API_SECRET_KEY = ZZZ_API_KEY
BASE_URL = "https://api.zhizengzeng.com/v1"

# 检测 openai 版本
try:
    from openai import OpenAI
    OPENAI_NEW_VERSION = True
    print(f"[检测] 使用新版本 openai (v{openai.__version__})")
except ImportError:
    OPENAI_NEW_VERSION = False
    print(f"[检测] 使用旧版本 openai (v{openai.__version__})")


# ==================== 步骤 1: 上传训练数据 ====================
def upload_file(file_path):
    """
    上传训练数据文件
    
    参数:
        file_path: JSONL 文件路径
    返回:
        file_id: 文件 ID
    """
    print(f"\n[步骤 1/4] 上传训练数据: {file_path}")
    
    if OPENAI_NEW_VERSION:
        # 新版本
        client = OpenAI(api_key=API_SECRET_KEY, base_url=BASE_URL)
        with open(file_path, "rb") as f:
            resp = client.files.create(file=f, purpose='fine-tune')
        file_id = resp.id
    else:
        # 旧版本
        openai.api_key = API_SECRET_KEY
        openai.api_base = BASE_URL
        resp = openai.File.create(
            file=open(file_path, "rb"),
            purpose='fine-tune'
        )
        file_id = resp['id']
    
    print(f"[OK] 上传成功！文件ID: {file_id}")
    return file_id


# ==================== 步骤 2: 创建训练任务 ====================
def create_finetune_job(file_id, model="gpt-3.5-turbo"):
    """
    创建 fine-tuning 任务
    
    参数:
        file_id: 训练数据文件 ID
        model: 基础模型 (gpt-3.5-turbo, gpt-4o 等)
    返回:
        job_id: 任务 ID
    """
    print(f"\n[步骤 2/4] 创建训练任务（基础模型: {model}）")
    
    try:
        if OPENAI_NEW_VERSION:
            # 新版本
            client = OpenAI(api_key=API_SECRET_KEY, base_url=BASE_URL)
            resp = client.fine_tuning.jobs.create(
                training_file=file_id,
                model=model
            )
            print(f"[调试] API 响应: {resp}")
            
            # 尝试多种方式获取 job_id
            job_id = None
            if hasattr(resp, 'id'):
                job_id = resp.id
            elif isinstance(resp, dict) and 'id' in resp:
                job_id = resp['id']
            else:
                print(f"[警告] 无法从响应中提取 job_id，完整响应: {resp}")
                
        else:
            # 旧版本
            openai.api_key = API_SECRET_KEY
            openai.api_base = BASE_URL
            resp = openai.FineTuningJob.create(
                training_file=file_id,
                model=model
            )
            print(f"[调试] API 响应: {resp}")
            job_id = resp['id'] if isinstance(resp, dict) else resp.id
        
        if job_id:
            print(f"[OK] 任务创建成功！任务ID: {job_id}")
        else:
            print(f"[ERROR] 任务创建失败：无法获取任务ID")
            print(f"完整响应: {resp}")
        
        return job_id
        
    except Exception as e:
        print(f"[ERROR] 创建任务失败: {e}")
        import traceback
        traceback.print_exc()
        return None


# ==================== 步骤 3: 检查训练状态 ====================
def check_status(job_id):
    """
    检查 fine-tuning 任务状态
    
    参数:
        job_id: 任务 ID
    返回:
        status: 状态 (validating_files, running, succeeded, failed, cancelled)
        fine_tuned_model: fine-tuned 模型名称（如果完成）
    """
    if not job_id:
        print(f"[错误] job_id 为空！")
        return None, None
    
    try:
        if OPENAI_NEW_VERSION:
            # 新版本
            client = OpenAI(api_key=API_SECRET_KEY, base_url=BASE_URL)
            resp = client.fine_tuning.jobs.retrieve(job_id)
            status = resp.status
            fine_tuned_model = resp.fine_tuned_model
        else:
            # 旧版本
            openai.api_key = API_SECRET_KEY
            openai.api_base = BASE_URL
            resp = openai.FineTuningJob.retrieve(job_id)
            status = resp['status']
            fine_tuned_model = resp.get('fine_tuned_model')
        
        return status, fine_tuned_model
    except Exception as e:
        print(f"[错误] 检查状态失败: {e}")
        return None, None


def wait_for_completion(job_id, check_interval=60):
    """
    等待训练完成
    
    参数:
        job_id: 任务 ID
        check_interval: 检查间隔（秒），默认 60 秒
    返回:
        fine_tuned_model: fine-tuned 模型名称
    """
    print(f"\n[步骤 3/4] 等待训练完成（每 {check_interval} 秒检查一次）")
    print(f"[调试] job_id = {job_id}")
    print("[WAIT] 训练中，请耐心等待...")
    
    if not job_id:
        print("[错误] job_id 为空，无法检查状态！")
        return None
    
    while True:
        status, fine_tuned_model = check_status(job_id)
        
        if status is None:
            print("[错误] 无法获取任务状态，停止等待")
            return None
        
        if status == "succeeded":
            print(f"[SUCCESS] 训练完成！模型名称: {fine_tuned_model}")
            return fine_tuned_model
        elif status == "failed":
            print(f"[FAILED] 训练失败！")
            return None
        elif status in ["validating_files", "queued", "running"]:
            print(f"[WAIT] 当前状态: {status}，继续等待...")
            time.sleep(check_interval)
        else:
            print(f"[WARN] 未知状态: {status}")
            return None


# ==================== 步骤 4: 生成配置文件 ====================
def save_config(agent_name, fine_tuned_model):
    """
    保存 fine-tuned 模型配置
    
    参数:
        agent_name: Agent 名称 (SceneAgent, NarrativeAgent)
        fine_tuned_model: fine-tuned 模型名称
    """
    print(f"\n[步骤 4/4] 保存配置文件")
    
    config_file = f"llm_config_{agent_name.lower()}_finetuned.py"
    
    config_content = f'''# {agent_name} Fine-tuned 模型配置
# 生成时间: {time.strftime("%Y-%m-%d %H:%M:%S")}

ZZZ_API_KEY = "{API_SECRET_KEY}"

LLM_CONFIG = {{
    "config_list": [
        {{
            "model": "{fine_tuned_model}",  # Fine-tuned 模型
            "api_key": ZZZ_API_KEY,
            "base_url": "https://api.zhizengzeng.com/v1"
        }}
    ],
    "temperature": 0.7,
    "seed": None,
}}

# 使用方法：
# 在 {agent_name.lower()}.py 中，将 from llm_config import LLM_CONFIG
# 改为 from llm_config_{agent_name.lower()}_finetuned import LLM_CONFIG
'''
    
    with open(config_file, 'w', encoding='utf-8') as f:
        f.write(config_content)
    
    print(f"[OK] 配置已保存到: {config_file}")


# ==================== 完整流程 ====================
def finetune_agent(training_file, agent_name, model="gpt-3.5-turbo"):
    """
    完整的 fine-tuning 流程
    
    参数:
        training_file: 训练数据文件路径 (JSONL)
        agent_name: Agent 名称 (SceneAgent, NarrativeAgent)
        model: 基础模型 (gpt-3.5-turbo, gpt-4o)
    """
    print("=" * 70)
    print(f"开始 Fine-tune {agent_name}")
    print("=" * 70)
    
    # 步骤 1: 上传文件
    file_id = upload_file(training_file)
    
    # 步骤 2: 创建任务
    job_id = create_finetune_job(file_id, model)
    
    # 步骤 3: 等待完成
    fine_tuned_model = wait_for_completion(job_id)
    
    if fine_tuned_model:
        # 步骤 4: 保存配置
        save_config(agent_name, fine_tuned_model)
        
        print("\n" + "=" * 70)
        print(f"🎉 {agent_name} Fine-tuning 完成！")
        print("=" * 70)
        print(f"\n模型名称: {fine_tuned_model}")
        print(f"配置文件: llm_config_{agent_name.lower()}_finetuned.py")
        print(f"\n下一步：在 {agent_name.lower()}.py 中使用新模型")
        return fine_tuned_model
    else:
        print("\n[FAILED] Fine-tuning 失败")
        return None


# ==================== 使用示例 ====================
if __name__ == "__main__":
    import sys
    
    print("智增增 Fine-tuning 工具")
    print("=" * 70)
    
    if len(sys.argv) < 3:
        print("\n使用方法:")
        print("  python simple_finetune.py <agent名称> <训练数据文件>")
        print("\n示例:")
        print("  python simple_finetune.py SceneAgent training_data/scene_data.jsonl")
        print("  python simple_finetune.py NarrativeAgent training_data/narrative_data.jsonl")
        print("\n支持的 Agent: SceneAgent, NarrativeAgent")
        print("支持的模型: gpt-3.5-turbo (默认), gpt-4o")
        sys.exit(1)
    
    agent_name = sys.argv[1]
    training_file = sys.argv[2]
    model = sys.argv[3] if len(sys.argv) > 3 else "gpt-3.5-turbo"
    
    # 验证 Agent 名称
    if agent_name not in ["SceneAgent", "NarrativeAgent"]:
        print(f"[ERROR] 错误：不支持的 Agent 名称 '{agent_name}'")
        print("支持的 Agent: SceneAgent, NarrativeAgent")
        sys.exit(1)
    
    # 验证文件存在
    import os
    if not os.path.exists(training_file):
        print(f"[ERROR] 错误：文件不存在 '{training_file}'")
        sys.exit(1)
    
    # 开始 fine-tuning
    finetune_agent(training_file, agent_name, model)

