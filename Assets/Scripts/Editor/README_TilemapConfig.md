# Tilemap 配置永久化说明

## 📝 问题
之前每次打开 Unity 编辑器，Tilemap Settings 中的所有引用都会丢失，需要重新拖拽：
- Water Tilemap
- Land Tilemap
- Mountain Tilemap
- Cliff Tilemap
- Elements Tilemap
- 各种 RuleTile

这非常麻烦！😫

## ✅ 解决方案
现在使用 **TilemapConfig** 配置文件来永久保存这些引用！

---

## 🚀 使用步骤

### 第一次设置（只需一次）

1. **打开 RPG Generator 窗口**
   - Unity 菜单：`Tools` → `Multi-Agent RPG Generator`

2. **切换到 Scene Generation 标签**
   - 点击 "Scene Generation" 标签

3. **展开 Tilemap Settings**
   - 点击 "Tilemap Settings" 折叠面板

4. **拖拽所有 Tilemap 和 RuleTile**
   - 按照之前的方式，手动拖拽所有需要的资源
   - Water Tilemap
   - Land Tilemap
   - Mountain Tilemap
   - Cliff Tilemap
   - Elements Tilemap
   - 以及所有的 RuleTile

5. **点击"创建新的 TilemapConfig"按钮**
   - 选择保存位置（推荐：`Assets/Settings/` 或 `Assets/`）
   - 输入文件名（默认：`TilemapConfig`）
   - 点击保存

6. **完成！** 🎉
   - 配置已永久保存
   - 下次打开编辑器会自动加载

---

### 之后的使用

#### 方式一：自动加载（推荐）
- 编辑器会自动查找并加载第一个 `TilemapConfig` 文件
- 无需任何操作，直接使用！

#### 方式二：手动选择
- 在 "Tilemap Config" 字段拖入你的配置文件
- 系统会立即加载配置

---

## 🔧 高级功能

### 1. 保存当前配置
如果你修改了 Tilemap 或 RuleTile 的引用：
- 点击 **"保存当前配置到文件"** 按钮
- 配置会更新到文件中

### 2. 重新加载配置
如果配置出错或需要重置：
- 点击 **"从文件重新加载"** 按钮
- 会重新从配置文件加载所有引用

### 3. 自动查找（配置文件中）
打开你的 TilemapConfig 文件：
- 在 Project 窗口中选中配置文件
- Inspector 中会有两个自动查找按钮：
  - **"自动查找场景中的 Tilemap"**：根据名称自动匹配场景中的 Tilemap
  - **"自动查找项目中的 RuleTile"**：根据名称自动匹配项目中的 RuleTile

### 4. 配置状态显示
- ✅ **配置完整**：所有必需的 Tilemap 和 RuleTile 都已设置
- ⚠️ **配置不完整**：会列出缺少的项目

---

## 📂 文件结构

```
Assets/
├── Settings/                    # 推荐：配置文件目录
│   └── TilemapConfig.asset     # 你的配置文件
├── Scripts/
│   └── Editor/
│       ├── TilemapConfig.cs    # 配置文件脚本
│       └── MultiAgentRPGEditor.cs  # 编辑器窗口
└── ...
```

---

## 🎯 优点

- ✅ **一次配置，永久使用**：不用每次都拖拽
- ✅ **自动加载**：打开编辑器即可使用
- ✅ **可共享**：可以将配置文件提交到版本控制
- ✅ **团队协作**：团队成员可以共享同一个配置
- ✅ **备份容易**：配置文件是一个 .asset 文件
- ✅ **错误检测**：自动检测配置是否完整

---

## 🐛 常见问题

### Q1: 编辑器没有自动加载配置？
**A:** 确保配置文件在项目中，并且名称包含 "TilemapConfig"。也可以手动拖入配置文件。

### Q2: 配置文件显示"不完整"？
**A:** 
1. 打开配置文件（双击）
2. 使用 "自动查找" 按钮自动填充
3. 或手动拖入缺少的 Tilemap/RuleTile

### Q3: 我有多个配置文件？
**A:** 系统会自动加载第一个找到的配置文件。你也可以手动选择要使用的配置文件。

### Q4: 兼容旧版本吗？
**A:** 完全兼容！如果不使用配置文件，仍然可以像之前一样手动拖拽。

---

## 💡 提示

- **推荐**：创建一个 `Assets/Settings/` 目录来统一管理配置文件
- **团队协作**：将 `TilemapConfig.asset` 添加到版本控制
- **备份**：定期备份配置文件
- **命名规范**：使用清晰的名称，如 `MainSceneTilemapConfig.asset`

---

## 📝 技术细节

### TilemapConfig 结构
```csharp
public class TilemapConfig : ScriptableObject
{
    // Tilemap 引用
    public Tilemap waterTilemap;
    public Tilemap landTilemap;
    public Tilemap mountainTilemap;
    public Tilemap cliffTilemap;
    public Tilemap elementsTilemap;
    
    // RuleTile 引用
    public TileBase waterRuleTile;
    public TileBase landRuleTile;
    public TileBase mountainRuleTile;
    public TileBase cliffRuleTile;
}
```

### 自动加载逻辑
- 在 `OnEnable()` 时调用 `LoadTilemapFromConfig()`
- 如果有配置文件，从配置文件加载
- 如果没有，尝试查找项目中的第一个 `TilemapConfig`

---

## 🎉 享受轻松的开发体验！

现在你再也不用每次都手动拖拽 Tilemap 了！😊

