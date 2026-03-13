using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "Scriptable Objects/Data")]
public class Data : ScriptableObject
{
    public List<DataItem> Items;
}

[Serializable]
public record DataItem
{
    public string Name;
    public int Value;
    [SerializeReference] public List<DataItem> Children;
}
