﻿using UnityEngine;
using UnityEngine.Events;

namespace mazing.common.Runtime.Helpers
{
    public abstract class MonoBehInitBase : MonoBehaviour, IInit
    {
        public bool              Initialized { get; private set; }
        public event UnityAction Initialize;
        
        public virtual void Init()
        {
            Initialize?.Invoke();
            Initialized = true;
        }
    }
}