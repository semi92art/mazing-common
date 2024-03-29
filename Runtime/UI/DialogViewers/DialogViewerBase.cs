﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using mazing.common.Runtime.CameraProviders;
using mazing.common.Runtime.CameraProviders.Camera_Effects_Props;
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
    public abstract class DialogViewerBase : InitBase, IDialogViewer
    {
        #region types

        private class DialogPanelTransitionInfo
        {
            public Dictionary<Graphic, float> StartAlphaChannelsDict { get; set; }
        }

        #endregion

        #region nonpublic members

        protected readonly IDialogPanel        FakePanel   = new DialogPanelFake();
        protected readonly Stack<IDialogPanel> PanelsStack = new Stack<IDialogPanel>();

        private readonly Dictionary<IDialogPanel, DialogPanelTransitionInfo> m_PanelsTransitionInfoDict
            = new Dictionary<IDialogPanel, DialogPanelTransitionInfo>();

        private UnityAction<bool, float> m_AdditionalCameraEffectsAction;
        
        #endregion

        #region inject

        protected IViewUICanvasGetter CanvasGetter     { get; }
        protected ICameraProvider     CameraProvider   { get; }
        protected IUITicker           Ticker           { get; }
        protected IPrefabSetManager   PrefabSetManager { get; }

        protected DialogViewerBase(
            IViewUICanvasGetter _CanvasGetter,
            ICameraProvider     _CameraProvider,
            IUITicker           _Ticker,
            IPrefabSetManager   _PrefabSetManager)
        {
            CanvasGetter     = _CanvasGetter;
            CameraProvider   = _CameraProvider;
            Ticker           = _Ticker;
            PrefabSetManager = _PrefabSetManager;
        }

        #endregion

        #region api

        public abstract int    Id         { get; }
        public abstract string CanvasName { get; }

        public IDialogPanel CurrentPanel => PanelsStack.Any() ? PanelsStack.Peek() : null;

        public abstract RectTransform Container                 { get; }
        public          Func<bool>    OtherDialogViewersShowing { get; set; }

        public abstract void Back(UnityAction _OnFinish = null);

        public virtual void Show(
            IDialogPanel             _Panel,
            float                    _AnimationSpeed                = 1,
            bool                     _HidePrevious                  = true,
            UnityAction<bool, float> _AdditionalCameraEffectsAction = null)
        {
            PanelsStack.Push(_Panel);
        }

        public override void Init()
        {
            Ticker.Register(this);
            base.Init();
        }

        #endregion

        #region nonpublic methods

        protected IEnumerator DoTransparentTransition(
            IDialogPanel               _DialogPanel,
            Dictionary<Graphic, float> _GraphicsAndAlphas,
            float                      _Time,
            bool                       _Appear,
            UnityAction                _OnFinish,
            bool                       _UseInteractableInsteadEnabledOnSelectables,
            UnityAction<bool, float>   _AdditionalCameraEffectsAction)
        {
            if (_Appear)
                m_AdditionalCameraEffectsAction = _AdditionalCameraEffectsAction;
            var rectTr = _DialogPanel.PanelRectTransform;
            rectTr.gameObject.SetActive(true);
            var selectables = rectTr.GetComponentsInChildrenEx<Selectable>();
            var selectablesActiveDict = selectables.ToDictionary(
                _S => _S,
                _S => _UseInteractableInsteadEnabledOnSelectables ? _S.interactable : _S.enabled);
            foreach (var selectable in selectablesActiveDict.Keys)
            {
                if (_UseInteractableInsteadEnabledOnSelectables)
                    selectable.interactable = false;
                else 
                    selectable.enabled = false;
            }
            //do transition for graphic elements
            float currTime = Ticker.Time;
            var addCamEffectsAction = m_AdditionalCameraEffectsAction ?? AdditionalCameraEffectsActionDefault;
            addCamEffectsAction(_Appear, _Time);
            if (_AdditionalCameraEffectsAction != null)
                _AdditionalCameraEffectsAction(_Appear, _Time);
            else
                AdditionalCameraEffectsActionDefault(_Appear, _Time);
            if (_Appear)
            {
                if (!m_PanelsTransitionInfoDict.ContainsKey(_DialogPanel))
                {
                    var transitionInfo = new DialogPanelTransitionInfo
                    {
                        StartAlphaChannelsDict = _GraphicsAndAlphas
                    };
                    m_PanelsTransitionInfoDict.Add(_DialogPanel, transitionInfo);
                }
                while (Ticker.Time < currTime + _Time)
                {
                    float timeCoeff = (currTime + _Time - Ticker.Time) / _Time;
                    float alphaCoeff = 1 - timeCoeff;
                    SetGraphicAlphaChannels(m_PanelsTransitionInfoDict[_DialogPanel].StartAlphaChannelsDict, alphaCoeff);
                    yield return new WaitForEndOfFrame();
                }
            }
            //enable selectable elements (buttons, toggles, etc.)
            foreach ((var selectable, bool active) in selectablesActiveDict
                .Where(_Button => !_Button.Key.IsNull()))
            {
                if (_UseInteractableInsteadEnabledOnSelectables)
                    selectable.interactable = active;
                else 
                    selectable.enabled = active;
            }
            //set color alphas to finish values for all graphic elements
            foreach ((var graphic, float value) in m_PanelsTransitionInfoDict[_DialogPanel].StartAlphaChannelsDict)
            {
                if (!graphic.IsNull())
                    graphic.color = graphic.color.SetA(!_Appear ? 0 : value);
            }
            if (!_Appear)
            {
                var collection = m_PanelsTransitionInfoDict[_DialogPanel].StartAlphaChannelsDict;
                SetGraphicAlphaChannels(collection, 1f);
                rectTr.gameObject.SetActive(false);
            }
            _OnFinish?.Invoke();
        }

        private static void SetGraphicAlphaChannels(
            IEnumerable<KeyValuePair<Graphic, float>> _Dict,
            float                                     _AlphaCoefficient)
        {
            var collection = _Dict.ToList();
            foreach ((var key, float value) in from ga
                                                   in collection
                                               let graphic = ga.Key
                                               where !graphic.IsNull()
                                               select ga)
                key.color = key.color.SetA(value * _AlphaCoefficient);
        }

        private void AdditionalCameraEffectsActionDefault(bool _Disappear, float _Time)
        {
            Cor.Run(AdditionalCameraEffectsActionDefaultCoroutine(_Disappear, _Time));
        }

        protected virtual IEnumerator AdditionalCameraEffectsActionDefaultCoroutine(bool _Appear, float _Time)
        {
            CameraProvider.EnableEffect(ECameraEffect.DepthOfField, _Appear);
            CameraProvider.EnableEffect(ECameraEffect.Glitch, _Appear);
            if (!_Appear)
                yield break;
            float currTime = Ticker.Time;
            while (Ticker.Time < currTime + _Time)
            {
                float timeCoeff = (currTime + _Time - Ticker.Time) / _Time;
                var depthOfFieldProps = new FastDofProps
                {
                    BlurAmount = (1 - timeCoeff) * 0.2f
                };
                CameraProvider.SetEffectProps(ECameraEffect.DepthOfField, depthOfFieldProps);
                var glitchProps = new FastGlitchProps
                {
                    ChromaticGlitch = 0.03f,
                    FrameGlitch = 0.03f,
                    PixelGlitch = 0.3f
                };
                CameraProvider.SetEffectProps(ECameraEffect.Glitch, glitchProps);
                yield return new WaitForEndOfFrame();
            }
        }

        #endregion
    }
}