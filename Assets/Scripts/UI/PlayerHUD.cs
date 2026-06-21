using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

/// <summary>
/// 화면 좌하단 HUD의 체력바/허기바를 로컬 플레이어의 PlayerStats에 연동합니다.
///
/// [동작 방식]
/// - 씬에 상주하는 HUD입니다. 로컬 플레이어 캐릭터는 직업 선택 후 PhotonNetwork.Instantiate로
///   런타임에 생성되므로, 이 스크립트는 매 프레임 "아직 내 플레이어를 못 찾았으면 찾는다"를 시도합니다.
/// - 내 PlayerStats를 찾으면 OnStatsChanged 이벤트를 구독해, 값이 바뀔 때만 바를 갱신합니다(매 프레임 폴링 아님).
/// - 플레이어를 찾기 전까지 HUD는 숨겨둡니다.
///
/// [씬 세팅]
/// - Canvas_HUD 등 적당한 오브젝트에 붙이고, 인스펙터에서 4개의 바/라벨 참조를 연결하세요.
/// </summary>
public class PlayerHUD : MonoBehaviour
{
    [Header("Health Hearts (left to right)")]
    [Tooltip("하트 칸들의 Image. 각 칸은 maxHealth/heartCount 만큼의 체력을 담당합니다.")]
    public Image[] hearts = new Image[5];

    [Tooltip("한 칸의 단계별 스프라이트. index 0 = 가득 찬 하트, 마지막 = 빈 하트. (예: heart_top_0 .. heart_top_4)")]
    public Sprite[] heartStages;

    [Header("Hunger Apple")]
    public Image hungerOverlay;
    public CanvasGroup hungerGroup;

    [Header("Hunger visibility")]
    public float hungerAlphaWhenFull = 0.5f;
    public float hungerAlphaWhenEmpty = 1f;

    [Header("Options")]
    public bool hideUntilPlayerFound = true;

    private PlayerStats trackedStats;
    private RectTransform heartsRoot;

    private void Start()
    {
        if (hearts.Length > 0 && hearts[0] != null)
        {
            heartsRoot = hearts[0].transform.parent as RectTransform;
        }

        if (hideUntilPlayerFound)
        {
            SetHudVisible(false);
        }
    }

    private void Update()
    {
        if (trackedStats == null)
        {
            TryBindLocalPlayer();
        }
    }

    private void TryBindLocalPlayer()
    {
        PlayerStats[] all = FindObjectsOfType<PlayerStats>();
        foreach (PlayerStats stats in all)
        {
            if (stats.photonView != null && stats.photonView.IsMine)
            {
                Bind(stats);
                return;
            }
        }
    }

    private void Bind(PlayerStats stats)
    {
        trackedStats = stats;
        trackedStats.OnStatsChanged += HandleStatsChanged;
        HandleStatsChanged(stats.currentHealth, stats.maxHealth, stats.currentHunger, stats.maxHunger);
        SetHudVisible(true);
    }

    private void HandleStatsChanged(float health, float maxHealth, float hunger, float maxHunger)
    {
        UpdateHearts(health, maxHealth);
        UpdateHunger(hunger, maxHunger);
    }

    private void UpdateHearts(float health, float maxHealth)
    {
        if (hearts == null || hearts.Length == 0 || maxHealth <= 0f || heartStages == null || heartStages.Length == 0)
        {
            return;
        }

        float healthPerHeart = maxHealth / hearts.Length;
        int lastStage = heartStages.Length - 1; // 빈 하트 index

        for (int i = 0; i < hearts.Length; i++)
        {
            if (hearts[i] == null) continue;

            // 이 하트 칸이 담은 채움 비율 0~1
            float heartFloor = healthPerHeart * i;
            float frac = Mathf.Clamp01((health - heartFloor) / healthPerHeart);

            // frac 1 -> stage 0(가득), frac 0 -> 마지막(빈). 중간은 단계별로 반올림.
            int stage = Mathf.RoundToInt((1f - frac) * lastStage);
            stage = Mathf.Clamp(stage, 0, lastStage);
            hearts[i].sprite = heartStages[stage];
        }
    }

    private void UpdateHunger(float hunger, float maxHunger)
    {
        float fraction = maxHunger > 0f ? Mathf.Clamp01(hunger / maxHunger) : 0f;

        if (hungerOverlay != null)
        {
            hungerOverlay.fillAmount = 1f - fraction;
        }

        if (hungerGroup != null)
        {
            hungerGroup.alpha = Mathf.Lerp(hungerAlphaWhenEmpty, hungerAlphaWhenFull, fraction);
        }
    }

    private void SetHudVisible(bool visible)
    {
        if (heartsRoot != null) heartsRoot.gameObject.SetActive(visible);
        if (hungerGroup != null) hungerGroup.gameObject.SetActive(visible);
    }

    private void OnDisable()
    {
        if (trackedStats != null)
        {
            trackedStats.OnStatsChanged -= HandleStatsChanged;
        }
    }
}
