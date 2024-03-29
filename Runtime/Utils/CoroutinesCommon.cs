﻿using System;
using System.Collections;
using mazing.common.Runtime.Ticker;
using UnityEngine;
using UnityEngine.Events;

namespace mazing.common.Runtime.Utils
{
    public static partial class Cor
    {
        public static IEnumerator Action(UnityAction _Action)
        {
            _Action?.Invoke();
            yield break;
        }
    
        public static IEnumerator WaitNextFrame(
            UnityAction _Action,
            bool        _EndOfFrame = false,
            uint        _FramesNum  = 1)
        {
            yield return null;
            for (uint i = 0; i < _FramesNum; i++)
                yield return null;
            if (_EndOfFrame)
                yield return new WaitForEndOfFrame();
            _Action?.Invoke();
        }

        public static IEnumerator Delay(
            float       _Delay,
            ITicker     _Ticker,
            UnityAction _OnDelay = null,
            Func<bool>  _OnBreak = null,
            UnityAction _OnStart = null)
        {
            _OnStart?.Invoke();
            float time = _Ticker?.Time ?? Time.time;
            bool IsTimeValid()
            {
                return time + _Delay > (_Ticker?.Time ?? Time.time);
            }
            while (IsTimeValid())
            {
                if (_OnBreak != null && _OnBreak())
                    yield break;
                yield return new WaitForSeconds(_Delay);
            }
            _OnDelay?.Invoke();
        }

        public static IEnumerator WaitWhile(
            Func<bool>  _Predicate,
            UnityAction _Action  = null,
            Func<bool>  _OnBreak = null,
            float?      _Seconds = null,
            ITicker     _Ticker  = null)
        {
            if (_Predicate == null)
                yield break;
            float time = _Ticker?.Time ?? Time.time;
            bool IsTimeValid()
            {
                return time + _Seconds.Value > (_Ticker?.Time ?? Time.time);
            }
            bool FinalPredicate()
            {
                return _Seconds.HasValue ? _Predicate() && IsTimeValid() : _Predicate();
            }
            while (FinalPredicate())
            {
                yield return new WaitForEndOfFrame();
                if (_OnBreak != null && _OnBreak())
                    yield break;
            }
            _Action?.Invoke();
        }
        
        public static IEnumerator DoWhile(
            Func<bool>   _Predicate,
            UnityAction  _Action,
            UnityAction  _FinishAction,
            IUnityTicker _Ticker,
            Func<bool>   _Pause          = null,
            bool         _WaitEndOfFrame = true,
            bool         _FixedUpdate    = false)
        {
            if (_Action == null || _Predicate == null)
                yield break;
            while (_Predicate.Invoke())
            {
                if (_Pause != null && _Pause())
                    continue;
                _Action();
                if (!_WaitEndOfFrame) 
                    continue;
                float dt = _FixedUpdate ? _Ticker.FixedDeltaTime : _Ticker.DeltaTime;
                yield return new WaitForSecondsRealtime(dt);
            }
            _FinishAction?.Invoke();
        }

        public static IEnumerator Repeat(
            UnityAction _Action,
            float _RepeatDelta,
            float _RepeatTime,
            ITicker _Ticker,
            Func<bool> _DoStop = null,
            UnityAction _OnFinish = null)
        {
            if (_Action == null)
                yield break;
            
            float startTime = _Ticker.Time;
            
            while (_Ticker.Time - startTime < _RepeatTime 
                   && (_DoStop == null || !_DoStop()))
            {
                _Action();
                yield return new WaitForSeconds(_RepeatDelta);
            }
            
            _OnFinish?.Invoke();
        }
        
         public static IEnumerator Lerp(
             IUnityTicker             _Ticker,
             float                    _Time,
             float                    _From            = 0f,
             float                    _To              = 1f,
             UnityAction<float>       _OnProgress      = null,
             UnityAction              _OnFinish        = null,
             Func<bool>               _BreakPredicate  = null,
             Func<float, float>       _ProgressFormula = null,
             bool                     _FixedUpdate     = false,
             UnityAction<bool, float> _OnFinishEx      = null)
        {
            if (_OnProgress == null)
                yield break;
            float GetTime()
            {
                if (_Ticker == null)
                    return Time.time;
                return _FixedUpdate ? _Ticker.FixedTime : _Ticker.Time;
            }
            float currTime = GetTime();
            float progress = _From;
            bool breaked = false;
            while (GetTime() < currTime + _Time)
            {
                if (_BreakPredicate != null && _BreakPredicate())
                {
                    breaked = true;
                    break;
                }
                if (_Ticker != null && _Ticker.Pause)
                {
                    yield return PauseCoroutine(_FixedUpdate);
                    continue;
                }
                float timeCoeff = 1 - (currTime + _Time - GetTime()) / _Time;
                progress = Mathf.Lerp(_From, _To, timeCoeff);
                if (_ProgressFormula != null)
                    progress = _ProgressFormula(progress);
                _OnProgress(progress);
                if (_FixedUpdate)
                    yield return new WaitForFixedUpdate();
                else
                    yield return new WaitForEndOfFrame();
            }
            if (_BreakPredicate != null && _BreakPredicate())
                breaked = true;
            if (_Ticker != null && _Ticker.Pause)
                yield return PauseCoroutine(_FixedUpdate);
            if (!breaked)
            {
                progress = _ProgressFormula?.Invoke(_To) ?? _To;
                _OnProgress(progress);
            }
            if (_Ticker != null && _Ticker.Pause)
                yield return PauseCoroutine(_FixedUpdate);
            _OnFinish?.Invoke();
            _OnFinishEx?.Invoke(breaked, breaked ? progress : _To);
        }

        private static IEnumerator PauseCoroutine(bool _FixedUpdate)
        {
            if (_FixedUpdate)
                yield return new WaitForFixedUpdate();
            else 
                yield return new WaitForEndOfFrame();
        }
    }
}