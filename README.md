# RPGAgent

Unity RPG agent tooling with Python-based agents and Unity editor/runtime scripts.

## Repository Structure

- `python_agents/` - Python agent framework, data tools, and training utilities.
- `Assets/Scripts/` - Unity C# scripts and editor tools.

## Requirements

- Unity (for `Assets/Scripts` usage)
- Python 3.10+ (for `python_agents`)

## Python Setup

```bash
cd python_agents
python -m venv .venv
.venv\Scripts\activate
pip install -r requirements.txt
```

## Notes

- Unity generated folders (Library, Temp, Logs, UserSettings) are ignored.
- Python cache folders are ignored.
