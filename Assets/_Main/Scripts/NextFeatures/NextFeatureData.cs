using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class NextFeatureData
{
    [SerializeField] private int unlockLevel;
    [SerializeField] private Sprite backgroundSprite;
    [SerializeField] private Sprite finalSprite;
    [SerializeField] private string finalText;
    
    public int UnlockLevel => unlockLevel;
    public Sprite BackgroundSprite => backgroundSprite;
    public Sprite FinalSprite => finalSprite;
    public string FinalText => finalText;
}
[System.Serializable]
public enum NextFeatureObstacleType
{
    Obstacle1,
    Obstacle2,
}
