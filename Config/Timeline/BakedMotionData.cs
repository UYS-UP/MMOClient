using UnityEngine;

[CreateAssetMenu(
    menuName = "Skill/BakedMotion",
    fileName = "BakedMotionData")]
public class BakedMotionData : ScriptableObject
{
    public string animationName;

    public float clipLength;

    public float moveStartTime;  
    public float moveEndTime;     
    public float totalDistance;  

    public AnimationCurve motionCurve; 
}