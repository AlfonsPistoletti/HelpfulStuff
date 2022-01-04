using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.Linq;

public class PrefabBrush : EditorWindow
{
    [MenuItem("Tools/Prefab Brush")]
    public static void OpenPrefabBrush() => GetWindow<PrefabBrush>();

    public float brushRadius = 2f;
    public int spawnCount = 4;
    public Transform parentTransform = null;

    // Other Settings
    public bool randomizeScale;
    public float randomScaleMin = 1f;
    public float randomScaleMax = 2f;
    public bool onlyDrawOnHorizontal;

    public LayerMask paintOnlyLayers;

    SerializedObject so;
    SerializedProperty propRadius;
    SerializedProperty propSpawnCount;
    SerializedProperty propSpawnPrefab;
    SerializedProperty propParentTransform;

    SerializedProperty propRandomizeScale;
    SerializedProperty propRandomScaleMin;
    SerializedProperty propRandomScaleMax;
    SerializedProperty propPaintOnlyLayers;
    SerializedProperty propOnlyUpwards;

Vector2[] randomPoints;
    GameObject[] prefabs;
    List<GameObject> spawnPrefabs = new List<GameObject>();
    [SerializeField] bool[] prefabSelectionStates;

    #region SUBSCRIPTION
    private void OnEnable()
    {
        so = new SerializedObject(this);
        propRadius = so.FindProperty("brushRadius");
        propSpawnCount = so.FindProperty("spawnCount");
        propSpawnPrefab = so.FindProperty("spawnPrefab");
        propParentTransform = so.FindProperty("parentTransform");

        propRandomizeScale = so.FindProperty("randomizeScale");
        propRandomScaleMin = so.FindProperty("randomScaleMin");
        propRandomScaleMax = so.FindProperty("randomScaleMax");
        propPaintOnlyLayers = so.FindProperty("paintOnlyLayers");
        propOnlyUpwards = so.FindProperty("onlyDrawOnHorizontal");

        SceneView.duringSceneGui += DuringSceneGUI;

        GenerateRandomPoints();

        // Load Prefabs
        string[] guids = AssetDatabase.FindAssets("t:prefab", new[] { "Assets/03_Prefabs/Placement" });
        IEnumerable<string> paths = guids.Select(AssetDatabase.GUIDToAssetPath);
        prefabs = paths.Select(AssetDatabase.LoadAssetAtPath<GameObject>).ToArray();

        // For multi selection
        if(prefabSelectionStates == null || prefabSelectionStates.Length != prefabs.Length)
        {
            prefabSelectionStates = new bool[prefabs.Length];
        }
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= DuringSceneGUI;
    }
    #endregion

    private void OnGUI()
    {
        // Draws the Editor Window
        GUILayout.Space(10);
        GUILayout.Label("Prefab Brush", EditorStyles.largeLabel);
        GUILayout.Space(10);


        using (new GUILayout.VerticalScope(EditorStyles.helpBox))
        {
            GUILayout.Label("Brush", EditorStyles.boldLabel);
            GUILayout.Space(5);
            // Show Properties
            so.Update();
            EditorGUILayout.PropertyField(propRadius);
            propRadius.floatValue = propRadius.floatValue.AtLeast(1f);
            GUILayout.Space(10);
            EditorGUILayout.PropertyField(propSpawnCount);
            propSpawnCount.intValue = propSpawnCount.intValue.AtLeast(1);
        }

        GUILayout.Space(20);

        using (new GUILayout.VerticalScope(EditorStyles.helpBox))
        {
            GUILayout.Label("Settings", EditorStyles.boldLabel);
            GUILayout.Space(5);
            //EditorGUILayout.PropertyField(propSpawnPrefab);
            //GUILayout.Space(10);

            EditorGUILayout.PropertyField(propPaintOnlyLayers);
            GUILayout.Space(5);

            using (new GUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(propParentTransform);
                if (GUILayout.Button("Reset Parent", GUILayout.Width(90)))
                {
                    parentTransform = null;
                }
            }

            GUILayout.Space(10);

            // Other Settings
            if (EditorGUILayout.PropertyField(propRandomizeScale))
            {
                randomizeScale = !randomizeScale;
            }

            if (randomizeScale == true)
            {
                GUILayout.Label("Random Scale");
                EditorGUILayout.MinMaxSlider(ref randomScaleMin, ref randomScaleMax, 1f, 3f);
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Min Value: " + randomScaleMin.ToString(), EditorStyles.miniLabel);
                    GUILayout.Label("Max Value: " + randomScaleMax.ToString(), EditorStyles.miniLabel);
                }
            }

            GUILayout.Space(10);

            if (EditorGUILayout.PropertyField(propOnlyUpwards))
            {
                onlyDrawOnHorizontal = !onlyDrawOnHorizontal;
            }
        }

        GUILayout.Space(20);

        using (new GUILayout.VerticalScope())
        {
            GUILayout.Label("Info", EditorStyles.miniLabel);
            GUILayout.Label("Place all Prefabs you want to use into 03_Prefabs > Placement\n" +
                "Press Space to paint. Use single Clicks.\n" +
                "Use Alt + Scroll Wheel to change Brush Size\n" +
                "Use Alt + Ctrl + Scroll Wheel to change Spawn Count\n\n" +
                "Copyright: Christoph Weinreich junge!", EditorStyles.helpBox);
        }

        // Check if change happens
        if (so.ApplyModifiedProperties())
        {
            GenerateRandomPoints();
            SceneView.RepaintAll();
        }

        // If you clicked left Mouse Button in the Editor Window
        if(Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            GUI.FocusControl(null);
            Repaint(); // Repaint UI for no delay
        }
    }

    void DuringSceneGUI(SceneView sceneView)
    {
        // Screen GUI
        Handles.BeginGUI();

        Rect rect = new Rect(8, 8, 52, 52);

        for (int i = 0; i < prefabs.Length; i++)
        {
            Texture icon = AssetPreview.GetAssetPreview(prefabs[i]);

            EditorGUI.BeginChangeCheck();
            prefabSelectionStates[i] = GUI.Toggle(rect, prefabSelectionStates[i], new GUIContent(icon, prefabs[i].name));
            if(EditorGUI.EndChangeCheck())
            {

                // Update Selection List
                spawnPrefabs.Clear();
                for (int j = 0; j < prefabs.Length; j++)
                {
                    if (prefabSelectionStates[j])
                    {
                        spawnPrefabs.Add(prefabs[j]);
                    }
                }
            }

            rect.y += rect.height;
        }

        Handles.EndGUI();




        Handles.zTest = CompareFunction.LessEqual;
        Transform camTransform = sceneView.camera.transform;

        // Repaint Scene on Mouse Move
        if(Event.current.type == EventType.MouseMove)
        {
            sceneView.Repaint();
        }

        bool holdingAlt = (Event.current.modifiers & EventModifiers.Alt) != 0;
        bool holdingCtrl = (Event.current.modifiers & EventModifiers.Control) != 0;

        // Change Radius with Mouse Scrollwheel
        if(Event.current.type == EventType.ScrollWheel && holdingAlt == true)
        {
            float scrollDir = Mathf.Sign(Event.current.delta.y);

            so.Update();
            if (holdingCtrl)
            {
                propSpawnCount.intValue = Mathf.RoundToInt(propSpawnCount.intValue - scrollDir);
                GenerateRandomPoints();
            }
            else
            {
                propRadius.floatValue *= 1 - scrollDir * 0.1f;
            }
   
            so.ApplyModifiedProperties();
            Repaint();

            Event.current.Use(); // Consume the event
        }

        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

        //Ray ray = new Ray(camTransform.position, camTransform.forward);
        if(Physics.Raycast(ray, out RaycastHit hit))
        {
            // Setting Up Tanget Space
            Vector3 hitNormal = hit.normal;
            Vector3 hitTangent = Vector3.Cross(hitNormal, camTransform.up).normalized;
            Vector3 hitBitangent = Vector3.Cross(hitNormal, hitTangent);

            Ray GetTangentRay(Vector2 tangentSpacePos)
            {
                Vector3 rayOrigin = hit.point + (hitTangent * tangentSpacePos.x + hitBitangent * tangentSpacePos.y) * brushRadius;
                rayOrigin += hitNormal * 2f;
                Vector3 rayDirection = -hit.normal;
                return new Ray(rayOrigin, rayDirection);
            }

            #region Tangents
            /*
            Handles.color = Color.red;
            Handles.DrawAAPolyLine(4, hit.point, hit.point + hitTangent);
            Handles.color = Color.green;
            Handles.DrawAAPolyLine(4, hit.point, hit.point + hitBitangent);
            Handles.color = Color.blue;
            Handles.DrawAAPolyLine(4, hit.point, hit.point + hitNormal);
            */
            #endregion

            // Normal Line
            Handles.color = Color.cyan;
            Handles.DrawAAPolyLine(6, hit.point, hit.point + hit.normal * 3f);

            // Brush Radius
            Handles.color = Color.white;
            Handles.DrawWireDisc(hit.point, hit.normal, brushRadius);

            // Draw Random Points
            Handles.color = Color.red;


            List<RaycastHit> hitPts = new List<RaycastHit>();

            foreach (Vector2 p in randomPoints)
            {
                // Create Ray for this point
                Ray ptRay = GetTangentRay(p);

                // Raycast to find point on Surface
                if(Physics.Raycast(ptRay, out RaycastHit ptHit, 5f, paintOnlyLayers)) // MAKE MAX LENGTH ADJUSTABLE
                {
                    // if only Draw Horizontal is checked - check the angle
                    if(onlyDrawOnHorizontal)
                    {
                        float angle = Vector3.Dot(Vector3.up, ptHit.normal);

                        if(angle > 0.7f)
                        {
                            hitPts.Add(ptHit);
                        }
                    }
                    else
                    {
                        hitPts.Add(ptHit);
                    }

                    // Draw Normal on Surfaces
                    Handles.DrawAAPolyLine(4, ptHit.point, ptHit.point + ptHit.normal);
                }         
            }
            Handles.color = Color.white;


            // Check if Space is pressed
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Space)
            {
                // SPAWN STUFF HERE
                TrySpawnObjects(hitPts);
            }
        }    
    }

    void TrySpawnObjects(List<RaycastHit> _hitPts)
    {
        if (spawnPrefabs.Count == 0) return;

        foreach(RaycastHit hit in _hitPts)
        {
            GameObject spawnedGO = (GameObject)PrefabUtility.InstantiatePrefab(spawnPrefabs[Random.Range(0, spawnPrefabs.Count)]);
            Undo.RegisterCreatedObjectUndo(spawnedGO, "Spawn Objects");
            spawnedGO.transform.position = hit.point;

            // Other Settings
            if(randomizeScale)
            {
                float newRandomScale = Random.Range(randomScaleMin, randomScaleMax);
                spawnedGO.transform.localScale *= newRandomScale;
            }

            if(parentTransform != null)
            {
                spawnedGO.transform.SetParent(parentTransform);
            }

            float randomAngDeg = Random.value * 360f;
            Quaternion randomRot = Quaternion.Euler(0f, randomAngDeg, 0f);

            Quaternion rot = Quaternion.LookRotation(hit.normal) * Quaternion.Euler(90f, 0f, 0f) * randomRot;
            spawnedGO.transform.rotation = rot;
        }

        GenerateRandomPoints();
    }

    void GenerateRandomPoints()
    {
        randomPoints = new Vector2[spawnCount];

        for (int i = 0; i < spawnCount; i++)
        {
            randomPoints[i] = Random.insideUnitCircle;
        }
    }

}

public static class ExtensionMethods
{
    public static float AtLeast(this float v, float min)
    {
        return Mathf.Max(v, min);
    }

    public static int AtLeast(this int v, int min)
    {
        return Mathf.Max(v, min);
    }
}