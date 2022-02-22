using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraGizmoDrawer : MonoBehaviour
{
    Camera m_camera;
    public Color color = Color.white;

    void OnDrawGizmos()
    {
        if (m_camera == null)
        {
            m_camera = gameObject.GetComponent<Camera>();
        }

        Color tempColor = Gizmos.color;
        Matrix4x4 tempMat = Gizmos.matrix;
        if (this.m_camera.orthographic)
        {
            Camera c = m_camera;
            var size = c.orthographicSize;
            Gizmos.color = color;
            Gizmos.matrix = Matrix4x4.TRS(this.transform.position, this.transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.forward * (c.nearClipPlane + (c.farClipPlane - c.nearClipPlane) / 2)
                , new Vector3(size * 2.0f * c.aspect,size * 2.0f, c.farClipPlane - c.nearClipPlane));
        }
        else
        {
            Camera c = m_camera;
            Gizmos.color = color;
            Gizmos.matrix = Matrix4x4.TRS(this.transform.position, this.transform.rotation, Vector3.one);
            Gizmos.DrawFrustum(Vector3.zero, c.fieldOfView, c.farClipPlane, c.nearClipPlane, c.aspect);
        }
        Gizmos.color = tempColor;
        Gizmos.matrix = tempMat;
    }
}
