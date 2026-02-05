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

        // Corner hole positions (local coordinates)
        private static readonly Vector2[] holePositions = new Vector2[] {
            new Vector2(2.03f, 2.06f),    // Top-Right
            new Vector2(-2.05f, 2.06f),   // Top-Left
            new Vector2(2.04f, -2.05f),   // Bottom-Right
            new Vector2(-2.05f, -2.05f)   // Bottom-Left
        };

        IEnumerator ShootBotCoroutine(){
            Debug.Log("=== BOT TURN START ===");

            // Reset striker
            transform.rotation = Quaternion.Euler(0, 0, 90);
            float strikerY = -puckStartPos.y;
            transform.localPosition = new Vector2(0, strikerY);

            // Enable physics
            GetComponent<CircleCollider2D>().enabled = true;
            rb.velocity = Vector2.zero;
            rb.simulated = true;
            spriteRenderer.color = new Color32(255, 255, 255, 255);
            lastVel = Vector2.zero;
            isMoving = false;
            forceMultiplier = 0;

            yield return new WaitForSeconds(0.1f);

            float minX = GameController.Instance.playerPuckMinX;
            float maxX = GameController.Instance.playerPuckMaxX;

            // Find BLACK pucks
            Puck[] blackPucks = GameController.Instance.allPucks
                .Where(x => x.CompareTag("Black") && x.gameObject.activeInHierarchy).ToArray();

            if (blackPucks.Length == 0){
                Debug.Log("BOT: No black pucks!");
                yield break;
            }

            // Find the BEST shot - one that will POT a puck into a hole
            Puck bestPuck = null;
            Vector2 bestHole = Vector2.zero;
            float bestStrikerX = 0f;
            float bestScore = float.MinValue;

            foreach (Puck puck in blackPucks){
                Vector2 puckPos = puck.transform.localPosition;

                // Check each hole for a potential pot
                foreach (Vector2 hole in holePositions){
                    // Direction from puck to hole
                    Vector2 puckToHole = (hole - puckPos).normalized;

                    // Where should striker hit the puck from?
                    // Opposite side of where we want puck to go
                    Vector2 calcHitPoint = puckPos - puckToHole * 0.15f;

                    // Where should striker be positioned to hit that point?
                    // Striker is at top (positive Y), needs to shoot downward
                    // For a straight shot, striker X should align with hit point X
                    float neededStrikerX = calcHitPoint.x;

                    // Check if striker can reach this position
                    if (neededStrikerX < minX || neededStrikerX > maxX){
                        continue; // Can't make this shot
                    }

                    // Calculate shot angle
                    Vector2 strikerPos = new Vector2(neededStrikerX, strikerY);
                    Vector2 shotDir = (calcHitPoint - strikerPos).normalized;

                    // Score this shot
                    float score = 0f;

                    // Prefer shots where puck is close to hole
                    float puckToHoleDist = Vector2.Distance(puckPos, hole);
                    score += (5f - puckToHoleDist) * 20f; // Closer = better

                    // BOT is at TOP (positive Y), so prefer BOTTOM holes (negative Y)
                    // This makes for straighter, easier shots
                    if (hole.y < 0){
                        score += 80f; // Big bonus for bottom holes
                    }

                    // Prefer pucks that are BELOW the striker (easier to hit)
                    if (puckPos.y < strikerY){
                        score += 30f;
                    }

                    // Prefer straight shots (striker Y aligned with puck)
                    float angleToHit = Vector2.Angle(Vector2.down, shotDir);
                    score -= angleToHit * 2f; // Straighter = much better

                    // Prefer pucks in striker's direct line
                    float xDiff = Mathf.Abs(puckPos.x - neededStrikerX);
                    score -= xDiff * 5f;

                    // Bonus for pucks already near holes
                    if (puckToHoleDist < 1f) score += 50f;
                    if (puckToHoleDist < 0.5f) score += 150f;

                    if (score > bestScore){
                        bestScore = score;
                        bestPuck = puck;
                        bestHole = hole;
                        bestStrikerX = neededStrikerX;
                    }
                }
            }

            // Fallback: just hit closest puck toward nearest hole
            if (bestPuck == null){
                float closestDist = float.MaxValue;
                foreach (Puck puck in blackPucks){
                    float dist = Mathf.Abs(puck.transform.localPosition.x);
                    if (dist < closestDist && puck.transform.localPosition.x >= minX && puck.transform.localPosition.x <= maxX){
                        closestDist = dist;
                        bestPuck = puck;
                        bestStrikerX = Mathf.Clamp(puck.transform.localPosition.x, minX, maxX);
                        // Find nearest hole
                        float nearestHoleDist = float.MaxValue;
                        foreach (Vector2 h in holePositions){
                            float d = Vector2.Distance(puck.transform.localPosition, h);
                            if (d < nearestHoleDist){
                                nearestHoleDist = d;
                                bestHole = h;
                            }
                        }
                    }
                }
            }

            if (bestPuck == null){
                bestPuck = blackPucks[0];
                bestStrikerX = Mathf.Clamp(bestPuck.transform.localPosition.x, minX, maxX);
                bestHole = holePositions[0];
            }

            Debug.Log("BOT: Target=" + bestPuck.name + " Hole=" + bestHole + " Score=" + bestScore);

            // Thinking delay
            yield return new WaitForSeconds(0.3f);

            // Move slider
            opponentSliderValue = Mathf.InverseLerp(minX, maxX, bestStrikerX);
            if (GameController.Instance.opponentSlider != null){
                GameController.Instance.opponentSlider.value = opponentSliderValue;
            }

            // Move striker
            Vector2 startPos = transform.localPosition;
            Vector2 endPos = new Vector2(bestStrikerX, strikerY);
            float moveTime = 0.25f;
            float elapsed = 0f;

            while (elapsed < moveTime){
                elapsed += Time.deltaTime;
                transform.localPosition = Vector2.Lerp(startPos, endPos, elapsed / moveTime);
                yield return null;
            }
            transform.localPosition = endPos;

            yield return new WaitForSeconds(0.15f);

            // Use LOCAL coordinates for everything (same coordinate space as pucks)
            Vector2 puckLocal = bestPuck.transform.localPosition;
            Vector2 holeLocal = bestHole;

            // Direction puck needs to travel to reach hole (in local space)
            Vector2 puckToHoleDir = (holeLocal - puckLocal).normalized;

            // IMPROVED AIM: Hit point should be directly behind the puck relative to hole
            // The striker needs to hit the puck on the EXACT opposite side from the hole
            // Using a smaller offset (0.08) for more precise contact
            Vector2 hitPointLocal = puckLocal - puckToHoleDir * 0.08f;

            // Current striker position (local)
            Vector2 strikerLocal = transform.localPosition;

            // For PRECISE aiming: aim THROUGH the puck center toward the hole
            // This ensures the puck goes directly to hole
            Vector2 aimDir = puckToHoleDir; // Aim in the same direction puck needs to go

            float angle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);

            Debug.Log("BOT: puck=" + puckLocal + " hole=" + holeLocal + " aimDir=" + aimDir + " angle=" + angle);

            // Show arrow
            arrow.SetActive(true);
            arrow.transform.localScale = new Vector3(1.2f, 1.2f, 1);
            yield return new WaitForSeconds(0.1f);
            arrow.SetActive(false);

            // REDUCED FORCE - gentle push to guide puck into hole
            float distToHole = Vector2.Distance(puckLocal, holeLocal);
            float distToPuck = Vector2.Distance(strikerLocal, puckLocal);

            // Lower force = smoother pot, puck stays with striker direction
            // Close pucks need less force, far pucks need slightly more
            forceMultiplier = Mathf.Clamp(0.35f + distToHole * 0.08f + distToPuck * 0.02f, 0.4f, 0.65f);
            float finalForce = forceMultiplier * force;

            Debug.Log("BOT: FIRE! force=" + finalForce + " distToHole=" + distToHole);

            rb.AddForce(transform.right * finalForce, ForceMode2D.Impulse);
            GameController.Instance.Shoot();

            Debug.Log("=== BOT SHOT FIRED ===");
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