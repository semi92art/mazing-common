﻿using System.Collections.Generic;
using mazing.common.Runtime.Entities;
using UnityEngine.Events;

namespace mazing.common.Runtime.Settings
{
    public interface ISetting<T>
    {
        event UnityAction<T> ValueSet;
        SaveKey<T>           Key          { get; }
        string               TitleKey     { get; }
        ESettingLocation     Location     { get; }
        ESettingType         Type         { get; }
        List<T>              Values       { get; }
        object               Min          { get; }
        object               Max          { get; }
        string               SpriteOffKey { get; }
        string               SpriteOnKey  { get; }
        T                    Get();
        void                 Put(T _Value);
    }

    public enum ESettingType
    {
        OnOff,
        InPanelSelector
    }

    public enum ESettingLocation
    {
        Main,
        MiniButtons
    }
}