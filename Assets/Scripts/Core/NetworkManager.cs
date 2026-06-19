using UnityEngine;
using Photon.Pun; 
using Photon.Realtime; 

public class NetworkManager : MonoBehaviourPunCallbacks
{
    void Start()
    {
        // [수정됨] 로컬 테스트를 위해 기기 저장 기능을 무시하고 켤 때마다 새 ID 발급
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
        
        // [핵심] 유저가 튕겨도 5분(300,000ms) 동안 방에 정보를 남겨둡니다!
        roomOptions.PlayerTtl = 300000; 

        PhotonNetwork.JoinOrCreateRoom("MainRoom", roomOptions, TypedLobby.Default);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("4단계: 방 입장 완료!");
        
        string[] classNames = { "Player_Vanguard", "Player_Crafter", "Player_Alchemist", "Player_Builder" };

        // ==========================================
        // 2. 내 직업 기억 시스템
        // ==========================================
        string myClass = PlayerPrefs.GetString("MY_CLASS", "");

        // 만약 저장된 직업이 없다면 (생전 처음 방에 들어왔다면)
        if (string.IsNullOrEmpty(myClass))
        {
            int actorNum = PhotonNetwork.LocalPlayer.ActorNumber;
            int classIndex = (actorNum - 1) % classNames.Length;
            myClass = classNames[classIndex];
            
            // 배정받은 직업을 기기에 영구 저장!
            PlayerPrefs.SetString("MY_CLASS", myClass);
        }

        Vector3 randomSpawnPos = new Vector3(Random.Range(-2f, 2f), Random.Range(-2f, 2f), 0);

        // 이제 무조건 내가 저장해둔 직업으로만 소환됩니다.
        PhotonNetwork.Instantiate(myClass, randomSpawnPos, Quaternion.identity);
        
        Debug.Log($"[{PhotonNetwork.NickName}]님이 {myClass} 직업으로 소환되었습니다.");
    }
}