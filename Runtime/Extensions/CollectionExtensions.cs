﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace mazing.common.Runtime.Extensions
{
    public static class CollectionExtensions
    {
        public static ICollection<T> RemoveRange<T>(this ICollection<T> _Collection, IEnumerable<T> _Items)
        {
            var result = _Collection.ToList();
            foreach (var item in _Items)
                result.Remove(item);
            return result;
        }
        
        public static List<T> Clone<T>(this IEnumerable<T> _Collection) where T: ICloneable
        {
            return _Collection.Select(_Item => (T)_Item.Clone()).ToList();
        }

        public static void Shuffle<T>(this IList<T> _List)  
        {  
            var rng = new Random(); 
            int n = _List.Count;  
            while (n > 1) {  
                n--;  
                int k = rng.Next(n + 1);  
                T value = _List[k];  
                _List[k] = _List[n];  
                _List[n] = value;  
            }  
        }

        public static Dictionary<T1, T2> ConcatWithDictionary<T1, T2>(
            this Dictionary<T1, T2> _Dictionary1, 
            Dictionary<T1, T2> _Dictionary2)
        {
            return new[] {_Dictionary1, _Dictionary2}
                .SelectMany(_Dict => _Dict)
                .ToDictionary(
                    _Kvp => _Kvp.Key,
                    _Kvp => _Kvp.Value);
        }

        public static Dictionary<TKey, TValue> CloneAlt<TKey, TValue>
            (this Dictionary<TKey, TValue> _Original) where TValue : struct
        {
            var res = new Dictionary<TKey, TValue>(_Original.Count,
                _Original.Comparer);
            foreach (var entry in _Original)
            {
                res.Add(entry.Key, entry.Value);
            }
            return res;
        }

        public static void SetSafe<T1, T2>(this Dictionary<T1, T2> _Dict, T1 _Key, T2 _Value)
        {
            if (_Dict == null)
                return;
            if (!_Dict.ContainsKey(_Key))
                _Dict.Add(_Key, _Value);
            else _Dict[_Key] = _Value;
        }

        public static T2 GetSafe<T1, T2>(this Dictionary<T1, T2> _Dict, T1 _Key, out bool _ContainsKey)
        {
            if (_Dict == null)
            {
                _ContainsKey = false;
                return default;
            }
            _ContainsKey = _Dict.ContainsKey(_Key);
            return _ContainsKey ? _Dict[_Key] : default;
        }

        public static void RemoveSafe<T1, T2>(this Dictionary<T1, T2> _Dict, T1 _Key, out bool _ContainsKey)
        {
            if (_Dict == null)
            {
                _ContainsKey = false;
                return;
            }
            _ContainsKey = _Dict.ContainsKey(_Key);
            if (_ContainsKey)
                _Dict.Remove(_Key);
        }

        public static bool ContainsAlt<T>(this T[] _Array, T _Item)
        {
            for (int i = 0; i < _Array.Length; i++)
            {
                if (_Array[i].Equals(_Item))
                    return true;
            }
            return false;
        }

        public static bool NullOrEmpty<T>(this IEnumerable<T> _Collection)
        {
            return _Collection == null || !_Collection.Any();
        }

        public static string ToStringForDisplay<T>(this IEnumerable<T> _Collection)
        {
            var collectionCopy = _Collection.ToList();
            var sb = new StringBuilder();
            sb.AppendLine("Count: " + collectionCopy.Count + ", values: ");
            foreach (var collectionValue in collectionCopy)
                sb.AppendLine(collectionValue.ToString());
            return sb.ToString();
        }
    }
}