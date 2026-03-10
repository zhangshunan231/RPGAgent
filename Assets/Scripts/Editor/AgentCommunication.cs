using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System;

public static class AgentCommunication
{
    public static readonly HttpClient httpClient = new HttpClient();
    public const string SERVER_URL = "http://127.0.0.1:5000";

    // 添加序列化用的类
    [System.Serializable]
    public class NarrativeRequest
    {
        public string input;
        
        public NarrativeRequest(string input)
        {
            this.input = input;
        }
    }

    public static async Task<string> CallPythonAgent(string agentType, string input)
    {
        try
        {
            // 添加详细的输入调试信息
            Debug.Log($"[AgentCommunication] === 调用开始 ===");
            Debug.Log($"[AgentCommunication] agentType: '{agentType}'");
            Debug.Log($"[AgentCommunication] input: '{input}'");
            Debug.Log($"[AgentCommunication] input长度: {input?.Length ?? 0}");
            Debug.Log($"[AgentCommunication] input是否为null: {input == null}");
            Debug.Log($"[AgentCommunication] input是否为空白: {string.IsNullOrWhiteSpace(input)}");
            
            string json;
            // 针对不同agent类型进行特殊处理
            if (agentType == "scene")
            {
                // 直接使用input作为请求体
                json = input;
                Debug.Log($"[AgentCommunication] 发送到/scene的JSON: {(json.Length > 200 ? json.Substring(0, 200) + "..." : json)}");
            }
            else if (agentType == "narrative")
            {
                // 叙事生成需要包装为JSON格式，使用input字段
                Debug.Log($"[AgentCommunication] 准备包装为JSON，input='{input}'");
                var request = new NarrativeRequest(input);
                json = JsonUtility.ToJson(request);
                Debug.Log($"[AgentCommunication] 包装后的JSON: '{json}'");
                Debug.Log($"[AgentCommunication] 发送到/narrative的JSON: {(json.Length > 200 ? json.Substring(0, 200) + "..." : json)}");
            }
            else if (agentType == "codegen")
            {
                // 代码生成也直接使用input作为请求体，但需要检查格式
                // 如果input不是有效的JSON，则进行包装
                if (string.IsNullOrWhiteSpace(input))
                {
                    Debug.LogError("[AgentCommunication] 输入为空，无法发送请求");
                    throw new System.Exception("代码生成输入为空，请输入有效的机制描述");
                }
                
                // 检查输入格式
                if (input.Trim().StartsWith("[") || input.Trim().StartsWith("{"))
                {
                    // 可能已经是JSON格式，直接使用
                    json = input;
                    Debug.Log("[DEBUG] 解析后的JSON: " + (json.Length > 200 ? json.Substring(0, 200) + "..." : json));
                }
                else
                {
                    // 不是JSON格式，包装为JSON
                    json = JsonUtility.ToJson(new { input = input });
                    Debug.Log("[DEBUG] 包装后的JSON: " + (json.Length > 200 ? json.Substring(0, 200) + "..." : json));
                }
                Debug.Log($"[DEBUG] 传递给CodeGen Agent的内容: {(json.Length > 200 ? json.Substring(0, 200) + "..." : json)}");
            }
            else
            {
                // 其他类型直接使用input作为请求体
                json = input;
                Debug.Log($"[AgentCommunication] 发送到/{agentType}的JSON: {(json.Length > 200 ? json.Substring(0, 200) + "..." : json)}");
            }
            
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            Debug.Log($"[AgentCommunication] 请求URL: {SERVER_URL}/generate_{agentType}");
            
            var response = await httpClient.PostAsync($"{SERVER_URL}/generate_{agentType}", content);
            var responseString = await response.Content.ReadAsStringAsync();
            Debug.Log($"[AgentCommunication] 响应状态码: {response.StatusCode}");
            Debug.Log($"[AgentCommunication] 响应内容: {(responseString.Length > 200 ? responseString.Substring(0, 200) + "..." : responseString)}");
            
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    // 尝试使用JsonUtility解析
                    var responseData = JsonUtility.FromJson<AgentResponse>(responseString);
                    
                    // 检查是否为codegen类型且响应包含markdown代码块
                    if (agentType == "codegen" && responseData != null && responseData.result != null)
                    {
                        // LLM可能会返回markdown代码块格式，需要提取JSON
                        string result = responseData.result;
                        
                        // 检查是否包含markdown代码块
                        if (result.Contains("```json") || result.Contains("```"))
                        {
                            Debug.Log("[AgentCommunication] 检测到markdown代码块，尝试提取内容");
                            var codeBlockMatch = System.Text.RegularExpressions.Regex.Match(result, "```(?:json)?\\s*\\n(.*?)\\n```", System.Text.RegularExpressions.RegexOptions.Singleline);
                            if (codeBlockMatch.Success)
                            {
                                string extractedJson = codeBlockMatch.Groups[1].Value.Trim();
                                Debug.Log("[AgentCommunication] 成功提取代码块内容: " + 
                                    (extractedJson.Length > 200 ? extractedJson.Substring(0, 200) + "..." : extractedJson));
                                
                                // 进一步验证提取的JSON格式是否正确
                                try
                                {
                                    // 尝试解析提取的JSON
                                    var testData = MiniJSON.Json.Deserialize(extractedJson) as Dictionary<string, object>;
                                    if (testData != null)
                                    {
                                        Debug.Log("[AgentCommunication] JSON格式有效，包含字段: " + string.Join(", ", testData.Keys));
                                        
                                        // 检查是否包含target_objects字段
                                        if (testData.ContainsKey("target_objects"))
                                        {
                                            Debug.Log("[AgentCommunication] 检测到target_objects字段");
                                            var targetObjects = testData["target_objects"] as List<object>;
                                            if (targetObjects != null && targetObjects.Count > 0)
                                            {
                                                Debug.Log($"[AgentCommunication] target_objects: {string.Join(", ", targetObjects)}");
                                            }
                                        }
                                    }
                                    return extractedJson;
                                }
                                catch (Exception jsonEx)
                                {
                                    Debug.LogWarning($"[AgentCommunication] 提取的JSON格式无效: {jsonEx.Message}，将返回原始提取内容");
                                    return extractedJson;
                                }
                            }
                        }
                        
                        // 如果不是markdown格式，检查是否本身就是JSON
                        if (result.Trim().StartsWith("{"))
                        {
                            try
                            {
                                // 尝试解析原始结果
                                var testData = MiniJSON.Json.Deserialize(result) as Dictionary<string, object>;
                                if (testData != null)
                                {
                                    Debug.Log("[AgentCommunication] 原始结果为有效JSON，包含字段: " + string.Join(", ", testData.Keys));
                                    
                                    // 检查是否包含target_objects字段
                                    if (testData.ContainsKey("target_objects"))
                                    {
                                        Debug.Log("[AgentCommunication] 检测到target_objects字段");
                                        var targetObjects = testData["target_objects"] as List<object>;
                                        if (targetObjects != null && targetObjects.Count > 0)
                                        {
                                            Debug.Log($"[AgentCommunication] target_objects: {string.Join(", ", targetObjects)}");
                                        }
                                    }
                                }
                                return result;
                            }
                            catch (Exception jsonEx)
                            {
                                Debug.LogWarning($"[AgentCommunication] 原始结果JSON格式无效: {jsonEx.Message}，将返回原始内容");
                                return result;
                            }
                        }
                    }
                    
                    return responseData.result;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[AgentCommunication] JSON解析错误: {ex.Message}");
                    // 尝试直接返回响应字符串
                    if (!string.IsNullOrEmpty(responseString))
                    {
                        return responseString;
                    }
                    throw new System.Exception($"解析响应数据失败: {ex.Message}");
                }
            }
            else
            {
                throw new System.Exception($"HTTP错误: {response.StatusCode} - {responseString}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AgentCommunication] 调用失败: {e.Message}");
            throw new System.Exception($"调用Python Agent失败: {e.Message}");
        }
    }

    public static async Task<string> CallLLM(string prompt)
    {
        try
        {
            var json = JsonUtility.ToJson(new { prompt = prompt });
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync($"{SERVER_URL}/llm", content);
            var responseString = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                var responseData = JsonUtility.FromJson<LLMResponse>(responseString);
                return responseData.result;
            }
            else
            {
                throw new System.Exception($"HTTP错误: {response.StatusCode} - {responseString}");
            }
        }
        catch (System.Exception e)
        {
            throw new System.Exception($"调用LLM失败: {e.Message}");
        }
    }

    [System.Serializable]
    private class AgentResponse
    {
        public string result;
        public string status;
    }
    [System.Serializable]
    private class LLMResponse
    {
        public string result;
        public string status;
    }
} 