using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Photon.Pun;
using BEKStudio;

namespace BEKStudio{
    public class PlayerPuck : MonoBehaviourPun, IPunObservable{
        public static PlayerPuck Instance;
        Rigidbody2D rb;
        SpriteRenderer spriteRenderer;
        PhotonView photonView;
        public PhysicsMaterial2D physicMaterial;
        public Vector2 startPos;
        public Vector2 endPos;
        public bool isTouch;
        float opponentSliderValue;
        Vector2 puckStartPos;
        public GameObject arrow;
        public float force;
        float forceMultiplier;
        Vector2 lastVel;
        public bool isMoving;
        private Vector2 arrowLocalScale;
        public GameObject tutorial;
        public float tutorialShowDelay = 2;


        void Awake(){
            if (Instance == null){
                Instance = this;
            }
        }

        void Start(){
            rb = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            photonView = GetComponent<PhotonView>();

            puckStartPos = transform.localPosition;

            opponentSliderValue = 0.5f;

            lastVel = Vector2.zero;
            isMoving = false;
            arrowLocalScale = arrow.transform.localScale;
        }

        void Update(){
            isMoving = lastVel != Vector2.zero;

            if (GameController.Instance.opponentSlider != null && !PhotonController.Instance.playWithBot){
                GameController.Instance.opponentSlider.value = Mathf.MoveTowards(
                    GameController.Instance.opponentSlider.value, opponentSliderValue, 5 * Time.deltaTime);
            }

            if (photonView.IsMine){
                if (!PhotonController.Instance.playWithBot) {
                    opponentSliderValue = 0.5f;
                }

                if (rb.sharedMaterial == null){
                    rb.sharedMaterial = physicMaterial;
                }

                if (arrow.activeInHierarchy){
                    arrow.transform.localScale =
                        Vector2.MoveTowards(arrow.transform.localScale, arrowLocalScale, 5 * Time.deltaTime);
                }

                if (!isTouch && GameController.Instance.gameState == GameController.GameState.READY){
                    if (tutorialShowDelay > 0){
                        tutorialShowDelay -= 1 * Time.deltaTime;
                    }
                    else{
                        if (!tutorial.activeInHierarchy && GameController.Instance.whichPlayer == GameController.WhichPlayer.ME){
                            tutorial.SetActive(true);
                        }
                    }
                }
            }
            else{
                tutorial.SetActive(false);

                if (rb.sharedMaterial != null){
                    rb.sharedMaterial = null;
                }

                if (arrow.activeInHierarchy){
                    arrow.transform.localScale = Vector2.MoveTowards(arrow.transform.localScale, arrowLocalScale,
                        50 * Time.deltaTime);
                }

                return;
            }

            if (isTouch){
                endPos = Input.mousePosition;
                
                forceMultiplier = Mathf.InverseLerp(0, 500, (endPos - startPos).magnitude);

                if (forceMultiplier >= 0.2f){
                    arrow.GetComponent<SpriteRenderer>().enabled = true;
                }
                else{
                    arrow.GetComponent<SpriteRenderer>().enabled = false;
                }

                arrow.transform.localScale = new Vector3(forceMultiplier * 2.3f, forceMultiplier * 2.3f, 1);

                float AngleRad = Mathf.Atan2(-(endPos.y - startPos.y), -(endPos.x - startPos.x));
                float AngleDeg = (180 / Mathf.PI) * AngleRad;
                transform.rotation = Quaternion.Euler(0, 0, AngleDeg);
            }
        }

        void FixedUpdate(){
            if (photonView.IsMine){
                lastVel = rb.velocity;
            }
        }

        public void OnMouseDown(){
            if (!photonView.IsMine || GameController.Instance.gameState != GameController.GameState.READY || GameController.Instance.whichPlayer != GameController.WhichPlayer.ME) return;


            arrow.transform.localScale = Vector2.zero;
            arrow.SetActive(true);
            startPos = Input.mousePosition;
            endPos = startPos;

            isTouch = true;

            tutorial.SetActive(false);
            tutorialShowDelay = 2;
        }

        public void OnMouseUp(){
            if (!photonView.IsMine || GameController.Instance.gameState != GameController.GameState.READY || GameController.Instance.whichPlayer != GameController.WhichPlayer.ME) return;

            arrow.SetActive(false);
            isTouch = false;

            if (forceMultiplier >= 0.2f){
                photonView.RPC("Shoot", RpcTarget.AllBuffered);
            }
        }

        [PunRPC]
        void Shoot(){
            if (photonView.IsMine){
                rb.AddForce(transform.right * (forceMultiplier * force), ForceMode2D.Impulse);
            }

            GameController.Instance.Shoot();
        }

        public void ResetPosition(){
            transform.rotation = Quaternion.Euler(0, 0, 90);
            if (PhotonController.Instance.playWithBot){
                if (GameController.Instance.whichPlayer == GameController.WhichPlayer.OTHER){
                    arrowLocalScale = Vector2.zero;
                    arrow.transform.localScale = Vector2.zero;
                    transform.localPosition = new Vector2(puckStartPos.x, -puckStartPos.y);
                } else {
                    transform.localPosition = puckStartPos;
                }
            } else {
                if (photonView.IsMine){
                    transform.localPosition = puckStartPos;
                }
            }
        }

        public void EnablePuckPhysic() {
            float t = photonView.IsMine ? 0.3f : 1f;

            LeanTween.value(0, 1, t).setOnComplete(() => {
                GetComponent<CircleCollider2D>().enabled = true;
                rb.velocity = Vector2.zero;
                rb.simulated = true;
                spriteRenderer.color = new Color32(255, 255, 255, 255);
                lastVel = Vector2.zero;
                isMoving = false;
            });
        }

        public void ShootBot(){
            StartCoroutine(ShootBotCoroutine());
        }

        IEnumerator ShootBotCoroutine(){
            EnablePuckPhysic();

            arrowLocalScale = Vector2.zero;
            arrow.transform.localScale = Vector2.zero;
            transform.localPosition = new Vector2(puckStartPos.x, -puckStartPos.y);
            Vector2 newPos = transform.position;
            forceMultiplier = 0;
            arrow.SetActive(true);
            
            Puck[] blackPucks = GameController.Instance.allPucks.Where(x => x.CompareTag("Black") && x.gameObject.activeInHierarchy).ToArray();
            Puck target = blackPucks[Random.Range(0, blackPucks.Length)];

            float randExtraXPos = Random.Range(0f, 1f);
            if (target.transform.position.x > 0){
                newPos.x += randExtraXPos;
            } else {
                newPos.x -= randExtraXPos;
            }

            yield return new WaitForSeconds(Random.Range(1.5f, 4f));

            opponentSliderValue = Mathf.InverseLerp(GameController.Instance.playerPuckMinX, GameController.Instance.playerPuckMaxX, newPos.x);
            LeanTween.value(0.5f, opponentSliderValue, 0.5f).setOnUpdate((float val) => {
                GameController.Instance.opponentSlider.value = val;
            });

            LeanTween.move(gameObject, newPos, 0.5f).setOnComplete(() => {
                Vector2 diff = target.transform.position - transform.position;
                diff.Normalize();  
                float zRot = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, zRot);
                
                LeanTween.value(0, 1f, 1f).setOnUpdate((float val) => {
                    arrow.transform.localScale = new Vector3(val * 2.3f, val * 2.3f, 1);
                }).setOnComplete(() => {
                    arrow.SetActive(false);
                    forceMultiplier = 1f;
                    photonView.RPC("Shoot", RpcTarget.AllBuffered);
                });
            });
        }

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info){
            if (stream.IsWriting){
                stream.SendNext(rb.velocity);
                stream.SendNext(GameController.Instance.playerSlider.value);
                stream.SendNext(arrow.activeInHierarchy);
                stream.SendNext(arrow.transform.localScale);
                stream.SendNext(arrow.transform.localRotation);
            } else {
                lastVel = (Vector2)stream.ReceiveNext();
                opponentSliderValue = (float)stream.ReceiveNext();
                arrow.SetActive((bool)stream.ReceiveNext());
                arrowLocalScale = (Vector3)stream.ReceiveNext();
                arrow.transform.localRotation = (Quaternion)stream.ReceiveNext();
            }
        }

        void OnTriggerEnter2D(Collider2D col){
            if (col.CompareTag("Hole")){
                GetComponent<CircleCollider2D>().enabled = false;
                rb.velocity = Vector2.zero;
                rb.simulated = false;
                lastVel = Vector2.zero;
                isMoving = false;
                
                LeanTween.color(gameObject, new Color32(255, 255, 255, 0), 0.2f).setOnComplete(() => {
                    if (!photonView.IsMine) return;
                    
                    photonView.RPC("PuckOnHole", RpcTarget.AllBuffered, gameObject.tag, gameObject.name);
                });
            }
        }

        [PunRPC]
        void PuckOnHole(string puckTag, string puckName){
            GameController.Instance.PuckOnHole(puckTag, puckName);
        }

        void OnDisable() {
            LeanTween.cancelAll();
        }

    }
}