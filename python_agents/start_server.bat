@echo off
echo 启动Python Agent服务器...
echo.
echo 请确保已激活autogen_agents虚拟环境
echo 如果没有激活，请先运行: conda activate autogen_agents
echo.
echo 安装依赖包...
pip install -r requirements.txt
echo.
echo 启动服务器...
python server.py
pause 