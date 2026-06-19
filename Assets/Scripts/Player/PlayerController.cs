using UnityEngine;
using Photon.Pun;

public class PlayerController : MonoBehaviourPun
{
    public float moveSpeed = 5f;
    
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
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

            transform.position += moveInput * moveSpeed * Time.deltaTime;

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
            // [변경됨] MoveX가 음수(왼쪽)면 스프라이트 반전!
            spriteRenderer.flipX = true;
        }
        else if (syncedMoveX > 0.01f) 
        {
            // [변경됨] MoveX가 양수(오른쪽)면 원본 그대로(오른쪽)
            spriteRenderer.flipX = false;
        }
    }
}