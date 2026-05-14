using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[AttributeUsage(AttributeTargets.Class, Inherited = true)]
public class AutoInjectionTarget : Attribute
{

}

[AttributeUsage(AttributeTargets.Field)]
public abstract class AutoInjectionField : PropertyAttribute
{
    public abstract bool Inject(MonoBehaviour target, FieldInfo field);
}


[CustomEditor(typeof(MonoBehaviour), true)]
[CanEditMultipleObjects]
public class AutoInjectionEditor : Editor
{
    #region Util Funcion
    static Transform GetDescendantRecursive(Transform target, string name)
    {
        foreach (Transform child in target)
        {
            if (child.name == name)
                return child;

            var descendant = GetDescendantRecursive(child, name);
            if (descendant != null)
                return descendant;
        }
        return null;
    }

    static IEnumerable<FieldInfo> GetPublicOrSerializeFields(MonoBehaviour target)
    {
        var type = target.GetType();

        while (type != null && type != typeof(MonoBehaviour))
        {
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            var fields = type.GetFields(flags);

            foreach (var field in fields)
            {
                if (field.IsPublic || Attribute.IsDefined(field, typeof(SerializeField)))
                    yield return field;
            }

            type = type.BaseType;
        }
    }
    #endregion


    static bool ClearAllFields(MonoBehaviour target)
    {
        if (!target.GetType().IsDefined(typeof(AutoInjectionTarget), true))
            return false;

        bool isChanged = false;

        foreach (var field in GetPublicOrSerializeFields(target))
        {
            if (Attribute.IsDefined(field, typeof(AutoInjectionField)))
            {
                if (field.FieldType.IsValueType)
                    continue;

                if (field.GetValue(target) == null)
                    continue;

                Undo.RecordObject(target, "Clear Auto Inject Fields");

                field.SetValue(target, null);

                PrefabUtility.RecordPrefabInstancePropertyModifications(target);

                isChanged = true;
            }
        }

        if (isChanged)
            EditorUtility.SetDirty(target);

        return isChanged;
    }

    static bool InjectAllFields(MonoBehaviour target)
    {
        if (!target.GetType().IsDefined(typeof(AutoInjectionTarget), true))
            return false;

        bool isChanged = false;

        Undo.RecordObject(target, "Inject Auto Fields");

        foreach (var field in GetPublicOrSerializeFields(target))
        {
            var attr = field.GetCustomAttribute<AutoInjectionField>(true);
            if (attr == null)
                continue;

            if (attr.Inject(target, field))
                isChanged = true;
        }

        if (isChanged)
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
            EditorUtility.SetDirty(target);
        }

        return isChanged;
    }


    static void InjectFromSceneObject(GameObject root)
    {
        var components = root.GetComponentsInChildren<MonoBehaviour>(true);

        foreach (var com in components)
        {
            if (PrefabUtility.IsPartOfNonAssetPrefabInstance(com.gameObject))
                continue;

            InjectAllFields(com);
        }
    }

    static void InjectFromPrefabAsset(GameObject prefabRoot)
    {
        var assetPath = AssetDatabase.GetAssetPath(prefabRoot);

        if (!AssetDatabase.IsOpenForEdit(assetPath))
        {
            Debug.Log($"읽기 전용 프리펩 스킵: {prefabRoot.name}", prefabRoot);
            return;
        }

        var components = prefabRoot.GetComponentsInChildren<MonoBehaviour>(true);

        if (components.Any(c => c == null))
        {
            Debug.Log($"missing script가 있는 프리펩 스킵: {prefabRoot.name}", prefabRoot);
            return;
        }

        bool isChanged = false;

        foreach (var com in components)
        {
            // nested prefab 내부는 스킵
            if (PrefabUtility.IsAnyPrefabInstanceRoot(com.gameObject) &&
                com.gameObject != prefabRoot)
            {
                Debug.Log($"프리펩 속의 프리펩 스킵: {com.gameObject.name}", com.gameObject);
                continue;
            }

            if (InjectAllFields(com))
                isChanged = true;
        }

        if (isChanged)
            PrefabUtility.SavePrefabAsset(prefabRoot);
    }


    static void InjectFromAllScenes()
    {
        var sceneSetup = EditorSceneManager.GetSceneManagerSetup();

        try
        {
            var openScenePaths = new List<string>();

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);

                if (!scene.isLoaded)
                    continue;

                openScenePaths.Add(scene.path);

                foreach (var root in scene.GetRootGameObjects())
                    InjectFromSceneObject(root);

                if (scene.isDirty)
                    EditorSceneManager.SaveScene(scene);
            }

            foreach (var buildSettingsScene in EditorBuildSettings.scenes)
            {
                if (!buildSettingsScene.enabled)
                    continue;

                if (openScenePaths.Contains(buildSettingsScene.path))
                    continue;

                var scene = EditorSceneManager.OpenScene(buildSettingsScene.path, OpenSceneMode.Single);

                foreach (var root in scene.GetRootGameObjects())
                    InjectFromSceneObject(root);

                if (scene.isDirty)
                    EditorSceneManager.SaveScene(scene);
            }
        }
        finally
        {
            EditorSceneManager.RestoreSceneManagerSetup(sceneSetup);
        }
    }

    static void InjectFromAllPrefabs()
    {
        var guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab == null)
                continue;

            InjectFromPrefabAsset(prefab);
        }
    }


    #region 커스텀 에디터 코드
    const string TopMenuPath = "Tools/** Auto Inject Fields **";
    const string HierarchyMenuPath = "GameObject/** Inject Fields From Hierarchy **";
    const string PrefabMenuPath = "Assets/** Inject Fields From Prefabs **";

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var targetObject = (MonoBehaviour)target;

        if (!targetObject.GetType().IsDefined(typeof(AutoInjectionTarget), true))
            return;

        if (GUILayout.Button("Find Components"))
            InjectAllFields(targetObject);

        if (GUILayout.Button("Clear Components"))
            ClearAllFields(targetObject);
    }

    [MenuItem(TopMenuPath)]
    static void TopMenu()
    {
        try
        {
            InjectFromAllScenes();
        }
        finally
        {
            InjectFromAllPrefabs();
        }
    }

    [MenuItem(HierarchyMenuPath, true)]
    static bool ValidateHierarchyMenu()
    {
        var objects = Selection.objects;
        var gameObjects = Selection.gameObjects;

        if (gameObjects == null || gameObjects.Length == 0 || gameObjects.Length != objects.Length)
            return false;

        foreach (var obj in gameObjects)
        {
            if (PrefabUtility.IsPartOfPrefabAsset(obj))
                return false;
        }

        return true;
    }

    [MenuItem(HierarchyMenuPath, false, -900)]
    static void HierarchyMenu()
    {
        foreach (var obj in Selection.gameObjects)
        {
            InjectFromSceneObject(obj);
        }
    }

    [MenuItem(PrefabMenuPath, true)]
    static bool ValidatePrefabMenu()
    {
        var objects = Selection.objects;
        var gameObjects = Selection.gameObjects;

        if (gameObjects == null || gameObjects.Length == 0 || gameObjects.Length != objects.Length)
            return false;

        foreach (var obj in gameObjects)
        {
            if (!PrefabUtility.IsPartOfPrefabAsset(obj))
                return false;
        }

        return true;
    }

    [MenuItem(PrefabMenuPath, false, -900)]
    static void Prefabmenu()
    {
        foreach (var obj in Selection.gameObjects)
        {
            InjectFromPrefabAsset(obj);
        }
    }
    #endregion
}