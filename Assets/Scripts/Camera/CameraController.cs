using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Camera mainCamera;
    public float zoomSpeed = 5f;
    public float minZoom = 3f;  // 최대 확대 크기
    public float maxZoom = 10f; // 최대 축소 크기

    void Update()
    {
        // 마우스 휠 스크롤 값 받아오기
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        
        if (scroll != 0.0f)
        {
            // Orthographic Size 조절로 줌 인/아웃
            mainCamera.orthographicSize -= scroll * zoomSpeed;
            mainCamera.orthographicSize = Mathf.Clamp(mainCamera.orthographicSize, minZoom, maxZoom);
        }
    }
}