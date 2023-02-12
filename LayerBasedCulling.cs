using System.Collections;
using UnityEngine;

public class LayerBasedCulling : MonoBehaviour
{
    Camera cam;
    [SerializeField] float verticalAndGroundCullDistance = 4000f;
    [SerializeField] float connectionCullDistance = 1000f;
    [SerializeField] float defaultCullDistance = 1000f;
    [SerializeField] float decorationCullDistance = 700f;
    [SerializeField] float mechanismCullDistance = 1200f;
    [SerializeField] float grassCullDistance = 200f;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void Start()
    {
        float[] distances = new float[32];

        // DEFAULT
        distances[0] = defaultCullDistance;

        // VERT & GROUND
        distances[8] = verticalAndGroundCullDistance;
        distances[9] = verticalAndGroundCullDistance;
        distances[29] = verticalAndGroundCullDistance;

        // CONNECTIONS
        distances[10] = connectionCullDistance;

        // DECOR
        distances[28] = decorationCullDistance;
        distances[27] = grassCullDistance;

        // MECHANISMS
        distances[23] = mechanismCullDistance;


        cam.layerCullDistances = distances;
    }
}
