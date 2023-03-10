using System.Collections.Generic;
using System.Linq;
using mazing.common.Runtime.Helpers;
using UnityEngine.Events;
using UnityEngine.Purchasing;

namespace mazing.common.Runtime.Managers.IAP
{
    public class ProductInfo
    {
        public int         Key  { get; }
        public string      Id   { get; }
        public ProductType Type { get; }
            
        public ProductInfo(int _Key, string _Id, ProductType _Type)
        {
            Key = _Key;
            Id = _Id;
            Type = _Type;
        }
    }
    
    public abstract class ShopManagerBase : InitBase, IShopManager
    {
        #region nonpublic members
        
        protected List<ProductInfo> Products { get; private set; }

        #endregion

        #region api
 
        public UnityAction<string, string> ShowAlertDialogAction { protected get; set; }
        
        public void RegisterProductInfos(List<ProductInfo> _Products)
        {
            Products = _Products;
        }

        public abstract void         RestorePurchases();
        public abstract void         Purchase(int _Key);
        public abstract bool         RateGame();
        public abstract ShopItemArgs GetItemInfo(int       _Key);
        public abstract void         AddPurchaseAction(int _ProductKey, UnityAction _Action);
        public abstract void         AddDeferredAction(int _ProductKey, UnityAction _Action);

        #endregion

        #region nonpublic methods
        
        protected string GetProductId(int _Key)
        {
            var product = Products.FirstOrDefault(_P => _P.Key == _Key);
            if (product != null) 
                return product.Id;
            Dbg.LogError($"{nameof(UnityIapShopManagerBase)}: " +
                         $"Get Product Id failed. Product with key {_Key} does not exist");
            return string.Empty;
        }

        #endregion

    }
}