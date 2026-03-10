"""
测试智增增支持的 GPT-4o fine-tuning 模型名称
"""

import openai
from llm_config import ZZZ_API_KEY

API_SECRET_KEY = ZZZ_API_KEY
BASE_URL = "https://api.zhizengzeng.com/v1"

# 检测版本
try:
    from openai import OpenAI
    OPENAI_NEW_VERSION = True
    print(f"使用新版本 openai (v{openai.__version__})")
except ImportError:
    OPENAI_NEW_VERSION = False
    print(f"使用旧版本 openai (v{openai.__version__})")

# 可能的 GPT-4o 模型名称
possible_models = [
    "gpt-4o",
    "gpt-4o-2024-08-06",
    "gpt-4o-2024-05-13",
    "gpt-4o-2024-11-20",
    "gpt-4o-mini",
    "gpt-4o-mini-2024-07-18",
]

print("=" * 70)
print("测试 GPT-4o Fine-tuning 模型名称")
print("=" * 70)

# 上传一个测试文件（使用现有的文件）
test_file = "training_data/narrative_data.jsonl"

print(f"\n[1] 上传测试文件: {test_file}")

try:
    if OPENAI_NEW_VERSION:
        client = OpenAI(api_key=API_SECRET_KEY, base_url=BASE_URL)
        with open(test_file, "rb") as f:
            resp = client.files.create(file=f, purpose='fine-tune')
        file_id = resp.id
    else:
        openai.api_key = API_SECRET_KEY
        openai.api_base = BASE_URL
        resp = openai.File.create(
            file=open(test_file, "rb"),
            purpose='fine-tune'
        )
        file_id = resp['id']
    
    print(f"[OK] 文件上传成功: {file_id}")
    
    # 测试每个模型名称
    print(f"\n[2] 测试模型名称...")
    print("=" * 70)
    
    for model in possible_models:
        print(f"\n测试: {model}")
        try:
            if OPENAI_NEW_VERSION:
                client = OpenAI(api_key=API_SECRET_KEY, base_url=BASE_URL)
                resp = client.fine_tuning.jobs.create(
                    training_file=file_id,
                    model=model
                )
                
                if resp.error:
                    print(f"  [X] 不支持: {resp.error.message}")
                else:
                    print(f"  [OK] 支持！任务ID: {resp.id}")
                    print(f"       >>> 使用这个模型名称: {model} <<<")
                    break
            else:
                openai.api_key = API_SECRET_KEY
                openai.api_base = BASE_URL
                resp = openai.FineTuningJob.create(
                    training_file=file_id,
                    model=model
                )
                
                if 'error' in resp:
                    print(f"  [X] 不支持: {resp['error']}")
                else:
                    print(f"  [OK] 支持！任务ID: {resp.get('id')}")
                    print(f"       >>> 使用这个模型名称: {model} <<<")
                    break
                    
        except Exception as e:
            error_msg = str(e)
            if "not available" in error_msg or "does not exist" in error_msg:
                print(f"  [X] 不支持")
            else:
                print(f"  [?] 错误: {e}")
    
    print("\n" + "=" * 70)
    print("测试完成")
    print("=" * 70)
    
except Exception as e:
    print(f"[ERROR] 错误: {e}")
    import traceback
    traceback.print_exc()

