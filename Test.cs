using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

public class Test : MonoBehaviour
{
    public AnimationCurve animationCurve;
    public AnimationCurve animationCurve2;
    private void Start()
    {
        // var settings = new JsonSerializerSettings
        // {
        //     Converters = new List<JsonConverter> { new AnimationCurveConverter() },
        //     Formatting = Formatting.Indented
        // };
        // string json = JsonConvert.SerializeObject(GameContext.Instance.Attack1BakedMotionData.motionCurve, settings);
        // Debug.Log(json);
        // animationCurve = JsonConvert.DeserializeObject<AnimationCurve>(json, settings);
        // animationCurve2 = GameContext.Instance.Attack1BakedMotionData.motionCurve;
    }

}