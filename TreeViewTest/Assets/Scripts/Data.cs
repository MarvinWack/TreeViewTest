using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "Scriptable Objects/Data")]
public class Data : ScriptableObject
{
    public List<DataItem> activeTasks;
    public List<DataItem> archivedTasks;
}

[Serializable]
public record DataItem
{
    public string Name;
    public string Description;
    public string ExpectedChallenges;
    public int EstimatedMinutes;
    public string EncounteredChallenges;
    public int ActualMinutes;
    [SerializeReference] public List<DataItem> Children;
}
