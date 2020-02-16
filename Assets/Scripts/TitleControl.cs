using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(PhotonView))]
public class TitleControl : MonoBehaviour {

    [SerializeField] private GameObject PlayerName;
    [SerializeField] private Text txtPlayerName;            // プレイヤー名の入力先
    [SerializeField] private Text txtLog;                   // Photon進行ログを表示
    [SerializeField] private Text TitleLogo;                // ゲームタイトル
    [SerializeField] private Text VersionNumber;            // 

    [SerializeField] private GameObject Page1;
    [SerializeField] private Button Entry;

    [SerializeField] private GameObject Page2;
    [SerializeField] private Button Search;
    [SerializeField] private Button Create;
    [SerializeField] private Button Back;

    [SerializeField] private GameObject Page3;
    [SerializeField] private Button Play;
    [SerializeField] private Button Exit;

    private string m_playerName;
    private enum TitleMode
    {
        Input_Wait,
        Entry_Wait,
        Search_Wait,
        Create_Wait,
        Player_Wait,
        Exit_Wait,
        Start_Wait,
    };
    private TitleMode titleMode;
    private float waitTime = 0.0f;
    private PhotonView photonView;

    // 同期変数
    private float startWaitTime = 0.0f;
    private int[] playerIds;
    private int startingOniId = 0;


    private void Awake()
    {
        // ボタン押下時のメソッド関連付け
        Entry.onClick.AddListener(OnEntry);
        Search.onClick.AddListener(OnSearch);
        Create.onClick.AddListener(OnCreate);
        Back.onClick.AddListener(OnBack);
        Play.onClick.AddListener(OnPlay);
        Exit.onClick.AddListener(OnExit);

        //
        photonView = GetComponent<PhotonView>();
    }

    // Use this for initialization
    void Start () {

        //
        this.titleMode = TitleMode.Input_Wait;

        // 画面コントローラの初期化
        Page1.SetActive(true);
        Page2.SetActive(false);
        Page3.SetActive(false);

    }

    // Update is called once per frame
    void Update () {

        UI_DisplayGameVersion();

        // TODO:test こういうのもある！
        if (PhotonNetwork.isMasterClient == true)
        {
            Debug.Log("TestCounter");
/*
            ExitGames.Client.Photon.Hashtable customProp = new ExitGames.Client.Photon.Hashtable();
            customProp.Add("TestCounter", "aaaaa");

            // カスタムPlayerプロパティの変更
            PhotonNetwork.SetPlayerCustomProperties(customProp);

            // カスタムRoomプロパティの変更
            PhotonNetwork.room.SetCustomProperties(customProp);
*/
        }






        switch (this.titleMode)
        {
            case TitleMode.Input_Wait:
                break;

            case TitleMode.Entry_Wait:

                // Lobbyに入るまで待機
                if(GlobalGameManager._instance.photonManager.isJoinedLobby == true)
                {
                    DisplayLog(">ネットワークへの接続が完了。");

                    // Entryボタンへの入力受付状態に戻しておく
                    Entry.interactable = true;

                    // Page1 -> Page2への切替
                    Page1.SetActive(false);
                    Page2.SetActive(true);

                    //
                    this.titleMode = TitleMode.Input_Wait;
                }
                break;

            case TitleMode.Search_Wait:
            case TitleMode.Create_Wait:

                // Room検索の結果待ち
                if(GlobalGameManager._instance.photonManager.isJoinedRoom == true)
                {
                    // Roomへの入室完了
                    this.titleMode = TitleMode.Player_Wait;

                    //
                    Search.interactable = true;
                    Create.interactable = true;
                    Back.interactable = true;

                    // Page2 -> Page3への切替
                    Page2.SetActive(false);
                    Page3.SetActive(true);

                    DisplayLog(">Roomへ入室しました。");
                }
                else if(GlobalGameManager._instance.photonManager.isJoinedRoom_Wait == false)
                {
                    // 入室フラグの前に待ちフラグがリセットされた場合、入室失敗
                    if(this.titleMode == TitleMode.Create_Wait)
                    {
                        DisplayLog(">Roomの作成に失敗しました。");
                    }
                    else
                    {
                        DisplayLog(">入室可能なRoomが存在しません。");
                    }

                    this.titleMode = TitleMode.Input_Wait;

                    //
                    Search.interactable = true;
                    Create.interactable = true;
                    Back.interactable = true;

                }
                break;

            case TitleMode.Player_Wait:

                // ゲーム開始条件の確認し、OKならPlayボタンを解放する
                if (GameStartCheck() == true)
                {
                    // Playボタンの解放
                    Play.interactable = true;
                }
                else
                {
                    // Room出入りによりボタンの状況は変化する
                    Play.interactable = false;
                }
                break;

            case TitleMode.Exit_Wait:

                // Roomからの退出結果待ち
                if(GlobalGameManager._instance.photonManager.isExitRoom_Wait == false)
                {
                    //
                    this.waitTime = 0.0f;

                    // Roomからの退出が確定
                    this.titleMode = TitleMode.Input_Wait;

                    //
                    Search.interactable = true;
                    Create.interactable = true;
                    Back.interactable = true;
                }
                else
                {
                    Search.interactable = false;
                    Create.interactable = false;
                    Back.interactable = false;
                }
                break;

            case TitleMode.Start_Wait:

                // 参加プレイヤーのIDを退避する
                GlobalGameManager._instance.playerIds = this.playerIds;

                // 最初の鬼IDを退避する
                // NOTE:RPC内の一度の設定では正しく退避できないので開始待ち時間の間に何度も退避する
                GlobalGameManager._instance.startingOniID = this.startingOniId;



                // ログ出力情報をクリアする。
                DisplayLogClear();
                DisplayLog(string.Format("ゲームを開始しています。{0:00.00}", this.startWaitTime));

                //
                DisplayLog("");
                DisplayLog("[playerIds]");
                for(int n = 0; n < this.playerIds.Length; n++)
                {
                    DisplayLog(string.Format("[{0}]{1}", n, this.playerIds[n]));
                }

                // MasterClientのみ実行する
                if (GlobalGameManager._instance.photonManager.IsMasterClient() == true)
                {
                    // 開始までのカウントダウン
                    this.startWaitTime -= Time.deltaTime;

                    // シーンの移動
                    if (this.startWaitTime <= 0.0f)
                    {
                        this.startWaitTime = 0.0f;
                        GlobalGameManager._instance.photonManager.SceneChangeGame("main");
                    }
                }
                break;

            default:
                break;
        }
    }




    private void OnEntry()
    {
        Debug.Log("OnEntry");

        // ログ出力情報をクリアする。
        DisplayLogClear();

        // 入力エラーは処理を中断
        if (InputCheck() == false)
            return;

        // 入力されたPlayerNameの値を一旦退避する
        GlobalGameManager._instance.PlayerName = txtPlayerName.text;

        // 入力オブジェクトの無効化
        PlayerName.GetComponent<InputField>().interactable = false;
        Entry.interactable = false;

        //
        this.titleMode = TitleMode.Entry_Wait;

        // Photonとの接続(初期設定)
        StartCoroutine(GlobalGameManager._instance.photonManager.InitPhotonConnect());

        // 
        DisplayLog(">ネットワークへの接続開始...");
    }

    private void OnSearch()
    {
        Debug.Log("OnSearch");

        // 入室可能なRoomへ入る
        StartCoroutine(GlobalGameManager._instance.photonManager.JoinRandomRoom());

        //
        Search.interactable = false;
        Create.interactable = false;
        Back.interactable = false;

        //
        this.titleMode = TitleMode.Search_Wait;

        DisplayLog(">入室可能なRoom検索を実行しています...");
    }

    private void OnCreate()
    {
        Debug.Log("OnCreate");

        // Roomを作成する
        StartCoroutine(GlobalGameManager._instance.photonManager.CreateRoom());

        //
        Search.interactable = false;
        Create.interactable = false;
        Back.interactable = false;

        //
        this.titleMode = TitleMode.Create_Wait;

        // 
        DisplayLog(">新しいRoomの作成を実行しています...");
    }

    private void OnBack()
    {
        Debug.Log("OnBack");

        // ログ出力情報をクリアする。
        DisplayLogClear();

        // Page2 -> Page1への切替
        Page1.SetActive(true);
        Page2.SetActive(false);
        PlayerName.GetComponent<InputField>().interactable = true;


        // Photonとの接続解除
        GlobalGameManager._instance.photonManager.ClosePhotonConnect();

        //
        this.titleMode = TitleMode.Input_Wait;

        // 
        DisplayLog(">ネットワークとの接続を解除しました。");
    }


    private bool GameStartCheck()
    {
        bool result = false;
        int playersCount = 0;

        //
        this.waitTime += Time.deltaTime;

        // ログ出力情報をクリアする。
        DisplayLogClear();

        // 自分自身のPlayer情報を表示する 
        DisplayLog("[Player Info]");
        DisplayLog(GlobalGameManager._instance.photonManager.CheckPlayerStatus());
        DisplayLog("");

        // 入室したRoom情報を表示する
        DisplayLog("[Room Info]");
        //DisplayLog(GlobalGameManager._instance.photonManager.CheckRoomStatus());
        playersCount = GlobalGameManager._instance.photonManager.GetPlayerCountByRoom();
        DisplayLog(string.Format("Players: {0}/{1}",
                                playersCount,
                                GlobalGameManager._instance.photonManager.GetRoomPlayersMax()));
        //DisplayLog(GlobalGameManager._instance.photonManager.CheckPlayerList());
        DisplayLog("");

        // 
        DisplayLog("[Comment]");

        // プレイヤー人数が揃っているかの確認
        if (playersCount >= GlobalGameManager._instance.photonManager.GetRoomPlayersMax())
        {
            // 参加可能なプレイヤー数が上限に到達
            DisplayLog("ゲーム開始します。");

            // 人数の上限はすぐに開始モードに切り替える
            if(GlobalGameManager._instance.photonManager.IsMasterClient() == true)
            {
                this.photonView.RPC("RPC_GameStartWait", PhotonTargets.All);
            }
        }
        else if(playersCount >= GlobalGameManager._instance.GAME_PLAYER_MIN)
        {
            // ゲーム開始可能な最小数を超えるプレイヤーが集まった時
            if (GlobalGameManager._instance.photonManager.IsMasterClient() == true)
            {
                DisplayLog("ゲーム開始に必要な人数が揃いました。");
                DisplayLog("「Play」にてゲームを開始させる事が可能です。");

                result = true;
            }
            else
            {
                DisplayLog("ゲーム開始の操作を待っています。");
            }
        }
        else
        {
            // ゲーム開始に必要なプレイヤー数が集まっていない
            DisplayLog("ゲーム開始に必要な人数が揃っていません。");
        }

        //
        DisplayLog(string.Format("(WaitTime:{0:N0})",this.waitTime));

        return result;
    }

    [PunRPC]
    private void RPC_GameStartWait()
    {
        Debug.Log("RPC_GameStartWait");

        // ゲーム開始待ちへ切り替え
        this.titleMode = TitleMode.Start_Wait;

        //
        this.waitTime = 0.0f;

        // ログ出力情報をクリアする。
        DisplayLogClear();

        // MasterClientのみ処理する
        if (GlobalGameManager._instance.photonManager.IsMasterClient() == true)
        {
            //
            this.startWaitTime = GlobalGameManager._instance.GAME_START_WAIT;

            // Roomへの入室禁止にする
            GlobalGameManager._instance.photonManager.RoomLock();

            // Room内のPlayerIDを取得
            // NOTE:ダイレクトではないのは一度、全体に共有しておきたかった
            //GlobalGameManager._instance.playerIds = GlobalGameManager._instance.photonManager.GetPlayerList();
            this.playerIds = GlobalGameManager._instance.photonManager.GetPlayerList();

            //
            //TODO:プレイヤーの人数は適切か？

            // 最初に鬼となるプレイヤーを決定
            //TODO:ちょっと固定にする
            //TODO:これって０が戻ることあるんだっけ？
            //this.startingOniId = this.playerIds[Random.Range((int)0, this.playerIds.Length)];
            this.startingOniId = 1;
        }
    }


    private void OnPlay()
    {
        Debug.Log("OnPlay");

        //
        if(GlobalGameManager._instance.photonManager.IsMasterClient() == true)
        {
            this.photonView.RPC("RPC_GameStartWait", PhotonTargets.All);
        }
    }


    private void OnExit()
    {
        Debug.Log("OnExit");

        // Page3 -> Page2への切替
        Page2.SetActive(true);
        Page3.SetActive(false);

        //
        this.titleMode = TitleMode.Exit_Wait;

        // Roomからの退室
        GlobalGameManager._instance.photonManager.ExitRoom();

        // ログ出力情報をクリアする。
        DisplayLogClear();
        DisplayLog(">Roomより退室しました。");
    }


    // ログに記載されている情報をクリアする
    private void DisplayLogClear()
    {
        txtLog.text = "";
    }

    // ログへ情報を記載する
    private void DisplayLog(string msg)
    {
        txtLog.text += string.Format("\n{0}", msg);
    }


    // 画面からの入力確認を行う
    private bool InputCheck()
    {
        bool result = true;

        // 入力値の確認を行う
        if (string.IsNullOrEmpty(txtPlayerName.text) == true)
        {
            DisplayLog("Player Name is Empty.");
            result = false;
        }

        return result;
    }


    /// <summary>
    /// ゲームバージョンを表示する
    /// </summary>
    private void UI_DisplayGameVersion()
    {
        VersionNumber.text = string.Format("(v{0})", GlobalGameManager._instance.GAME_VERSION);
    }


    /// <summary>
    /// Photon間でのパラメータ同期を行う
    /// </summary>
    /// <param name="stream">Stream.</param>
    /// <param name="info">Info.</param>
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        Debug.Log("OnPhotonSerializeView");

        if (stream.isWriting)
        {
            // 送信処理
            stream.SendNext(this.startWaitTime);                    // 開始までの待ち時間
            stream.SendNext(this.playerIds);                        // PlayerIDリスト
            stream.SendNext(this.startingOniId);                    // 最初に鬼をやるPlayerID
        }
        else
        {
            // 受信処理
            this.startWaitTime = (float)stream.ReceiveNext();       // 開始までの待ち時間
            this.playerIds = (int[])stream.ReceiveNext();           // PlayerIDリスト
            this.startingOniId = (int)stream.ReceiveNext();         // 最初に鬼をやるPlayerID
        }
    }


}
