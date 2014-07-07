﻿using System;
using System.Collections.Generic;
using Android.Views;
using Android.Widget;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Net;
using XPlatUtils;
using Toggl.Joey.UI.Utils;
using Toggl.Joey.UI.Views;

namespace Toggl.Joey.UI.Adapters
{
    class DrawerListAdapter : BaseAdapter
    {
        protected static readonly int ViewTypeDrawerHeader = 0;
        protected static readonly int ViewTypeDrawerItem = 1;
        public static readonly int TimerPageId = 0;
        public static readonly int ReportsPageId = 1;
        public static readonly int SettingsPageId = 2;
        public static readonly int LogoutPageId = 3;
        private readonly List<DrawerItem> rowItems;
        private readonly AuthManager authManager;

        public DrawerListAdapter ()
        {
            rowItems = new List<DrawerItem> () {
                new DrawerItem () {
                    Id = TimerPageId,
                    TextResId = Resource.String.MainDrawerTimer,
                    ImageResId = Resource.Drawable.IcNavTimer,
                    IsEnabled = true,
                },
                new DrawerItem () {
                    Id = ReportsPageId,
                    TextResId = Resource.String.MainDrawerReports,
                    ImageResId = Resource.Drawable.IcNavReports,
                    IsEnabled = false,
                },
                new DrawerItem () {
                    Id = SettingsPageId,
                    TextResId = Resource.String.MainDrawerSettings,
                    ImageResId = Resource.Drawable.IcNavSettings,
                    IsEnabled = true,
                },
                new DrawerItem () {
                    Id = LogoutPageId,
                    TextResId = Resource.String.MainDrawerLogout,
                    ImageResId = Resource.Drawable.IcNavLogout,
                    IsEnabled = true,
                }
            };
            authManager = ServiceContainer.Resolve<AuthManager> ();
        }

        public override int ViewTypeCount {
            get { return 2; }
        }

        public override int GetItemViewType (int position)
        {
            if (position == 0) {
                return ViewTypeDrawerHeader;
            } else {
                return ViewTypeDrawerItem;
            }
        }

        public override View GetView (int position, View convertView, ViewGroup parent)
        {
            View view = convertView;

            if (GetItemViewType (position) == ViewTypeDrawerHeader) {
                if (view == null) {
                    view = LayoutInflater.FromContext (parent.Context).Inflate (
                        Resource.Layout.MainDrawerListHeader, parent, false);
                    view.Tag = new HeaderViewHolder (view);
                }

                var holder = (HeaderViewHolder)view.Tag;
                holder.Bind ((UserModel)authManager.User);
            } else {
                if (view == null) {
                    view = LayoutInflater.FromContext (parent.Context).Inflate (
                        Resource.Layout.MainDrawerListItem, parent, false);
                    view.Tag = new DrawerItemViewHolder (view);
                }

                var holder = (DrawerItemViewHolder)view.Tag;
                holder.Bind (GetDrawerItem (position));
            }

            return view;
        }

        public override int Count {
            get{ return rowItems.Count + 1; } // + 1 is for header
        }

        public override Java.Lang.Object GetItem (int position)
        {
            return null;
        }

        public int GetItemPosition (int id)
        {
            var idx = rowItems.FindIndex (i => i.Id == id);
            return idx >= 0 ? idx + 1 : -1;
        }

        private DrawerItem GetDrawerItem (int position)
        {
            if (position == 0)
                return null;
            return rowItems [position - 1]; //Header is 0
        }

        public override long GetItemId (int position)
        {
            if (GetItemViewType (position) == ViewTypeDrawerHeader) {
                return -1;
            } else {
                return GetDrawerItem (position).Id;
            }
        }

        public override bool IsEnabled (int position)
        {
            var item = GetDrawerItem (position);
            return item != null && item.IsEnabled;
        }

        private class DrawerItem
        {
            public int Id;
            public int TextResId;
            public int ImageResId;
            public bool IsEnabled;
        }

        private class HeaderViewHolder : ModelViewHolder<UserModel>
        {
            public ProfileImageView IconProfileImageView { get; private set; }

            public TextView TitleTextView { get; private set; }

            public HeaderViewHolder (View root) : base (root)
            {
                IconProfileImageView = root.FindViewById<ProfileImageView> (Resource.Id.IconProfileImageView);
                TitleTextView = root.FindViewById<TextView> (Resource.Id.TitleTextView).SetFont (Font.Roboto);
            }

            protected override void ResetTrackedObservables ()
            {
                Tracker.MarkAllStale ();

                if (DataSource != null) {
                    Tracker.Add (DataSource, HandleUserPropertyChanged);
                }

                Tracker.ClearStale ();
            }

            private void HandleUserPropertyChanged (string prop)
            {
                if (prop == UserModel.PropertyName
                    || prop == UserModel.PropertyImageUrl)
                    Rebind ();
            }


            protected override void Rebind ()
            {
                // Protect against Java side being GCed
                if (Handle == IntPtr.Zero)
                    return;

                ResetTrackedObservables ();

                if (DataSource == null)
                    return;

                IconProfileImageView.ImageUrl = DataSource.ImageUrl;
                TitleTextView.Text = DataSource.Name;
            }
        }

        private class DrawerItemViewHolder : BindableViewHolder<DrawerItem>
        {
            public ImageView IconImageView { get; private set; }

            public TextView TitleTextView { get; private set; }

            public DrawerItemViewHolder (View root) : base (root)
            {
                IconImageView = root.FindViewById<ImageView> (Resource.Id.IconImageView);
                TitleTextView = root.FindViewById<TextView> (Resource.Id.TitleTextView).SetFont (Font.Roboto);
            }

            protected override void Rebind ()
            {
                if (DataSource == null)
                    return;
                IconImageView.SetImageResource (DataSource.ImageResId);
                TitleTextView.SetText (DataSource.TextResId);
                TitleTextView.Enabled = DataSource.IsEnabled;
            }
        }
    }
}

