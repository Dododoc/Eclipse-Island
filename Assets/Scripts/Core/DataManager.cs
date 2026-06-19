using UnityEngine;
using System.IO; // 파일 입출력을 위해 필수

public class DataManager : MonoBehaviour
{
    public static DataManager instance; // 어디서든 쉽게 접근하기 위한 싱글톤
    public PlayerData saveData;         // 현재 게임에서 쓸 데이터 통
    private string savePath;            // 파일이 저장될 경로

    void Awake()
    {
        if (instance == null) instance = this;

        // 아까 발급받은 내 User ID를 가져와서 "내아이디_Save.json" 이라는 파일을 만듭니다.
        string myId = PlayerPrefs.GetString("MY_USER_ID", "Guest");
        savePath = Application.persistentDataPath + "/" + myId + "_Save.json";
        
        LoadGame(); // 게임이 켜지자마자 내 데이터를 불러옵니다.
    }

    // [저장하기]
    public void SaveGame()
    {
        // 1. 데이터를 JSON 형태의 텍스트(문자열)로 변환
        string json = JsonUtility.ToJson(saveData, true); 
        
        // 2. 변환된 텍스트를 파일로 컴퓨터에 씁니다.
        File.WriteAllText(savePath, json);
        
        Debug.Log("💾 세이브 완료! 저장 경로: " + savePath);
    }

    // [불러오기]
    public void LoadGame()
    {
        if (File.Exists(savePath))
        {
            // 파일이 있으면 읽어와서 saveData 통에 덮어씌웁니다.
            string json = File.ReadAllText(savePath);
            saveData = JsonUtility.FromJson<PlayerData>(json);
            Debug.Log("📂 로드 완료! 현재 체력: " + saveData.currentHealth);
        }
        else
        {
            // 파일이 없으면 깡통 데이터(초기값)로 새로 시작합니다.
            saveData = new PlayerData();
            Debug.Log("새로운 세이브 파일을 생성합니다.");
        }
    }
}