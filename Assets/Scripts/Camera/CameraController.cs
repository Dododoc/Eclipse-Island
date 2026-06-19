using UnityEngine;

public class CameraController : MonoBehaviour
{
    private Transform target;
    private Camera cam;

    [Header("Camera Settings")]
    public float smoothSpeed = 5f; // 카메라가 따라가는 속도
    public float zoomSpeed = 2f;   // 휠 줌 속도
    public float minZoom = 3f;     // 최대 확대
    public float maxZoom = 8f;     // 최대 축소

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    // 캐릭터의 Update 이동이 끝난 후 카메라가 쫓아가도록 LateUpdate 사용
    void LateUpdate()
    {
        // 1. 마우스 휠 줌 인/아웃
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0.0f)
        {
            cam.orthographicSize -= scroll * zoomSpeed;
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
        }

        // 2. 부드러운 위치 추적
        if (target != null)
        {
            // Z축은 카메라의 원래 위치(-10)를 유지해야 화면이 보입니다.
            Vector3 desiredPosition = new Vector3(target.position.x, target.position.y, transform.position.z);
            // Lerp를 사용해 현재 위치에서 목표 위치로 부드럽게 이동
            transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        }
    }

    // 캐릭터가 소환될 때 이 함수를 불러서 타겟을 설정해줍니다.
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}