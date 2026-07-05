using UnityEngine;

/// <summary>
/// Attach to Main Camera. Configures the classic mobile-strategy isometric
/// look (orthographic projection, angled down) without any manual tilemap
/// sorting — Unity's own 3D depth handles occlusion between buildings/units.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class IsometricCameraRig : MonoBehaviour
{
    [Range(15f, 60f)] public float xAngle = 35f;
    [Range(0f, 90f)] public float yAngle = 45f;
    public float orthographicSize = 6f;

    Camera cam;

    void OnEnable() { cam = GetComponent<Camera>(); Apply(); }
    void OnValidate() => Apply();

    void Apply()
    {
        if (cam == null) cam = GetComponent<Camera>();
        if (cam == null) return;
        cam.orthographic = true;
        cam.orthographicSize = orthographicSize;
        transform.rotation = Quaternion.Euler(xAngle, yAngle, 0f);
    }
}
