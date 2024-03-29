﻿

using UnityEngine;

namespace mazing.common.Runtime.Helpers
{
    public class AdProviderInfo
    {
        public string          Source   { get; set; }
        public float           ShowRate { get; set; }
        public bool            Enabled  { get; set; }
        public RuntimePlatform Platform { get; set; }
    }
}