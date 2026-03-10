@echo off
REM 一键 Fine-tuning 脚本（Windows）
echo ========================================
echo 智增增 Fine-tuning 快速工具
echo ========================================
echo.

echo [步骤 1] 合并训练数据...
python -c "from data_collector import get_collector; get_collector().merge_all_sessions()"
echo.

echo [步骤 2] 分离数据...
python split_data.py
echo.

echo ========================================
echo 准备完成！请选择要 fine-tune 的 Agent：
echo ========================================
echo 1. SceneAgent (推荐)
echo 2. NarrativeAgent
echo 3. 两个都 fine-tune
echo.

set /p choice="请输入选择 (1/2/3): "

if "%choice%"=="1" (
    echo.
    echo 开始 Fine-tune SceneAgent...
    python simple_finetune.py SceneAgent training_data/scene_data.jsonl
) else if "%choice%"=="2" (
    echo.
    echo 开始 Fine-tune NarrativeAgent...
    python simple_finetune.py NarrativeAgent training_data/narrative_data.jsonl
) else if "%choice%"=="3" (
    echo.
    echo 开始 Fine-tune SceneAgent...
    python simple_finetune.py SceneAgent training_data/scene_data.jsonl
    echo.
    echo 开始 Fine-tune NarrativeAgent...
    python simple_finetune.py NarrativeAgent training_data/narrative_data.jsonl
) else (
    echo 无效选择，退出
)

echo.
echo 完成！
pause

