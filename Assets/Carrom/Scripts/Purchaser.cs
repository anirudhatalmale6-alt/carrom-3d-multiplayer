using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;

namespace BEKStudio{
    public class Purchaser : MonoBehaviour, IStoreListener {
        public static Purchaser Instance;
        private static IStoreController m_StoreController;
        private static IExtensionProvider m_StoreExtensionProvider;


        void Awake() {
            if (Instance == null) {
                Instance = this;
            }
        }

        void Start() {
            if (m_StoreController == null) {
                InitializePurchasing();
            }
        }

        public void InitializePurchasing() {
            if (IsInitialized()) {
                return;
            }

            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
            builder.AddProduct(Constants.SHOP_PACK_1_ID, ProductType.Consumable);
            builder.AddProduct(Constants.SHOP_PACK_2_ID, ProductType.Consumable);
            builder.AddProduct(Constants.SHOP_PACK_3_ID, ProductType.Consumable);
            builder.AddProduct(Constants.SHOP_PACK_4_ID, ProductType.Consumable);

            UnityPurchasing.Initialize(this, builder);
        }


        private bool IsInitialized() {
            return m_StoreController != null && m_StoreExtensionProvider != null;
        }

        public void BuyConsumable(int id) {
            if (id == 0) {
                BuyProductID(Constants.SHOP_PACK_1_ID);
            } else if (id == 1) {
                BuyProductID(Constants.SHOP_PACK_2_ID);
            } else if (id == 2) {
                BuyProductID(Constants.SHOP_PACK_3_ID);
            } else if (id == 3) {
                BuyProductID(Constants.SHOP_PACK_4_ID);
            }
        }

        void BuyProductID(string productId) {
            if (IsInitialized()) {
                Product product = m_StoreController.products.WithID(productId);
                if (product != null && product.availableToPurchase) {
                    Debug.Log(string.Format("Purchasing product asychronously: '{0}'", product.definition.id));
                    m_StoreController.InitiatePurchase(product);
                } else {
                    Debug.Log("BuyProductID: FAIL. Not purchasing product, either is not found or is not available for purchase");
                }
            } else {
                Debug.Log("BuyProductID FAIL. Not initialized.");
            }
        }

        public void RestorePurchases() {
            if (!IsInitialized()) {
                Debug.Log("RestorePurchases FAIL. Not initialized.");
                return;
            }

            if (Application.platform == RuntimePlatform.IPhonePlayer ||
                Application.platform == RuntimePlatform.OSXPlayer) {
                Debug.Log("RestorePurchases started ...");

                var apple = m_StoreExtensionProvider.GetExtension<IAppleExtensions>();
                apple.RestoreTransactions((result) => {
                    Debug.Log("RestorePurchases continuing: " + result + ". If no further messages, no purchases available to restore.");
                });
            } else {
                Debug.Log("RestorePurchases FAIL. Not supported on this platform. Current = " + Application.platform);
            }
        }

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions) {
            Debug.Log("OnInitialized: PASS");
            m_StoreController = controller;
            m_StoreExtensionProvider = extensions;
        }

        public void OnInitializeFailed(InitializationFailureReason error) {
            Debug.Log("OnInitializeFailed InitializationFailureReason:" + error);
        }

        public void OnInitializeFailed(InitializationFailureReason error, string msg) {
            Debug.Log("OnInitializeFailed InitializationFailureReason:" + error);
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args) {
            if (string.Equals(args.purchasedProduct.definition.id, Constants.SHOP_PACK_1_ID, System.StringComparison.Ordinal)) {
                Debug.Log(string.Format("ProcessPurchase: PASS. Product: '{0}'", args.purchasedProduct.definition.id));
                PlayerPrefs.SetInt("coin", PlayerPrefs.GetInt("coin") + Constants.SHOP_PACK_1_COIN);
                PlayerPrefs.Save();
                MenuController.Instance.UpdateCurrencyText();
            } else if (string.Equals(args.purchasedProduct.definition.id, Constants.SHOP_PACK_2_ID, System.StringComparison.Ordinal)) {
                Debug.Log(string.Format("ProcessPurchase: PASS. Product: '{0}'", args.purchasedProduct.definition.id));
                PlayerPrefs.SetInt("coin", PlayerPrefs.GetInt("coin") + Constants.SHOP_PACK_2_COIN);
                PlayerPrefs.Save();
                MenuController.Instance.UpdateCurrencyText();
            } else if (string.Equals(args.purchasedProduct.definition.id, Constants.SHOP_PACK_3_ID, System.StringComparison.Ordinal)) {
                Debug.Log(string.Format("ProcessPurchase: PASS. Product: '{0}'", args.purchasedProduct.definition.id));
                PlayerPrefs.SetInt("coin", PlayerPrefs.GetInt("coin") + Constants.SHOP_PACK_3_COIN);
                PlayerPrefs.Save();
                MenuController.Instance.UpdateCurrencyText();
            } else if (string.Equals(args.purchasedProduct.definition.id, Constants.SHOP_PACK_4_ID, System.StringComparison.Ordinal)) {
                Debug.Log(string.Format("ProcessPurchase: PASS. Product: '{0}'", args.purchasedProduct.definition.id));
                PlayerPrefs.SetInt("coin", PlayerPrefs.GetInt("coin") + Constants.SHOP_PACK_4_COIN);
                PlayerPrefs.Save();
                MenuController.Instance.UpdateCurrencyText();
            } else {
                Debug.Log(string.Format("ProcessPurchase: FAIL. Unrecognized product: '{0}'", args.purchasedProduct.definition.id));
            }
            return PurchaseProcessingResult.Complete;
        }


        public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason) {
            Debug.Log(string.Format("OnPurchaseFailed: FAIL. Product: '{0}', PurchaseFailureReason: {1}", product.definition.storeSpecificId, failureReason));
        }
    }
}