using UnityEngine;

// [System.Serializable]을 꼭 붙여야 JSON으로 변환(저장)할 수 있습니다!
[System.Serializable]
public class PlayerData
{
    public float currentHealth;
    public int woodCount;
    public int stoneCount;
    // 권희님이 나중에 여기에 무기 레벨, 스킬 쿨타임 등을 마음껏 추가하면 됩니다.

    // 처음 게임을 시작할 때 부여할 기본값 세팅
    public PlayerData()
    {
        currentHealth = 100f;
        woodCount = 0;
        stoneCount = 0;
    }
}