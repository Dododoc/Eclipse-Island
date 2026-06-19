using UnityEngine;
using Photon.Pun;

public class CoreStats : MonoBehaviourPun
{
    // [핵심] 몬스터들이 맵 어디서든 코어의 위치를 바로 찾을 수 있도록 싱글톤 세팅
    public static CoreStats Instance;

    [Header("마나 엔진 스탯")]
    public float maxHealth = 1000f;
    public float currentHealth;

    void Awake()
    {
        // 맵에 코어는 단 하나만 존재해야 하므로 싱글톤 패턴을 적용합니다.
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        currentHealth = maxHealth;
    }

    // ---------------------------------------------------------
    // 1. 코어 피격 로직 (몬스터가 때릴 때 호출)
    // ---------------------------------------------------------
    [PunRPC]
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        Debug.Log($"[마나 엔진] 피격! 남은 체력: {currentHealth}");

        // TODO: 피격 시 화면 흔들림(Camera Shake)이나 붉은 조명 깜빡임 이펙트 추가 예정

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            DestroyCore();
        }
    }

    // ---------------------------------------------------------
    // 2. 코어 수리 로직 (엔지니어가 수리할 때 호출)
    // ---------------------------------------------------------
    [PunRPC]
    public void RepairCore(float healAmount)
    {
        currentHealth += healAmount;
        
        // 최대 체력을 넘지 않도록 제한
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
        Debug.Log($"[마나 엔진] 수리됨! 현재 체력: {currentHealth}");
    }

    // ---------------------------------------------------------
    // 3. 파괴 및 패배 처리
    // ---------------------------------------------------------
    private void DestroyCore()
    {
        Debug.Log("🚨 마나 엔진 파괴됨! 게임 오버!");
        // TODO: GameManager에 게임 오버 신호를 보내고 결과 창 띄우기
    }
}