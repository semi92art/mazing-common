﻿using System.Linq;
using mazing.common.Runtime.Entities.UI;
using mazing.common.Runtime.Utils;
using UnityEngine;

namespace mazing.common.Runtime.Extensions
{
    public static class TransformExtensions
    {
        #region api 
        
        public static Transform SetPosX   (this Transform _T, float _X)
        {
            _T.position = _T.position.SetX(_X);
            return _T;
        }

        public static Transform PlusPosX  (this Transform _T, float _X)
        {
            _T.position = _T.position.PlusX(_X);
            return _T;
        }

        public static Transform MinusPosX (this Transform _T, float _X)
        {
            _T.position = _T.position.MinusX(_X);
            return _T;
        }

        public static Transform SetPosY   (this Transform _T, float _Y)
        {
            _T.position = _T.position.SetY(_Y);
            return _T;
        }

        public static Transform      PlusPosY       (this Transform _T, float _Y)
        {
            _T.position = _T.position.PlusY(_Y);
            return _T;
        }

        public static Transform MinusPosY      (this Transform _T, float _Y)
        {
            _T.position = _T.position.MinusY(_Y);
            return _T;
        }

        public static Transform      SetPosZ        (this Transform _T, float _Z)
        {
            _T.position = _T.position.SetZ(_Z);
            return _T;
        }

        public static Transform      PlusPosZ       (this Transform _T, float _Z)
        {
            _T.position = _T.position.PlusZ(_Z);
            return _T;
        }

        public static Transform MinusPosZ      (this Transform _T, float _Z)
        {
            _T.position = _T.position.MinusZ(_Z);
            return _T;
        }

        public static Transform SetPosXY       (this Transform _T, float _X, float _Y)
        {
            _T.position = _T.position.SetX(_X).SetY(_Y);
            return _T;
        }

        public static Transform SetPosXY       (this Transform _T, Vector2 _XY)
        {
            _T.position = _T.position.SetX(_XY.x).SetY(_XY.y);
            return _T;
        }

        public static Transform PlusPosXY      (this Transform _T, float _X, float _Y)
        {
            _T.position = _T.position.PlusX(_X).PlusY(_Y);
            return _T;
        }

        public static Transform MinusPosXY     (this Transform _T, Vector2 _XY)
        {
            _T.position = _T.position.MinusX(_XY.x).MinusY(_XY.y);
            return _T;
        }

        public static Transform SetLocalPosX   (this Transform _T, float _X)
        {
            _T.localPosition = _T.localPosition.SetX(_X);
            return _T;
        }

        public static Transform PlusLocalPosX  (this Transform _T, float _X)
        {
            _T.localPosition = _T.localPosition.PlusX(_X);
            return _T;
        }

        public static Transform MinusLocalPosX (this Transform _T, float _X)
        {
            _T.localPosition = _T.localPosition.MinusX(_X);
            return _T;
        }

        public static Transform SetLocalPosY   (this Transform _T, float _Y)
        {
            _T.localPosition = _T.localPosition.SetY(_Y);
            return _T;
        }

        public static Transform PlusLocalPosY  (this Transform _T, float _Y)
        {
            _T.localPosition = _T.localPosition.PlusY(_Y);
            return _T;
        }

        public static Transform MinusLocalPosY (this Transform _T, float _Y)
        {
            _T.localPosition = _T.localPosition.MinusY(_Y);
            return _T;
        }

        public static Transform SetLocalPosZ   (this Transform _T, float _Z)
        {
            _T.localPosition = _T.localPosition.SetZ(_Z);
            return _T;
        }

        public static Transform PlusLocalPosZ  (this Transform _T, float _Z)
        {
            _T.localPosition = _T.localPosition.PlusZ(_Z);
            return _T;
        }

        public static Transform MinusLocalPosZ (this Transform _T, float _Z)
        {
            _T.localPosition = _T.localPosition.MinusZ(_Z);
            return _T;
        }

        public static Transform SetLocalPosXY  (this Transform _T, float _X, float _Y)
        {
            _T.localPosition = _T.localPosition.SetX(_X).SetY(_Y);
            return _T;
        }

        public static Transform SetLocalPosXY  (this Transform _T, Vector2 _XY)
        {
            _T.localPosition = _T.localPosition.SetXY(_XY);
            return _T;
        }

        public static Transform SetLocalScaleXY(this Transform _T, Vector2 _XY)
        {
            _T.localScale = (Vector3)_XY + Vector3.forward;
            return _T;
        }

        public static Transform PlusLocalPosXY (this Transform _T, float _X, float _Y)
        {
            _T.localPosition = _T.localPosition.PlusX(_X).PlusY(_Y);
            return _T;
        }

        public static Transform PlusLocalPosXY (this Transform _T, Vector2 _XY)
        {
            _T.localPosition = _T.localPosition.PlusX(_XY.x).PlusY(_XY.y);
            return _T;
        }

        public static Transform MinusLocalPosXY(this Transform _T, float _X, float _Y)
        {
            _T.localPosition = _T.localPosition.MinusX(_X).MinusY(_Y);
            return _T;
        }

        public static Transform MinusLocalPosXY(this Transform _T, Vector2 _XY)
        {
            _T.localPosition = _T.localPosition.MinusX(_XY.x).MinusY(_XY.y);
            return _T;
        }

        public static Transform LookAt2D(this Transform _T, Vector2 _To)
        {
            _T.eulerAngles = DirectionEulerAngles(_T.transform.position, _To);
            return _T;
        }
        
        public static Transform SetParentEx(this Transform _Item, Transform _Parent)
        {
            _Item.SetParent(_Parent);
            return _Item;
        }

        public static bool IsFullyVisibleFrom(this RectTransform _Item, RectTransform _Rect)
        {
            if (!_Item.gameObject.activeInHierarchy)
                return false;
            return _Item.CountCornersVisibleFrom(_Rect) == 4;
        }
        
        public static bool IsVisibleFrom(this RectTransform _Item, RectTransform _Rect)
        {
            if (!_Item.gameObject.activeInHierarchy)
                return false;
            return _Item.CountCornersVisibleFrom(_Rect) > 0;
        }

        public static RectTransform SetParams(this RectTransform _Item, RectTransformLite _Params)
        {
            _Item.SetParams(
                _Params.Anchor ?? default,
                _Params.AnchoredPosition ?? default,
                _Params.Pivot ?? default,
                _Params.SizeDelta ?? default);
            return _Item;
        }

        public static RectTransform SetParams(
            this RectTransform _Item,
            UiAnchor           _Anchor,
            Vector2            _AnchoredPosition,
            Vector2            _Pivot,
            Vector2            _SizeDelta)
        {
            _Item.anchorMin = _Anchor.Min;
            _Item.anchorMax = _Anchor.Max;
            _Item.anchoredPosition = _AnchoredPosition;
            _Item.pivot = _Pivot;
            _Item.sizeDelta = _SizeDelta;
            _Item.localScale = Vector3.one;
            return _Item;
        }

        
        #endregion
        
        #region nonpublic methods

        private static Vector3 DirectionEulerAngles(Vector2 _From, Vector2 _To) => Vector3.forward * GeometryUtils.ZAngle(_From, _To);
        
        private static int CountCornersVisibleFrom(this RectTransform _Item, RectTransform _Rect)
        {
            var itemCorners = new Vector3[4];
            _Item.GetWorldCorners(itemCorners);
            var rectCorners = new Vector3[4];
            _Rect.GetWorldCorners(rectCorners);
            var polygon = rectCorners.Select(_P => new Vector2(_P.x, _P.y)).ToArray();
            return itemCorners.Select(_Corner => new Vector2(_Corner.x, _Corner.y))
                .Count(_Point => GeometryUtils.IsPointInPolygon(polygon, _Point));
        }

        #endregion
    }
}