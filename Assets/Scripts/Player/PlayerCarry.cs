using UnityEngine;
using Photon.Pun;
using System.IO; // Resources 폴더 확인용

public class PlayerCarry : MonoBehaviourPun
{
    [Header("운반 상태")]
    public bool isCarrying = false;
    public string carriedItemName = ""; // 예: "Item_Wood"

    [Header("운반 패널티 설정")]
    public float carrySpeedMultiplier = 0.5f;

    [Header("그래픽 설정 (Inspector에서 할당)")]
    public SpriteRenderer heldItemSprite; // ✅ 캐릭터 하위의 HoldPoint 오브젝트에 있는 SpriteRenderer

    private PlayerController playerController;
    private float originalMoveSpeed;

    void Start()
    {
        playerController = GetComponent<PlayerController>();
        if (playerController != null)
        {
            originalMoveSpeed = playerController.moveSpeed;
        }

        // 시작할 때는 손이 비어있으므로 투명하게 처리
        if (heldItemSprite != null)
        {
            heldItemSprite.sprite = null;
            heldItemSprite.enabled = false;
        }
    }

    void Update()
    {
        if (!photonView.IsMine) return;

        // ✅ 조작키를 H키로 변경
        if (Input.GetKeyDown(KeyCode.H))
        {
            if (!isCarrying)
            {
                TryPickUpItem();
            }
            else
            {
                // ✅ 들고 있을 때 다시 H를 누르면 떨구기 실행
                DropItem();
            }
        }
    }

    private void TryPickUpItem()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, 0.5f);
        
        foreach (Collider2D col in colliders)
        {
            if (col.CompareTag("DroppedItem"))
            {
                isCarrying = true;
                // 포톤 Instantiate는 프리팹 이름 끝에 (Clone)이 붙으므로 이를 제거하여 순수 이름만 가져옵니다.
                carriedItemName = col.gameObject.name.Replace("(Clone)", "").Trim(); 
                
                if (playerController != null)
                {
                    playerController.moveSpeed = originalMoveSpeed * carrySpeedMultiplier;
                }

                // ✅ 그래픽 동기화 RPC 호출 (itemName 전달)
                photonView.RPC("SyncHeldItemGraphic", RpcTarget.All, true, carriedItemName);

                if (PhotonNetwork.IsMasterClient)
                {
                    PhotonNetwork.Destroy(col.gameObject);
                }
                break;
            }
        }
    }

    private void DropItem()
    {
        // ✅ 방장이 아닐 경우, 떨구기 소환 권한을 방장에게 요청하는 식으로 고도화 가능하지만,
        // 지금은 단기 개발이므로 가장 간단하게 RPC로 떨구기 신호만 모두에게 보냅니다.
        photonView.RPC("RPC_DropItem", RpcTarget.All);
    }

    [PunRPC]
    private void RPC_DropItem()
    {
        // 1. 속도 및 상태 원상복구 (로컬)
        if (playerController != null)
        {
            playerController.moveSpeed = originalMoveSpeed;
        }
        
        // 2. 그래픽 지우기 (모두)
        if (heldItemSprite != null)
        {
            heldItemSprite.sprite = null;
            heldItemSprite.enabled = false;
        }

        // 3. 바닥에 아이템 소환 (방장만 담당)
        if (PhotonNetwork.IsMasterClient && isCarrying)
        {
            // 플레이어 발밑에 소환
            PhotonNetwork.InstantiateRoomObject(carriedItemName, transform.position, Quaternion.identity);
        }

        // 4. 상태 변수 초기화 (로컬)
        isCarrying = false;
        carriedItemName = "";
    }


    [PunRPC]
    public void SyncHeldItemGraphic(bool isHolding, string itemName)
    {
        if (heldItemSprite == null) return;

        if (isHolding)
        {
            // ✅ Resources 폴더에서 아이템 이름과 동일한 이미지를 찾아 로드합니다.
            // (Resources/Item_Wood.png 가 있어야 함!)
            Sprite itemSprite = Resources.Load<Sprite>(itemName);
            
            if (itemSprite != null)
            {
                heldItemSprite.sprite = itemSprite;
                heldItemSprite.enabled = true;
                // ✅ 캐릭터 머리 위에 그리기 위해 Sorting Order를 아주 높게 설정하거나,
                // Foreground Sorting Layer를 사용합니다.
                heldItemSprite.sortingOrder = 100; 
            }
            else
            {
                Debug.LogError($"[운반] Resources 폴더에서 {itemName} 이미지를 찾을 수 없습니다.");
                heldItemSprite.enabled = false;
            }
        }
        else
        {
            heldItemSprite.sprite = null;
            heldItemSprite.enabled = false;
        }
    }
}