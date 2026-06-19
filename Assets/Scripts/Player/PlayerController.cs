using UnityEngine;
using Photon.Pun;

public class PlayerController : MonoBehaviourPun
{
    public float moveSpeed = 5f;
    
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb; 

    void Start()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>(); 

        // [복구 완료] 내 캐릭터일 경우에만 메인 카메라가 나를 쫓아오도록 설정!
        if (photonView.IsMine)
        {
            CameraController cam = Camera.main.GetComponent<CameraController>();
            if (cam != null)
            {
                cam.SetTarget(this.transform);
            }
        }
    }

    void Update()
    {
        // -----------------------------------------------------
        // 1. 내 캐릭터 조작 및 파라미터 갱신 (나만 실행)
        // -----------------------------------------------------
        if (photonView.IsMine)
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            Vector3 moveInput = new Vector3(h, v, 0).normalized;

            // [물리 이동] 유니티 6 최신 문법 적용 완벽함!
            rb.linearVelocity = moveInput * moveSpeed;

            // 움직임이 있을 때만 방향 파라미터 갱신
            if (moveInput.magnitude > 0)
            {
                animator.SetFloat("MoveX", moveInput.x);
                animator.SetFloat("MoveY", moveInput.y);
            }
            
            animator.SetFloat("Speed", moveInput.magnitude);
        }

        // -----------------------------------------------------
        // 2. 캐릭터 스프라이트 좌우 반전 (모든 접속자가 동기화)
        // -----------------------------------------------------
        float syncedMoveX = animator.GetFloat("MoveX");
        
        if (syncedMoveX < -0.01f) 
        {
            spriteRenderer.flipX = true;
        }
        else if (syncedMoveX > 0.01f) 
        {
            spriteRenderer.flipX = false;
        }
    }
}