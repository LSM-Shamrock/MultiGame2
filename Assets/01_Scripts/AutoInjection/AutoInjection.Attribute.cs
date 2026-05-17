using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

[AttributeUsage(AttributeTargets.Class)]
public class AutoInjectionTarget : Attribute
{

}
[AttributeUsage(AttributeTargets.Field)]
public abstract class AutoInjectionField : PropertyAttribute
{
    public abstract bool Inject(MonoBehaviour target, FieldInfo field);
}


[AttributeUsage(AttributeTargets.Field)]
public class SceneComponentField : AutoInjectionField
{
    public override bool Inject(MonoBehaviour target, FieldInfo field)
    {
        #if UNITY_EDITOR
        if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(target))
            return false;
        #endif

        var component = UnityEngine.Object.FindAnyObjectByType(field.FieldType, FindObjectsInactive.Include);
        if (component == null)
        {
            Debug.LogWarning($"씬에서 {field.FieldType.Name} 컴포넌트를 찾지 못함", target);
            return false;
        }

        if (Equals(field.GetValue(target), component))
            return false;

        field.SetValue(target, component);
        return true;
    }
}



[AttributeUsage(AttributeTargets.Field)]
public class ComponentField : AutoInjectionField
{
    public override bool Inject(MonoBehaviour target, FieldInfo field)
    {
        var component = target.GetComponent(field.FieldType);
        if (component == null)
        {
            Debug.LogWarning($"{target.gameObject.name}에서 {field.FieldType.Name} 컴포넌트를 찾지 못함", target);
            return false;
        }

        if (Equals(field.GetValue(target), component))
            return false;

        field.SetValue(target, component);
        return true;
    }
}

[AttributeUsage(AttributeTargets.Field)]
public class ParentField : AutoInjectionField
{
    public override bool Inject(MonoBehaviour target, FieldInfo field)
    {
        var find = target.GetComponentInParent(field.FieldType);
        if (find == null)
        {
            Debug.LogWarning($"{target.gameObject.name}의 부모에서 {field.FieldType.Name} 컴포넌트를 찾지 못함", target);
            return false;
        }

        field.SetValue(target, find);
        return true;
    }
}

[AttributeUsage(AttributeTargets.Field)]
public class ChildField : AutoInjectionField
{
    private readonly string _childName;

    public ChildField() { }
    public ChildField(string childName = null) 
    { 
        _childName = childName; 
    }

    public override bool Inject(MonoBehaviour target, FieldInfo field)
    {
        var nameToFind = _childName;
        if (nameToFind == null)
        {
            nameToFind = field.Name;
            nameToFind = nameToFind.Replace("<", "").Replace(">k__BackingField", "");
            nameToFind = nameToFind.TrimStart('_');
            nameToFind = char.ToUpperInvariant(nameToFind[0]) + nameToFind[1..];
        }

        var find = AutoInjectionUtil.FindChildByNameRecursive(target.transform, nameToFind);
        if (find == null)
        {
            Debug.LogWarning($"{target.gameObject.name}에서 {nameToFind} 이름의 자식을 찾지 못함", target);
            return false;
        }

        var component = AutoInjectionUtil.GetComponentOrGameObject(find, field.FieldType);
        if (component == null)
        {
            Debug.LogWarning($"{target.gameObject.name}의 자식 {find.gameObject.name}에서 {field.FieldType.Name} 컴포넌트를 찾지 못함", target);
            return false;
        }

        field.SetValue(target, component);
        return true;
    }
}

[AttributeUsage(AttributeTargets.Field)]
public class ChildrenGroupField : AutoInjectionField
{
    private readonly string _childrenGroupName;

    public ChildrenGroupField() { }
    public ChildrenGroupField(string childrenGroupName = null) 
    { 
        _childrenGroupName = childrenGroupName; 
    }

    public override bool Inject(MonoBehaviour target, FieldInfo field)
    {
        if (field.FieldType.IsArray == false)
        {
            Debug.LogWarning($"{GetType().Name} 필드 타입이 배열이 아님");
            return false;
        }
        var elementType = field.FieldType.GetElementType();

        var nameToFind = _childrenGroupName;
        if (nameToFind == null)
        {
            nameToFind = field.Name;
            nameToFind = nameToFind.Replace("<", "").Replace(">k__BackingField", "");
            nameToFind = nameToFind.TrimStart('_');
            nameToFind = char.ToUpperInvariant(nameToFind[0]) + nameToFind[1..];
        }

        var find = AutoInjectionUtil.FindChildByNameRecursive(target.transform, nameToFind);
        if (find == null)
        {
            Debug.LogWarning($"{target.gameObject.name}에서 {nameToFind} 이름의 자식을 찾지 못함", target);
            return false;
        }

        var components = find.transform.Cast<Transform>().Select(child => child.GetComponent(elementType)).ToArray();
        var arr = Array.CreateInstance(elementType, components.Length);
        Array.Copy(components, arr, components.Length);

        field.SetValue(target, arr);
        return true;
    }
}


