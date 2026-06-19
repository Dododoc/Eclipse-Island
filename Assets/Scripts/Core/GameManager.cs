using UnityEngine;
using Photon.Pun;
using UnityEngine.Rendering.Universal; // 💡 2D 조명(Light 2D)을 제어하기 위해 반드시 필요합니다!

public class GameManager : MonoBehaviourPun
{
    [Header("게임 시간 설정")]
    public float dayDuration = 60f;   // 낮 시간 (초)
    public float nightDuration = 120f; // 밤 웨이브 시간 (초)
    private float currentTimer;
    
    [Header("진행도 설정")]
    public int currentDay = 1;        // 현재 일차 (1 ~ 15일)
    public int maxDays = 15;          // 최종 탈출 일차
    
    // 현재 상태 열거형
    public enum GamePhase { Day, Night }
    public GamePhase currentPhase = GamePhase.Day;

    [Header("조명 세팅 (Inspector에서 할당)")]
    public Light2D globalLight;       // 맵 전체를 밝히는 빛
    public Light2D coreLight;         // 마나 엔진이 뿜어내는 빛
    
    [Header("조명 밝기 수치")]
    public float dayGlobalLight = 1f;   // 낮일 때 맵 밝기
    public float nightGlobalLight = 0.1f; // 밤일 때 맵 밝기 (어두움)
    public float coreLightIntensity = 2f; // 밤에 코어가 뿜는 빛의 세기

    void Start()
    {
        currentTimer = dayDuration;
        ApplyLightSettings(currentPhase); // 시작할 때 조명 세팅
    }

    void Update()
    {
        // -----------------------------------------------------------------
        // [오직 방장만] 시간을 깎고, 페이즈(낮/밤)를 바꿀 권한을 가집니다.
        // -----------------------------------------------------------------
        if (PhotonNetwork.IsMasterClient)
        {
            currentTimer -= Time.deltaTime;

            // 매 프레임마다 모든 유저에게 현재 시간과 일차, 상태를 전송 (동기화)
            photonView.RPC("SyncGameState", RpcTarget.All, currentTimer, currentPhase, currentDay);

            if (currentTimer <= 0)
            {
                PhaseChange();
            }
        }
    }

    // -----------------------------------------------------------------
    // [모든 접속자] 방장이 보낸 시간과 상태를 받아 내 화면에 똑같이 적용합니다.
    // -----------------------------------------------------------------
    [PunRPC]
    public void SyncGameState(float syncedTime, GamePhase syncedPhase, int syncedDay)
    {
        currentTimer = syncedTime;
        currentDay = syncedDay;
        
        // 만약 방장이 보낸 페이즈와 내 페이즈가 다르다면? (낮->밤이 바뀌는 순간)
        if (currentPhase != syncedPhase)
        {
            currentPhase = syncedPhase;
            ApplyLightSettings(currentPhase); // 조명 분위기 바꾸기
        }
    }

    // -----------------------------------------------------------------
    // 낮과 밤이 전환될 때 실행되는 로직 (방장만 실행)
    // -----------------------------------------------------------------
    private void PhaseChange()
    {
        if (currentPhase == GamePhase.Day)
        {
            currentPhase = GamePhase.Night;
            currentTimer = nightDuration; 
            
            // [여기에 추가!] 방장 본인의 화면도 조명을 업데이트하도록 명령
            ApplyLightSettings(currentPhase); 
            
            Debug.Log($"🌙 [{currentDay}일차] 밤이 되었습니다!");
        }
        else
        {
            currentPhase = GamePhase.Day;
            currentTimer = dayDuration; 
            currentDay++; 

            // [여기에 추가!] 아침이 되었을 때 방장 화면 조명 업데이트
            ApplyLightSettings(currentPhase); 

            Debug.Log($"☀️ [{currentDay}일차] 아침이 밝았습니다.");
        }
    }

    // -----------------------------------------------------------------
    // 페이즈에 따라 화면 조명 끄고 켜기 (모두가 실행)
    // -----------------------------------------------------------------
    private void ApplyLightSettings(GamePhase phase)
    {
        if (globalLight == null || coreLight == null) return; // 조명 연결 안 해뒀을 때 에러 방지

        if (phase == GamePhase.Day)
        {
            globalLight.intensity = dayGlobalLight; // 맵 전체를 밝게
            coreLight.intensity = 0f;               // 낮에는 코어 불빛 끄기
        }
        else
        {
            globalLight.intensity = nightGlobalLight; // 맵 전체를 어둡게
            coreLight.intensity = coreLightIntensity; // 코어에서 강렬한 불빛 켜기
        }
    }
}