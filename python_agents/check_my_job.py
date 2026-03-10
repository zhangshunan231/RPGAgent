"""
手动检查你的 fine-tuning 任务状态
"""

import openai
from llm_config import ZZZ_API_KEY

# 配置
API_SECRET_KEY = ZZZ_API_KEY
BASE_URL = "https://api.zhizengzeng.com/v1"

# 你的任务 ID（从之前的输出复制）
JOB_ID = "ftjob-vYG5GG0duwW6jvz64aXTzfzL"  # NarrativeAgent 任务

# 检测版本
try:
    from openai import OpenAI
    OPENAI_NEW_VERSION = True
    print(f"使用新版本 openai (v{openai.__version__})")
except ImportError:
    OPENAI_NEW_VERSION = False
    print(f"使用旧版本 openai (v{openai.__version__})")

print("=" * 70)
print(f"检查任务: {JOB_ID}")
print("=" * 70)

try:
    if OPENAI_NEW_VERSION:
        # 新版本
        client = OpenAI(api_key=API_SECRET_KEY, base_url=BASE_URL)
        resp = client.fine_tuning.jobs.retrieve(JOB_ID)
        
        print(f"\n当前状态: {resp.status}")
        print(f"创建时间: {resp.created_at}")
        print(f"基础模型: {resp.model}")
        
        if resp.fine_tuned_model:
            print(f"[OK] Fine-tuned 模型: {resp.fine_tuned_model}")
        else:
            print(f"[WAIT] 模型尚未生成（训练中）")
        
        if resp.error:
            print(f"[ERROR] 错误信息: {resp.error}")
        
    else:
        # 旧版本
        openai.api_key = API_SECRET_KEY
        openai.api_base = BASE_URL
        resp = openai.FineTuningJob.retrieve(JOB_ID)
        
        print(f"\n当前状态: {resp['status']}")
        print(f"创建时间: {resp.get('created_at')}")
        print(f"基础模型: {resp.get('model')}")
        
        if resp.get('fine_tuned_model'):
            print(f"[OK] Fine-tuned 模型: {resp['fine_tuned_model']}")
        else:
            print(f"[WAIT] 模型尚未生成（训练中）")
        
        if resp.get('error'):
            print(f"[ERROR] 错误信息: {resp['error']}")
    
    print("\n" + "=" * 70)
    print("状态说明：")
    print("- validating_files: 验证文件中")
    print("- queued: 排队等待")
    print("- running: 训练中")
    print("- succeeded: 训练完成 [OK]")
    print("- failed: 训练失败 [FAILED]")
    print("=" * 70)
    
except Exception as e:
    print(f"[ERROR] 错误: {e}")
    import traceback
    traceback.print_exc()

print("\n提示：如果状态是 running，请等待 10-30 分钟后再次检查")
print("重新运行此脚本: python check_my_job.py")

