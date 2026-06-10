using System;
using System.Collections.Generic;
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

#region Component
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
            nameToFind = AutoInjectionUtil.GetDefaultFindNameByFieldName(field.Name);

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
public class ChildrenArrayField : AutoInjectionField
{
    private readonly string _childrenRootName;

    public ChildrenArrayField() { }
    public ChildrenArrayField(string chidrenRootName = null)
    {
        _childrenRootName = chidrenRootName;
    }

    public override bool Inject(MonoBehaviour target, FieldInfo field)
    {
        if (field.FieldType.IsArray == false)
        {
            Debug.LogWarning($"{GetType().Name} 필드 타입이 배열이 아님");
            return false;
        }
        var elementType = field.FieldType.GetElementType();

        var nameToFind = _childrenRootName;
        if (nameToFind == null)
            nameToFind = AutoInjectionUtil.GetDefaultFindNameByFieldName(field.Name);

        var find = AutoInjectionUtil.FindChildByNameRecursive(target.transform, nameToFind);
        if (find == null)
        {
            Debug.LogWarning($"{target.gameObject.name}에서 {nameToFind} 이름의 자식을 찾지 못함", target);
            return false;
        }

        var components = new List<Component>();
        foreach (Transform child in find.transform)
        {
            var childComponent = child.GetComponent(elementType);
            if (childComponent != null) components.Add(childComponent); 
        }

        var arr = Array.CreateInstance(elementType, components.Count);
        Array.Copy(components.ToArray(), arr, components.Count);

        field.SetValue(target, arr);
        return true;
    }
}
#endregion

[AttributeUsage(AttributeTargets.Field)]
public class AssetField : AutoInjectionField
{
    private readonly string _assetPass;

    public AssetField(string assetPass = null)
    {
        _assetPass = assetPass;
    }

    public override bool Inject(MonoBehaviour target, FieldInfo field)
    {
        string assetPath = _assetPass;
        if (assetPath == null)
            assetPath = AutoInjectionUtil.GetDefaultFindNameByFieldName(field.Name);

#if UNITY_EDITOR

        // 경로에 확장자가 없으면 타입 기반으로 에셋 검색
        UnityEngine.Object asset;
        if (System.IO.Path.HasExtension(assetPath))
        {
            asset = UnityEditor.AssetDatabase.LoadAssetAtPath(assetPath, field.FieldType);
        }
        else
        {
            var guids = UnityEditor.AssetDatabase.FindAssets($"{assetPath} t:{field.FieldType.Name}");
            if (guids.Length == 0)
            {
                Debug.LogWarning($"AssetDatabase에서 '{assetPath}' 이름의 {field.FieldType.Name} 에셋을 찾지 못함", target);
                return false;
            }
            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);

            asset = UnityEditor.AssetDatabase.LoadAssetAtPath(path, field.FieldType);
        }

        if (asset == null)
        {
            Debug.LogWarning($"AssetDatabase에서 '{assetPath}' 경로의 {field.FieldType.Name} 에셋을 찾지 못함", target);
            return false;
        }

        if (Equals(field.GetValue(target), asset))
            return false;

        field.SetValue(target, asset);
        return true;

#else
        return false;
#endif
    }
}