"""
检查 Fine-tuning 任务状态
用于中途关闭终端后手动检查
"""

from simple_finetune import check_status
import sys

if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("使用方法:")
        print("  python check_finetune_status.py <job_id>")
        print("\n示例:")
        print("  python check_finetune_status.py ftjob-xxxxxxxxxxxx")
        sys.exit(1)
    
    job_id = sys.argv[1]
    
    print(f"检查任务状态: {job_id}")
    print("=" * 60)
    
    status, model = check_status(job_id)
    
    print(f"\n当前状态: {status}")
    
    if status == "succeeded":
        print(f"✅ 训练完成！")
        print(f"模型名称: {model}")
        print(f"\n下一步：使用这个模型名称更新配置文件")
    elif status == "failed":
        print(f"❌ 训练失败")
    elif status in ["validating_files", "queued", "running"]:
        print(f"⏳ 训练中，请继续等待...")
    else:
        print(f"⚠️ 未知状态")

