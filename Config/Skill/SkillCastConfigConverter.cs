using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Serialization;

public class SkillCastConfigConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return typeof(SkillCastConfig).IsAssignableFrom(objectType);
    }

    public override object ReadJson(JsonReader reader, Type type, object existingValue, JsonSerializer serializer)
    {
        var jo = JObject.Load(reader);

        // 根据 $type 创建实际对象（自动处理）
        var typeToken = jo["$type"];
        if (typeToken != null)
        {
            var targetType = Type.GetType(typeToken.ToString());
            return jo.ToObject(targetType, serializer);
        }

        throw new Exception("SkillCastConfig 缺少 Type 字段，无法多态反序列化");
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        JObject jo = JObject.FromObject(value, serializer);
        jo.WriteTo(writer);
    }
}


public class SkillCastBinder : ISerializationBinder
{
    private readonly Dictionary<string, Type> _typeMap = new()
    {
        ["NoneCastConfig"] = typeof(NoneCastConfig),
        ["MeleeSectorCastConfig"] = typeof(MeleeSectorCastConfig),
        ["GroundCircleCastConfig"] = typeof(GroundCircleCastConfig),
        ["DirectionLineCastConfig"] = typeof(DirectionLineCastConfig),
        ["UnitTargetCastConfig"] = typeof(UnitTargetCastConfig)
    };

    public void BindToName(Type serializedType, out string assemblyName, out string typeName)
    {
        assemblyName = null;
        typeName = serializedType.Name;
    }
    
    public Type BindToType(string assemblyName, string typeName)
    {
        if (_typeMap.TryGetValue(typeName, out Type type))
            return type;
        
        throw new JsonSerializationException($"无法解析类型: {typeName}");
    }
}