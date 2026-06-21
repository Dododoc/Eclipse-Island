using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

/// <summary>
/// 게임 시작 시 4개의 직업(Vanguard, Builder, Alchemist, Crafter) 중 하나를 선택하는 UI.
///
/// [2단계 선택 흐름]
/// 1. (로컬) 카드를 클릭하면 네트워크 통신 없이 "미리보기 선택"만 된다. 골드 테두리로 강조되고 하단 확정 버튼이 활성화된다.
///    아직 아무것도 잠그지 않으므로 마음껏 다른 카드로 바꿀 수 있다.
/// 2. (네트워크) 하단 "게임 시작" 버튼을 누르면, 미리보기 중인 직업에 대해 Room CustomProperties로 실제 선점을 시도한다.
///    - SetCustomProperties + expectedProperties(=CAS, Check-And-Swap)를 사용해 "그 키가 비어있을 때만" 성공하도록 한다.
///    - 같은 직업을 두 명이 동시에 확정해도 Photon 서버가 단 한 명만 통과시킨다(선점 경쟁 해결의 핵심).
/// 3. 결과는 OnRoomPropertiesUpdate로 전원에게 통보된다.
///    - 내가 선점 성공 -> 즉시 PhotonNetwork.Instantiate로 소환 + UI 닫기 + PlayerPrefs 저장
///    - 다른 사람이 선점 -> 그 카드 비활성화 + 흑백 톤 + "선택됨" 오버레이.
///      만약 내가 미리보기 중이던 카드를 남이 먼저 가져갔다면, 내 미리보기는 자동 해제된다.
/// 4. 재접속(5분 내 재입장) 시에는 PlayerPrefs에 저장된 직업으로 UI 없이 바로 재소환한다.
///
/// [씬 세팅 요구사항]
/// - 이 스크립트는 Canvas_CharacterSelect 오브젝트에 붙인다.
/// - cardSlots 배열에 4개 카드의 참조를 인스펙터에서 연결한다.
/// - confirmButton에 하단 "게임 시작" 버튼을, hintText에 안내 텍스트(선택)를 연결한다.
/// - 각 직업의 Player 프리팹은 반드시 Assets/Resources 폴더에 "Player_{jobId}" 이름으로 존재해야 한다.
/// </summary>
public class CharacterSelectionUI : MonoBehaviourPunCallbacks
{
    [System.Serializable]
    public class CharacterCardSlot
    {
        [Tooltip("직업 식별자. Room CustomProperties 키(Lock_{jobId})와 소환 프리팹 이름(Player_{jobId})에 그대로 사용됩니다.")]
        public string jobId;

        [Tooltip("카드 전체를 감싸는 루트의 Button. 클릭/호버 판정이 여기 붙습니다.")]
        public Button selectButton;

        [Tooltip("호버 시 위로 떠오르는 실제 비주얼 패널(CardVisual). LayoutGroup이 직접 건드리지 않는 자식 RectTransform이어야 합니다.")]
        public RectTransform cardVisual;

        [Tooltip("테두리 역할을 하는 카드 루트의 Image (호버/미리보기/잠금 상태에 따라 색이 바뀝니다)")]
        public Image borderImage;

        [Tooltip("카드 내부 채움 Image (잠겼을 때 무채색 톤으로 바뀝니다)")]
        public Image fillImage;

        [Tooltip("\"선택됨\" 오버레이 오브젝트. 평소엔 비활성 상태로 둡니다.")]
        public GameObject selectedOverlay;

        [Tooltip("이 직업의 테마 컬러. 평상시(잠금/미리보기/호버가 아닐 때) 테두리에 적용되어 카드를 한눈에 구분되게 합니다.")]
        public Color themeColor = Color.white;
    }

    [Header("직업 카드 슬롯 (인스펙터에서 4개 연결)")]
    public CharacterCardSlot[] cardSlots = new CharacterCardSlot[4];

    [Header("확정 / 안내 UI")]
    [Tooltip("하단 '게임 시작' 버튼")]
    public Button confirmButton;
    [Tooltip("(선택) 상태 안내용 텍스트")]
    public Text hintText;

    [Header("색상 설정")]
    public Color normalBorderColor = new Color(0.85f, 0.85f, 0.85f, 1f);
    public Color hoverBorderColor = Color.cyan;
    public Color previewBorderColor = new Color(1f, 0.85f, 0.2f, 1f); // 내가 미리보기로 고른 카드(골드)
    public Color lockedBorderColor = new Color(0.35f, 0.35f, 0.35f, 1f);
    public Color normalFillColor = new Color(0.08f, 0.1f, 0.16f, 1f);
    public Color lockedFillColor = new Color(0.18f, 0.18f, 0.18f, 1f);

    [Header("호버 피드백")]
    public float hoverLiftPixels = 10f;
    public float hoverTransitionSpeed = 12f;

    private const string ROOM_PROP_PREFIX = "Lock_";
    private const string PLAYERPREFS_CLASS_KEY = "MY_CLASS";
    private const string PREFAB_PREFIX = "Player_";

    private bool hasSpawned = false;
    private string previewJobId = null; // 현재 로컬에서 미리보기로 골라둔 직업 (아직 확정 전)

    private void Awake()
    {
        // [임시/테스트용] 매 실행마다 직업 선택을 새로 하기 위해 저장값을 지웁니다.
        // 실제 빌드에서 재접속-재소환을 테스트할 때는 이 줄을 주석 처리하세요.
        PlayerPrefs.DeleteKey(PLAYERPREFS_CLASS_KEY);

        foreach (CharacterCardSlot slot in cardSlots)
        {
            if (slot == null || slot.selectButton == null)
            {
                continue;
            }

            string capturedJobId = slot.jobId;
            slot.selectButton.onClick.AddListener(() => OnCardPreviewClicked(capturedJobId));

            CardHoverEffect hover = slot.selectButton.gameObject.GetComponent<CardHoverEffect>();
            if (hover == null)
            {
                hover = slot.selectButton.gameObject.AddComponent<CardHoverEffect>();
            }
            hover.Initialize(slot, this);
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmClicked);
            confirmButton.interactable = false; // 미리보기 선택 전에는 비활성
        }

        SetHint("직업을 선택하세요");

        // 방 입장이 확정되기 전까지 UI를 숨겨둡니다.
        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.enabled = false;
        }
    }

    // -----------------------------------------------------------------
    // Photon 콜백 : 방 입장 완료
    // -----------------------------------------------------------------
    public override void OnJoinedRoom()
    {
        hasSpawned = false;
        previewJobId = null;

        string savedJobId = PlayerPrefs.GetString(PLAYERPREFS_CLASS_KEY, string.Empty);
        if (!string.IsNullOrEmpty(savedJobId))
        {
            // 재접속 케이스 -> UI 없이 바로 소환.
            Debug.Log($"[CharacterSelectionUI] 저장된 직업({savedJobId})으로 재소환합니다.");
            SpawnAndClose(savedJobId, false);
            return;
        }

        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.enabled = true;
        }

        RefreshAllCardStatesFromRoomProperties();
        UpdateConfirmButtonState();
    }

    // -----------------------------------------------------------------
    // Photon 콜백 : Room CustomProperties 변경됨 (나 포함 전원에게 호출)
    // -----------------------------------------------------------------
    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        foreach (var entry in propertiesThatChanged)
        {
            string key = entry.Key as string;
            if (string.IsNullOrEmpty(key) || !key.StartsWith(ROOM_PROP_PREFIX))
            {
                continue;
            }

            string jobId = key.Substring(ROOM_PROP_PREFIX.Length);
            int? ownerActorNumber = entry.Value == null ? (int?)null : (int)entry.Value;

            ApplyLockVisual(jobId, ownerActorNumber);

            if (ownerActorNumber.HasValue)
            {
                if (ownerActorNumber.Value == PhotonNetwork.LocalPlayer.ActorNumber)
                {
                    // 내가 선점에 성공 -> 소환
                    if (!hasSpawned)
                    {
                        SpawnAndClose(jobId, true);
                    }
                }
                else
                {
                    // 다른 사람이 이 직업을 가져감. 내가 미리보기 중이던 직업이면 미리보기 해제.
                    if (previewJobId == jobId)
                    {
                        previewJobId = null;
                        SetHint("앗, 방금 다른 플레이어가 선택했어요. 다른 직업을 골라주세요.");
                        UpdateConfirmButtonState();
                    }
                }
            }
        }
    }

    // -----------------------------------------------------------------
    // 늦게 입장한 플레이어를 위해, 이미 잠긴 직업들을 카드에 반영
    // -----------------------------------------------------------------
    private void RefreshAllCardStatesFromRoomProperties()
    {
        if (PhotonNetwork.CurrentRoom == null)
        {
            return;
        }

        Hashtable roomProps = PhotonNetwork.CurrentRoom.CustomProperties;

        foreach (CharacterCardSlot slot in cardSlots)
        {
            if (slot == null || string.IsNullOrEmpty(slot.jobId))
            {
                continue;
            }

            string key = ROOM_PROP_PREFIX + slot.jobId;
            if (roomProps != null && roomProps.TryGetValue(key, out object value) && value != null)
            {
                ApplyLockVisual(slot.jobId, (int)value);
            }
            else
            {
                ApplyLockVisual(slot.jobId, null);
            }
        }
    }

    // -----------------------------------------------------------------
    // 1단계 : 카드 클릭 = 로컬 미리보기 선택 (네트워크 통신 없음)
    // -----------------------------------------------------------------
    private void OnCardPreviewClicked(string jobId)
    {
        if (hasSpawned || PhotonNetwork.CurrentRoom == null)
        {
            return;
        }

        // 이미 다른 사람이 확정해 잠근 직업이면 미리보기 불가
        string lockKey = ROOM_PROP_PREFIX + jobId;
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(lockKey, out object existing) && existing != null)
        {
            return;
        }

        previewJobId = jobId;
        SetHint($"'{GetDisplayName(jobId)}' 선택됨 — 하단 버튼으로 확정하세요");

        // 모든 카드의 테두리를 다시 그려서, 미리보기 카드만 골드로 강조.
        RefreshAllBorders();
        UpdateConfirmButtonState();
    }

    // -----------------------------------------------------------------
    // 2단계 : 확정 버튼 = 실제 네트워크 선점 시도
    // -----------------------------------------------------------------
    private void OnConfirmClicked()
    {
        if (hasSpawned || string.IsNullOrEmpty(previewJobId) || PhotonNetwork.CurrentRoom == null)
        {
            return;
        }

        string lockKey = ROOM_PROP_PREFIX + previewJobId;

        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(lockKey, out object existing) && existing != null)
        {
            // 누르기 직전에 누가 가져간 경우
            previewJobId = null;
            SetHint("앗, 방금 다른 플레이어가 선택했어요. 다른 직업을 골라주세요.");
            UpdateConfirmButtonState();
            return;
        }

        Hashtable newProps = new Hashtable { { lockKey, PhotonNetwork.LocalPlayer.ActorNumber } };
        Hashtable expectedProps = new Hashtable { { lockKey, null } }; // "비어있을 때만" 적용 (CAS)

        bool requestSent = PhotonNetwork.CurrentRoom.SetCustomProperties(newProps, expectedProps);
        if (requestSent)
        {
            // 응답을 기다리는 동안 중복 클릭 방지
            if (confirmButton != null) confirmButton.interactable = false;
            SetHint("선택을 확정하는 중...");
        }
        else
        {
            SetHint("네트워크 상태를 확인해 주세요.");
        }
        // 성공/실패 결과는 OnRoomPropertiesUpdate에서 비동기로 받습니다.
    }

    // -----------------------------------------------------------------
    // 특정 직업의 잠금 상태를 카드 비주얼에 반영 (전 클라이언트 공통)
    // -----------------------------------------------------------------
    private void ApplyLockVisual(string jobId, int? ownerActorNumber)
    {
        CharacterCardSlot slot = FindSlot(jobId);
        if (slot == null)
        {
            return;
        }

        bool isLocked = ownerActorNumber.HasValue;
        bool isLockedByMe = isLocked && ownerActorNumber.Value == PhotonNetwork.LocalPlayer.ActorNumber;

        if (slot.selectButton != null)
        {
            slot.selectButton.interactable = !isLocked;
        }

        if (slot.selectedOverlay != null)
        {
            slot.selectedOverlay.SetActive(isLocked && !isLockedByMe);
        }

        if (slot.fillImage != null)
        {
            slot.fillImage.color = isLocked ? lockedFillColor : normalFillColor;
        }

        if (slot.borderImage != null)
        {
            slot.borderImage.color = ResolveBorderColor(slot, isLocked);
        }
    }

    // 카드 한 장의 현재 테두리 색을 상태에 따라 결정
private Color ResolveBorderColor(CharacterCardSlot slot, bool isLocked)
    {
        if (isLocked)
        {
            return lockedBorderColor;
        }
        if (previewJobId == slot.jobId)
        {
            return previewBorderColor;
        }
        return slot.themeColor;
    }

    // 미리보기 변경 시 모든 카드 테두리를 다시 칠함 (잠긴 카드는 그대로 유지)
    private void RefreshAllBorders()
    {
        foreach (CharacterCardSlot slot in cardSlots)
        {
            if (slot == null || slot.borderImage == null)
            {
                continue;
            }
            bool isLocked = slot.selectButton != null && !slot.selectButton.interactable;
            slot.borderImage.color = ResolveBorderColor(slot, isLocked);
        }
    }

    private void UpdateConfirmButtonState()
    {
        if (confirmButton != null)
        {
            confirmButton.interactable = !hasSpawned && !string.IsNullOrEmpty(previewJobId);
        }
    }

    // -----------------------------------------------------------------
    // 직업 확정 순간 : 소환 + UI 닫기 + (필요시) 저장
    // -----------------------------------------------------------------
    private void SpawnAndClose(string jobId, bool saveToPlayerPrefs)
    {
        if (hasSpawned)
        {
            return;
        }
        hasSpawned = true;

        if (saveToPlayerPrefs)
        {
            PlayerPrefs.SetString(PLAYERPREFS_CLASS_KEY, jobId);
            PlayerPrefs.Save();
        }

        string prefabName = PREFAB_PREFIX + jobId;
        Vector3 randomSpawnPos = new Vector3(Random.Range(-2f, 2f), Random.Range(-2f, 2f), 0f);

        PhotonNetwork.Instantiate(prefabName, randomSpawnPos, Quaternion.identity);
        Debug.Log($"[{PhotonNetwork.NickName}] '{jobId}' 직업으로 소환되었습니다.");

        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.enabled = false;
        }
    }

    private CharacterCardSlot FindSlot(string jobId)
    {
        foreach (CharacterCardSlot slot in cardSlots)
        {
            if (slot != null && slot.jobId == jobId)
            {
                return slot;
            }
        }
        return null;
    }

    private string GetDisplayName(string jobId)
    {
        switch (jobId)
        {
            case "Vanguard": return "개척자";
            case "Builder": return "방벽지기";
            case "Alchemist": return "연금술사";
            case "Crafter": return "엔지니어";
            default: return jobId;
        }
    }

    private void SetHint(string message)
    {
        if (hintText != null)
        {
            hintText.text = message;
        }
    }

    /// <summary>
    /// 카드 하나의 호버(마우스 오버) 피드백만 담당하는 가벼운 컴포넌트.
    /// 잠긴 카드에는 반응하지 않으며, 호버 해제 시 미리보기/일반 색으로 정확히 되돌립니다.
    /// </summary>
    private class CardHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private CharacterCardSlot slot;
        private CharacterSelectionUI owner;
        private Vector2 baseAnchoredPos;
        private Vector2 targetAnchoredPos;
        private bool hasBasePos;

        public void Initialize(CharacterCardSlot slot, CharacterSelectionUI owner)
        {
            this.slot = slot;
            this.owner = owner;

            CacheBasePositionIfNeeded();
            targetAnchoredPos = baseAnchoredPos;
        }

        private void OnEnable()
        {
            if (slot == null) return;
            CacheBasePositionIfNeeded();
            targetAnchoredPos = baseAnchoredPos;
        }

        private void CacheBasePositionIfNeeded()
        {
            if (hasBasePos || slot == null || slot.cardVisual == null)
            {
                return;
            }
            baseAnchoredPos = slot.cardVisual.anchoredPosition;
            targetAnchoredPos = baseAnchoredPos;
            hasBasePos = true;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (slot == null || slot.selectButton == null || !slot.selectButton.interactable) return;

            CacheBasePositionIfNeeded();
            targetAnchoredPos = baseAnchoredPos + new Vector2(0f, owner.hoverLiftPixels);

            if (slot.borderImage != null)
            {
                slot.borderImage.color = owner.hoverBorderColor;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!hasBasePos || slot == null) return;

            targetAnchoredPos = baseAnchoredPos;

            // 호버를 벗어나면 현재 상태(미리보기/일반)에 맞는 색으로 되돌립니다.
            if (slot.borderImage != null && slot.selectButton != null && slot.selectButton.interactable)
            {
                slot.borderImage.color = owner.ResolveBorderColor(slot, false);
            }
        }

        private void Update()
        {
            if (!hasBasePos || slot == null || slot.cardVisual == null) return;

            slot.cardVisual.anchoredPosition = Vector2.Lerp(
                slot.cardVisual.anchoredPosition,
                targetAnchoredPos,
                Time.unscaledDeltaTime * owner.hoverTransitionSpeed);
        }
    }
}
