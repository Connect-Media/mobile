﻿using System;
using System.Linq.Expressions;
using MonoTouch.Foundation;
using Toggl.Phoebe;
using Toggl.Phoebe.Data;
using XPlatUtils;

namespace Toggl.Ross.Data
{
    public class SettingsStore : ISettingsStore
    {
        private const string PhoebeUserIdKey = "phoebeUserId";
        private const string PhoebeApiTokenKey = "phoebeApiToken";
        private const string PhoebeSyncLastRunKey = "phoebeSyncLastRun";
        private const string PhoebeUseDefaultTagKey = "phoebeUseDefaultTag";

        private static string GetPropertyName<T> (Expression<Func<SettingsStore, T>> expr)
        {
            return expr.ToPropertyName ();
        }

        protected Guid? GetGuid (string key)
        {
            var val = (string)(NSString)NSUserDefaults.StandardUserDefaults [key];
            if (String.IsNullOrEmpty (val))
                return null;
            return Guid.Parse (val);
        }

        protected void SetGuid (string key, Guid? value)
        {
            if (value != null) {
                NSUserDefaults.StandardUserDefaults [key] = (NSString)value.Value.ToString ();
            } else {
                NSUserDefaults.StandardUserDefaults.RemoveObject (key);
            }
            NSUserDefaults.StandardUserDefaults.Synchronize ();
        }

        protected string GetString (string key)
        {
            return (string)(NSString)NSUserDefaults.StandardUserDefaults [key];
        }

        protected void SetString (string key, string value)
        {
            if (value != null) {
                NSUserDefaults.StandardUserDefaults [key] = (NSString)value;
            } else {
                NSUserDefaults.StandardUserDefaults.RemoveObject (key);
            }
            NSUserDefaults.StandardUserDefaults.Synchronize ();
        }

        protected int? GetInt (string key)
        {
            var raw = NSUserDefaults.StandardUserDefaults [key];
            if (raw == null)
                return null;
            return (int)(NSNumber)raw;
        }

        protected void SetInt (string key, int? value)
        {
            if (value != null) {
                NSUserDefaults.StandardUserDefaults [key] = (NSNumber)value.Value;
            } else {
                NSUserDefaults.StandardUserDefaults.RemoveObject (key);
            }
            NSUserDefaults.StandardUserDefaults.Synchronize ();
        }

        protected DateTime? GetDateTime (string key)
        {
            var raw = NSUserDefaults.StandardUserDefaults [key];
            if (raw == null)
                return null;
            return DateTime.FromBinary ((long)(NSNumber)raw);
        }

        protected void SetDateTime (string key, DateTime? value)
        {
            if (value != null) {
                NSUserDefaults.StandardUserDefaults [key] = (NSNumber)value.Value.ToBinary ();
            } else {
                NSUserDefaults.StandardUserDefaults.RemoveObject (key);
            }
            NSUserDefaults.StandardUserDefaults.Synchronize ();
        }

        protected void OnSettingChanged (string name)
        {
            var bus = ServiceContainer.Resolve<MessageBus> ();
            bus.Send (new SettingChangedMessage (this, name));
        }

        public static readonly string PropertyUserId = GetPropertyName (s => s.UserId);

        public Guid? UserId {
            get { return GetGuid (PhoebeUserIdKey); }
            set {
                SetGuid (PhoebeUserIdKey, value);
                OnSettingChanged (PropertyUserId);
            }
        }

        public static readonly string PropertyApiToken = GetPropertyName (s => s.ApiToken);

        public string ApiToken {
            get { return GetString (PhoebeApiTokenKey); }
            set {
                SetString (PhoebeApiTokenKey, value);
                OnSettingChanged (PropertyApiToken);
            }
        }

        public static readonly string PropertySyncLastRun = GetPropertyName (s => s.SyncLastRun);

        public DateTime? SyncLastRun {
            get { return GetDateTime (PhoebeSyncLastRunKey); }
            set {
                SetDateTime (PhoebeSyncLastRunKey, value);
                OnSettingChanged (PropertySyncLastRun);
            }
        }

        public static readonly string PropertyUseDefaultTag = GetPropertyName (s => s.UseDefaultTag);

        public bool UseDefaultTag {
            get { return (GetInt (PhoebeUseDefaultTagKey) ?? 1) == 1; }
            set {
                SetInt (PhoebeUseDefaultTagKey, value ? 1 : 0);
                OnSettingChanged (PropertyUseDefaultTag);
            }
        }
    }
}
