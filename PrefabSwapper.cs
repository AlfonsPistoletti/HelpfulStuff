using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class PrefabSwapper : EditorWindow
{
    [MenuItem("Tools/Prefab Swapper")]

    public static void OpenPrefabSwapper() => GetWindow<PrefabSwapper>();

    SerializedObject so;
    public GameObject swapPrefab;
    SerializedProperty propSwapPrefab;
    Texture icon;

    public bool useSameScale = false;
    SerializedProperty propUseSameScale;

    public bool resetRotation = false;
    SerializedProperty propResetRot;

    public Vector3 rotateBy = Vector3.zero;
    SerializedProperty propRotateBy;
    [SerializeField] int radioButtonInt = 0;

    public string rootObjectName = "";
    SerializedProperty propRootObjName;

    public Transform rootObjectParent = null;
    SerializedProperty propRootParent;

    List<GameObject> foundGOs = new List<GameObject>();
    string goNames = "";

    private void OnEnable()
    {
        so = new SerializedObject(this);
        propSwapPrefab = so.FindProperty("swapPrefab");
        propUseSameScale = so.FindProperty("useSameScale");
        propResetRot = so.FindProperty("resetRotation");
        propRootObjName = so.FindProperty("rootObjectName");
        propRootParent = so.FindProperty("rootObjectParent");
        propRotateBy = so.FindProperty("rotateBy");

        SceneView.duringSceneGui += DuringSceneGUI;
        Selection.selectionChanged += UpdateGOList;

        foundGOs.Clear();
        goNames = "";
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= DuringSceneGUI;
        Selection.selectionChanged -= UpdateGOList;
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        GUILayout.Label("Prefab Swapper", EditorStyles.boldLabel);
        GUILayout.Space(20);

        so.Update();
        using (new GUILayout.HorizontalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.PropertyField(propSwapPrefab);

            if(swapPrefab != null)
            {
                icon = AssetPreview.GetAssetPreview(swapPrefab);   
            }
            else
            {
                icon = null;
            }
            GUILayout.Box(icon, GUILayout.Width(96), GUILayout.Height(96));
        }

        if(EditorGUILayout.PropertyField(propUseSameScale))
        {
            useSameScale = !useSameScale;
        }

        if(EditorGUILayout.PropertyField(propResetRot))
        {
            resetRotation = !resetRotation;
        }



        using(new GUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.PropertyField(propRootParent);
            EditorGUILayout.PropertyField(propRootObjName);
            if(GUILayout.Button("Find Objects that contain Name"))
            {
                GetAllGameObjectsByName(rootObjectName);
            }
        }

        GUILayout.Space(10);

        if(foundGOs.Count > 0)
        {
            using(new GUILayout.HorizontalScope())
            {
                using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    GUILayout.Label(goNames, EditorStyles.miniLabel);
                }

                using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    if (GUILayout.Button("Change found Prefabs"))
                    {
                        SwapPrefabsFromList(foundGOs);
                        Debug.Log("Change Found Prefabs");
                    }
                    if(GUILayout.Button("Clear"))
                    {
                        ResetGOList();
                    }
                }
            }
        }

        GUILayout.Space(20);

        // Rotation Area
        using (new GUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.PropertyField(propRotateBy);

            GUILayout.Space(10);

            string[] radioButtonText = { "Absolute", "Relative" };

            using(new GUILayout.HorizontalScope())
            {
                radioButtonInt = GUILayout.SelectionGrid(radioButtonInt, radioButtonText, 2, EditorStyles.radioButton);

                if(GUILayout.Button("Set Rotation"))
                {
                    SetRotation(foundGOs, rotateBy, radioButtonInt);
                }
            }
        }

        if(so.ApplyModifiedProperties())
        {
            Repaint();
        }

        // If you clicked left Mouse Button in the Editor Window
        if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            GUI.FocusControl(null);
            Repaint(); // Repaint UI for no delay
        }
    }

    const string undoString = "Undo Swap Prefabs";

    void SwapPrefabsFromList(List<GameObject> _goList)
    {
        Selection.activeGameObject = null;

        List<GameObject> newSelectionGOs = new List<GameObject>();

        for (int i = 0; i < _goList.Count; i++)
        {
            Undo.RecordObject(_goList[i], undoString);
            Vector3 goPos = _goList[i].transform.position;
            Quaternion goRot = _goList[i].transform.rotation;
            Vector3 goScale = _goList[i].transform.localScale;

            DestroyImmediate(_goList[i]);

            GameObject newGO = (GameObject)PrefabUtility.InstantiatePrefab(swapPrefab);
            newGO.transform.position = goPos;

            if (resetRotation)
            {
                newGO.transform.rotation = Quaternion.identity;
            }
            else
            {
                newGO.transform.rotation = goRot;
            }

            if (useSameScale)
            {
                newGO.transform.localScale = goScale;
            }

            newGO.transform.SetParent(rootObjectParent);

            newGO.name = newGO.name + " (" + i.ToString() + ")";

            newSelectionGOs.Add(newGO);
        }

        foundGOs.Clear();
        goNames = "";
        Repaint();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        // Select new Objects
        Selection.objects = newSelectionGOs.ToArray();
    }

    void ResetGOList()
    {
        so.Update();
        foundGOs.Clear();
        goNames = "";
        //rootObjectName = "";
        so.ApplyModifiedProperties();
        Repaint();
    }

    void SetRotation(List<GameObject> _goList, Vector3 _targetRot, int _methodInt)
    {
        // 0 = Absolute, 1 = Relative

        string undoString = "Rotate Objects";

        if(_methodInt == 0)
        {
            foreach (GameObject go in _goList)
            {
                Undo.RecordObject(go.transform, undoString);
                go.transform.rotation = Quaternion.Euler(_targetRot);
                //go.transform.localEulerAngles = _targetRot;
            }
        }
        else if(_methodInt == 1)
        {
            foreach (GameObject go in _goList)
            {
                Undo.RecordObject(go.transform, undoString);
                go.transform.rotation *= Quaternion.Euler(_targetRot);
                //go.transform.localEulerAngles += _targetRot;
            }
        }
        else
        {
            Debug.LogWarning("Prefab Swapper: Something Went Wrong");
        }
        
    }

    void DuringSceneGUI(SceneView sceneView)
    {
        if (foundGOs.Count == 0) return;

        foreach(GameObject go in foundGOs)
        {
            Handles.color = Color.red;
            Handles.DrawAAPolyLine(10f, go.transform.position, go.transform.position + Vector3.up * 200f);
        }
    }

    void GetAllGameObjectsByName(string _searchName)
    {
        if (_searchName == "" || rootObjectParent == null) return;

        foundGOs.Clear();
        goNames = "";
        foreach(Transform child in rootObjectParent)
        {
            if (child.name.Contains(_searchName))
            {
                foundGOs.Add(child.gameObject);

                goNames += child.name + "\n";
            }

        }
    }

    void UpdateGOList()
    {
        foundGOs.Clear();
        goNames = "";

        foreach(GameObject go in Selection.gameObjects)
        {
            foundGOs.Add(go);

            goNames += go.transform.name + "\n";
        }
        Repaint();
    }
}
