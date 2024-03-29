﻿using System;
using System.Collections.Generic;
using System.Linq;
using mazing.common.Runtime.CameraProviders;
using mazing.common.Runtime.Constants;
using mazing.common.Runtime.Entities.UI;
using mazing.common.Runtime.Enums;
using mazing.common.Runtime.Extensions;
using mazing.common.Runtime.Helpers;
using mazing.common.Runtime.Managers;
using mazing.common.Runtime.Ticker;
using mazing.common.Runtime.Utils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace mazing.common.Runtime.UI.DialogViewers
{
    public interface IDialogViewerMedium : IDialogViewer { }
    
    public class DialogViewerMediumFake : InitBase, IDialogViewerMedium
    {
        public int           Id                        => default;
        public string        CanvasName                => null;
        public IDialogPanel  CurrentPanel              => null;
        public RectTransform Container                 => null;
        public Func<bool>    OtherDialogViewersShowing { get; set; }

        public void Back(UnityAction _OnFinish = null) { }

        public void Show(
            IDialogPanel             _Panel,
            float                    _AnimationSpeed                = 1,
            bool                     _HidePrevious                  = true,
            UnityAction<bool, float> _AdditionalCameraEffectsAction = null) { }
    }
    
    public abstract class DialogViewerCommonBase : DialogViewerBase, IDialogViewerMedium
    {
        #region nonpublic members
        
        private RectTransform              m_DialogContainer;
        private Dictionary<Graphic, float> m_Alphas;
        
        protected abstract string PrefabName { get; }

        #endregion

        #region inject
        
        protected DialogViewerCommonBase(
            IViewUICanvasGetter _CanvasGetter,
            IUITicker           _Ticker,
            ICameraProvider     _CameraProvider,
            IPrefabSetManager   _PrefabSetManager)
            : base(
                _CanvasGetter,
                _CameraProvider, 
                _Ticker, 
                _PrefabSetManager) { }

        #endregion

        #region api

        public override RectTransform Container => m_DialogContainer;
        
        public override void Init()
        {
            var parent = CanvasGetter.GetCanvas(CanvasName).RTransform();
            var go = PrefabSetManager.InitUiPrefab(
                UIUtils.UiRectTransform(
                    parent,
                    RectTransformLite.FullFill),
                "dialog_viewers",
                PrefabName);
            Cor.Run(Cor.WaitNextFrame(() =>
            {
                m_DialogContainer = go.GetCompItem<RectTransform>("dialog_container");
                base.Init();
            }));
        }

        public override void Show(
            IDialogPanel             _Panel,
            float                    _Speed                         = 1f,
            bool                     _HidePrevious                  = true,
            UnityAction<bool, float> _AdditionalCameraEffectsAction = null)
        {
            if (_Panel == null)
                return;
            CameraProvider.EnableEffect(ECameraEffect.DepthOfField, true);
            base.Show(_Panel, _Speed, _HidePrevious);
            m_Alphas = CurrentPanel.PanelRectTransform
                .GetComponentsInChildrenEx<Graphic>()
                .Distinct()
                .ToDictionary(_El => _El, _El => _El.color.a);
            var canvas = CanvasGetter.GetCanvas(CanvasName);
            canvas.enabled = true;
            Cor.Run(DoTransparentTransition(
                _Panel, 
                m_Alphas,
                0.2f / _Speed,
                true,
                () =>
                {
                    _Panel.AppearingState = EAppearingState.Appeared;
                },
                false,
                _AdditionalCameraEffectsAction));
            _Panel.AppearingState = EAppearingState.Appearing;
            if (_Panel.Animator.IsNull())
                return;
            _Panel.PanelRectTransform.gameObject.SetActive(true);
            _Panel.Animator.speed = _Speed;
            _Panel.Animator.SetTrigger(AnimKeys.Anim);
        }

        public override void Back(UnityAction _OnFinish = null)
        {
            var panel = PanelsStack.Pop();
            Cor.Run(DoTransparentTransition(
                panel,
                m_Alphas,
                0.2f,
                false,
                () =>
                {
                    panel.AppearingState = EAppearingState.Dissapeared;
                    if (OtherDialogViewersShowing())
                        CameraProvider.EnableEffect(ECameraEffect.DepthOfField, false);
                    panel.PanelRectTransform.gameObject.SetActive(false);
                    _OnFinish?.Invoke();
                    var canvas = CanvasGetter.GetCanvas(CanvasName);
                    if (CurrentPanel == null 
                        && canvas.enabled 
                        && !OtherDialogViewersShowing())
                    {
                        canvas.enabled = false;
                    }
                },
                false,
                null));
            panel.AppearingState = EAppearingState.Dissapearing;
        }

        #endregion
    }
}