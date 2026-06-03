using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AutoInjectionEditor : Editor
{
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

    static bool IsTargetBehaviour(MonoBehaviour target)
    {
        return Attribute.IsDefined(target.GetType(), typeof(AutoInjectionTarget), true);
    }

    static bool ClearAllFields(MonoBehaviour target)
    {
        if (!IsTargetBehaviour(target))
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
        if (!IsTargetBehaviour(target))
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
        // 빌드 씬 경로 목록을 먼저 수집
        var buildScenePaths = new HashSet<string>(
            EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
        );

        var sceneSetup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            var openScenePaths = new List<string>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                    continue;
                // 빌드 씬에 포함된 경우만 처리
                if (!buildScenePaths.Contains(scene.path))
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
    const string MENUPATH_CONTEXT_INJECT = "CONTEXT/MonoBehaviour/** Inject Fields **";
    const string MENUPATH_CONTEXT_CLEAR = "CONTEXT/MonoBehaviour/** Clear Fields **";
    const string MENUPATH_TOPBAR = "Tools/** Auto Inject Fields **";
    const string MENUPATH_HIERARCHY = "GameObject/** Inject Fields From Hierarchy **";
    const string MENUPATH_PREFAB = "Assets/** Inject Fields From Prefabs **";


    [MenuItem(MENUPATH_CONTEXT_INJECT, false, -902)]
    static void ContextMenu_Inject(MenuCommand command)
    {
        InjectAllFields(command.context as MonoBehaviour);
    }
    [MenuItem(MENUPATH_CONTEXT_INJECT, true)]
    static bool ContextMenu_Inject_Validate(MenuCommand command)
    {
        return IsTargetBehaviour(command.context as MonoBehaviour);
    }

    [MenuItem(MENUPATH_CONTEXT_CLEAR, false, -901)]
    static void ContextMenu_Clear(MenuCommand command)
    {
        ClearAllFields(command.context as MonoBehaviour);
    }
    [MenuItem(MENUPATH_CONTEXT_CLEAR, true)]
    static bool ContextMenu_Clear_Validate(MenuCommand command)
    {
        return IsTargetBehaviour(command.context as MonoBehaviour);
    }


    [MenuItem(MENUPATH_TOPBAR)]
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


    [MenuItem(MENUPATH_HIERARCHY, false, -900)]
    static void HierarchyMenu()
    {
        foreach (var obj in Selection.gameObjects)
        {
            InjectFromSceneObject(obj);
        }
    }

    [MenuItem(MENUPATH_HIERARCHY, true)]
    static bool HierarchyMenu_Validate()
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


    [MenuItem(MENUPATH_PREFAB, false, -900)]
    static void Prefabmenu()
    {
        foreach (var obj in Selection.gameObjects)
        {
            InjectFromPrefabAsset(obj);
        }
    }

    [MenuItem(MENUPATH_PREFAB, true)]
    static bool PrefabMenu_Validate()
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
    #endregion
}