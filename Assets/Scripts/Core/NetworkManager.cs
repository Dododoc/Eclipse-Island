using UnityEngine;
using Photon.Pun; // 포톤 기본 기능을 쓰기 위해 필수
using Photon.Realtime; // 포톤 리얼타임 기능을 쓰기 위해 필수

// MonoBehaviour 대신 MonoBehaviourPunCallbacks를 상속받아야 
// 포톤 서버에서 보내주는 응답(콜백)을 받아 처리할 수 있습니다.
public class NetworkManager : MonoBehaviourPunCallbacks
{
    void Start()
    {
        // 1단계: 내 컴퓨터를 포톤 마스터 서버에 접속시킵니다. (App ID 기반)
        Debug.Log("1단계: 마스터 서버 접속 시도 중...");
        PhotonNetwork.ConnectUsingSettings();
    }

    // 마스터 서버 접속에 성공했을 때 자동으로 실행되는 포톤 함수
    public override void OnConnectedToMaster()
    {
        Debug.Log("2단계: 마스터 서버 접속 성공! 로비 진입 시도 중...");
        
        // 2단계: 로비(방 목록을 볼 수 있는 공간)에 진입합니다.
        PhotonNetwork.JoinLobby();
    }

    // 로비 진입에 성공했을 때 자동으로 실행되는 포톤 함수
    public override void OnJoinedLobby()
    {
        Debug.Log("3단계: 로비 진입 성공! 방 생성 또는 입장 시도 중...");

        // 3단계: 방을 만들거나 이미 만들어진 방에 들어갑니다.
        // 혼자 테스트할 때 방이 없으면 자동으로 만들고 있으면 들어가는 가장 안전한 함수입니다.
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 4; // 최대 입장 인원을 4명으로 제한

        PhotonNetwork.JoinOrCreateRoom("MainRoom", roomOptions, TypedLobby.Default);
    }

    // 최종적으로 방 입장에 성공했을 때 자동으로 실행되는 포톤 함수
    public override void OnJoinedRoom()
    {
        Debug.Log("4단계: 방 입장 완료! 이제 멀티플레이를 시작할 수 있습니다.");
        Debug.Log("현재 방 이름: " + PhotonNetwork.CurrentRoom.Name);
        Debug.Log("현재 방 인원수: " + PhotonNetwork.CurrentRoom.PlayerCount + "명");
        // Resources 폴더에 있는 "PlayerDummy"를 x:0, y:0 위치에 소환
        PhotonNetwork.Instantiate("PlayerDummy", Vector3.zero, Quaternion.identity);
    }
}