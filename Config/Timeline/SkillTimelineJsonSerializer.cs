using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;


public static class SkillTimelineJsonSerializer
{
    public static Dictionary<int, SkillTimelineConfig> SkillConfigs {  get; private set; }
    
    private static JsonSerializerSettings CreateSerializerSettings()
    {
        var settings = new JsonSerializerSettings();

        settings.Formatting = Formatting.Indented;
        settings.TypeNameHandling = TypeNameHandling.Auto;
        settings.NullValueHandling = NullValueHandling.Ignore;
        settings.Converters.Add(new Vector3Converter());
        settings.Converters.Add(new Vector2Converter());
        settings.Converters.Add(new AnimationCurveConverter());
        settings.Converters.Add(new QuaternionConverter());

        settings.SerializationBinder = new AliasBinder(new[]
        {
            // SkillEvent 子类
            typeof(AnimationEvent),
            typeof(EffectEvent),

            // SkillPhase 子类
            typeof(OpenComboWindowPhase),
            typeof(MoveStepPhase),
        });
        return settings;
    }

    public static void Deserializer(string filePath)
    {
        if(!File.Exists(filePath))
        {
            throw new FileNotFoundException("反序列化失败:" + filePath + "文件不存在");
        }

        try
        {
            string json = File.ReadAllText(filePath);
            var settings = CreateSerializerSettings();

            var asset = JsonConvert.DeserializeObject<List<SkillTimelineConfig>>(json, settings);
            SkillConfigs = new Dictionary<int, SkillTimelineConfig>();
            foreach (var skill in asset)
            {
                SkillConfigs[skill.Id] = skill;
            }
        }
        catch (Exception ex)
        {
            throw new JsonSerializationException("反序列化失败:" + ex);
        }
    }


    public static void Serializer(List<SkillTimelineConfig> skillTimelineConfigs, string filePath)
    {
        try
        {
            var settings = CreateSerializerSettings();
            string json = JsonConvert.SerializeObject(skillTimelineConfigs, settings);

            // 确保目录存在
            string directory = Path.GetDirectoryName(filePath);
            if (Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllText(filePath, json);
        }
        catch (Exception ex)
        {
            throw new JsonSerializationException("序列化失败:" + ex);
        }
    }
    
}