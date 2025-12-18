using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;


[AttributeUsage(AttributeTargets.Class)]
public sealed class JsonTypeAliasAttribute : Attribute
{
    public string Alias { get; }
    public JsonTypeAliasAttribute(string alias) => Alias = alias;
}

public sealed class AliasBinder : ISerializationBinder
{
    private readonly Dictionary<string, Type> _aliasToType;
    private readonly Dictionary<Type, string> _typeToAlias;

    public AliasBinder(IEnumerable<Type> knownTypes)
    {
        _aliasToType = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        _typeToAlias = new Dictionary<Type, string>();

        foreach (var t in knownTypes)
        {
            var alias = t.GetCustomAttribute<JsonTypeAliasAttribute>()?.Alias ?? t.Name;
            _aliasToType[alias] = t;
            _typeToAlias[t] = alias;
        }
    }

    // 反序列化："$type": "Animation" -> typeof(AnimationEvent)
    public Type BindToType(string assemblyName, string typeName)
        => _aliasToType.TryGetValue(typeName, out var t) ? t
            : throw new JsonSerializationException($"Unknown type alias: {typeName}");

    // 序列化：typeof(AnimationEvent) -> "$type": "Animation"
    public void BindToName(Type serializedType, out string assemblyName, out string typeName)
    {
        assemblyName = null; // 关键：不输出程序集
        if (!_typeToAlias.TryGetValue(serializedType, out typeName))
            typeName = serializedType.Name; // 或者直接 throw，强制必须注册
    }
}

public class AnimationCurveConverter : JsonConverter<AnimationCurve>
{
    public override void WriteJson(JsonWriter writer, AnimationCurve value, JsonSerializer serializer)
    {
        writer.WriteStartObject();
        
        // 序列化关键帧
        writer.WritePropertyName("keys");
        writer.WriteStartArray();
        foreach (var keyframe in value.keys)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("time");
            writer.WriteValue(keyframe.time);
            writer.WritePropertyName("value");
            writer.WriteValue(keyframe.value);
            writer.WritePropertyName("inTangent");
            writer.WriteValue(keyframe.inTangent);
            writer.WritePropertyName("outTangent");
            writer.WriteValue(keyframe.outTangent);
            writer.WritePropertyName("inWeight");
            writer.WriteValue(keyframe.inWeight);
            writer.WritePropertyName("outWeight");
            writer.WriteValue(keyframe.outWeight);
            writer.WritePropertyName("weightedMode");
            writer.WriteValue((int)keyframe.weightedMode);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
        
        // 序列化其他属性
        writer.WritePropertyName("preWrapMode");
        writer.WriteValue((int)value.preWrapMode);
        writer.WritePropertyName("postWrapMode");
        writer.WriteValue((int)value.postWrapMode);
        
        writer.WriteEndObject();
    }

    public override AnimationCurve ReadJson(JsonReader reader, Type objectType, AnimationCurve existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        JObject jsonObject = JObject.Load(reader);
        
        var curve = new AnimationCurve();
        
        // 反序列化关键帧
        var keysArray = jsonObject["keys"] as JArray;
        if (keysArray != null)
        {
            var keyframes = new Keyframe[keysArray.Count];
            for (int i = 0; i < keysArray.Count; i++)
            {
                var keyframeObj = keysArray[i] as JObject;
                var keyframe = new Keyframe(
                    keyframeObj["time"].Value<float>(),
                    keyframeObj["value"].Value<float>()
                );
                
                keyframe.inTangent = keyframeObj["inTangent"].Value<float>();
                keyframe.outTangent = keyframeObj["outTangent"].Value<float>();
                keyframe.inWeight = keyframeObj["inWeight"].Value<float>();
                keyframe.outWeight = keyframeObj["outWeight"].Value<float>();
                keyframe.weightedMode = (WeightedMode)keyframeObj["weightedMode"].Value<int>();
                
                keyframes[i] = keyframe;
            }
            curve.keys = keyframes;
        }
        
        // 反序列化其他属性
        if (jsonObject["preWrapMode"] != null)
            curve.preWrapMode = (WrapMode)jsonObject["preWrapMode"].Value<int>();
        if (jsonObject["postWrapMode"] != null)
            curve.postWrapMode = (WrapMode)jsonObject["postWrapMode"].Value<int>();
        
        return curve;
    }
}

// public class SkillPhaseConverter : JsonConverter<SkillPhase>
// {
//
//     public override void WriteJson(JsonWriter writer, SkillPhase value, JsonSerializer serializer)
//     {
//         JObject jo = JObject.FromObject(value, serializer);
//         jo.AddFirst(new JProperty("Type", value.GetType().Name));
//         jo.WriteTo(writer);
//     }
//
//     public override SkillPhase ReadJson(JsonReader reader, Type objectType, SkillPhase? existingValue, bool hasExistingValue, JsonSerializer serializer)
//     {
//         JObject jo = JObject.Load(reader);
//
//         // 读取 Type 字段来确定具体类型
//         if (!jo.TryGetValue("Type", StringComparison.OrdinalIgnoreCase, out var typeToken))
//         {
//             throw new JsonSerializationException("缺少 'Type' 字段，无法确定 SkillEvent 的具体类型");
//         }
//
//         string type = typeToken.Value<string>();
//
//         // 根据 Type 字段创建对应的具体类型实例
//         SkillPhase skillPhase = type switch
//         {
//             nameof(OpenComboWindowPhase) => new OpenComboWindowPhase(),
//             nameof(MoveStepPhase) => new MoveStepPhase(),
//             _ => throw new JsonSerializationException($"未知的 SkillEvent 类型: {type}")
//         };
//
//         // 使用默认的序列化器填充对象属性
//         serializer.Populate(jo.CreateReader(), skillPhase);
//
//         return skillPhase;
//     }
//
//
//    
// }
//
// public class SkillEventConverter : JsonConverter<SkillEvent>
// {
//     
//
//     public override void WriteJson(JsonWriter writer, SkillEvent value, JsonSerializer serializer)
//     {
//         JObject jo = JObject.FromObject(value, serializer);
//         jo.AddFirst(new JProperty("Type", value.GetType().Name));
//         jo.WriteTo(writer);
//     }
//     public override SkillEvent ReadJson(JsonReader reader, Type objectType, SkillEvent? existingValue, bool hasExistingValue, JsonSerializer serializer)
//     {
//         JObject jo = JObject.Load(reader);
//
//         // 读取 Type 字段来确定具体类型
//         if (!jo.TryGetValue("Type", StringComparison.OrdinalIgnoreCase, out var typeToken))
//         {
//             throw new JsonSerializationException("缺少 'Type' 字段，无法确定 SkillEvent 的具体类型");
//         }
//
//         string type = typeToken.Value<string>();
//
//         // 根据 Type 字段创建对应的具体类型实例
//         SkillEvent skillEvent = type switch
//         {
//             nameof(AnimationEvent) => new AnimationEvent(),
//             nameof(EffectEvent) => new EffectEvent(),
//             _ => throw new JsonSerializationException($"未知的 SkillEvent 类型: {type}")
//         };
//
//         // 使用默认的序列化器填充对象属性
//         serializer.Populate(jo.CreateReader(), skillEvent);
//
//         return skillEvent;
//     }
// }

public class Vector2Converter : JsonConverter<Vector2>
{
    public override void WriteJson(JsonWriter writer, Vector2 value, JsonSerializer serializer)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("x");
        writer.WriteValue(value.x);
        writer.WritePropertyName("y");
        writer.WriteValue(value.y);
        writer.WriteEndObject();
    }

    public override Vector2 ReadJson(JsonReader reader, Type objectType, Vector2 existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var obj = JObject.Load(reader);
        return new Vector2(
            obj.Value<float>("x"),
            obj.Value<float>("y")
        );
    }
}

/// <summary>
/// Unity Vector3 的Json转换器
/// </summary>
public class Vector3Converter : JsonConverter<Vector3>
{
    public override void WriteJson(JsonWriter writer, Vector3 value, JsonSerializer serializer)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("x");
        writer.WriteValue(value.x);
        writer.WritePropertyName("y");
        writer.WriteValue(value.y);
        writer.WritePropertyName("z");
        writer.WriteValue(value.z);
        writer.WriteEndObject();
    }

    public override Vector3 ReadJson(JsonReader reader, Type objectType, Vector3 existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var obj = JObject.Load(reader);
        return new Vector3(
            obj.Value<float>("x"),
            obj.Value<float>("y"),
            obj.Value<float>("z")
        );
    }
}

/// <summary>
/// Unity Quaternion 的Json转换器
/// </summary>
public class QuaternionConverter : JsonConverter<Quaternion>
{
    public override void WriteJson(JsonWriter writer, Quaternion value, JsonSerializer serializer)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("x");
        writer.WriteValue(value.x);
        writer.WritePropertyName("y");
        writer.WriteValue(value.y);
        writer.WritePropertyName("z");
        writer.WriteValue(value.z);
        writer.WritePropertyName("w");
        writer.WriteValue(value.w);
        writer.WriteEndObject();
    }

    public override Quaternion ReadJson(JsonReader reader, Type objectType, Quaternion existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var obj = JObject.Load(reader);
        return new Quaternion(
            obj.Value<float>("x"),
            obj.Value<float>("y"),
            obj.Value<float>("z"),
            obj.Value<float>("w")
        );
    }
}
