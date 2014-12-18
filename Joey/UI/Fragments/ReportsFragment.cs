﻿using System;
using System.Collections.Generic;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Views;
using Android.Widget;
using Toggl.Joey.UI.Utils;
using Toggl.Joey.UI.Views;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.Reports;
using Fragment = Android.Support.V4.App.Fragment;

namespace Toggl.Joey.UI.Fragments
{
    public class ReportsFragment : Fragment
    {
        private static readonly string ReportPeriodArgument = "com.toggl.timer.report_period";
        private static readonly string ReportZoomArgument = "com.toggl.timer.report_zoom";

        private bool isLoading;
        private Controller controller;
        private Pool<Controller> controllerPool;
        private int position;

        public int Period
        {
            get {
                int period = 0;
                if (Arguments != null) {
                    period = Arguments.GetInt (ReportPeriodArgument);
                }
                return period;
            }
        }

        public ZoomLevel ZoomLevel
        {
            get {
                var zoomValue = ZoomLevel.Week;
                if (Arguments != null) {
                    zoomValue = (ZoomLevel)Arguments.GetInt (ReportZoomArgument, (int)zoomValue);
                }
                return zoomValue;
            }
        }

        public ReportsFragment ()
        {
        }

        public ReportsFragment (int period, ZoomLevel zoom) : base()
        {
            var args = new Bundle ();
            args.PutInt (ReportPeriodArgument, period);
            args.PutInt (ReportZoomArgument, (int)zoom);

            Arguments = args;
        }

        public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var pagerFragment = ParentFragment as ReportsPagerFragment;
            if (pagerFragment == null) {
                return new View (Activity);
            }

            controllerPool = pagerFragment.ReportsControllers;

            controller = controllerPool.Obtain();
            controller.SnapPosition = position;
            controller.SnapPositionChanged += OnControllerSnapPositionChanged;

            return controller.View;
        }

        public override void OnDestroyView ()
        {
            base.OnDestroyView ();

            if (controller != null) {
                controller.SnapPositionChanged -= OnControllerSnapPositionChanged;
                controllerPool.Release (controller);
                controller = null;
            }

            controllerPool = null;
        }

        public override void OnResume ()
        {
            base.OnResume ();
            EnsureLoaded ();
        }

        private void OnControllerSnapPositionChanged (object sender, EventArgs e)
        {
            // Cascade event to our listeners
            if (PositionChanged != null) {
                PositionChanged (this, EventArgs.Empty);
            }
        }

        public event EventHandler PositionChanged;

        public int Position
        {
            get {
                if (controller == null) {
                    return position;
                }
                return controller.SnapPosition;
            } set {
                position = value;
                if (controller != null) {
                    controller.SnapPosition = position;
                }
            }
        }

        public override bool UserVisibleHint
        {
            get { return base.UserVisibleHint; }
            set {
                base.UserVisibleHint = value;
                EnsureLoaded ();
            }
        }

        private async void EnsureLoaded ()
        {
            if (isLoading || !UserVisibleHint || controller == null || controller.Data != null) {
                return;
            }

            isLoading = true;
            try {
                var data = new SummaryReportView() {
                    Period = ZoomLevel,
                };
                await data.Load (Period);

                if (controller != null) {
                    controller.Data = data;
                }
            } finally {
                isLoading = false;
            }
        }

        public sealed class Controller : Java.Lang.Object, ViewGroup.IOnHierarchyChangeListener
        {
            private readonly List<View> trackedProjectListItems = new List<View>();
            private Context ctx;
            private Pool<View> projectListItemPool;
            private View rootView;
            private SnappyLayout snappyLayout;
            private BarChart barChart;
            private PieChart pieChart;
            private TextView totalValue;
            private ListView listView;
            private TextView billableValue;

            private SummaryReportView data;

            public Controller (Context ctx, Pool<View> projectListItemPool)
            {
                this.ctx = ctx;
                this.projectListItemPool = projectListItemPool;
                var inflater = LayoutInflater.From (ctx);

                var view = rootView = inflater.Inflate (Resource.Layout.ReportsFragment, null, false);
                snappyLayout = view.FindViewById<SnappyLayout> (Resource.Id.SnappyLayout);
                barChart = view.FindViewById<BarChart> (Resource.Id.BarChart);
                pieChart = view.FindViewById<PieChart> (Resource.Id.PieChart);
                listView = view.FindViewById<ListView> (Resource.Id.ReportList);
                totalValue = view.FindViewById<TextView> (Resource.Id.TotalValue);
                billableValue = view.FindViewById<TextView> (Resource.Id.BillableValue);

                snappyLayout.ActiveChildChanged += OnSnappyActiveChildChanged;

                pieChart.SliceClicked += OnSliceSelect;

                listView.LayoutMode = ViewLayoutMode.ClipBounds;
                listView.SetClipToPadding (false);
                listView.ItemClick += OnListItemClick;
                listView.SetOnHierarchyChangeListener (this);
            }

            protected override void Dispose (bool disposing)
            {
                base.Dispose (disposing);

                if (disposing) {
                    snappyLayout.ActiveChildChanged -= OnSnappyActiveChildChanged;
                    pieChart.SliceClicked -= OnSliceSelect;
                    listView.ItemClick -= OnListItemClick;
                    listView.SetOnHierarchyChangeListener (null);

                    DisposeAndNull (ref totalValue);
                    DisposeAndNull (ref billableValue);
                    DisposeAndNull (ref listView);
                    DisposeAndNull (ref barChart);
                    DisposeAndNull (ref pieChart);
                    DisposeAndNull (ref snappyLayout);
                    DisposeAndNull (ref rootView);
                }
            }

            private static void DisposeAndNull<T> (ref T disposable)
            where T : class, IDisposable
            {
                if (disposable != null) {
                    disposable.Dispose ();
                    disposable = null;
                }
            }

            private void OnListItemClick (object sender, AdapterView.ItemClickEventArgs args)
            {
                var adapter = (ReportProjectAdapter)listView.Adapter;
                if (pieChart.CurrentSlice == args.Position) {
                    pieChart.SelectSlice (-1);
                    adapter.SetFocus (-1);
                } else {
                    pieChart.SelectSlice (args.Position);
                    adapter.SetFocus (args.Position);
                }
            }

            private void OnSliceSelect (int pos)
            {
                var adapter = (ReportProjectAdapter)listView.Adapter;
                adapter.SetFocus (pos);
                if (pos != -1) {
                    listView.SmoothScrollToPositionFromTop (pos, 0);
                }
            }

            private void OnSnappyActiveChildChanged (object sender, EventArgs e)
            {
                // Cascade the event down to our listeners
                if (SnapPositionChanged != null) {
                    SnapPositionChanged (this, EventArgs.Empty);
                }
            }

            public View ObtainProjectListItem()
            {
                var v = projectListItemPool.Obtain ();
                trackedProjectListItems.Add (v);
                return v;
            }

            void ViewGroup.IOnHierarchyChangeListener.OnChildViewAdded (View parent, View child)
            {
            }

            void ViewGroup.IOnHierarchyChangeListener.OnChildViewRemoved (View parent, View child)
            {
                // Monitor when the ListView is done with the child and release it back to the pool
                if (trackedProjectListItems.Remove (child)) {
                    projectListItemPool.Release (child);
                }
            }

            public View View
            {
                get { return rootView; }
            }

            public Context Context
            {
                get { return ctx; }
            }

            public int SnapPosition
            {
                get { return snappyLayout.ActiveChild; }
                set { snappyLayout.ActiveChild = value; }
            }

            public event EventHandler SnapPositionChanged;

            public SummaryReportView Data
            {
                get { return data; }
                set {
                    if (value == data) {
                        return;
                    }

                    data = value;

                    if (data == null) {
                        // Reset everything to blank
                        totalValue.Text = String.Empty;
                        billableValue.Text = String.Empty;
                        barChart.Reset (null);
                        ResetPieChart (null);
                        listView.Adapter = null;
                    } else {
                        // Bind the data to the view
                        totalValue.Text = data.TotalGrand;
                        billableValue.Text = data.TotalBillale;
                        barChart.Reset (data);
                        ResetPieChart (data);
                        listView.Adapter = new ReportProjectAdapter (this, data.ListChartProjects);
                    }
                }
            }

            private void ResetPieChart (SummaryReportView data)
            {
                pieChart.Reset ();

                if (data != null) {
                    foreach (var project in data.PieChartProjects) {
                        var slice = new PieSlice ();
                        slice.Value = project.TotalTime;
                        if (project.Color == ProjectModel.GroupedProjectColorIndex) {
                            slice.Color = Color.ParseColor (ProjectModel.GroupedProjectColor);
                        } else {
                            slice.Color = Color.ParseColor (ProjectModel.HexColors [project.Color % ProjectModel.HexColors.Length]);
                        }
                        pieChart.AddSlice (slice);
                    }
                    pieChart.Refresh ();
                } else {
                    pieChart.Invalidate ();
                }
            }
        }

        private class ReportProjectAdapter : BaseAdapter<ReportProject>
        {
            private List<ReportProject> dataView;
            private int focus = -1;
            private Controller controller;

            public ReportProjectAdapter (Controller controller, List<ReportProject> dataView)
            {
                this.controller = controller;
                this.dataView = dataView;
            }

            public override Java.Lang.Object GetItem (int position)
            {
                return null;
            }

            public override long GetItemId (int position)
            {
                return position;
            }

            public override ReportProject this [int index]
            {
                get { return dataView[index]; }
            }

            public override View GetView (int position, View convertView, ViewGroup parent)
            {
                View view = convertView;

                if (convertView == null) {
                    view = controller.ObtainProjectListItem ();
                }
                var holder = (ProjectListItemHolder)view.Tag;
                holder.Bind (dataView [position]);
                holder.SetFocus (focus, position); // mmm...

                return view;
            }

            public void SetFocus (int selected)
            {
                focus = selected;
                NotifyDataSetChanged ();
            }

            public override int Count
            {
                get {
                    return dataView.Count;
                }
            }
        }

        public class ProjectListItemHolder : BindableViewHolder<ReportProject>
        {
            private View _root;

            public View ColorSquareView  { get; private set; }

            public TextView NameTextView  { get; private set; }

            public TextView DurationTextView  { get; private set; }

            public ProjectListItemHolder ( View root)  : base (root)
            {
                NameTextView = root.FindViewById<TextView> (Resource.Id.ProjectName).SetFont (Font.Roboto);

                ColorSquareView = root.FindViewById<View> (Resource.Id.ColorSquare);

                DurationTextView = root.FindViewById<TextView> (Resource.Id.ProjectDuration).SetFont (Font.Roboto);

                _root = root;
            }

            protected override void Rebind ()
            {
                if (String.IsNullOrEmpty (DataSource.Project)) {
                    NameTextView.SetText (Resource.String.ReportsListViewNoProject);
                } else if (DataSource.Color == ProjectModel.GroupedProjectColorIndex) {
                    NameTextView.SetText (String.Format ( _root.Context.GetText ( Resource.String.ReportsListViewGroupedProject), DataSource.Project), TextView.BufferType.Normal);
                } else {
                    NameTextView.Text = DataSource.Project;
                }

                DurationTextView.Text = DataSource.FormattedTotalTime;
                var squareDrawable = new GradientDrawable ();
                squareDrawable.SetCornerRadius (5);
                var color = (DataSource.Color == ProjectModel.GroupedProjectColorIndex) ? ProjectModel.GroupedProjectColor : ProjectModel.HexColors [ DataSource.Color % ProjectModel.HexColors.Length];
                squareDrawable.SetColor (Color.ParseColor (color));
                ColorSquareView.SetBackgroundDrawable (squareDrawable);
            }

            public void SetFocus ( int focus, int position )
            {
                var squareDrawable = (GradientDrawable)ColorSquareView.Background;
                if (focus != -1) {
                    _root.Alpha = (focus == position) ? 1 : 0.5f;
                    var radius = (focus == position) ? Convert.ToSingle ( ColorSquareView.Height / 2) : 5.0f;
                    squareDrawable.SetCornerRadius ( radius);
                } else {
                    _root.Alpha = 1;
                    squareDrawable.SetCornerRadius (5);
                }
            }
        }
    }
}
