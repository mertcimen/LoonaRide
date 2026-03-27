using System.Collections;
using System.Collections.Generic;
using Base_Systems.Scripts.LevelSystem;
using Fiber.LevelSystem;
using UnityEngine;

namespace _Main.Scripts.Data
{
    [CreateAssetMenu(fileName = "Levels",menuName = "Data/Levels")]
    public class LevelsSO : ScriptableObject
    {
        [SerializeField] private  List<LevelData> levelDatas = new();
        public List<LevelData> Levels
        {
            get => levelDatas;
            set => levelDatas = value;
        }
        public void AddLevel(Level level)
        {
            LevelData levelData = new LevelData();
            levelData.Level = level;
            levelData.IsLoopingLevel = true;
            levelDatas.Add(levelData);
        }
    }
    [System.Serializable]
    public class LevelData
    {
        [SerializeField] private Level level;
        [SerializeField] private bool isLoopingLevel = true;

        public Level Level
        {
            get => level;
            set => level = value;
        }

        public bool IsLoopingLevel
        {
            get => isLoopingLevel;
            set => isLoopingLevel = value;
        }
    }
}