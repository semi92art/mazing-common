﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using mazing.common.Runtime.Utils;

namespace mazing.common.Runtime.SpawnPools
{
    public abstract class SpawnPoolBase<T> : ISpawnPool<T> where T : class
    {
        #region nonpublic members

        protected readonly List<T> Collection = new List<T>();
        protected          int     ItemsCount;

        #endregion

        #region api

        #region inherited interface

        public int Count => ItemsCount;

        public bool IsReadOnly => false;

        public IEnumerator<T> GetEnumerator()
        {
            return Collection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(T _Item)
        {
            if (_Item == null)
                return;
            Collection.Add(_Item);
            ItemsCount++;
        }

        public void AddRange(IEnumerable<T> _Items)
        {
            foreach (var item in _Items)
                Add(item);
        }

        public virtual void Clear()
        {
            Collection.Clear();
            ItemsCount = 0;
        }

        public bool Contains(T _Item)
        {
            return Collection.Contains(_Item);
        }

        public void CopyTo(T[] _Array, int _ArrayIndex)
        {
            Collection.CopyTo(_Array, _ArrayIndex);
        }

        public abstract bool Remove(T _Item);

        public int IndexOf(T _Item)
        {
            return Collection.IndexOf(_Item);
        }

        public void Insert(int _Index, T _Item)
        {
            if (_Item == null)
                return;
            Collection.Insert(_Index, _Item);
            Activate(Collection[_Index], false);
        }

        public virtual void RemoveAt(int _Index)
        {
            Collection.RemoveAt(_Index);
            ItemsCount--;
        }

        public T this[int _Index]
        {
            get => Collection[_Index];
            set => Collection[_Index] = value;
        }

        #endregion

        public abstract int CountActivated   { get; }
        public          int CountDeactivated => Count - CountActivated;
        public          T   FirstActive      => GetFirstOrLastActiveOrInactive(true, true);
        public          T   FirstInactive    => GetFirstOrLastActiveOrInactive(true, false);
        public          T   LastActive       => GetFirstOrLastActiveOrInactive(false, true);
        public          T   LastInactive     => GetFirstOrLastActiveOrInactive(false, false);

        public virtual void Activate(
            T          _Item,
            Func<bool> _Predicate = null,
            Action     _OnFinish  = null,
            bool       _Forced    = false)
        {
            int index = IndexOf(_Item);
            if (index == -1)
                return;
            Activate(index, _Predicate, _OnFinish);
        }

        public virtual void Deactivate(
            T          _Item,
            Func<bool> _Predicate = null,
            Action     _OnFinish  = null,
            bool       _Forced    = false)
        {
            int index = IndexOf(_Item);
            if (index == -1)
                return;
            Deactivate(index, _Predicate, _OnFinish);
        }

        public List<T> GetAllActiveItems()
        {
            return Collection.Where(IsActive).ToList();
        }

        public List<T> GetAllInactiveItems()
        {
            return Collection.Where(_Item => !IsActive(_Item)).ToList();
        }

        public void ActivateAll(bool _Forced = false)
        {
            for (int idx = 0; idx < Count; idx++)
                Activate(idx, _Forced: _Forced);
        }

        public void DeactivateAll(bool _Forced = false)
        {
            for (int idx = 0; idx < Count; idx++)
                Deactivate(idx, _Forced: _Forced);
        }

        #endregion

        #region nonpublic methods

        private void Activate(int _Index, Func<bool> _Predicate = null, Action _OnFinish = null, bool _Forced = false)
        {
            ActivateOrDeactivate(_Index, _Predicate, _OnFinish, true, _Forced);
        }

        private void Deactivate(int _Index, Func<bool> _Predicate = null, Action _OnFinish = null, bool _Forced = false)
        {
            ActivateOrDeactivate(_Index, _Predicate, _OnFinish, false, _Forced);
        }

        private void ActivateOrDeactivate(
            int        _Index,
            Func<bool> _Predicate,
            Action     _OnFinish,
            bool       _Activate,
            bool       _Forced)
        {
            if (_Predicate == null)
            {
                ActivateOrDeactivate(_Index, _Activate, _Forced);
                _OnFinish?.Invoke();
                return;
            }

            Cor.Run(Cor.WaitWhile(
                _Predicate.Invoke,
                () =>
                {
                    ActivateOrDeactivate(_Index, _Activate, _Forced);
                    _OnFinish?.Invoke();
                }));
        }

        private void ActivateOrDeactivate(int _Index, bool _Activate, bool _Forced)
        {
            if (!MathUtils.IsInRange(_Index, 0, Count - 1))
                return;
            var item = Collection[_Index];
            if (item == null)
                return;
            bool isActive = IsActive(item);
            if (isActive == _Activate && !_Forced)
                return;
            Activate(item, _Activate);
        }

        private T GetFirstOrLastActiveOrInactive(bool _First, bool _Active)
        {
            var collection = _First
                ? Collection
                : ((IEnumerable<T>) Collection).Reverse().ToList();
            for (int i = 0; i < Count; i++)
            {
                var item = collection[i];
                if (_Active ? IsActive(item) : !IsActive(item))
                    return item;
            }

            return default;
        }

        protected abstract void Activate(T _Item, bool _Active);
        protected abstract bool IsActive(T _Item);

        #endregion
    }
}