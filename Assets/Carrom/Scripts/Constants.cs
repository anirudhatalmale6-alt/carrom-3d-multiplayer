using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BEKStudio{
    public class Constants{

        public static int DEFAULT_COIN = 0;  // No free coins - must use Tera Wallet
        public static float PLAY_TIME_FOR_PLAYER = 15;

        // Quick Match rule - First to 5 pucks wins
        public static int PUCKS_TO_WIN = 5;

        // Entry prices - Using WordPress API for actual payment
        // These are just for display purposes
        public static int ROOM_LONDON_ENTRY_PRICE = 10;  // Rs 10 Match
        public static int ROOM_PARIS_ENTRY_PRICE = 10;   // Disabled - same as London
        public static int ROOM_BERLIN_ENTRY_PRICE = 10;  // Disabled - same as London

        public static string ADMOB_INTERSTITIAL_ANDROID_ID = "ca-app-pub-3940256099942544/1033173712";
        public static string ADMOB_INTERSTITIAL_IOS_ID = "ca-app-pub-3940256099942544/4411468910";
        public static string ADMOB_REWARDED_ANDROID_ID = "ca-app-pub-3940256099942544/5224354917";
        public static string ADMOB_REWARDED_IOS_ID = "ca-app-pub-3940256099942544/1712485313";

        public static int REWARDED_AD_PRIZE = 0;  // Disabled - use Tera Wallet
    
        public static string SHOP_PACK_1_ID = "coin5000";
        public static string SHOP_PACK_2_ID = "coin10000";
        public static string SHOP_PACK_3_ID = "coin25000";
        public static string SHOP_PACK_4_ID = "coin50000";

        // In-app purchases disabled - use Tera Wallet instead
        public static int SHOP_PACK_1_COIN = 0;
        public static int SHOP_PACK_2_COIN = 0;
        public static int SHOP_PACK_3_COIN = 0;
        public static int SHOP_PACK_4_COIN = 0;

    }
}

