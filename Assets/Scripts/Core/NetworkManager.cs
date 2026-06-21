using UnityEngine;
using Photon.Pun; 
using Photon.Realtime; 

public class NetworkManager : MonoBehaviourPunCallbacks
{
    void Start()
    {
        // 로컬 테스트를 위해 기기 저장 기능을 무시하고 켤 때마다 새 ID 발급
        string myId = System.Guid.NewGuid().ToString(); 
        
        PhotonNetwork.AuthValues = new AuthenticationValues(myId);
        Debug.Log("1단계: 내 ID(" + myId + ")로 서버 접속 시도 중...");
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 4;
        
        // 유저가 튕겨도 5분(300,000ms) 동안 방에 정보를 남겨둡니다
        roomOptions.PlayerTtl = 300000; 

        PhotonNetwork.JoinOrCreateRoom("MainRoom", roomOptions, TypedLobby.Default);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("4단계: 방 입장 완료! 이제 소환은 UI가 담당합니다.");
        
        // [삭제됨] 이전에 있던 플레이어 소환(PhotonNetwork.Instantiate) 및 PlayerPrefs 로직은
        // 이제 CharacterSelectionUI.cs가 알아서 처리하므로 여기서 완전히 지웠습니다!
    }
}