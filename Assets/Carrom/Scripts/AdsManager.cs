using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoogleMobileAds.Api;
using BEKStudio;

namespace BEKStudio{
    public class AdsManager : MonoBehaviour{
        public static AdsManager Instance;
        InterstitialAd interstitialAd;
        RewardedAd rewardedAd;


        void Awake(){
            if (Instance == null){
                Instance = this;
            }
        }

        void Start(){
#if UNITY_ANDROID || UNITY_IPHONE
            MobileAds.RaiseAdEventsOnUnityMainThread = true;

            RequestInterstitialAd();
            RequestRewardedAd();
#endif
        }

        void RequestInterstitialAd(){
            if (Application.internetReachability == NetworkReachability.NotReachable) return;

            if (interstitialAd != null){
                interstitialAd.Destroy();
                interstitialAd = null;
            }

            string adUnitId = "";
#if UNITY_ANDROID
            adUnitId = Constants.ADMOB_INTERSTITIAL_ANDROID_ID;
#else
            adUnitId = Constants.ADMOB_INTERSTITIAL_IOS_ID;
#endif
            InterstitialAd.Load(adUnitId, new AdRequest(),
                (InterstitialAd ad, LoadAdError error) => {
                    if (error != null || ad == null){
                        return;
                    }

                    interstitialAd = ad;

                    ad.OnAdFullScreenContentClosed += () => { RequestInterstitialAd(); };

                    ad.OnAdFullScreenContentFailed += (AdError error) => { RequestInterstitialAd(); };
                });
        }

        public void HandleOnAdClosed(object sender, System.EventArgs args){
            RequestInterstitialAd();
        }


        public void ShowInterstitialAd(){
            if (interstitialAd != null && interstitialAd.CanShowAd()){
                interstitialAd.Show();
            }
        }

        void RequestRewardedAd(){
            if (Application.internetReachability == NetworkReachability.NotReachable) return;

            if (rewardedAd != null){
                rewardedAd.Destroy();
                rewardedAd = null;
            }

            string adUnitId = "";
#if UNITY_ANDROID
            adUnitId = Constants.ADMOB_REWARDED_ANDROID_ID;
#else
            adUnitId = Constants.ADMOB_REWARDED_IOS_ID;
#endif

            RewardedAd.Load(adUnitId, new AdRequest(),
                (RewardedAd ad, LoadAdError error) => {
                    if (error != null || ad == null){
                        return;
                    }

                    rewardedAd = ad;
                });
        }

        public void ShowRewardedAd(){
            if (rewardedAd != null && rewardedAd.CanShowAd()){
                rewardedAd.Show((Reward reward) => {
                    PlayerPrefs.SetInt("coin", PlayerPrefs.GetInt("coin") + Constants.REWARDED_AD_PRIZE);
                    MenuController.Instance.UpdateCurrencyText();
                    RequestRewardedAd();
                });
            }
        }
    }
}