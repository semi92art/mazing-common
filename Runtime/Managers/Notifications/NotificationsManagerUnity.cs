﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using mazing.common.Runtime.Settings;
using mazing.common.Runtime.Ticker;
using UnityEngine;
#if UNITY_ANDROID
using Unity.Notifications.Android;
using mazing.common.Runtime.Managers.Notifications.Android;
#elif UNITY_IOS
using mazing.common.Runtime.Managers.Notifications.iOS;
#endif

namespace mazing.common.Runtime.Managers.Notifications
{
    public class NotificationsManagerUnity 
        : NotificationsManagerBase,
          IUpdateTick,
          IApplicationFocus,
          IDestroy
    {
        #region constants

        private const string ChannelId       = "game_channel_0";
        private const string DefaultFilename = "notifications.bin";

        #endregion

        #region nonpublic members

        private bool m_InForeground = true;

        private          IGameNotificationsPlatform  Platform { get; set; }

        // Minimum amount of time that a notification should be into the future before it's queued when we background.
        private static readonly TimeSpan MinimumNotificationTime = new TimeSpan(0, 0, 2);

        /// <summary>
        /// Gets whether this manager automatically increments badge numbers.
        /// Check to make the notifications manager automatically set badge numbers so that they increment.
        /// Schedule notifications with no numbers manually set to make use of this feature.
        /// </summary>
        private bool m_AutoBadging;

        /// <summary>
        /// Gets or sets the serializer to use to save pending notifications to disk if we're in
        /// <see cref="ENotificationsOperatingMode.RescheduleAfterClearing"/> mode.
        /// </summary>
        private IPendingNotificationsSerializer m_Serializer;
        
        private List<PendingNotification> PendingNotifications { get; set; }

        #endregion

        #region inject

        private ICommonTicker        Ticker  { get; }
        private INotificationSetting Setting { get; }

        private NotificationsManagerUnity(
            ICommonTicker              _Ticker,
            INotificationSetting       _Setting)
        {
            Ticker                    = _Ticker;
            Setting                   = _Setting;
        }

        #endregion

        #region api

        public event Action<PendingNotification> LocalNotificationDelivered;
        public event Action<PendingNotification> LocalNotificationExpired;

        public override void Init()
        {
            if (Initialized)
                return;
            Setting.ValueSet += EnableNotifications;
            Ticker.Register(this);
            var channel = new GameNotificationChannel(
                ChannelId, 
                "Common notifications",
                "Common notifications", 
                GameNotificationChannel.NotificationStyle.Popup,
                true, 
                true, 
                true,
                true);
            InitNotifications(channel);
            base.Init();
            OnForegrounding();
        }

        public override void SendNotification(
            string   _Title,
            string   _Body,
            TimeSpan _TimeSpan,
            int?     _BadgeNumber = null,
            bool     _Reschedule  = false,
            string   _ChannelId   = null,
            string   _SmallIcon   = null,
            string   _LargeIcon   = null)
        {
            if (!Setting.Get())
                return;
            IGameNotification notification = CreateNotification();
            if (notification == null)
                return;
            notification.Title = _Title;
            notification.Body = _Body;
            notification.Group = !string.IsNullOrEmpty(_ChannelId) ? _ChannelId : ChannelId;
            notification.DeliveryTime = DateTime.Now.Add(_TimeSpan);
            notification.SmallIcon = _SmallIcon;
            notification.LargeIcon = _LargeIcon;
            if (_BadgeNumber != null)
                notification.BadgeNumber = _BadgeNumber;
            PendingNotification notificationToDisplay = ScheduleNotification(notification);
            notificationToDisplay.Reschedule = _Reschedule;
            Dbg.Log("Sending notification: \n" +
                    "Title: " + _Title + ", \n" +
                    "Body: " + _Body + ", \n" +
                    "Span: " + _TimeSpan);
        }
        
        public override void ClearAllNotifications()
        {
            Platform.CancelAllScheduledNotifications();
            m_Serializer.Serialize(new List<PendingNotification>());
        }

        #endregion

        #region nonpublic methods

        private IGameNotification CreateNotification()
        {
            if (!Initialized)
                throw new InvalidOperationException("Must call NotificationsManager.Init() first.");
            return Platform?.CreateNotification();
        }

        private void InitNotifications(params GameNotificationChannel[] _Channels)
        {
#if UNITY_ANDROID
            Platform = new AndroidNotificationsPlatform();
            // Register the notification channels
            bool doneDefault = false;
            foreach (GameNotificationChannel notificationChannel in _Channels)
            {
                if (!doneDefault)
                {
                    doneDefault = true;
                    ((AndroidNotificationsPlatform) Platform).DefaultChannelId = notificationChannel.Id;
                }
                long[] vibrationPattern = null;
                if (notificationChannel.VibrationPattern != null)
                    vibrationPattern = notificationChannel.VibrationPattern.Select(_V => (long) _V).ToArray();
                // Wrap channel in Android object
                var androidChannel = new AndroidNotificationChannel(notificationChannel.Id, notificationChannel.Name,
                    notificationChannel.Description,
                    (Importance) notificationChannel.Style)
                {
                    CanBypassDnd = notificationChannel.HighPriority,
                    CanShowBadge = notificationChannel.ShowsBadge,
                    EnableLights = notificationChannel.ShowLights,
                    EnableVibration = notificationChannel.Vibrates,
                    LockScreenVisibility = (LockScreenVisibility) notificationChannel.Privacy,
                    VibrationPattern = vibrationPattern
                };
                AndroidNotificationCenter.RegisterNotificationChannel(androidChannel);
            }
#elif UNITY_IOS
            Platform = new iOSNotificationsPlatform();
#endif
            if (Platform == null)
                return;
            PendingNotifications = new List<PendingNotification>();
            Platform.NotificationReceived += OnNotificationReceived;
            // Check serializer
            m_Serializer ??= new DefaultSerializer(Path.Combine(Application.persistentDataPath, DefaultFilename));
        }

        /// <summary>
        /// Event fired by <see cref="Platform"/> when a notification is received.
        /// </summary>
        private void OnNotificationReceived(IGameNotification _DeliveredNotification)
        {
            // Ignore for background messages (this happens on Android sometimes)
            if (!m_InForeground)
                return;
            // Find in pending list
            int deliveredIndex =
                PendingNotifications.FindIndex(_ScheduledNotification =>
                    _ScheduledNotification.Notification.Id == _DeliveredNotification.Id);
            if (deliveredIndex < 0)
                return;
            LocalNotificationDelivered?.Invoke(PendingNotifications[deliveredIndex]);
            PendingNotifications.RemoveAt(deliveredIndex);
        }

        /// <summary>
        /// Schedules a notification to be delivered.
        /// </summary>
        /// <param name="_Notification">The notification to deliver.</param>
        private PendingNotification ScheduleNotification(IGameNotification _Notification)
        {
            if (!Initialized)
                throw new InvalidOperationException("Must call Initialize() first.");
            if (_Notification == null || Platform == null)
                return null;
            // If we queue, don't schedule immediately.
            // Also immediately schedule non-time based deliveries (for iOS)
            if ((OperatingMode & ENotificationsOperatingMode.Queue) != ENotificationsOperatingMode.Queue ||
                _Notification.DeliveryTime == null)
            {
                Platform.ScheduleNotification(_Notification);
            }
            else if (!_Notification.Id.HasValue)
            {
                // Generate an ID for items that don't have one (just so they can be identified later)
                int id = Math.Abs(DateTime.Now.ToString("yyMMddHHmmssffffff").GetHashCode());
                _Notification.Id = id;
            }

            // Register pending notification
            var result = new PendingNotification(_Notification);
            PendingNotifications.Add(result);
            return result;
        }

        // Clear foreground notifications and reschedule stuff from a file
        private void OnForegrounding()
        {
            if (!Setting.Get())
                return;
            Platform.OnForeground();
            PendingNotifications.Clear();
            // Deserialize saved items
            IList<IGameNotification> loaded = m_Serializer?.Deserialize(Platform);
            // Foregrounding
            if ((OperatingMode & ENotificationsOperatingMode.ClearOnForegrounding) ==
                ENotificationsOperatingMode.ClearOnForegrounding)
            {
                // Clear on foregrounding
                Platform.CancelAllScheduledNotifications();
                // Only reschedule in reschedule mode, and if we loaded any items
                if (loaded == null ||
                    (OperatingMode & ENotificationsOperatingMode.RescheduleAfterClearing) !=
                    ENotificationsOperatingMode.RescheduleAfterClearing)
                {
                    return;
                }
                // Reschedule notifications from deserialization
                foreach (IGameNotification savedNotification in loaded)
                {
                    if (savedNotification.DeliveryTime < DateTime.Now)
                        continue;
                    PendingNotification pendingNotification = ScheduleNotification(savedNotification);
                    pendingNotification.Reschedule = true;
                }
            }
            else
            {
                // Just create PendingNotification wrappers for all deserialized items.
                // We're not rescheduling them because they were not cleared
                if (loaded == null)
                    return;
                foreach (IGameNotification savedNotification in loaded)
                {
                    if (savedNotification.DeliveryTime > DateTime.Now)
                        PendingNotifications.Add(new PendingNotification(savedNotification));
                }
            }
        }

        #endregion

        #region engine methods

        public void UpdateTick()
        {
            if (!Setting.Get())
                return;
            if (PendingNotifications == null || !PendingNotifications.Any()
                                             || (OperatingMode & ENotificationsOperatingMode.Queue) !=
                                             ENotificationsOperatingMode.Queue)
            {
                return;
            }

            // Check each pending notification for expiry, then remove it
            for (int i = PendingNotifications.Count - 1; i >= 0; --i)
            {
                PendingNotification queuedNotification = PendingNotifications[i];
                DateTime? time = queuedNotification.Notification.DeliveryTime;
                if (time == null || time > DateTime.Now)
                    continue;
                PendingNotifications.RemoveAt(i);
                LocalNotificationExpired?.Invoke(queuedNotification);
            }
        }

        public void OnApplicationFocus(bool _HasFocus)
        {
            if (!Setting.Get() || Platform == null || !Initialized)
                return;
            m_InForeground = _HasFocus;
            if (_HasFocus)
            {
                OnForegrounding();
            }
            else
            {
                Platform.OnBackground();
                QueueFutureDatesNotifications();
                try
                {
                    SaveFutureDatesNotifications();
                }
                catch (Exception ex)
                {
                    Dbg.Log(ex);
                }
            }
        }

        public void OnDestroy()
        {
            if (Platform == null)
                return;
            Platform.NotificationReceived -= OnNotificationReceived;
            if (Platform is IDisposable disposable)
                disposable.Dispose();
            m_InForeground = false;
        }

        #endregion

        #region nonpublic methods

        private void EnableNotifications(bool _Enable)
        {
            if (!_Enable)
                ClearAllNotifications();
        }

        private void QueueFutureDatesNotifications()
        {
            // Queue future dated notifications
            if ((OperatingMode & ENotificationsOperatingMode.Queue) == ENotificationsOperatingMode.Queue)
            {
                // Filter out past events
                for (int i = PendingNotifications.Count - 1; i >= 0; i--)
                {
                    PendingNotification pendingNotification = PendingNotifications[i];
                    // Ignore already scheduled ones
                    if (pendingNotification.Notification.Scheduled)
                        continue;
                    // If a non-scheduled notification is in the past (or not within our threshold)
                    // just remove it immediately
                    if (pendingNotification.Notification.DeliveryTime != null &&
                        pendingNotification.Notification.DeliveryTime - DateTime.Now < MinimumNotificationTime)
                    {
                        PendingNotifications.RemoveAt(i);
                    }
                }

                // Sort notifications by delivery time, if no notifications have a badge number set
                bool noBadgeNumbersSet =
                    PendingNotifications.All(_Notification => _Notification.Notification.BadgeNumber == null);

                if (noBadgeNumbersSet && m_AutoBadging)
                {
                    PendingNotifications.Sort((_A, _B) =>
                    {
                        if (!_A.Notification.DeliveryTime.HasValue)
                            return 1;
                        if (!_B.Notification.DeliveryTime.HasValue)
                            return -1;
                        return _A.Notification.DeliveryTime.Value.CompareTo(_B.Notification.DeliveryTime.Value);
                    });
                    // Set badge numbers incrementally
                    int badgeNum = 1;
                    foreach (var pendingNotification in PendingNotifications
                        .Where(_PendingNotification =>
                            _PendingNotification.Notification.DeliveryTime.HasValue &&
                            !_PendingNotification.Notification.Scheduled))
                    {
                        pendingNotification.Notification.BadgeNumber = badgeNum++;
                    }
                }

                for (int i = PendingNotifications.Count - 1; i >= 0; i--)
                {
                    PendingNotification pendingNotification = PendingNotifications[i];
                    // Ignore already scheduled ones
                    if (pendingNotification.Notification.Scheduled)
                        continue;
                    // Schedule it now
                    Platform.ScheduleNotification(pendingNotification.Notification);
                }

                // Clear badge numbers again (for saving)
                if (noBadgeNumbersSet && m_AutoBadging)
                {
                    foreach (var pendingNotification in PendingNotifications
                        .Where(_PendingNotification => _PendingNotification.Notification.DeliveryTime.HasValue))
                    {
                        pendingNotification.Notification.BadgeNumber = null;
                    }
                }
            }
        }

        private void SaveFutureDatesNotifications()
        {
            // Calculate notifications to save
            var notificationsToSave = new List<PendingNotification>(PendingNotifications.Count);
            var notificationsSortedByDeliveryTime = PendingNotifications
                .Where(_N => _N.Reschedule)
                .Where(_N => _N.Notification.DeliveryTime.HasValue)
                .OrderByDescending(_N => _N.Notification.DeliveryTime.Value)
                .ToList();
            int countToReschedule = LastNotificationsCountToReschedule ?? notificationsSortedByDeliveryTime.Count;
            for (int i = 0; i < countToReschedule; i++)
            {
                var pendingNotification = notificationsSortedByDeliveryTime[i];
                // If we're in clear mode, add nothing unless we're in rescheduling mode
                // Otherwise add everything
                if ((OperatingMode & ENotificationsOperatingMode.ClearOnForegrounding) ==
                    ENotificationsOperatingMode.ClearOnForegrounding)
                {
                    if ((OperatingMode & ENotificationsOperatingMode.RescheduleAfterClearing) !=
                        ENotificationsOperatingMode.RescheduleAfterClearing)
                    {
                        continue;
                    }
                    // In reschedule mode, add ones that have been scheduled, are marked for
                    // rescheduling, and that have a time
                    if (pendingNotification.Reschedule &&
                        pendingNotification.Notification.Scheduled &&
                        pendingNotification.Notification.DeliveryTime.HasValue)
                    {
                        notificationsToSave.Add(pendingNotification);
                    }
                }
                // In non-clear mode, just add all scheduled notifications
                else if (pendingNotification.Notification.Scheduled)
                {
                    notificationsToSave.Add(pendingNotification);
                }
            }

            // Save to disk
            m_Serializer.Serialize(notificationsToSave);
        }

        #endregion
    }
}