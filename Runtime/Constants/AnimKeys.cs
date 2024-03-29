﻿using UnityEngine;

namespace mazing.common.Runtime.Constants
{
    public static class AnimKeys
    {
        public static int Anim { get; }
        public static int Anim2 { get; }
        public static int Anim3 { get; }
        public static int Anim4 { get; }
        public static int Anim5 { get; }
        public static int Anim6 { get; }

        public static int Stop { get; }
        public static int Stop2 { get; }
        public static int State { get; }

        static AnimKeys()
        {
            Anim  = Animator.StringToHash("anim");
            Anim2 = Animator.StringToHash("anim2");
            Anim3 = Animator.StringToHash("anim3");
            Anim4 = Animator.StringToHash("anim4");
            Anim5 = Animator.StringToHash("anim5");
            Anim6 = Animator.StringToHash("anim6");
            Stop  = Animator.StringToHash("stop");
            Stop2 = Animator.StringToHash("stop2");
            State = Animator.StringToHash("state");
        }
    }
}