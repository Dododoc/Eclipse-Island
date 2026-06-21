using UnityEngine;
using Photon.Pun;
using UnityEngine.EventSystems;

public class PlayerInteraction : MonoBehaviourPun
{
    [Header("타격 설정")]
    public float toolDamage = 10f; 
    public LayerMask interactableLayer;
    public float maxMouseDistance = 1.5f; // 캐릭터가 상호작용 가능한 실제 거리 (알맞게 조절하세요)

    [Header("커서 이미지")]
    public Texture2D defaultCursor;  
    public Texture2D axeCursor;
    public Texture2D pickaxeCursor;
    public Texture2D hoeCursor;      

    private Texture2D currentCursor;

    void Start()
    {
        if (!photonView.IsMine) return;
        SetCustomCursor(defaultCursor);
    }

    void Update()
    {
        if (!photonView.IsMine) return;

        HandleCursorHover();

        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            PerformMouseInteraction();
        }
    }

    // ---------------------------------------------------
    // 1. 거리에 상관없이 작동하는 마우스 호버 로직
    // ---------------------------------------------------
    private void HandleCursorHover()
    {
        // UI 위에 마우스가 있을 때는 상호작용 커서로 변하지 않도록 방어
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            SetCustomCursor(defaultCursor);
            return;
        }

        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        
        // [수정됨] 이전에 있던 거리 체크(dist > maxMouseDistance)를 완전히 삭제했습니다!
        // 이제 맵 끝에 있는 나무에 마우스를 올려도 레이저가 닿아서 커서가 변합니다.

        RaycastHit2D hit = Physics2D.Raycast(mouseWorldPos, Vector2.zero, 0f, interactableLayer);

        if (hit.collider != null)
        {
            if (hit.collider.CompareTag("Tree"))
            {
                SetCustomCursor(axeCursor); 
                return;
            }
            else if (hit.collider.CompareTag("Rock")) 
            {
                SetCustomCursor(pickaxeCursor); 
                return;
            }
            else if (hit.collider.CompareTag("Weed") || hit.collider.CompareTag("Farm"))
            {
                SetCustomCursor(hoeCursor);
                return;
            }
        }

        SetCustomCursor(defaultCursor);
    }

    // ---------------------------------------------------
    // 2. 실제 타격 처리 (가까이 가야만 캘 수 있음)
    // ---------------------------------------------------
    private void PerformMouseInteraction()
    {
        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        float dist = Vector2.Distance(transform.position, mouseWorldPos);
        
        // [핵심] 실제 마우스 클릭은 거리가 멀면 여기서 차단(return)됩니다!
        if (dist > maxMouseDistance) return;

        RaycastHit2D hit = Physics2D.Raycast(mouseWorldPos, Vector2.zero, 0f, interactableLayer);

        if (hit.collider != null)
        {
            if (hit.collider.CompareTag("Tree"))
            {
                InteractableTree tree = hit.collider.GetComponent<InteractableTree>();
                if (tree != null)
                {
                    tree.photonView.RPC("TakeDamage", RpcTarget.All, toolDamage, transform.position);
                }
            }
        }
    }

    private void SetCustomCursor(Texture2D cursorTexture)
    {
        if (currentCursor == cursorTexture) return;
        currentCursor = cursorTexture;

        Cursor.SetCursor(cursorTexture, Vector2.zero, CursorMode.Auto);
    }
}