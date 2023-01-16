﻿using mazing.common.Runtime.Entities;
using mazing.common.Runtime.Enums;
using TMPro;
using UnityEngine.Events;

namespace mazing.common.Runtime.Managers
{
    public interface ILocalizationManager : IInit
    {
        event UnityAction<ELanguage> LanguageChanged;
        string                       GetTranslation(string _Key);
        void                         SetLanguage(ELanguage _Language);
        ELanguage                    GetCurrentLanguage();
        void                         AddTextObject(LocalizableTextObjectInfo    _Info);
        void                         RemoveTextObject(LocalizableTextObjectInfo _Info);

        TMP_FontAsset GetFont(ETextType _TextType, ELanguage? _Language = null);
    }
}