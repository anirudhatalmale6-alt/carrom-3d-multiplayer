using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BEKStudio{
    public class Puck : MonoBehaviourPun, IPunObservable{
        public PhotonView photonView;
        Rigidbody2D rb;
        SpriteRenderer spriteRenderer;
        Vector2 puckStartPos;
        Vector2 lastVel;
        public bool practiceMode;
        public bool isMoving;

        void Start(){
            rb = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            puckStartPos = transform.localPosition;

            lastVel = Vector2.zero;
            isMoving = false;

            if (practiceMode){
                ResetPosition();
            }
        }

        void Update(){
            isMoving = lastVel != Vector2.zero;
        }

        void FixedUpdate(){
            if (photonView.IsMine){
                lastVel = rb.velocity;
            }
        }

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info){
            if (stream.IsWriting){
                stream.SendNext(rb.velocity);
                stream.SendNext(rb.simulated);
                stream.SendNext(spriteRenderer.color.a);
            } else {
                lastVel = (Vector2)stream.ReceiveNext();
                rb.simulated = (bool)stream.ReceiveNext();
                spriteRenderer.color = new Color(1, 1, 1, (float)stream.ReceiveNext());

            }
        }

        void OnTriggerEnter2D(Collider2D col){
            if (col.CompareTag("Hole")){
                GetComponent<CircleCollider2D>().enabled = false;
                rb.velocity = Vector2.zero;
                rb.simulated = false;
                lastVel = Vector2.zero;
                isMoving = false;
                
                LeanTween.color(gameObject, new Color32(255, 255, 255, 0), 0.2f);
                
                if (!photonView.IsMine) return;
                GetComponent<PhotonView>().RPC("PuckOnHole", RpcTarget.AllBuffered, gameObject.tag, gameObject.name);
            }
        }

        public void ResetPosition(){
            if (practiceMode){
                transform.localPosition = new Vector2(Random.Range(-1.89f, 1.89f), Random.Range(-1.16f, 1.16f));
            } else {
                if (photonView.IsMine){
                    transform.localPosition = puckStartPos;
                }
            }

            GetComponent<CircleCollider2D>().enabled = true;
            rb.velocity = Vector2.zero;
            rb.simulated = true;
            spriteRenderer.color = new Color32(255, 255, 255, 255);
            lastVel = Vector2.zero;
            isMoving = false;
        }

        [PunRPC]
        void PuckOnHole(string puckTag, string puckName){
            GameController.Instance.PuckOnHole(puckTag, puckName);
        }

        void OnCollisionEnter2D(Collision2D col){
            if (col.gameObject.CompareTag("Player")){
                if (!GetComponent<AudioSource>().isPlaying){
                    GetComponent<AudioSource>().Play();
                }
            }
        }

    }
}
