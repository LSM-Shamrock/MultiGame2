using System;
using UnityEngine;

public class AutoInjectionUtil
{
    private AutoInjectionUtil _instance;
    private AutoInjectionUtil Instance => _instance ?? (_instance = new AutoInjectionUtil());

    public static Transform FindChildByNameRecursive(Transform target, string name)
    {
        foreach (Transform child in target)
        {
            if (child.name == name)
                return child;

            var descendant = FindChildByNameRecursive(child, name);
            if (descendant != null)
                return descendant;
        }
        return null;
    }

    public static object GetComponentOrGameObject(Transform target, Type t)
    {
        if (t == typeof(Transform))
            return target;

        if (t == typeof(GameObject))
            return target.gameObject;

        return target.GetComponent(t);
    }

    public static string GetDefaultFindNameByFieldName(string fieldName)
    {
        string findName = fieldName;

        findName = findName.Replace("<", "").Replace(">k__BackingField", "");
        findName = findName.TrimStart('_');
        findName = char.ToUpperInvariant(findName[0]) + findName[1..];

        return findName;
    }
}
