using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public static class MapGenerator
{
    public enum TerrainType { Water, Grass, Mountain }

    public static TerrainType[,] GenerateTerrainTypeMap(int mapWidth, int mapHeight, float noiseScale, float landThreshold, float islandFactor, float mountainThreshold, int landSeed, int mountainSeed)
    {
        TerrainType[,] map = new TerrainType[mapWidth, mapHeight];
        for (int x = 0; x < mapWidth; x++)
        for (int y = 0; y < mapHeight; y++)
        {
            float nx = (float)x / mapWidth - 0.5f;
            float ny = (float)y / mapHeight - 0.5f;
            float d = Mathf.Sqrt(nx * nx + ny * ny) / islandFactor;
            float noise = Mathf.PerlinNoise((x + landSeed) / noiseScale, (y + landSeed) / noiseScale);
            if (noise - d < landThreshold)
                map[x, y] = TerrainType.Water;
            else
                map[x, y] = TerrainType.Grass;
        }
        // 山地
        for (int x = 0; x < mapWidth; x++)
        for (int y = 0; y < mapHeight; y++)
        {
            if (map[x, y] == TerrainType.Grass)
            {
                float noise = Mathf.PerlinNoise((x + 1000 + mountainSeed) / noiseScale, (y + 1000 + mountainSeed) / noiseScale);
                if (noise > mountainThreshold)
                    map[x, y] = TerrainType.Mountain;
            }
        }
        return map;
    }

    public static void GenerateMap(Tilemap waterTilemap, Tilemap landTilemap, Tilemap mountainTilemap, Tilemap cliffTilemap,
        TileBase waterRuleTile, TileBase landRuleTile, TileBase mountainRuleTile, TileBase cliffRuleTile,
        int mapWidth, int mapHeight, float noiseScale, float landThreshold, float islandFactor, float mountainThreshold, int cliffHeight, int landSeed, int mountainSeed, Tilemap elementsTilemap = null)
    {
        TerrainType[,] terrainMap = GenerateTerrainTypeMap(mapWidth, mapHeight, noiseScale, landThreshold, islandFactor, mountainThreshold, landSeed, mountainSeed);
        // 填充水层
        waterTilemap.ClearAllTiles();
        for (int x = 0; x < mapWidth; x++)
        for (int y = 0; y < mapHeight; y++)
        {
            if (terrainMap[x, y] == TerrainType.Water)
                waterTilemap.SetTile(new Vector3Int(x, y, 0), waterRuleTile);
        }
        // 填充陆地（包括Grass和Mountain区域）
        landTilemap.ClearAllTiles();
        for (int x = 0; x < mapWidth; x++)
        for (int y = 0; y < mapHeight; y++)
        {
            if (terrainMap[x, y] == TerrainType.Grass || terrainMap[x, y] == TerrainType.Mountain)
                landTilemap.SetTile(new Vector3Int(x, y, 0), landRuleTile);
        }
        // 填充山地
        mountainTilemap.ClearAllTiles();
        for (int x = 0; x < mapWidth; x++)
        for (int y = 0; y < mapHeight; y++)
        {
            if (terrainMap[x, y] == TerrainType.Mountain)
                mountainTilemap.SetTile(new Vector3Int(x, y, 0), mountainRuleTile);
        }
        // 填充悬崖
        cliffTilemap.ClearAllTiles();
        for (int x = 0; x < mapWidth; x++)
        for (int y = 0; y < mapHeight; y++)
        {
            if (terrainMap[x, y] == TerrainType.Mountain)
            {
                for (int h = 1; h <= cliffHeight; h++)
                {
                    int cy = y - h;
                    if (cy >= 0 && terrainMap[x, cy] != TerrainType.Mountain)
                        cliffTilemap.SetTile(new Vector3Int(x, cy, 0), cliffRuleTile);
                }
            }
        }
        // 清空元素层（后续元素分配用）
        if (elementsTilemap != null) elementsTilemap.ClearAllTiles();
    }

    /// <summary>
    /// 生成支持分区RuleTile的地图
    /// </summary>
    public static void GenerateMapWithPartitionRuleTiles(Tilemap waterTilemap, Tilemap landTilemap, Tilemap mountainTilemap, Tilemap cliffTilemap,
        TileBase defaultWaterRuleTile, TileBase defaultLandRuleTile, TileBase defaultMountainRuleTile, TileBase defaultCliffRuleTile,
        List<MultiAgentRPGEditor.PartitionRuleTileConfig> partitionConfigs, List<MultiAgentRPGEditor.NarrativeStep> steps,
        int mapWidth, int mapHeight, float noiseScale, float landThreshold, float islandFactor, float mountainThreshold, 
        int cliffHeight, int landSeed, int mountainSeed, float perlinScale, float perlinStrength, Tilemap elementsTilemap = null, bool enableAreaBalancing = true)
    {
        TerrainType[,] terrainMap = GenerateTerrainTypeMap(mapWidth, mapHeight, noiseScale, landThreshold, islandFactor, mountainThreshold, landSeed, mountainSeed);
        
        // 生成分区信息
        int stepCount = steps.Count;
        List<Vector2Int> seeds = GeneratePartitionSeeds(stepCount, mapWidth, mapHeight, landSeed, mountainSeed, terrainMap);
        int[,] areaMap = new int[mapWidth, mapHeight];
        
        // 生成初始Voronoi图
        for (int x = 0; x < mapWidth; x++)
        for (int y = 0; y < mapHeight; y++)
        {
            if (terrainMap[x, y] == TerrainType.Water) { areaMap[x, y] = -1; continue; }
            int minIdx = 0;
            float minDist = float.MaxValue;
            for (int i = 0; i < seeds.Count; i++)
            {
                float dist = (seeds[i].x - x) * (seeds[i].x - x) + (seeds[i].y - y) * (seeds[i].y - y);
                float noise = Mathf.PerlinNoise(
                    (x + seeds[i].x * 31) * perlinScale / mapWidth,
                    (y + seeds[i].y * 47) * perlinScale / mapHeight
                ) * 2f - 1f;
                dist += noise * perlinStrength;
                if (dist < minDist) { minDist = dist; minIdx = i; }
            }
            areaMap[x, y] = minIdx;
        }
        
        // 可选：进行区域平衡优化（最多3次迭代）
        //bool enableAreaBalancing = true; // 可以通过参数控制
        if (stepCount > 1)
        {
            for (int iteration = 0; iteration < 3; iteration++)
            {
                // 计算每个区域的中心点
                List<Vector2> centroids = new List<Vector2>();
                for (int i = 0; i < stepCount; i++)
                {
                    float sumX = 0, sumY = 0;
                    int count = 0;
                    for (int x = 0; x < mapWidth; x++)
                    for (int y = 0; y < mapHeight; y++)
                    {
                        if (areaMap[x, y] == i && terrainMap[x, y] != TerrainType.Water)
                        {
                            sumX += x;
                            sumY += y;
                            count++;
                        }
                    }
                    if (count > 0)
                    {
                        centroids.Add(new Vector2(sumX / count, sumY / count));
                    }
                    else
                    {
                        centroids.Add(new Vector2(seeds[i].x, seeds[i].y));
                    }
                }
                
                // 更新种子位置到区域中心
                bool hasSignificantChange = false;
                for (int i = 0; i < stepCount; i++)
                {
                    Vector2Int newSeed = new Vector2Int(Mathf.RoundToInt(centroids[i].x), Mathf.RoundToInt(centroids[i].y));
                    newSeed.x = Mathf.Clamp(newSeed.x, 0, mapWidth - 1);
                    newSeed.y = Mathf.Clamp(newSeed.y, 0, mapHeight - 1);
                    
                    // 确保新种子不在水中
                    if (terrainMap[newSeed.x, newSeed.y] == TerrainType.Water)
                    {
                        // 寻找最近的陆地位置
                        for (int radius = 1; radius < 10; radius++)
                        {
                            bool found = false;
                            for (int dx = -radius; dx <= radius; dx++)
                            for (int dy = -radius; dy <= radius; dy++)
                            {
                                int nx = newSeed.x + dx;
                                int ny = newSeed.y + dy;
                                if (nx >= 0 && nx < mapWidth && ny >= 0 && ny < mapHeight && 
                                    terrainMap[nx, ny] != TerrainType.Water)
                                {
                                    newSeed = new Vector2Int(nx, ny);
                                    found = true;
                                    break;
                                }
                            }
                            if (found) break;
                        }
                    }
                    
                    if (Vector2Int.Distance(seeds[i], newSeed) > 1f)
                    {
                        seeds[i] = newSeed;
                        hasSignificantChange = true;
                    }
                }
                
                // 如果变化很小，停止迭代
                if (!hasSignificantChange) break;
                
                // 重新生成Voronoi图
                for (int x = 0; x < mapWidth; x++)
                for (int y = 0; y < mapHeight; y++)
                {
                    if (terrainMap[x, y] == TerrainType.Water) { areaMap[x, y] = -1; continue; }
                    int minIdx = 0;
                    float minDist = float.MaxValue;
                    for (int i = 0; i < seeds.Count; i++)
                    {
                        float dist = (seeds[i].x - x) * (seeds[i].x - x) + (seeds[i].y - y) * (seeds[i].y - y);
                        float noise = Mathf.PerlinNoise(
                            (x + seeds[i].x * 31) * perlinScale / mapWidth,
                            (y + seeds[i].y * 47) * perlinScale / mapHeight
                        ) * 2f - 1f;
                        dist += noise * perlinStrength;
                        if (dist < minDist) { minDist = dist; minIdx = i; }
                    }
                    areaMap[x, y] = minIdx;
                }
            }
        }
        
        // 清空所有tilemap
        waterTilemap.ClearAllTiles();
        landTilemap.ClearAllTiles();
        mountainTilemap.ClearAllTiles();
        cliffTilemap.ClearAllTiles();
        if (elementsTilemap != null) elementsTilemap.ClearAllTiles();
        
        // 为每个分区应用对应的RuleTile
        for (int x = 0; x < mapWidth; x++)
        for (int y = 0; y < mapHeight; y++)
        {
            int area = areaMap[x, y];
            
            // 处理水域：水域的area是-1，使用默认配置
            if (terrainMap[x, y] == TerrainType.Water)
            {
                waterTilemap.SetTile(new Vector3Int(x, y, 0), defaultWaterRuleTile);
                continue;
            }
            
            // 处理陆地：检查是否有有效的分区配置
            if (area >= 0 && area < partitionConfigs.Count)
            {
                var config = partitionConfigs[area];
                
                // 应用陆地tile
                if (terrainMap[x, y] == TerrainType.Grass || terrainMap[x, y] == TerrainType.Mountain)
                {
                    var landTile = config.landRuleTile ?? defaultLandRuleTile;
                    landTilemap.SetTile(new Vector3Int(x, y, 0), landTile);
                }
                
                // 应用山地tile
                if (terrainMap[x, y] == TerrainType.Mountain)
                {
                    var mountainTile = config.mountainRuleTile ?? defaultMountainRuleTile;
                    mountainTilemap.SetTile(new Vector3Int(x, y, 0), mountainTile);
                }
                
                // 应用悬崖tile
                if (terrainMap[x, y] == TerrainType.Mountain)
                {
                    for (int h = 1; h <= cliffHeight; h++)
                    {
                        int cy = y - h;
                        if (cy >= 0 && terrainMap[x, cy] != TerrainType.Mountain)
                        {
                            var cliffTile = config.cliffRuleTile ?? defaultCliffRuleTile;
                            cliffTilemap.SetTile(new Vector3Int(x, cy, 0), cliffTile);
                        }
                    }
                }
            }
            else
            {
                // 如果没有有效的分区配置，使用默认配置
                if (terrainMap[x, y] == TerrainType.Grass || terrainMap[x, y] == TerrainType.Mountain)
                {
                    landTilemap.SetTile(new Vector3Int(x, y, 0), defaultLandRuleTile);
                }
                
                if (terrainMap[x, y] == TerrainType.Mountain)
                {
                    mountainTilemap.SetTile(new Vector3Int(x, y, 0), defaultMountainRuleTile);
                    
                    // 应用悬崖tile
                    for (int h = 1; h <= cliffHeight; h++)
                    {
                        int cy = y - h;
                        if (cy >= 0 && terrainMap[x, cy] != TerrainType.Mountain)
                        {
                            cliffTilemap.SetTile(new Vector3Int(x, cy, 0), defaultCliffRuleTile);
                        }
                    }
                }
            }
        }
        
        Debug.Log($"[MapGenerator] 已生成分区RuleTile地图，分区数={stepCount}");
    }

    public static void AutoDistributeElements(Tilemap elementsTilemap, List<ElementDistributionConfig> areaElementConfigs, List<MultiAgentRPGEditor.NarrativeStep> steps,
        int mapWidth, int mapHeight, float perlinScale, float perlinStrength, int landSeed, int mountainSeed, 
        float noiseScale = 20f, float landThreshold = 0.5f, float islandFactor = 1.2f, float mountainThreshold = 0.75f, Tilemap cliffTilemap = null, bool enableAreaBalancing = true)
    {
        if (elementsTilemap == null || areaElementConfigs == null || areaElementConfigs.Count == 0 || steps == null) return;
        elementsTilemap.ClearAllTiles();
        int stepCount = steps.Count;
        
        // 使用传递的参数生成地形类型矩阵，确保与地图生成一致
        TerrainType[,] terrainMap = GenerateTerrainTypeMap(mapWidth, mapHeight, noiseScale, landThreshold, islandFactor, mountainThreshold, landSeed, mountainSeed);
        // 重新生成分区种子，保证和预览一致
        List<Vector2Int> seeds = GeneratePartitionSeeds(stepCount, mapWidth, mapHeight, landSeed, mountainSeed, terrainMap);
        int[,] areaMap = new int[mapWidth, mapHeight];
        
        // 生成初始Voronoi图
        for (int x = 0; x < mapWidth; x++)
        for (int y = 0; y < mapHeight; y++)
        {
            if (terrainMap[x, y] == TerrainType.Water) { areaMap[x, y] = -1; continue; }
            int minIdx = 0;
            float minDist = float.MaxValue;
            for (int i = 0; i < seeds.Count; i++)
            {
                float dist = (seeds[i].x - x) * (seeds[i].x - x) + (seeds[i].y - y) * (seeds[i].y - y);
                float noise = Mathf.PerlinNoise(
                    (x + seeds[i].x * 31) * perlinScale / mapWidth,
                    (y + seeds[i].y * 47) * perlinScale / mapHeight
                ) * 2f - 1f;
                dist += noise * perlinStrength;
                if (dist < minDist) { minDist = dist; minIdx = i; }
            }
            areaMap[x, y] = minIdx;
        }
        
        // 可选：进行区域平衡优化（最多3次迭代）
        // bool enableAreaBalancing = true; // 可以通过参数控制
        if (stepCount > 1)
        {
            for (int iteration = 0; iteration < 3; iteration++)
            {
                // 计算每个区域的中心点
                List<Vector2> centroids = new List<Vector2>();
                for (int i = 0; i < stepCount; i++)
                {
                    float sumX = 0, sumY = 0;
                    int count = 0;
                    for (int x = 0; x < mapWidth; x++)
                    for (int y = 0; y < mapHeight; y++)
                    {
                        if (areaMap[x, y] == i && terrainMap[x, y] != TerrainType.Water)
                        {
                            sumX += x;
                            sumY += y;
                            count++;
                        }
                    }
                    if (count > 0)
                    {
                        centroids.Add(new Vector2(sumX / count, sumY / count));
                    }
                    else
                    {
                        centroids.Add(new Vector2(seeds[i].x, seeds[i].y));
                    }
                }
                
                // 更新种子位置到区域中心
                bool hasSignificantChange = false;
                for (int i = 0; i < stepCount; i++)
                {
                    Vector2Int newSeed = new Vector2Int(Mathf.RoundToInt(centroids[i].x), Mathf.RoundToInt(centroids[i].y));
                    newSeed.x = Mathf.Clamp(newSeed.x, 0, mapWidth - 1);
                    newSeed.y = Mathf.Clamp(newSeed.y, 0, mapHeight - 1);
                    
                    // 确保新种子不在水中
                    if (terrainMap[newSeed.x, newSeed.y] == TerrainType.Water)
                    {
                        // 寻找最近的陆地位置
                        for (int radius = 1; radius < 10; radius++)
                        {
                            bool found = false;
                            for (int dx = -radius; dx <= radius; dx++)
                            for (int dy = -radius; dy <= radius; dy++)
                            {
                                int nx = newSeed.x + dx;
                                int ny = newSeed.y + dy;
                                if (nx >= 0 && nx < mapWidth && ny >= 0 && ny < mapHeight && 
                                    terrainMap[nx, ny] != TerrainType.Water)
                                {
                                    newSeed = new Vector2Int(nx, ny);
                                    found = true;
                                    break;
                                }
                            }
                            if (found) break;
                        }
                    }
                    
                    if (Vector2Int.Distance(seeds[i], newSeed) > 1f)
                    {
                        seeds[i] = newSeed;
                        hasSignificantChange = true;
                    }
                }
                
                // 如果变化很小，停止迭代
                if (!hasSignificantChange) break;
                
                // 重新生成Voronoi图
                for (int x = 0; x < mapWidth; x++)
                for (int y = 0; y < mapHeight; y++)
                {
                    if (terrainMap[x, y] == TerrainType.Water) { areaMap[x, y] = -1; continue; }
                    int minIdx = 0;
                    float minDist = float.MaxValue;
                    for (int i = 0; i < seeds.Count; i++)
                    {
                        float dist = (seeds[i].x - x) * (seeds[i].x - x) + (seeds[i].y - y) * (seeds[i].y - y);
                        float noise = Mathf.PerlinNoise(
                            (x + seeds[i].x * 31) * perlinScale / mapWidth,
                            (y + seeds[i].y * 47) * perlinScale / mapHeight
                        ) * 2f - 1f;
                        dist += noise * perlinStrength;
                        if (dist < minDist) { minDist = dist; minIdx = i; }
                    }
                    areaMap[x, y] = minIdx;
                }
            }
        }
        // 分区内自动分布元素
        bool[,] occupied = new bool[mapWidth, mapHeight];
        Debug.Log($"[ElementDist] 开始分布元素，总分区数={stepCount}，配置数={areaElementConfigs?.Count ?? 0}");
        
        // 统计所有元素的分布模式
        int byCountElements = 0, byDensityElements = 0;
        foreach (var config in areaElementConfigs ?? new List<ElementDistributionConfig>())
        {
            if (config?.elements != null)
            {
                foreach (var element in config.elements)
                {
                    if (element.distributionMode == ElementDistributionConfig.DistributionMode.ByCount)
                        byCountElements++;
                    else
                        byDensityElements++;
                }
            }
        }
        Debug.Log($"[ElementDist] 元素分布模式统计：ByCount={byCountElements}个元素，ByDensity={byDensityElements}个元素");
        for (int area = 0; area < stepCount; area++)
        {
            var step = steps[area];
            // 匹配LocationType
            var cfg = areaElementConfigs.Find(c => c != null && c.location == step.location);
            Debug.Log($"[ElementDist] 分区{area+1} location={step.location} 配置{(cfg==null?"未找到":"已找到")} 元素数={cfg?.elements?.Count ?? 0}");
            if (cfg == null || cfg.elements == null) 
            {
                Debug.LogWarning($"[ElementDist] 分区{area+1} location={step.location} 跳过分布，原因：{(cfg==null?"未找到配置":"配置元素为空")}");
                continue;
            }
            // 统计可用格子
            List<Vector2Int> validCells = new List<Vector2Int>();
            for (int x = 0; x < mapWidth; x++)
            for (int y = 0; y < mapHeight; y++)
            {
                if (areaMap[x, y] == area && (terrainMap[x, y] == TerrainType.Grass || terrainMap[x, y] == TerrainType.Mountain))
                    validCells.Add(new Vector2Int(x, y));
            }
            Debug.Log($"[ElementDist] 分区{area+1} location={step.location} 可用格子数={validCells.Count}");
            // 不再需要权重池，直接使用元素列表
            System.Random rand = new System.Random(landSeed + mountainSeed + area * 1000);
            Debug.Log($"[ElementDist] 分区{area+1} location={step.location} 开始分布{cfg.elements.Count}种元素");
            
            // 计算每个元素的目标数量
            var elementTargets = new Dictionary<ElementDistributionConfig.ElementEntry, int>();
            foreach (var entry in cfg.elements)
            {
                int targetCount = CalculateTargetCount(entry, entry.distributionMode, validCells.Count);
                string modeDescription = entry.distributionMode == ElementDistributionConfig.DistributionMode.ByDensity 
                    ? $"密度模式：每100格子{entry.count:F2}个，可用格子{validCells.Count}，目标数量={targetCount}"
                    : $"数量模式：目标数量={targetCount}";
                Debug.Log($"[ElementDist] 分区{area+1} location={step.location} 元素{entry.template?.name ?? "未知"} {modeDescription}");
                elementTargets[entry] = targetCount;
            }
            
            foreach (var entry in cfg.elements)
            {
                int placed = 0;
                int tryLimit = 10000;
                int targetCount = elementTargets[entry];
                Debug.Log($"[ElementDist] 分区{area+1} location={step.location} 尝试分布元素：{entry.template?.name ?? "未知"} 模式={entry.distributionMode} 目标数量={targetCount}");
                while (placed < targetCount && tryLimit-- > 0)
                {
                    var candidates = validCells.OrderBy(_ => rand.Next()).ToList();
                    bool found = false;
                    foreach (var cell in candidates)
                    {
                        if (CanPlaceElement(entry.template, cell.x, cell.y, occupied, terrainMap, areaMap, area, mapWidth, mapHeight, cliffTilemap))
                        {
                            PlaceElement(entry.template, cell.x, cell.y, elementsTilemap);
                            MarkOccupied(entry.template, cell.x, cell.y, occupied, mapWidth, mapHeight);
                            found = true; placed++;
                            Debug.Log($"[ElementDist] 分区{area+1} location={step.location} 成功放置元素：{entry.template?.name ?? "未知"} 位置({cell.x},{cell.y}) 已放置{placed}/{targetCount}");
                            if (entry.minSpacing > 0)
                                MarkSpacing(cell.x, cell.y, entry.template.width, entry.template.height, entry.minSpacing, occupied, mapWidth, mapHeight);
                            break;
                        }
                    }
                    if (!found) 
                    {
                        Debug.LogWarning($"[ElementDist] 分区{area+1} location={step.location} 元素{entry.template?.name ?? "未知"} 无法找到合适位置，已放置{placed}/{targetCount}");
                        break;
                    }
                }
            }
            // 统计分布结果
            int totalPlaced = 0;
            foreach (var entry in cfg.elements)
            {
                int targetCount = elementTargets[entry];
                totalPlaced += targetCount;
            }
            Debug.Log($"[ElementDist] 分区{area+1} location={step.location} 元素分布完成，总计{totalPlaced}个元素");
        }
    }

    public static void GeneratePartitionPreview(MapGenerator.TerrainType[,] terrainMap, int mapWidth, int mapHeight, float perlinScale, float perlinStrength, int landSeed, int mountainSeed, int stepCount, ref Color[] voronoiColors, ref Texture2D partitionPreviewTex)
    {
        
        if (stepCount <= 0) return;
        List<Vector2Int> seeds = GeneratePartitionSeeds(stepCount, mapWidth, mapHeight, landSeed, mountainSeed, terrainMap);
        int actualCount = seeds.Count;
        // Debug.Log($"[分区预览] seeds.Count={actualCount} seeds={string.Join(",", seeds)} stepCount={stepCount}");
        if (actualCount == 0) return;
        if (voronoiColors == null || voronoiColors.Length != actualCount)
        {
            voronoiColors = new Color[actualCount];
                for (int i = 0; i < actualCount; i++)
                {
                    // 均匀分布在色环上，保证每个分区颜色都不同
                    float h = (float)i / actualCount;
                    float s = 0.7f;
                    float v = 0.9f;
                    voronoiColors[i] = Color.HSVToRGB(h, s, v);
                }
        }
        partitionPreviewTex = new Texture2D(mapWidth, mapHeight, TextureFormat.RGBA32, false);
        //Debug.Log($"[分区预览] voronoiColors={string.Join(",", voronoiColors)}");
        for (int x = 0; x < mapWidth; x++){
           
            for (int y = 0; y < mapHeight; y++)
            {
                if (terrainMap[x, y] == TerrainType.Water)
                {
                    partitionPreviewTex.SetPixel(x, y, new Color(0.2f, 0.4f, 1f));
                    continue;
                }
                int minIdx = 0;
                float minDist = float.MaxValue;
                for (int i = 0; i < actualCount; i++)
                {
                    float dist = (seeds[i].x - x) * (seeds[i].x - x) + (seeds[i].y - y) * (seeds[i].y - y);
                    float noise = Mathf.PerlinNoise(
                        (x + seeds[i].x * 31) * perlinScale / mapWidth,
                        (y + seeds[i].y * 47) * perlinScale / mapHeight
                    ) * 2f - 1f;
                    dist += noise * perlinStrength;
                    if (dist < minDist)
                    {
                        minDist = dist;
                        minIdx = i;
                    }
                }
                if (x == 0 && y == 0) Debug.Log($"[分区预览] (0,0) minIdx={minIdx} seeds[minIdx]={seeds[minIdx]}");
                partitionPreviewTex.SetPixel(x, y, voronoiColors[Mathf.Clamp(minIdx, 0, voronoiColors.Length - 1)]);
                //Debug.Log($"voronoiColors[minIdx]={voronoiColors[Mathf.Clamp(minIdx, 0, voronoiColors.Length - 1)]}");
            }
        }
        partitionPreviewTex.Apply();
    }
    private static Color RandomColor(System.Random rnd)
    {
        // 生成色相均匀分布的高饱和度颜色，避免偏红
        float h = (float)rnd.NextDouble(); // 0~1
        float s = 0.5f + (float)rnd.NextDouble()*0.5f; // 0.7~1
        float v = 0.5f + (float)rnd.NextDouble()*0.5f ; // 0.7~1
        return Color.HSVToRGB(h, s, v);
    }

    private static bool CanPlaceElement(ElementTemplate template, int x, int y, bool[,] occupied, TerrainType[,] terrainMap, int[,] areaMap, int area, int mapWidth, int mapHeight, Tilemap cliffTilemap = null)
    {
        for (int dx = 0; dx < template.width; dx++)
        for (int dy = 0; dy < template.height; dy++)
        {
            int tx = x + dx, ty = y + dy;
            if (tx >= mapWidth || ty >= mapHeight) return false;
            if (occupied[tx, ty]) return false;
            if (areaMap[tx, ty] != area) return false;
            if (terrainMap[tx, ty] != TerrainType.Grass && terrainMap[tx, ty] != TerrainType.Mountain) return false;
            
            // 检查cliff区域：元素的最下面一行不能出现在Cliff
            if (cliffTilemap != null && dy == template.height - 1) // 最下面一行
            {
                var cliffTile = cliffTilemap.GetTile(new Vector3Int(tx, ty, 0));
                if (cliffTile != null) return false; // 如果最下面一行有cliff，则不能放置
            }
        }
        TerrainType? baseType = null;
        for (int dx = 0; dx < template.width; dx++)
        {
            int tx = x + dx, ty = y;
            if (tx >= mapWidth || ty >= mapHeight) return false;
            var t = terrainMap[tx, ty];
            if (t != TerrainType.Grass && t != TerrainType.Mountain) return false;
            if (baseType == null)
                baseType = t;
            else if (baseType != t)
                return false;
        }
        return true;
    }
    private static void MarkOccupied(ElementTemplate template, int x, int y, bool[,] occupied, int mapWidth, int mapHeight)
    {
        for (int dx = 0; dx < template.width; dx++)
        for (int dy = 0; dy < template.height; dy++)
        {
            int tx = x + dx, ty = y + dy;
            if (tx < mapWidth && ty < mapHeight)
                occupied[tx, ty] = true;
        }
    }
    private static void MarkSpacing(int x, int y, int w, int h, int spacing, bool[,] occupied, int mapWidth, int mapHeight)
    {
        for (int dx = -spacing; dx < w + spacing; dx++)
        for (int dy = -spacing; dy < h + spacing; dy++)
        {
            int tx = x + dx, ty = y + dy;
            if (tx >= 0 && tx < mapWidth && ty >= 0 && ty < mapHeight)
                occupied[tx, ty] = true;
        }
    }
    private static void PlaceElement(ElementTemplate template, int x, int y, Tilemap elementsTilemap)
    {
        for (int dx = 0; dx < template.width; dx++)
        for (int dy = 0; dy < template.height; dy++)
        {
            int idx = dy * template.width + dx;
            if (idx < template.tiles.Length)
                elementsTilemap.SetTile(new Vector3Int(x + dx, y + dy, 0), template.tiles[idx]);
        }
    }

    /// <summary>
    /// 计算元素的目标分布数量
    /// </summary>
    /// <param name="entry">元素配置</param>
    /// <param name="distributionMode">分布模式</param>
    /// <param name="availableCellsCount">可用格子数量</param>
    /// <returns>目标数量</returns>
    private static int CalculateTargetCount(ElementDistributionConfig.ElementEntry entry, ElementDistributionConfig.DistributionMode distributionMode, int availableCellsCount)
    {
        if (distributionMode == ElementDistributionConfig.DistributionMode.ByDensity)
        {
            // 按密度分布：每100个可用格子放置指定数量的元素
            float densityCount = entry.count * availableCellsCount / 100f;
            // 确保至少放置1个（如果密度配置大于0）
            return entry.count > 0 ? Mathf.Max(1, Mathf.RoundToInt(densityCount)) : 0;
        }
        else
        {
            // 按数量分布：直接使用指定的数量
            return Mathf.RoundToInt(entry.count);
        }
    }

    // 公共：生成分区种子
    public static List<Vector2Int> GeneratePartitionSeeds(int stepCount, int mapWidth, int mapHeight, int landSeed, int mountainSeed, TerrainType[,] terrainMap)
    {
        List<Vector2Int> seeds = new List<Vector2Int>();
        System.Random rnd = new System.Random(landSeed + mountainSeed + stepCount + mapWidth + mapHeight);
        
        // 收集所有可用的陆地位置
        List<Vector2Int> availablePositions = new List<Vector2Int>();
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                if (terrainMap[x, y] != TerrainType.Water)
                {
                    availablePositions.Add(new Vector2Int(x, y));
                }
            }
        }
        
        if (availablePositions.Count == 0)
        {
            // 如果没有可用位置，返回默认种子
            for (int i = 0; i < stepCount; i++)
            {
                seeds.Add(new Vector2Int(mapWidth / 2, mapHeight / 2));
            }
            return seeds;
        }
        
        // 使用K-means++算法生成更均匀的种子
        if (stepCount <= availablePositions.Count)
        {
            // 第一步：随机选择第一个种子
            int firstIndex = rnd.Next(0, availablePositions.Count);
            seeds.Add(availablePositions[firstIndex]);
            availablePositions.RemoveAt(firstIndex);
            
            // 第二步：使用距离加权选择剩余种子
            for (int i = 1; i < stepCount; i++)
            {
                // 计算每个可用位置到最近种子的距离
                List<float> distances = new List<float>();
                for (int j = 0; j < availablePositions.Count; j++)
                {
                    float minDist = float.MaxValue;
                    for (int k = 0; k < seeds.Count; k++)
                    {
                        float dist = Vector2Int.Distance(availablePositions[j], seeds[k]);
                        if (dist < minDist) minDist = dist;
                    }
                    distances.Add(minDist * minDist); // 平方距离，增加权重差异
                }
                
                // 根据距离权重随机选择下一个种子
                float totalWeight = 0;
                for (int j = 0; j < distances.Count; j++)
                {
                    totalWeight += distances[j];
                }
                
                float randomValue = (float)rnd.NextDouble() * totalWeight;
                float currentWeight = 0;
                int selectedIndex = 0;
                
                for (int j = 0; j < distances.Count; j++)
                {
                    currentWeight += distances[j];
                    if (currentWeight >= randomValue)
                    {
                        selectedIndex = j;
                        break;
                    }
                }
                
                seeds.Add(availablePositions[selectedIndex]);
                availablePositions.RemoveAt(selectedIndex);
            }
        }
        else
        {
            // 如果需要的种子数超过可用位置数，使用简单随机选择
            for (int i = 0; i < stepCount; i++)
            {
                int index = rnd.Next(0, availablePositions.Count);
                seeds.Add(availablePositions[index]);
                availablePositions.RemoveAt(index);
                
                if (availablePositions.Count == 0)
                {
                    // 如果位置不够，重复使用最后一个位置
                    for (int j = i + 1; j < stepCount; j++)
                    {
                        seeds.Add(seeds[seeds.Count - 1]);
                    }
                    break;
                }
            }
        }
        
        return seeds;
    }

    // 获取指定分区的随机位置
    public static Vector3 GetRandomPositionInPartition(int partitionIndex, int mapWidth, int mapHeight, 
        float noiseScale, float landThreshold, float islandFactor, float mountainThreshold, 
        int landSeed, int mountainSeed, float perlinScale, float perlinStrength,
        Tilemap elementsTilemap = null, List<Vector3> occupiedPositions = null, bool preferCenter = false,
        Tilemap cliffTilemap = null, int totalPartitions = -1)
    {
        if (partitionIndex < 0) return Vector3.zero;
        
        // 生成地形类型矩阵
        TerrainType[,] terrainMap = GenerateTerrainTypeMap(mapWidth, mapHeight, noiseScale, landThreshold, islandFactor, mountainThreshold, landSeed, mountainSeed);
        
        // 生成分区种子 - 使用传入的总分区数，如果没有传入则使用估计值
        int partitionCount = totalPartitions > 0 ? totalPartitions : Mathf.Max(partitionIndex + 1, 5);
        List<Vector2Int> seeds = GeneratePartitionSeeds(partitionCount, mapWidth, mapHeight, landSeed, mountainSeed, terrainMap);
        
        if (seeds.Count <= partitionIndex) return Vector3.zero;
        
        // 生成分区地图
        int[,] areaMap = new int[mapWidth, mapHeight];
        for (int x = 0; x < mapWidth; x++)
        for (int y = 0; y < mapHeight; y++)
        {
            if (terrainMap[x, y] == TerrainType.Water) { areaMap[x, y] = -1; continue; }
            int minIdx = 0;
            float minDist = float.MaxValue;
            for (int i = 0; i < seeds.Count; i++)
            {
                float dist = (seeds[i].x - x) * (seeds[i].x - x) + (seeds[i].y - y) * (seeds[i].y - y);
                float noise = Mathf.PerlinNoise(
                    (x + seeds[i].x * 31) * perlinScale / mapWidth,
                    (y + seeds[i].y * 47) * perlinScale / mapHeight
                ) * 2f - 1f;
                dist += noise * perlinStrength;
                if (dist < minDist) { minDist = dist; minIdx = i; }
            }
            areaMap[x, y] = minIdx;
        }
        
        // 在指定分区内找到有效的随机位置
        List<Vector2Int> validPositions = new List<Vector2Int>();
        for (int x = 0; x < mapWidth; x++)
        for (int y = 0; y < mapHeight; y++)
        {
            if (areaMap[x, y] == partitionIndex && (terrainMap[x, y] == TerrainType.Grass || terrainMap[x, y] == TerrainType.Mountain))
            {
                // 检查是否是cliff区域
                bool isCliff = false;
                if (cliffTilemap != null)
                {
                    var cliffTile = cliffTilemap.GetTile(new Vector3Int(x, y, 0));
                    if (cliffTile != null)
                    {
                        isCliff = true;
                    }
                }
                
                if (isCliff) continue; // 跳过cliff区域
                
                // 检查是否与 Elements Tilemap 重叠
                bool hasElement = false;
                if (elementsTilemap != null)
                {
                    var tile = elementsTilemap.GetTile(new Vector3Int(x, y, 0));
                    hasElement = tile != null;
                }
                
                // 检查是否与已占用的位置重叠
                bool isOccupied = false;
                if (occupiedPositions != null)
                {
                    foreach (var pos in occupiedPositions)
                    {
                        float distance = Vector3.Distance(new Vector3(x, y, 0), pos);
                        if (distance < 1.5f) // 最小间距1.5个单位，与KeyObjectGenerator保持一致
                        {
                            isOccupied = true;
                            break;
                        }
                    }
                }
                
                // 只有没有元素且没有被占用的位置才有效
                if (!hasElement && !isOccupied)
                {
                    validPositions.Add(new Vector2Int(x, y));
                }
            }
        }
        
        if (validPositions.Count == 0)
        {
            // 如果没有找到完全符合条件的位置，尝试放宽条件（只检查地形和分区，不检查重叠）
            for (int x = 0; x < mapWidth; x++)
            for (int y = 0; y < mapHeight; y++)
            {
                if (areaMap[x, y] == partitionIndex && (terrainMap[x, y] == TerrainType.Grass || terrainMap[x, y] == TerrainType.Mountain))
                {
                    validPositions.Add(new Vector2Int(x, y));
                }
            }
            
            if (validPositions.Count == 0) return Vector3.zero;
        }
        
        // 如果需要优先选择中心附近的点
        if (preferCenter && validPositions.Count > 1)
        {
            // 计算分区重心
            float sumX = 0, sumY = 0;
            foreach (var pos in validPositions)
            {
                sumX += pos.x;
                sumY += pos.y;
            }
            float centerX = sumX / validPositions.Count;
            float centerY = sumY / validPositions.Count;
            
            // 按距离重心排序，优先选择中心附近的点
            validPositions.Sort((a, b) => {
                float distA = (a.x - centerX) * (a.x - centerX) + (a.y - centerY) * (a.y - centerY);
                float distB = (b.x - centerX) * (b.x - centerX) + (b.y - centerY) * (b.y - centerY);
                return distA.CompareTo(distB);
            });
            
            // 从前50%的点中随机选择，确保有一定随机性
            int selectRange = Mathf.Max(1, validPositions.Count / 2);
            int randomIndex = new System.Random(landSeed + mountainSeed + partitionIndex + 1 + mapWidth + mapHeight).Next(0, selectRange);
            Vector2Int randomPos = validPositions[randomIndex];
            return new Vector3(randomPos.x, randomPos.y, 0);
        }
        else
        {
            // 完全随机选择
            Vector2Int randomPos = validPositions[new System.Random(landSeed + mountainSeed + partitionIndex + 1 + mapWidth + mapHeight).Next(0, validPositions.Count)];
            return new Vector3(randomPos.x, randomPos.y, 0);
        }
    }

    // 获取指定分区的中心点（最靠近分区重心的有效点）
    public static Vector3 GetCenterPositionInPartition(int partitionIndex, int mapWidth, int mapHeight,
        float noiseScale, float landThreshold, float islandFactor, float mountainThreshold,
        int landSeed, int mountainSeed, float perlinScale, float perlinStrength,
        Tilemap elementsTilemap = null, List<Vector3> occupiedPositions = null,
        Tilemap cliffTilemap = null, int totalPartitions = -1)
    {
        if (partitionIndex < 0) return Vector3.zero;
        TerrainType[,] terrainMap = GenerateTerrainTypeMap(mapWidth, mapHeight, noiseScale, landThreshold, islandFactor, mountainThreshold, landSeed, mountainSeed);
        // 生成分区种子 - 使用传入的总分区数，如果没有传入则使用估计值
        int partitionCount = totalPartitions > 0 ? totalPartitions : Mathf.Max(partitionIndex + 1, 5);
        List<Vector2Int> seeds = GeneratePartitionSeeds(partitionCount, mapWidth, mapHeight, landSeed, mountainSeed, terrainMap);
        if (seeds.Count <= partitionIndex) return Vector3.zero;
        // 生成分区地图
        int[,] areaMap = new int[mapWidth, mapHeight];
        for (int x = 0; x < mapWidth; x++)
        for (int y = 0; y < mapHeight; y++)
        {
            if (terrainMap[x, y] == TerrainType.Water) { areaMap[x, y] = -1; continue; }
            int minIdx = 0;
            float minDist = float.MaxValue;
            for (int i = 0; i < seeds.Count; i++)
            {
                float dist = (seeds[i].x - x) * (seeds[i].x - x) + (seeds[i].y - y) * (seeds[i].y - y);
                float noise = Mathf.PerlinNoise(
                    (x + seeds[i].x * 31) * perlinScale / mapWidth,
                    (y + seeds[i].y * 47) * perlinScale / mapHeight
                ) * 2f - 1f;
                dist += noise * perlinStrength;
                if (dist < minDist) { minDist = dist; minIdx = i; }
            }
            areaMap[x, y] = minIdx;
        }
        // 收集所有有效点
        List<Vector2Int> validPositions = new List<Vector2Int>();
        for (int x = 0; x < mapWidth; x++)
        for (int y = 0; y < mapHeight; y++)
        {
            if (areaMap[x, y] == partitionIndex && (terrainMap[x, y] == TerrainType.Grass || terrainMap[x, y] == TerrainType.Mountain))
            {
                // 检查是否是cliff区域
                bool isCliff = false;
                if (cliffTilemap != null)
                {
                    var cliffTile = cliffTilemap.GetTile(new Vector3Int(x, y, 0));
                    if (cliffTile != null)
                    {
                        isCliff = true;
                    }
                }
                
                if (isCliff) continue; // 跳过cliff区域
                
                // 检查是否与 Elements Tilemap 重叠
                bool hasElement = false;
                if (elementsTilemap != null)
                {
                    var tile = elementsTilemap.GetTile(new Vector3Int(x, y, 0));
                    hasElement = tile != null;
                }
                // 检查是否与已占用的位置重叠
                bool isOccupied = false;
                if (occupiedPositions != null)
                {
                    foreach (var pos in occupiedPositions)
                    {
                        float distance = Vector3.Distance(new Vector3(x, y, 0), pos);
                        if (distance < 1.5f) // 最小间距1.5个单位，与KeyObjectGenerator保持一致
                        {
                            isOccupied = true;
                            break;
                        }
                    }
                }
                if (!hasElement && !isOccupied)
                {
                    validPositions.Add(new Vector2Int(x, y));
                }
            }
        }
        if (validPositions.Count == 0) return Vector3.zero;
        // 计算分区重心
        float sumX = 0, sumY = 0;
        foreach (var pos in validPositions)
        {
            sumX += pos.x;
            sumY += pos.y;
        }
        float centerX = sumX / validPositions.Count;
        float centerY = sumY / validPositions.Count;
        // 找到最靠近重心的点
        Vector2Int best = validPositions[0];
        float minDist2 = float.MaxValue;
        foreach (var pos in validPositions)
        {
            float dist2 = (pos.x - centerX) * (pos.x - centerX) + (pos.y - centerY) * (pos.y - centerY);
            if (dist2 < minDist2)
            {
                minDist2 = dist2;
                best = pos;
            }
        }
        return new Vector3(best.x, best.y, 0);
    }
}

/// <summary>
/// 关键对象生成器 - 负责生成和放置关键角色和道具
/// </summary>
public static class KeyObjectGenerator
{
    public static AssetIndex.AssetEntry FindBestAssetMatch(string query, AssetIndex.AssetType type, AssetIndex assetIndex)
    {
        if (assetIndex == null || assetIndex.entries == null) return null;
        
        // 1. 精确匹配
        var exact = assetIndex.entries.FirstOrDefault(e => e.type == type && e.prefab != null && (e.name == query || (e.aliases != null && e.aliases.Contains(query))));
        if (exact != null) return exact;
        
        // 2. 忽略大小写
        var ignoreCase = assetIndex.entries.FirstOrDefault(e => e.type == type && e.prefab != null && (e.name.Equals(query, System.StringComparison.OrdinalIgnoreCase) || (e.aliases != null && e.aliases.Any(a => a.Equals(query, System.StringComparison.OrdinalIgnoreCase)))));
        if (ignoreCase != null) return ignoreCase;
        
        // 3. 模糊包含
        var fuzzy = assetIndex.entries.FirstOrDefault(e => e.type == type && e.prefab != null && (e.name.Contains(query) || (e.aliases != null && e.aliases.Any(a => a.Contains(query)))));
        if (fuzzy != null) return fuzzy;
        
        // 4. 如果找不到匹配的，返回同类型的第一个有prefab的资产作为fallback
        var fallback = assetIndex.entries.FirstOrDefault(e => e.type == type && e.prefab != null);
        if (fallback != null) {
            Debug.LogWarning($"[FindBestAssetMatch] 为查询 '{query}' (类型: {type}) 使用fallback资产: {fallback.name}");
            return fallback;
        }
        
        // 5. 如果同类型也没有，返回任何有prefab的资产
        var anyFallback = assetIndex.entries.FirstOrDefault(e => e.prefab != null);
        if (anyFallback != null) {
            Debug.LogWarning($"[FindBestAssetMatch] 为查询 '{query}' (类型: {type}) 使用通用fallback资产: {anyFallback.name} (类型: {anyFallback.type})");
            return anyFallback;
        }
        
        return null;
    }

    public static async Task<AssetIndex.AssetEntry> FindBestAssetMatchAsync(string query, AssetIndex.AssetType type, AssetIndex assetIndex, System.Func<string, AssetIndex.AssetType, List<AssetIndex.AssetEntry>, Task<string>> llmSelector = null)
    {
        var local = FindBestAssetMatch(query, type, assetIndex);
        if (local != null) return local;
        
        if (llmSelector != null)
        {
            try {
                string llmName = await llmSelector(query, type, assetIndex.entries.Where(e => e.type == type && e.prefab != null).ToList());
                if (!string.IsNullOrEmpty(llmName))
                {
                    var llmResult = assetIndex.entries.FirstOrDefault(e => e.name == llmName && e.type == type && e.prefab != null);
                    if (llmResult != null) return llmResult;
                }
            }
            catch (System.Exception ex) {
                Debug.LogWarning($"[FindBestAssetMatchAsync] LLM选择器出错: {ex.Message}");
            }
        }
        
        // 如果LLM也失败了，使用fallback
        var fallback = assetIndex.entries.FirstOrDefault(e => e.type == type && e.prefab != null);
        if (fallback != null) {
            Debug.LogWarning($"[FindBestAssetMatchAsync] 为查询 '{query}' (类型: {type}) 使用fallback资产: {fallback.name}");
            return fallback;
        }
        
        var anyFallback = assetIndex.entries.FirstOrDefault(e => e.prefab != null);
        if (anyFallback != null) {
            Debug.LogWarning($"[FindBestAssetMatchAsync] 为查询 '{query}' (类型: {type}) 使用通用fallback资产: {anyFallback.name} (类型: {anyFallback.type})");
            return anyFallback;
        }
        
        return null;
    }

    public static List<GameObject> GenerateKeyObjects(List<MultiAgentRPGEditor.NarrativeStep> steps, AssetIndex assetIndex, System.Func<string, AssetIndex.AssetType, List<AssetIndex.AssetEntry>, Task<string>> llmSelector = null)
    {
        var spawned = new List<GameObject>();
        if (steps == null || assetIndex == null) return spawned;
        foreach (var step in steps)
        {
            if (step.key_characters != null)
            {
                foreach (var ch in step.key_characters)
                {
                    var asset = FindBestAssetMatch(ch, AssetIndex.AssetType.MainCharacter, assetIndex);
                    if (asset != null && asset.prefab != null)
                    {
                        var go = (GameObject)PrefabUtility.InstantiatePrefab(asset.prefab);
                        go.name = asset.name;
                        
                        // 设置Layer为KeyItem
                        go.layer = LayerMask.NameToLayer("KeyItem");
                        
                        // 不再自动添加角色脚本和设置属性
                        // 只记录基础信息以便后续代码生成阶段使用
                        // 使用元数据组件而不是标签
                        var metadata = go.AddComponent<KeyObjectMetadata>();
                        metadata.objectType = "MainCharacter";
                        metadata.displayName = asset.name;
                        metadata.description = asset.description ?? $"主角角色: {asset.name}";
                        
                        spawned.Add(go);
                    }
                    else
                    {
                        Debug.LogWarning($"[GenerateKeyObjects] 无法为关键角色 '{ch}' 找到匹配的资产");
                    }
                }
            }
            if (step.key_items != null)
            {
                foreach (var item in step.key_items)
                {
                    var asset = FindBestAssetMatch(item, AssetIndex.AssetType.Props, assetIndex);
                    if (asset != null && asset.prefab != null)
                    {
                        var go = (GameObject)PrefabUtility.InstantiatePrefab(asset.prefab);
                        go.name = asset.name;
                        
                        // 设置Layer为KeyItem
                        go.layer = LayerMask.NameToLayer("KeyItem");
                        
                        // 不再自动添加物品脚本和设置属性
                        // 只记录基础信息以便后续代码生成阶段使用
                        var metadata = go.AddComponent<KeyObjectMetadata>();
                        metadata.objectType = "Props";
                        metadata.displayName = asset.name;
                        metadata.description = asset.description ?? $"道具物品: {asset.name}";
                        
                        spawned.Add(go);
                    }
                    else
                    {
                        Debug.LogWarning($"[GenerateKeyObjects] 无法为关键物品 '{item}' 找到匹配的资产");
                    }
                }
            }
        }
        return spawned;
    }

    public static async Task<List<GameObject>> GenerateKeyObjectsAsync(List<MultiAgentRPGEditor.NarrativeStep> steps, AssetIndex assetIndex, System.Func<string, AssetIndex.AssetType, List<AssetIndex.AssetEntry>, Task<string>> llmSelector)
    {
        var spawned = new List<GameObject>();
        if (steps == null || assetIndex == null) return spawned;
        foreach (var step in steps)
        {
            if (step.key_characters != null)
            {
                foreach (var ch in step.key_characters)
                {
                    var asset = await FindBestAssetMatchAsync(ch, AssetIndex.AssetType.MainCharacter, assetIndex, llmSelector);
                    if (asset != null && asset.prefab != null)
                    {
                        var go = (GameObject)PrefabUtility.InstantiatePrefab(asset.prefab);
                        go.name = asset.name;
                        
                        // 设置Layer为KeyItem
                        go.layer = LayerMask.NameToLayer("KeyItem");
                        
                        // 不再自动添加角色脚本和设置属性
                        // 只记录基础信息以便后续代码生成阶段使用
                        // 使用元数据组件而不是标签
                        var metadata = go.AddComponent<KeyObjectMetadata>();
                        metadata.objectType = "MainCharacter";
                        metadata.displayName = asset.name;
                        metadata.description = asset.description ?? $"主角角色: {asset.name}";
                        
                        spawned.Add(go);
                    }
                    else
                    {
                        Debug.LogWarning($"[GenerateKeyObjectsAsync] 无法为关键角色 '{ch}' 找到匹配的资产");
                    }
                }
            }
            if (step.key_items != null)
            {
                foreach (var item in step.key_items)
                {
                    var asset = await FindBestAssetMatchAsync(item, AssetIndex.AssetType.Props, assetIndex, llmSelector);
                    if (asset != null && asset.prefab != null)
                    {
                        var go = (GameObject)PrefabUtility.InstantiatePrefab(asset.prefab);
                        go.name = asset.name;
                        
                        // 设置Layer为KeyItem
                        go.layer = LayerMask.NameToLayer("KeyItem");
                        
                        // 不再自动添加物品脚本和设置属性
                        // 只记录基础信息以便后续代码生成阶段使用
                        var metadata = go.AddComponent<KeyObjectMetadata>();
                        metadata.objectType = "Props";
                        metadata.displayName = asset.name;
                        metadata.description = asset.description ?? $"道具物品: {asset.name}";
                        
                        spawned.Add(go);
                    }
                    else
                    {
                        Debug.LogWarning($"[GenerateKeyObjectsAsync] 无法为关键物品 '{item}' 找到匹配的资产");
                    }
                }
            }
        }
        return spawned;
    }

    public static async Task<List<GameObject>> GenerateKeyObjectsFromSceneAgentResultAsync(
        List<MultiAgentRPGEditor.NarrativeStep> steps,
        AssetIndex assetIndex,
        List<MultiAgentRPGEditor.SelectedAsset> selectedAssets,
        int mapWidth = 100, int mapHeight = 100,
        float noiseScale = 20f, float landThreshold = 0.5f, float islandFactor = 1.2f, float mountainThreshold = 0.75f,
        int landSeed = 0, int mountainSeed = 1, float perlinScale = 3f, float perlinStrength = 3f,
        Tilemap elementsTilemap = null,
        Tilemap landTilemap = null,
        Tilemap mountainTilemap = null,
        Tilemap cliffTilemap = null)
    {
        var spawned = new List<GameObject>();
        if (steps == null || assetIndex == null || selectedAssets == null) return spawned;
        
        // 跟踪已占用的位置，避免对象重叠
        var occupiedPositions = new List<Vector3>();
        
        // 统一使用 selectedAssets.Count 作为分区数，确保与后续计算一致
        int partitionCount = selectedAssets.Count;
        Debug.Log($"[KeyObjectGen] 开始生成关键对象，分区数={partitionCount}，步骤数={steps?.Count ?? 0}");
        
        // 新增：获取场景元素分布的占用格子，但只标记真正被占用的位置
        bool[,] elementOccupied = null;
        if (elementsTilemap != null)
        {
            // 复用地图参数，生成地形和分区
            var terrainMap = MapGenerator.GenerateTerrainTypeMap(mapWidth, mapHeight, noiseScale, landThreshold, islandFactor, mountainThreshold, landSeed, mountainSeed);
            var seeds = MapGenerator.GeneratePartitionSeeds(partitionCount, mapWidth, mapHeight, landSeed, mountainSeed, terrainMap);
            int[,] areaMap = new int[mapWidth, mapHeight];
            for (int x = 0; x < mapWidth; x++)
            for (int y = 0; y < mapHeight; y++)
            {
                if (terrainMap[x, y] == MapGenerator.TerrainType.Water) { areaMap[x, y] = -1; continue; }
                int minIdx = 0;
                float minDist = float.MaxValue;
                for (int i = 0; i < seeds.Count; i++)
                {
                    float dist = (seeds[i].x - x) * (seeds[i].x - x) + (seeds[i].y - y) * (seeds[i].y - y);
                    float noise = Mathf.PerlinNoise(
                        (x + seeds[i].x * 31) * perlinScale / mapWidth,
                        (y + seeds[i].y * 47) * perlinScale / mapHeight
                    ) * 2f - 1f;
                    dist += noise * perlinStrength;
                    if (dist < minDist) { minDist = dist; minIdx = i; }
                }
                areaMap[x, y] = minIdx;
            }
            // 统计所有被元素占用的格子，但只标记真正被占用的位置
            elementOccupied = new bool[mapWidth, mapHeight];
            int elementCount = 0;
            for (int x = 0; x < mapWidth; x++)
            for (int y = 0; y < mapHeight; y++)
            {
                var tile = elementsTilemap.GetTile(new UnityEngine.Vector3Int(x, y, 0));
                if (tile != null)
                {
                    elementOccupied[x, y] = true;
                    occupiedPositions.Add(new Vector3(x, y, 0));
                    elementCount++;
                }
            }
            Debug.Log($"[KeyObjectGen] 检测到 {elementCount} 个被元素占用的位置");
        }
        
        // 新分布逻辑：严格按Scene Agent分配结果分布，partitionIndex用selectedAssets顺序i，保证和沃洛诺伊分区一致
        for (int i = 0; i < selectedAssets.Count; i++)
        {
            var selected = selectedAssets[i];
            int partitionIndex = i; // 沃洛诺伊分区顺序严格一致
            Debug.Log($"[KeyObjectGen] 分区{partitionIndex+1} (SceneAgent step={selected.step})，SceneAgent分配资产数={selected.assets?.Count ?? 0}");
            
            // 统计本分区剩余可用点 - 使用统一的分区计算
            var terrainMap = MapGenerator.GenerateTerrainTypeMap(mapWidth, mapHeight, noiseScale, landThreshold, islandFactor, mountainThreshold, landSeed, mountainSeed);
            var seeds = MapGenerator.GeneratePartitionSeeds(partitionCount, mapWidth, mapHeight, landSeed, mountainSeed, terrainMap);
            int[,] areaMap = new int[mapWidth, mapHeight];
            for (int x = 0; x < mapWidth; x++)
            for (int y = 0; y < mapHeight; y++)
            {
                if (terrainMap[x, y] == MapGenerator.TerrainType.Water) { areaMap[x, y] = -1; continue; }
                int minIdx = 0;
                float minDist = float.MaxValue;
                for (int si = 0; si < seeds.Count; si++)
                {
                    float dist = (seeds[si].x - x) * (seeds[si].x - x) + (seeds[si].y - y) * (seeds[si].y - y);
                    float noise = Mathf.PerlinNoise(
                        (x + seeds[si].x * 31) * perlinScale / mapWidth,
                        (y + seeds[si].y * 47) * perlinScale / mapHeight
                    ) * 2f - 1f;
                    dist += noise * perlinStrength;
                    if (dist < minDist) { minDist = dist; minIdx = si; }
                }
                areaMap[x, y] = minIdx;
            }
            
            // 统计本分区可用点 - 简化逻辑，只要不是cliff的陆地区域都可以
            List<Vector2Int> availableCells = new List<Vector2Int>();
            int totalCellsInPartition = 0;
            int occupiedCellsInPartition = 0;
            int waterCellsInPartition = 0;
            int cliffCellsInPartition = 0;
            
            for (int x = 0; x < mapWidth; x++)
            for (int y = 0; y < mapHeight; y++)
            {
                if (areaMap[x, y] == partitionIndex)
                {
                    totalCellsInPartition++;
                    
                    // 检查地形类型
                    if (terrainMap[x, y] == MapGenerator.TerrainType.Water)
                    {
                        waterCellsInPartition++;
                        continue; // 跳过水域
                    }
                    
                    // 检查是否是cliff区域（通过检查cliffTilemap）
                    bool isCliff = false;
                    if (cliffTilemap != null)
                    {
                        var cliffTile = cliffTilemap.GetTile(new Vector3Int(x, y, 0));
                        if (cliffTile != null)
                        {
                            isCliff = true;
                            cliffCellsInPartition++;
                        }
                    }
                    
                    if (isCliff)
                    {
                        continue; // 跳过cliff区域
                    }
                    
                    // 现在只考虑非cliff的陆地区域
                    bool occupied = false;
                    
                    // 检查是否被元素占用
                    if (elementOccupied != null && elementOccupied[x, y])
                    {
                        occupied = true;
                        occupiedCellsInPartition++;
                    }
                    else
                    {
                        // 检查是否与已放置的关键对象太近
                        foreach (var pos in occupiedPositions)
                        {
                            if (Vector3.Distance(new Vector3(x, y, 0), pos) < 1.5f)
                            {
                                occupied = true;
                                occupiedCellsInPartition++;
                                break;
                            }
                        }
                    }
                    
                    if (!occupied)
                        availableCells.Add(new Vector2Int(x, y));
                }
            }
            
            int needCount = selected.assets?.Count ?? 0;
            Debug.Log($"[KeyObjectGen] 分区{partitionIndex+1} (SceneAgent step={selected.step}) 总格子={totalCellsInPartition}，水域={waterCellsInPartition}，cliff={cliffCellsInPartition}，被占用={occupiedCellsInPartition}，可用点={availableCells.Count}，需分布SceneAgent资产={needCount}");
            
            // 如果可用点太少，输出更详细的调试信息
            if (availableCells.Count < needCount)
            {
                Debug.LogError($"[KeyObjectGen] 分区{partitionIndex+1} (SceneAgent step={selected.step}) 可用点({availableCells.Count})不足，无法分布全部SceneAgent资产({needCount})");
                Debug.LogError($"[KeyObjectGen] 分区{partitionIndex+1} 详细信息：总格子={totalCellsInPartition}，水域={waterCellsInPartition}，cliff={cliffCellsInPartition}，被占用={occupiedCellsInPartition}");
                
                // 输出分区边界信息
                int minX = mapWidth, maxX = 0, minY = mapHeight, maxY = 0;
                for (int x = 0; x < mapWidth; x++)
                for (int y = 0; y < mapHeight; y++)
                {
                    if (areaMap[x, y] == partitionIndex)
                    {
                        minX = Mathf.Min(minX, x);
                        maxX = Mathf.Max(maxX, x);
                        minY = Mathf.Min(minY, y);
                        maxY = Mathf.Max(maxY, y);
                    }
                }
                Debug.LogError($"[KeyObjectGen] 分区{partitionIndex+1} 边界：X({minX}-{maxX}) Y({minY}-{maxY})");
                continue;
            }
            
            if (selected.assets != null)
            {
                foreach (var assetName in selected.assets)
                {
                    // 尝试找到匹配的资产
                    var asset = assetIndex.entries.FirstOrDefault(e => e.name == assetName && (e.type == AssetIndex.AssetType.MainCharacter || e.type == AssetIndex.AssetType.NPC || e.type == AssetIndex.AssetType.Enemy || e.type == AssetIndex.AssetType.Props));
                    
                    // 如果找不到匹配的资产，使用默认资产作为fallback
                    if (asset == null)
                    {
                        Debug.LogWarning($"[KeyObjectGen] 分区{partitionIndex+1} (SceneAgent step={selected.step}) 资产{assetName} 未在AssetIndex中找到，使用默认资产");
                        
                        // 根据资产名称的特征选择合适的默认类型
                        AssetIndex.AssetType defaultType = AssetIndex.AssetType.Props; // 默认为道具
                        if (assetName.Contains("角色") || assetName.Contains("Character") || assetName.Contains("Player") || assetName.Contains("Hero"))
                        {
                            defaultType = AssetIndex.AssetType.MainCharacter;
                        }
                        else if (assetName.Contains("NPC") || assetName.Contains("商人") || assetName.Contains("村民"))
                        {
                            defaultType = AssetIndex.AssetType.NPC;
                        }
                        else if (assetName.Contains("敌人") || assetName.Contains("Enemy") || assetName.Contains("Monster") || assetName.Contains("Boss"))
                        {
                            defaultType = AssetIndex.AssetType.Enemy;
                        }
                        
                        // 选择该类型的第一个可用资产作为默认
                        asset = assetIndex.entries.FirstOrDefault(e => e.type == defaultType && e.prefab != null);
                        
                        if (asset == null)
                        {
                            // 如果该类型也没有，选择任何有prefab的资产
                            asset = assetIndex.entries.FirstOrDefault(e => e.prefab != null);
                        }
                        
                        if (asset == null)
                        {
                            Debug.LogError($"[KeyObjectGen] 分区{partitionIndex+1} (SceneAgent step={selected.step}) 无法为资产{assetName}找到任何可用的默认资产，跳过");
                            continue;
                        }
                        
                        Debug.Log($"[KeyObjectGen] 分区{partitionIndex+1} (SceneAgent step={selected.step}) 为资产{assetName}使用默认资产: {asset.name} (类型: {asset.type})");
                    }
                    
                    if (asset.prefab == null)
                    {
                        Debug.LogWarning($"[KeyObjectGen] 分区{partitionIndex+1} (SceneAgent step={selected.step}) 资产{asset.name} 的Prefab为空，尝试找到有prefab的同类型资产");
                        
                        // 尝试找到同类型的其他有prefab的资产
                        var fallbackAsset = assetIndex.entries.FirstOrDefault(e => e.type == asset.type && e.prefab != null);
                        if (fallbackAsset != null)
                        {
                            asset = fallbackAsset;
                            Debug.Log($"[KeyObjectGen] 分区{partitionIndex+1} (SceneAgent step={selected.step}) 使用fallback资产: {asset.name}");
                        }
                        else
                        {
                            Debug.LogError($"[KeyObjectGen] 分区{partitionIndex+1} (SceneAgent step={selected.step}) 无法为资产{asset.name}找到任何可用的prefab，跳过");
                            continue;
                        }
                    }
                    
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(asset.prefab);
                    go.name = asset.name;
                    
                    // 设置Layer为KeyItem
                    go.layer = LayerMask.NameToLayer("KeyItem");
                    
                    Vector3 position = MapGenerator.GetRandomPositionInPartition(
                        partitionIndex, mapWidth, mapHeight,
                        noiseScale, landThreshold, islandFactor, mountainThreshold,
                        landSeed, mountainSeed, perlinScale, perlinStrength,
                        elementsTilemap, occupiedPositions, true, // preferCenter = true
                        cliffTilemap, partitionCount
                    );
                    go.transform.position = position;
                    if (position != Vector3.zero)
                    {
                        occupiedPositions.Add(position);
                        Debug.Log($"[KeyObjectGen] 在分区 {partitionIndex+1} (SceneAgent step={selected.step}) 中放置SceneAgent资产 {asset.name} 到位置 {position}");
                    }
                    else
                    {
                        Debug.LogWarning($"[KeyObjectGen] 无法为SceneAgent资产 {asset.name} 在分区 {partitionIndex+1} (SceneAgent step={selected.step}) 中找到有效位置");
                    }
                    var metadata = go.AddComponent<KeyObjectMetadata>();
                    metadata.objectType = asset.type.ToString();
                    metadata.displayName = asset.name;
                    metadata.description = asset.description ?? $"SceneAgent资产: {asset.name}";
                    spawned.Add(go);
                }
            }
        }
        return spawned;
    }

    public static void ClearKeyObjects(List<GameObject> spawnedKeyObjects)
    {
        // 删除所有Layer为KeyItem的物体
        var allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
        foreach (var go in allObjects)
        {
            if (go != null && go.layer == LayerMask.NameToLayer("KeyItem"))
            {
                if (Application.isEditor)
                    Object.DestroyImmediate(go);
                else
                    Object.Destroy(go);
            }
        }
        
        // 兼容旧逻辑，删除已记录的spawnedKeyObjects
        foreach (var go in spawnedKeyObjects)
        {
            if (go != null)
            {
                if (Application.isEditor)
                    Object.DestroyImmediate(go);
                else
                    Object.Destroy(go);
            }
        }
        spawnedKeyObjects.Clear();
    }
}

/// <summary>
/// 地图生成管理器 - 提供地图生成和预览功能
/// </summary>
public static class MapGenerationManager
{
    /// <summary>
    /// 生成地形预览纹理
    /// </summary>
    public static Texture2D GenerateTerrainPreview(int mapWidth, int mapHeight, float noiseScale, float landThreshold, float islandFactor, float mountainThreshold, int cliffHeight, int landSeed, int mountainSeed)
    {
        var previewTex = new Texture2D(mapWidth, mapHeight, TextureFormat.RGBA32, false);
        MapGenerator.TerrainType[,] terrainMap = MapGenerator.GenerateTerrainTypeMap(mapWidth, mapHeight, noiseScale, landThreshold, islandFactor, mountainThreshold, landSeed, mountainSeed);
        
        // 先全部填充
        for (int x = 0; x < mapWidth; x++)
        for (int y = 0; y < mapHeight; y++)
        {
            Color c = Color.black;
            switch (terrainMap[x, y])
            {
                case MapGenerator.TerrainType.Water: c = new Color(0.2f, 0.4f, 1f); break;
                case MapGenerator.TerrainType.Grass: c = new Color(0.3f, 0.8f, 0.3f); break;
                case MapGenerator.TerrainType.Mountain: c = new Color(0.7f, 0.7f, 0.7f); break;
            }
            previewTex.SetPixel(x, y, c);
        }
        
        // 悬崖预览（叠加棕色）
        for (int x = 0; x < mapWidth; x++)
        for (int y = 0; y < mapHeight; y++)
        {
            if (terrainMap[x, y] == MapGenerator.TerrainType.Mountain)
            {
                for (int h = 1; h <= cliffHeight; h++)
                {
                    int cy = y - h;
                    if (cy >= 0 && terrainMap[x, cy] != MapGenerator.TerrainType.Mountain)
                        previewTex.SetPixel(x, cy, new Color(0.5f, 0.3f, 0.1f));
                }
            }
        }
        previewTex.Apply();
        return previewTex;
    }
    
    /// <summary>
    /// 清除所有Tilemap
    /// </summary>
    public static void ClearAllTilemaps(Tilemap waterTilemap, Tilemap landTilemap, Tilemap mountainTilemap, Tilemap cliffTilemap, Tilemap elementsTilemap)
    {
        if (waterTilemap) waterTilemap.ClearAllTiles();
        if (landTilemap) landTilemap.ClearAllTiles();
        if (mountainTilemap) mountainTilemap.ClearAllTiles();
        if (cliffTilemap) cliffTilemap.ClearAllTiles();
        if (elementsTilemap) elementsTilemap.ClearAllTiles();
    }
    
    /// <summary>
    /// 保存纹理为PNG文件
    /// </summary>
    public static void SaveTextureAsPNG(Texture2D tex, string defaultName)
    {
        string path = UnityEditor.EditorUtility.SaveFilePanel("保存图像", "Assets", defaultName, "png");
        if (!string.IsNullOrEmpty(path))
        {
            byte[] pngData = tex.EncodeToPNG();
            System.IO.File.WriteAllBytes(path, pngData);
            UnityEditor.AssetDatabase.Refresh();
            UnityEditor.EditorUtility.DisplayDialog("保存成功", $"图像已保存到: {path}", "确定");
        }
    }
} 