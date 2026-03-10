// MiniJSON.cs
// Unity开源轻量级JSON解析器
// 来源：https://gist.github.com/darktable/1411710
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;

public static class MiniJSON
{
    public static object JsonDeserialize(string json)
    {
        return Json.Deserialize(json);
    }
    public static string JsonSerialize(object obj)
    {
        return Json.Serialize(obj);
    }

    public sealed class Json
    {
        public static object Deserialize(string json)
        {
            if (json == null) return null;
            return Parser.Parse(json);
        }
        sealed class Parser : IDisposable
        {
            const string WORD_BREAK = "{}[],:\"";
            StringReader json;
            Parser(string jsonString) { json = new StringReader(jsonString); }
            public static object Parse(string jsonString) { using (var instance = new Parser(jsonString)) return instance.ParseValue(); }
            public void Dispose() { json.Dispose(); }
            enum TOKEN { NONE, CURLY_OPEN, CURLY_CLOSE, SQUARE_OPEN, SQUARE_CLOSE, COLON, COMMA, STRING, NUMBER, TRUE, FALSE, NULL }
            Dictionary<string, object> ParseObject() { var table = new Dictionary<string, object>(); json.Read(); while (true) { var token = NextToken; if (token == TOKEN.NONE) return null; if (token == TOKEN.CURLY_CLOSE) return table; var name = ParseString(); if (NextToken != TOKEN.COLON) return null; json.Read(); table[name] = ParseValue(); var next = NextToken; if (next == TOKEN.COMMA) { json.Read(); continue; } else if (next == TOKEN.CURLY_CLOSE) { json.Read(); return table; } else return null; } }
            List<object> ParseArray() { var array = new List<object>(); json.Read(); while (true) { var token = NextToken; if (token == TOKEN.NONE) return null; if (token == TOKEN.SQUARE_CLOSE) { json.Read(); break; } var value = ParseValue(); array.Add(value); var next = NextToken; if (next == TOKEN.COMMA) { json.Read(); continue; } else if (next == TOKEN.SQUARE_CLOSE) { json.Read(); break; } else return null; } return array; }
            object ParseValue() { switch (NextToken) { case TOKEN.STRING: return ParseString(); case TOKEN.NUMBER: return ParseNumber(); case TOKEN.CURLY_OPEN: return ParseObject(); case TOKEN.SQUARE_OPEN: return ParseArray(); case TOKEN.TRUE: json.Read(); return true; case TOKEN.FALSE: json.Read(); return false; case TOKEN.NULL: json.Read(); return null; default: return null; } }
            string ParseString() { var sb = new StringBuilder(); json.Read(); while (true) { if (json.Peek() == -1) break; var c = (char)json.Read(); if (c == '"') break; if (c == '\\') { if (json.Peek() == -1) break; c = (char)json.Read(); if (c == '"') sb.Append('"'); else if (c == '\\') sb.Append('\\'); else if (c == '/') sb.Append('/'); else if (c == 'b') sb.Append('\b'); else if (c == 'f') sb.Append('\f'); else if (c == 'n') sb.Append('\n'); else if (c == 'r') sb.Append('\r'); else if (c == 't') sb.Append('\t'); else if (c == 'u') { var hex = new char[4]; for (int i = 0; i < 4; i++) hex[i] = (char)json.Read(); sb.Append((char)Convert.ToInt32(new string(hex), 16)); } } else sb.Append(c); } return sb.ToString(); }
            object ParseNumber() { var number = NextWord; if (number.IndexOf('.') == -1) { long parsedInt; long.TryParse(number, out parsedInt); return parsedInt; } double parsedDouble; double.TryParse(number, out parsedDouble); return parsedDouble; }
            string NextWord { get { var sb = new StringBuilder(); while (!IsWordBreak(json.Peek())) sb.Append((char)json.Read()); return sb.ToString(); } }
            TOKEN NextToken { get { EatWhitespace(); if (json.Peek() == -1) return TOKEN.NONE; switch ((char)json.Peek()) { case '{': return TOKEN.CURLY_OPEN; case '}': return TOKEN.CURLY_CLOSE; case '[': return TOKEN.SQUARE_OPEN; case ']': return TOKEN.SQUARE_CLOSE; case ',': return TOKEN.COMMA; case ':': return TOKEN.COLON; case '"': return TOKEN.STRING; case '-': case '0': case '1': case '2': case '3': case '4': case '5': case '6': case '7': case '8': case '9': return TOKEN.NUMBER; } var word = NextWord; switch (word) { case "false": return TOKEN.FALSE; case "true": return TOKEN.TRUE; case "null": return TOKEN.NULL; } return TOKEN.NONE; } }
            void EatWhitespace() { while (char.IsWhiteSpace((char)json.Peek())) json.Read(); }
            static bool IsWordBreak(int c) { return c == -1 || WORD_BREAK.IndexOf((char)c) != -1 || char.IsWhiteSpace((char)c); }
        }
        public static string Serialize(object obj) { var builder = new StringBuilder(); Serializer.SerializeValue(obj, builder); return builder.ToString(); }
        sealed class Serializer
        {
            public static void SerializeValue(object value, StringBuilder builder)
            {
                if (value == null) builder.Append("null");
                else if (value is string) SerializeString((string)value, builder);
                else if (value is bool) builder.Append((bool)value ? "true" : "false");
                else if (value is IList) SerializeArray((IList)value, builder);
                else if (value is IDictionary) SerializeObject((IDictionary)value, builder);
                else if (value is char) SerializeString(new string((char)value, 1), builder);
                else if (value is double || value is float) builder.Append(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture));
                else builder.Append(value.ToString());
            }
            static void SerializeObject(IDictionary obj, StringBuilder builder)
            {
                builder.Append('{');
                bool first = true;
                foreach (object e in obj.Keys)
                {
                    if (!first) builder.Append(',');
                    SerializeString(e.ToString(), builder);
                    builder.Append(':');
                    SerializeValue(obj[e], builder);
                    first = false;
                }
                builder.Append('}');
            }
            static void SerializeArray(IList anArray, StringBuilder builder)
            {
                builder.Append('[');
                bool first = true;
                foreach (object obj in anArray)
                {
                    if (!first) builder.Append(',');
                    SerializeValue(obj, builder);
                    first = false;
                }
                builder.Append(']');
            }
            static void SerializeString(string str, StringBuilder builder)
            {
                builder.Append('"');
                foreach (var c in str)
                {
                    switch (c)
                    {
                        case '"': builder.Append("\\\""); break;
                        case '\\': builder.Append("\\\\"); break;
                        case '\b': builder.Append("\\b"); break;
                        case '\f': builder.Append("\\f"); break;
                        case '\n': builder.Append("\\n"); break;
                        case '\r': builder.Append("\\r"); break;
                        case '\t': builder.Append("\\t"); break;
                        default:
                            if (c < ' ' || c > 127)
                                builder.AppendFormat("\\u{0:X4}", (int)c);
                            else
                                builder.Append(c);
                            break;
                    }
                }
                builder.Append('"');
            }
        }
    }
}

// 用法示例：
// var obj = MiniJSON.JsonDeserialize(jsonString);
// var json = MiniJSON.JsonSerialize(obj);

// 兼容性提示：如需在Unity编辑器下使用，请确保本文件在Editor目录下。 