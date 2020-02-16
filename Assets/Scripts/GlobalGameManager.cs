using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalGameManager : MonoBehaviour {

    // Singleton(情報の一括管理)
    public static GlobalGameManager _instance = null;


    //----------------------------------
    public PhotonManager photonManager = null;
    public bool DebugMode = true;                   // true=デバッグモード
    public float GAME_VERSION = 1.1f;               //

    public float GAME_TIME = 60.0f;                 // ゲーム制限時間
    public float GAME_PLAYER_MIN = 2.0f;            // ゲームプレイに必要な最低人数
    public float GAME_START_WAIT = 10.0f;           // タイトルからゲーム画面への遷移待ち時間

    public string PlayerName = "";                  // Player名を退避

    public int[] playerIds;                         // 
    public int startingOniID;                       // 最初に鬼をやるPlayerID

    public string PATH_PREFAB = "Prefabs/";         // Resourceパス、Prefabs
    public string PATH_MATERIAL = "Materials/";     // Resourceパス、Materials


    //----------------------------------
    //singleton
    void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;               // 自分自身をpublicエリアにて共有を行なっておく
        DontDestroyOnLoad(gameObject);  // これを記述する事でこのゲームオブジェクトはシーンが切り替わっても破棄されない。
    }


    /// <summary>
    /// PlayerIDの格納順でNo決める
    /// 自分自身の番号を調べる
    /// </summary>
    /// <returns>The player no.</returns>
    public int GetPlayerNo()
    {
        int result = -1;

        for(int n=0; n < this.playerIds.Length; n++)
        {
            if(this.playerIds[n].ToString() == GlobalGameManager._instance.photonManager.isPlayer_ID.ToString())
            {
                result = (n+1);
                break;
            }
        }
        return result;
    }



}
