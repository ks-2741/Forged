using UnityEngine;

/// <summary>One required item + amount within a noble's commission.</summary>
[System.Serializable]
public class NobleOrderLine
{
    public ItemData item;
    public int amount = 1;
}