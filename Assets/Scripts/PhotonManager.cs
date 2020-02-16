using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhotonManager : Photon.MonoBehaviour {

    private const string PHOTON_GAME_VER = "v1.1";      // 同じ値のゲームのみマッチングが可能
    private const string GAMEROOM_NAME = "myroom01";    // 部屋の名前デフォルト値
    [SerializeField] private int GAMEROOM_LIMIT = 4;    // 部屋の制限人数

    // Roomプロパティ用のタグ名
    //private const string 



    public bool isInitPhoton = false;                   // photon初期化済
    public bool isJoinedLobby = false;                  // ロビー入場済
    public bool isJoinedRoom_Wait = false;              // ルームへの入室トライ中
    public bool isJoinedRoom = false;                   // ルーム入室済
    public bool isRoomMake = false;                     // 自分でルームを作成したか
    public bool isExitRoom_Wait = false;                // ルームからの退室トライ中

    public int isPlayer_ID = 0;
    public string isPlayer_UserId = "";

    // Use this for initialization
    void Start () {

        // シーン遷移などに影響を受けないゲームマネージャに対してPhotonManage参照の設定を装填
        // 他のクラスからも参照可能なようにする
        GlobalGameManager._instance.photonManager = this;
        	
	}
	
    private void DebugLog(string msg)
    {
        if(GlobalGameManager._instance.DebugMode)
        {
            Debug.Log(msg);
        }

//        GlobalGameManager._instance.photonManager.Se
//            SetCustomProperties

    }



    /// <summary>
    /// photonとの接続初期処理
    /// </summary>
    public IEnumerator InitPhotonConnect()
    {
        DebugLog("InitPhotonConnect Start");

        // photonへの接続を行う
        PhotonNetwork.ConnectUsingSettings(PHOTON_GAME_VER);

        // 1秒間に送信するパケット数を指定
        // 増やすと同期精度が上がるが通信負荷が上がる
        PhotonNetwork.sendRate = 60;                // default:15
        PhotonNetwork.sendRateOnSerialize = 60;     // default:15

        // LoadLevel()ではなく、PhotonNetwork.LoadLevel()を利用する事を許可する。
        // 設定する事でMasterClientがPhotonNetwork.LoadLevel()を行うと他のクライアントもシーン遷移する。
        // シーン遷移の同期処理を管理に便利
        PhotonNetwork.automaticallySyncScene = true;

        //
        isInitPhoton = true;

        DebugLog("InitPhotonConnect End");
        yield return null;

    }

    /// <summary>
    /// Photonとの接続解除
    /// </summary>
    public void ClosePhotonConnect()
    {
        DebugLog("ClosePhotonConnect Start");

        PhotonNetwork.Disconnect();

        //
        isInitPhoton = false;
        isJoinedLobby = false;

        DebugLog("ClosePhotonConnect End");
    }


    /// <summary>
    /// Room内のPlayer数を取得
    /// </summary>
    /// <returns>The count by room.</returns>
    public int GetPlayerCountByRoom()
    {
        int result = 0;

        Room room = PhotonNetwork.room;
        if(room != null)
        {
            result = room.PlayerCount;
        }

        return result;
    }

    public int[] GetPlayerList()
    {
        int[] playerIds = new int[PhotonNetwork.playerList.Length];

        int n = 0;
        foreach (PhotonPlayer player in PhotonNetwork.playerList)
        {
            playerIds[n] = player.ID;
            n++;
        }

        return playerIds;
    }



    /// <summary>
    /// Roomを作成する
    /// </summary>
    /// <param name="roomOwnerId"></param>
    /// <param name="roomName">Room name.</param>
    public IEnumerator CreateRoom(string roomOwnerId = "001", string roomName = GAMEROOM_NAME)
    {
        DebugLog(string.Format("CreateRoom Start:{0},{1}", roomOwnerId, roomName));

        // プレイヤー退出時のオブジェクト消滅を防ぐ？
        PhotonNetwork.autoCleanUpPlayerObjects = false;

        // カスタムプロパティ
        //ExitGames.Client.Photon.Hashtable customProp = new ExitGames.Client.Photon.Hashtable();
        //customProp.Add("roomName", roomName);
        //customProp.Add("roomOwnerId", roomOwnerId);
        //PhotonNetwork.SetPlayerCustomProperties(customProp);

        // ルームオプションにカスタムプロパティを設定
        // TODO:ここでタグを事前に設定しておく事のメリットは？
        RoomOptions roomOptions = new RoomOptions();
        //roomOptions.CustomRoomProperties = customProp;
        //roomOptions.CustomRoomPropertiesForLobby = new string[] { "roomName", "roomOwnerId" };
        roomOptions.CustomRoomPropertiesForLobby = new string[] { "roomName" };

        roomOptions.MaxPlayers = (byte)GAMEROOM_LIMIT;      // 部屋の最大人数
        roomOptions.IsOpen = true;                          // 入室許可する
        roomOptions.IsVisible = true;                       // ロビーから見えるようにする

        //
        PhotonNetwork.CreateRoom(roomName, roomOptions, null);

        //
        isRoomMake = true;
        isJoinedRoom_Wait = true;

        DebugLog("CreateRoom End");
        yield return null;
    }


    /// <summary>
    /// Roomを閉じる
    /// </summary>
    /// <param name="isOpen">If set to <c>true</c> is open.</param>
    public void CloseRoom(bool isOpen = false)
    {
        DebugLog("CloseRoom Start");

        Room room = PhotonNetwork.room;
        if(room != null)
        {
            DebugLog("Room Close");
            room.IsOpen = isOpen;
        }

        DebugLog("CloseRoom End");
    }


    /// <summary>
    /// 存在するRoomにランダムで入室する
    /// </summary>
    public IEnumerator JoinRandomRoom()
    {
        DebugLog("JoinRandomRoom");

        PhotonNetwork.JoinRandomRoom();
        isJoinedRoom_Wait = true;

        yield return null;
    }

    /// <summary>
    /// 既に存在しているルームへ入室する
    /// </summary>
    public void JoinRoom(string roomName = GAMEROOM_NAME)
    {
        DebugLog(string.Format("JoinRoom({0})", roomName));
        PhotonNetwork.JoinRoom(roomName);

        isRoomMake = false;
    }


    public void ExitRoom()
    {
        Debug.Log("ExitRoom");

        PhotonNetwork.LeaveRoom();

        isExitRoom_Wait = true;
    }


    /// <summary>
    /// Room内のメンバ情報を確認する
    /// </summary>
    public string CheckPlayerList()
    {
        string result = "";
        foreach (PhotonPlayer player in PhotonNetwork.playerList)
        {
            result += string.Format("ID:{0} UserId:{1} isMC:{2} \n",
                                    player.ID,
                                    player.UserId,
                                    player.IsMasterClient);
        }
        return result;
    }

    public string CheckPlayerStatus()
    {
        string result = "";

        if(PhotonNetwork.player != null)
        { 
            result = string.Format("ID:{0} name:{1}\n",
                                    PhotonNetwork.player.ID,
                                    PhotonNetwork.player.NickName);

        }
        else
        {
            result = "PhotonNetwork.playerの値が未設定です。";
        }

        return result;
    }

    public string CheckRoomStatus()
    {
        string result = "";

        if(PhotonNetwork.room != null)
        {
            result = string.Format("RoomName:{0} RoomOwnerId:{1}",
                                PhotonNetwork.room.CustomProperties["roomName"],
                                PhotonNetwork.room.CustomProperties["roomOwnerId"]
                            );
        }

        return result;
    }


    /// <summary>
    /// Roomへの入室を可能にする
    /// </summary>
    public void RoonUnlock()
    {
        // MasterClientのみRoomへの鍵を開ける事ができる
        if (PhotonNetwork.isMasterClient == true)
        {
            PhotonNetwork.room.IsOpen = true;
        }
    }


    /// <summary>
    /// Roomへの入室を不可能にする
    /// </summary>
    public void RoomLock()
    {
        // MasterClientのみRoomへの鍵を閉める事ができる
        if(PhotonNetwork.isMasterClient == true)
        {
            PhotonNetwork.room.IsOpen = false;
        }
    }


    /// <summary>
    /// Photonでシーン遷移を行うメソッド
    /// </summary>
    /// <param name="sceneName">Scene name.</param>
    public void SceneChangeGame(string sceneName)
    {
        DebugLog("SceneChangeGame");

        // MasterClientでのみシーン移動を実行する
        if (PhotonNetwork.isMasterClient == true)
        {
            // ゲームシーンへ遷移
            PhotonNetwork.LoadLevel(sceneName);
        }
        return;
    }


    /// <summary>
    /// MasterClientであるかどうかを返すだけ
    /// </summary>
    /// <returns><c>true</c>, if master client was ised, <c>false</c> otherwise.</returns>
    public bool IsMasterClient()
    {
        return PhotonNetwork.isMasterClient;
    }





    /// <summary>
    /// Player情報をセットする
    /// </summary>
    private void SetPlayerInfo()
    {
        this.isPlayer_ID = PhotonNetwork.player.ID;
        this.isPlayer_UserId = PhotonNetwork.player.UserId;
    }


    /// <summary>
    /// Player情報をリセットする
    /// </summary>
    private void ResetPlayerInfo()
    {
        this.isPlayer_ID = 0;
        this.isPlayer_UserId = "";
    }

    /// <summary>
    /// GameObjectの作成
    /// </summary>
    /// <returns>The game object.</returns>
    /// <param name="prefabName">Prefab name.</param>
    /// <param name="position">Position.</param>
    /// <param name="rotation">Rotation.</param>
    public GameObject CreateGameObject(string prefabName, Vector3 position, Quaternion rotation)
    {
        return PhotonNetwork.Instantiate(prefabName, position, rotation, 0);
    }

    /// <summary>
    /// GameObjectの削除
    /// </summary>
    /// <param name="obj">Object.</param>
    public void DestroyGameObject(GameObject obj)
    {
        PhotonNetwork.Destroy(obj);
    }


    public int GetRoomPlayersMax()
    {
        return PhotonNetwork.room.MaxPlayers;
    }



    // -------------------------------------
    // photon callback
    // -------------------------------------

    /// <summary>
    /// event:photonに接続した時
    /// </summary>
    public void OnConnectedToPhoton()
    {
        Debug.Log("OnConnectedToPhoton");
    }

    /// <summary>
    /// event:photonが切断した時
    /// </summary>
    public void OnDisconnectedFromPhoton()
    {
        Debug.Log("OnDisconnectedFromPhoton");
    }

    /// <summary>
    /// event:接続に失敗した
    /// </summary>
    public void OnConnectionFail()
    {
        // photonとの関連は？
        Debug.Log("OnConnectionFail");
    }

    /// <summary>
    /// event:photonとの接続に失敗した
    /// </summary>
    public void OnFailedToConnectToPhoton(object parameters)
    {
        Debug.Log("OnFailedToConnectToPhoton");
    }

    /// <summary>
    /// event:ロビーに入室
    /// </summary>
    public void OnJoinedLobby()
    {
        Debug.Log("OnJoinedLobby");
        isJoinedLobby = true;
    }

    /// <summary>
    /// event:ロビーより退室
    /// </summary>
    public void OnLeftLobby()
    {
        Debug.Log("OnLeftLobby");
    }

    /// <summary>
    /// event:Masterが接続した時
    /// autoJoinLobbyがtrue時、OnJoinedLobbyが代わりに呼ばれる
    /// </summary>
    public void OnConnectedToMaster()
    {
        Debug.Log("OnConnectedToMaster");
    }

    /// <summary>
    /// event:ルームリストが更新された
    /// </summary>
    public void OnReceivedRoomListUpdate()
    {
        Debug.Log("OnReceivedRoomListUpdate");
    }

    /// <summary>
    /// event:ルーム作成
    /// </summary>
    public void OnCreateRoom()
    {
        Debug.Log("OnCreateRoom");
        DebugLog(string.Format("Name:{0}",PhotonNetwork.room.Name));
    }

    /// <summary>
    /// event:ルーム作成に失敗
    /// </summary>
    public void OnPhotonCreateRoomFailed()
    {
        Debug.Log("OnPhotonCreateRoomFailed");

        this.isRoomMake = false;
        this.isJoinedRoom_Wait = false;

    }

    /// <summary>
    /// event:ルームへ入室
    /// </summary>
    public void OnJoinedRoom()
    {
        Debug.Log("OnJoinedRoom");
        DebugLog(string.Format("Name:{0}", PhotonNetwork.room.Name));

        this.isJoinedRoom = true;
        this.isJoinedRoom_Wait = false;

        // Room入室時にPlayer情報が割り当てられるのでセットする
        SetPlayerInfo();
    }

    /// <summary>
    /// event:ルームへの入室失敗
    /// </summary>
    public void OnPhotonJoinRoomFailed(object[] cause)
    {
        Debug.Log("OnPhotonJoinRoomFailed");

    }

    /// <summary>
    /// event:ランダム入室失敗
    /// </summary>
    public void OnPhotonRandomJoinFailed()
    {
        Debug.Log("OnPhotonRandomJoinFailed");
        this.isJoinedRoom_Wait = false;
    }

    /// <summary>
    /// event:ルームを退室
    /// </summary>
    public void OnLeftRoom()
    {
        Debug.Log("OnLeftRoom");
        this.isJoinedRoom = false;
        this.isRoomMake = false;
        this.isExitRoom_Wait = false;

        // Room退室時に設定されたPlayer情報をリセットする
        ResetPlayerInfo();
    }

    /// <summary>
    /// event:誰かプレイヤーが接続した
    /// </summary>
    public void OnPhotonPlayerConnected(PhotonPlayer player)
    {
        Debug.Log("OnPhotonPlayerConnected");
    }

    /// <summary>
    /// event:誰かプレイヤーの切断が切れた
    /// </summary>
    public void OnPhotonPlayerDisconnected(PhotonPlayer player)
    {
        Debug.Log("OnPhotonPlayerDisconnected");
    }

    /// <summary>
    /// event:マスタークライアントが切り替わった
    /// </summary>
    public void OnMasterClientSwitched(PhotonPlayer player)
    {
        Debug.Log("OnMasterClientSwitched");
    }


    //
    public void OnPhotonPlayerPropertiesChanged(object[] playerAndUpdatedProps)
    {
        Debug.Log("OnPhotonPlayerPropertiesChanged");
    }


    /// <summary>
    /// Roomに対するカスタムプロパティの変更を検知
    /// </summary>
    /// <param name="changedProperties">Changed properties.</param>
    public void OnPhotonCustomRoomPropertiesChanged(ExitGames.Client.Photon.Hashtable changedProperties)
    {
        Debug.Log("OnPhotonCustomRoomPropertiesChanged");
    }



}
