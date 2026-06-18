using UnityEngine;
using Photon.Pun;

public class PlayerController : MonoBehaviourPun
{
    public float moveSpeed = 5f;

    void Update()
    {
        // 1. 내 캐릭터가 아니면 조작 방지 (동기화 핵심)
        if (!photonView.IsMine) return; 

        // 2. 입력 받기 (-1 ~ 1)
        float h = Input.GetAxisRaw("Horizontal"); // A, D
        float v = Input.GetAxisRaw("Vertical");   // W, S

        // 3. 이동 벡터 생성 (대각선 이동 시 속도 폭주 방지용 정규화)
        Vector3 moveInput = new Vector3(h, v, 0).normalized;

        // 4. 실제 이동 처리
        transform.position += moveInput * moveSpeed * Time.deltaTime;
    }
}