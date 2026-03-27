using UnityEngine;

public class MainCameraSettingsSync : MonoBehaviour
{
    private void OnEnable()
    {
        Camera currentCamera = GetComponent<Camera>();
        Camera mainCamera = Camera.main;

        if (currentCamera == null || mainCamera == null)
        {
            Debug.LogError("NULL Camera!");
            return;
        }

        CopyTransform(mainCamera.transform, currentCamera.transform);
        CopyProjection(mainCamera, currentCamera);
    }

    private void CopyTransform(Transform source, Transform target)
    {
        target.position = source.position;
        target.rotation = source.rotation;
        target.localScale = source.localScale;
    }

    private void CopyProjection(Camera source, Camera target)
    {
        target.orthographic = source.orthographic;

        target.nearClipPlane = source.nearClipPlane;
        target.farClipPlane = source.farClipPlane;

        if (source.orthographic)
        {
            target.orthographicSize = source.orthographicSize;
        }
        else
            target.fieldOfView = source.fieldOfView;
    }
}