using UnityEngine;
using Photon.Pun;
using System.Collections;

// IPunInstantiateMagicCallback을 상속받아 생성될 때 나무의 데이터를 전달받습니다!
public class DroppedItem : MonoBehaviour, IPunInstantiateMagicCallback
{
    [Header("드롭 연출 설정")]
    public float spawnHeight = 2.0f;  // 나무 중심에서 튀어나올 때의 시작 높이
    public float bounceUp = 1.0f;     // 거기서 얼마나 더 위로 튀어 오를지
    public float dropDuration = 0.6f; // 떨어지는데 걸리는 총 시간

    private Vector3 startGroundPos; // 출발지점 (나무의 Pivot)
    private Vector3 finalGroundPos; // 도착지점 (흩뿌려진 바닥 위치)

    // 포톤을 통해 맵에 생성될 때 자동으로 실행되는 약속된 함수입니다.
    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        // 나무가 던져준 '나무 Pivot 위치'를 받아옵니다.
        object[] data = info.photonView.InstantiationData;
        if (data != null && data.Length > 0)
        {
            startGroundPos = (Vector3)data[0];
        }
        else
        {
            startGroundPos = transform.position; // 에러 방지용
        }

        // 내가 최종적으로 도착할 바닥 위치 (InteractableTree가 정해준 내 위치)
        finalGroundPos = transform.position;

        // 떨어지는 애니메이션 시작!
        StartCoroutine(DropAnimation());
    }

    private IEnumerator DropAnimation()
    {
        float elapsedTime = 0f;

        // 수학적 포물선 계산 (시작점, 최고점, 도착점을 부드럽게 잇는 공식)
        float h_start = spawnHeight;
        float h_max = spawnHeight + bounceUp;
        float h_end = 0f;
        
        float t_peak = Mathf.Sqrt(h_max - h_start) / (Mathf.Sqrt(h_max - h_start) + Mathf.Sqrt(h_max - h_end));
        float A = (h_max - h_start) / (t_peak * t_peak);

        while (elapsedTime < dropDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / dropDuration;

            // 1. X, Y축 이동 (나무 중심에서부터 목표 바닥 지점까지 부드럽게 퍼져나감)
            Vector3 currentGroundPos = Vector3.Lerp(startGroundPos, finalGroundPos, t);

            // 2. 가짜 높이(Z/Y축 튀어 오름) 계산
            float currentJumpHeight = -A * Mathf.Pow(t - t_peak, 2) + h_max;

            // 3. 실제 위치 적용 = 이동 중인 바닥 위치 + 공중으로 뜬 높이
            transform.position = currentGroundPos + new Vector3(0, currentJumpHeight, 0);

            yield return null;
        }

        // 연출이 끝나면 정확한 최종 바닥 위치(0,0,0이 아님!)에 딱 고정합니다.
        transform.position = finalGroundPos;
    }
}