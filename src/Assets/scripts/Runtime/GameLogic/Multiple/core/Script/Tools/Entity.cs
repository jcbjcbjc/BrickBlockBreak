using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Entity : MonoBehaviour
{
    [SerializeField]  List<string> tags;

    public bool CompareTags(string tag)     
    {
        return tags.Contains(tag);
    }
    public bool CompareTagsUnion(string[] tag) {
        bool flag = false;
        foreach (string s in tag) flag |= CompareTags(s);
        return flag;
    }
    public bool CompareTagsIntersection(string[] tag)
    {
        bool flag = false;
        foreach (string s in tag) flag &= CompareTags(s);
        return flag;
    }
    public void DeleteTag(string tag)
    {
        tags.Remove(tag);
    }
    public void AddTag(string tag)
    {
        tags.Add(tag);
    }
}
