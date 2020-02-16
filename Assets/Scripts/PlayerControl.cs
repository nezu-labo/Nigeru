using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class PlayerControl : MonoBehaviour
{
    private SceneControl sceneControl;                      // Sceneへのアクセス用

    private CharacterController cc;
    private PhotonView photonView;                          //


    [SerializeField] private GameObject mainCameraObject;
    private Transform viewCamera;

    // Playerのゲームに関するパラメータ
    public bool oniState = false;                                   // 鬼状態を格納 true=鬼
    public int scorePoint = 0;                                      // スコア
    public string playerName = "";
    public int photonId = 0;



    // Playerの挙動に関するパラメータ
    [SerializeField] private Vector3 move;                          // キャラクター移動
    [SerializeField] private Quaternion characterRotate;            // キャラクター回転
    [SerializeField] private Quaternion cameraRotate;               // 視点カメラの回転
    [SerializeField] private Quaternion initCameraRotate;           // 視点カメラの動かす前の回転量

    [SerializeField] private float moveSpeed = 10.0f;               // 移動スピード
    [SerializeField] private float jumpPower = 10.0f;               // ジャンプ力
    [SerializeField] private float throwBombPower = 20.0f;          // Bombを投げる力
    [SerializeField] private bool bombReloadState = false;          // Bombの再装填中かの状態フラグ  true=再装填中
    [SerializeField] private float bombReloaTime = 0.0f;            // Bombを投げてからの経過時間
    [SerializeField] private float bombReloadTime_Limit = 3.0f;     // Bombを投げる間隔時間


    [SerializeField] private float mouseSpeed = 4.0f;               // マウスの移動を回転量に変換
    [SerializeField] private float rotateSpeed = 10.0f;             // 横回転のスピード量
    [SerializeField] private float cameraRotateLimit = 30.0f;       // カメラ上下に対する制限角度

    [SerializeField] private GameObject bombPrefab;                 // BombのPrefabを設定する
    [SerializeField] private Transform inGameField;                 // GameObjectを配置する場所


    private Vector3 saveCharacterPosition;


    // シーン開始の時に呼ばれる
    // コンポーネントの参照を行う
    private void Awake()
    {

        // 参照先の初期化
        this.sceneControl = GameObject.Find("SceneManager").GetComponent<SceneControl>();
        this.cc = GetComponent<CharacterController>();
        this.photonView = GetComponent<PhotonView>();
        this.bombPrefab = (GameObject)Resources.Load("Prefabs/Bomb");       // ResourceからBombプレハブを参照
        this.inGameField = GameObject.Find("InGame").transform;             // GameObject配置位置を参照

        if (IsMine() == false)
        {
            // 操作対象ではないPlayerオブジェクトが持つカメラは無効化する
            this.mainCameraObject.SetActive(false);
        }
        else
        {
            // 操作対象のPlayerオブジェクトが持つカメラ情報を取得
            this.viewCamera = GetComponentInChildren<Camera>().transform;
            this.cameraRotate = this.viewCamera.localRotation;
            this.initCameraRotate = this.viewCamera.localRotation;
        }

        // PhotonViewよりIDを取得、退避
        if(this.photonView != null)
        {
            this.photonId = this.photonView.viewID;
        }

        // GameObjectの配置先に移動させる
        this.transform.SetParent(this.inGameField);
    }


    // Use this for initialization
    void Start()
    {

        this.sceneControl.playerList.Add(this);
        this.characterRotate = transform.localRotation;          // 初期値としてプレイヤーの回転量を退避


    }


    // Update is called once per frame
    void Update()
    {

        // Playerへの着色
        PlayerPaint();


        // 操作禁止状態の場合は終了
        if (this.sceneControl.inputStop == true)
            return;


        // 操作可能な自分が作成したオブジェクトの場合のみ処理を継続
        if (IsMine() == true)
        {

            // TODO:状態確認用
            this.sceneControl.DebugStatusDisplay("");
            this.sceneControl.DebugStatusDisplay("--- Player State ---");
            this.sceneControl.DebugStatusDisplay(string.Format("isGrounded:{0}", this.cc.isGrounded));





            // カメラが存在しない場合、以降の処理は全て実施する必要なし
            // 座標の同期も行われるのでその必要なし。
            // TODO:ここは必要？
            if (GetComponentInChildren<Camera>() == null)
            {
                return;
            }


            // 左右の回転
            RotateCharacter();

            // 上下の回転
            RotateCamera();

            // 地面に着地しているかを調べる
            if (this.cc.isGrounded)
            //if(isGround())
            {
                // 移動速度をリセット
                this.move = Vector3.zero;

                // キー入力による移動量を加える
                // nomarized:正規化
                // 前移動のベクトルと横移動のベクトルを入力分移動させた状態で正規化する。
                this.move = (transform.forward * Input.GetAxis("Vertical") +
                            transform.right * Input.GetAxis("Horizontal")).normalized;

                // 移動速度をかける
                this.move *= this.moveSpeed;

                // Jumpキーが入力された時の処理
                // 鬼の時のみ利用が可能
                if (Input.GetButtonDown("Jump") &&
                    this.oniState == true)
                {
                    this.move.y += this.jumpPower;
                }
            }
            else
            {
                // 落下速度 = 重力加速度 * 時間
                this.move.y += Physics.gravity.y * Time.deltaTime;


            }


            // キャラクターの移動
            this.cc.Move(this.move * Time.deltaTime);

            this.saveCharacterPosition = this.cc.transform.position;



            // Bombインスタンスを作成する
            // 鬼の時のみ利用可能
            // Bombの再装填中では無い事
            if ((Input.GetMouseButtonUp(0) || Input.GetKeyDown(KeyCode.RightCommand))
                && this.oniState == true
                && this.bombReloadState == false)
            {
                // Bomb発生位置の補正値
                // 自分自身でヒットしてしまう事を防ぐ為
                float bombPopPoint_offset = 1.0f;

                // Bombインスタンスの作成
                GameObject bombInstance =
                    GlobalGameManager._instance.photonManager.CreateGameObject(
                        GlobalGameManager._instance.PATH_PREFAB + this.bombPrefab.name,
                        this.viewCamera.position + this.viewCamera.forward * bombPopPoint_offset,
                        this.viewCamera.rotation);

                // BombオブジェクトをinGameオブジェクト配下に配置
                bombInstance.transform.SetParent(this.inGameField);

                // 作成したBombに対して力を加える
                bombInstance.GetComponent<Rigidbody>().velocity =
                    this.viewCamera.forward * this.throwBombPower;

                // 連打できないようにBombの装填状態にする
                this.bombReloadState = true;
                this.bombReloaTime = 0.0f;
            }

            // Bombの再装填中の場合は経過時間を進行させる
            if (this.bombReloadState == true)
            {
                this.bombReloaTime += Time.deltaTime;
                if (this.bombReloaTime >= this.bombReloadTime_Limit)
                {
                    // Bombの装填完了
                    this.bombReloadState = false;
                    this.bombReloaTime = 0.0f;

                    //ReloadTimeオフ
                    this.sceneControl.ReloadTime_OFF();
                }
                else
                {
                    // ReloadTimeオン
                    this.sceneControl.ReloadTime_ON();


                    // 現在の進行状況をアップ
                    float reloadTime = this.bombReloaTime / this.bombReloadTime_Limit;
                    this.sceneControl.SetReloadTime(reloadTime);

                }
            }

            // プレイヤーの視点をデバッグ表示しておく
            Debug.DrawRay(this.viewCamera.position, this.viewCamera.forward * 10.0f, Color.red);
        }

    }

    // キャラクターが回転を制御
    private void RotateCharacter()
    {

        // 横の回転量をマウス値より生成する
        float y_rotate = Input.GetAxis("Mouse X") * this.mouseSpeed;

        // 度数法からの変換、横回転の終点を決定する
        this.characterRotate *= Quaternion.Euler(0.0f, y_rotate, 0.0f);

        // 現在の回転量から終点に対して、回転スピードに反って回転を行う
        transform.localRotation = Quaternion.Slerp(transform.localRotation, this.characterRotate, this.rotateSpeed * Time.deltaTime);

    }

    // カメラの回転を制御
    private void RotateCamera()
    {
        // 
        float xRotate = Input.GetAxis("Mouse Y") * this.mouseSpeed;

        // 固定で
        xRotate *= -1;

        // 度数法からの変換、縦回転の終点を決定する
        this.cameraRotate *= Quaternion.Euler(xRotate, 0.0f, 0.0f);

        // カメラのX軸角度が限界角度を超えたら限界角度に設定
        // Mathf.Clamp : param1の値をparam2、param3の範囲に補正する
        // Mathf.DeltaAngle : ２つの入力値の最小、差分値を返す 
        var resultYRot = Mathf.Clamp(
            Mathf.DeltaAngle(
                this.initCameraRotate.eulerAngles.x, 
                this.cameraRotate.eulerAngles.x),
            -this.cameraRotateLimit,
            this.cameraRotateLimit);

        // 有効角度範囲の値で補正した後、再度クオータニオンに変換する
        // ポイントは一度オイラー角に戻して再計算しているという事
        this.cameraRotate = Quaternion.Euler(resultYRot, this.cameraRotate.eulerAngles.y, this.cameraRotate.eulerAngles.z);

        // param1からparam2へparam3のパラメータ分の旋回
        this.viewCamera.localRotation =
            Quaternion.Slerp(this.viewCamera.localRotation, this.cameraRotate, this.rotateSpeed * Time.deltaTime);

    }


    private bool isGround()
    {
        bool result = false;

        // Y軸のみ参照する
        if(Mathf.Abs(this.saveCharacterPosition.y) == Mathf.Abs(this.cc.transform.position.y))
        {
            result = true;
        }

        Debug.Log(string.Format("{0} - {1}", Mathf.Abs(this.saveCharacterPosition.y), Mathf.Abs(this.cc.transform.position.y)));

        return result;
    }



    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("PlayerControl.OnCollisionEnter()");

        // 自身で作成したオブジェクトの場合のみ当たり判定を行う
        if (IsMine() == false)
            return;

        Debug.Log("PlayerControl.OnCollisionEnter() step2");


        // 自分自身で生成した場合のみ処理を継続する
        //PhotonView pv = collision.gameObject.GetComponent<PhotonView>();

        //if(pc.IsMine() == false)
        //    return;

        // "Bome"との接触、且つ状態が鬼でじゃない時、変更通知を発砲する
        if (collision.collider.tag == "Bomb" && this.oniState == false)
        {

            // 鬼変更を発砲
            this.photonView.RPC("RPC_ChangeOniMode", PhotonTargets.All, this.photonId);

            // 接触判定をとった"Bomb"は削除を依頼
            collision.gameObject.GetComponent<BombControl>().SetDestroy();
/*
            // 状態が鬼であるかを
            if (this.oniState == false)
            {

                // 鬼状態であったプレイヤーは鬼状態が解除される
                this.photonView.RPC("ChangeOniMode",PhotonTargets.Others);

                // 鬼状態では無い状態でBombに接触すると、状態が鬼に変化する
                // TODO:RPCで自分も含めて更新を実行する方針
                //ChangeOniMode();

                // TODO:一旦コメントアウト、ややこい
                //this.sceneControl.OniChange(this.photonId);

                // 接触判定を取ったBombを削除
                GlobalGameManager._instance.photonManager.DestroyGameObject(collision.gameObject);

            }
*/          
        }

    }


    /// <summary>
    /// Photon間でのパラメータ同期を行う
    /// </summary>
    /// <param name="stream">Stream.</param>
    /// <param name="info">Info.</param>
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if(stream.isWriting)
        {
            // 送信処理
            stream.SendNext(this.oniState);                 // 鬼状態フラグ
            stream.SendNext(this.scorePoint);               // スコア
            stream.SendNext(this.playerName);               // プレイヤー名
        }
        else
        {
            // 受信処理
            this.oniState = (bool)stream.ReceiveNext();     // 鬼状態フラグ
            this.scorePoint = (int)stream.ReceiveNext();    // スコア
            this.playerName = (string)stream.ReceiveNext(); // プレイヤー名
        }
    }


    /// <summary>
    /// 作成したGameObjectかを判定する
    /// </summary>
    /// <returns><c>true</c>, if mine was ised, <c>false</c> otherwise.</returns>
    public bool IsMine()
    {
        bool result = false;

        if (this.photonView != null)
            result = this.photonView.isMine;

        return result;
    }


    /// <summary>
    /// Playerに対する着色を行う
    /// </summary>
    private void PlayerPaint()
    {
        if (this.oniState)
            transform.GetComponent<Renderer>().material = this.sceneControl.colorOni;
        else
            transform.GetComponent<Renderer>().material = this.sceneControl.colorDef;
    }



    // 鬼モードの切り替えメソッドをコールする
    [PunRPC]
    public void RPC_ChangeOniMode(int newOni_PhotonId)
    {
        Debug.Log("PlayerControl.RPC_ChangeOniMode()");

        // RPCのコール対象が各端末の同じGameObjectだけである為、
        // 全体に影響を与えるメソッドをコールする
        this.sceneControl.OniChange(newOni_PhotonId);

    }



}
