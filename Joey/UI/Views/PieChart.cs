using System;
using System.Collections.Generic;
using Android.Animation;
using Android.Content;
using Android.Graphics;
using Android.Text;
using Android.Util;
using Android.Views;

namespace Toggl.Joey.UI.Views
{
    public delegate void SliceClickedEventHandler (int position);

    public class PieChart : View
    {
        private List<PieSlice> dataObject = new List<PieSlice> ();
        private Path canvasPath = new Path ();
        private Paint canvasPaint = new Paint ();
        private TextPaint textPaint = new TextPaint ();
        private Rect textBoundsRect = new Rect ();
        private Color emptyStateColor = Color.ParseColor ("#808080");
        private Color baseCircleColor = Color.ParseColor ("#EDEDED");
        private Color centerTextColor = Color.ParseColor ("#666666");
        private const int chartThickness = 65;
        private const int slicePadding = 20;
        private const float angleCorrection = 270;
        private int indexSelected = -1;
        private int deselectedIndex = -1;
        private int animationProgress;
        private int centerHeaderTextSize = 30;
        private int centerTextSize = 20;
        private long totalValue;
        private long selectedSliceValue;
        private float currentAngle;
        private float centerX;
        private float centerY;
        private float slideAnimationProgress;
        private float radius;
        private float innerRadius;
        private bool loadAnimate;

        public event SliceClickedEventHandler SliceClicked;

        public PieChart (Context context, IAttributeSet attrs) : base (context, attrs)
        {
        }

        public PieChart (Context context, IAttributeSet attrs, int defStyle) : base (context, attrs, defStyle)
        {
        }

        public void Reset ()
        {
            dataObject.Clear ();
            indexSelected = -1;
        }

        public void Refresh ()
        {
            InitializeDrawParams ();
            StartDrawAnimation ();
        }

        private string FormatMilliseconds (long ms)
        {
            var timeSpan = TimeSpan.FromMilliseconds (ms);
            return String.Format ("{0}:{1:mm\\:ss}", Math.Floor (timeSpan.TotalHours).ToString ("00"), timeSpan);
        }

        protected virtual void OnSliceSelected ()
        {
            var sliceClicked = SliceClicked;
            if (sliceClicked != null) {
                sliceClicked (indexSelected);
            }
        }

        public void AddSlice (PieSlice slice)
        {
            dataObject.Add (slice);
        }

        public void SelectSlice (int position)
        {
            if (position == -1) {
                deselectedIndex = indexSelected;
                StartSlideBackAnimation ();
                indexSelected = position;
            } else {
                indexSelected = position;
                StartSliceSlideAnimation ();
            }
        }

        private void InitializeDrawParams()
        {
            centerX = Width / 2;
            centerY = Height / 2;
            radius = centerX < centerY ? centerX : centerY;
            innerRadius = radius - chartThickness;
        }

        public override void Draw (Canvas canvas)
        {
            if (radius == 0) {
                InitializeDrawParams ();
            }
            totalValue = 0;
            foreach (PieSlice slice in dataObject) {
                totalValue += slice.Value;
            }
            currentAngle = 0;
            float loadAnimation = loadAnimate ? (float)animationProgress / 360F : 1F;
            float sliceSlideOutAnimation = slideAnimationProgress / (float)slicePadding;

            canvas.DrawColor (Color.Transparent);
            canvasPaint.Reset ();
            canvasPaint.AntiAlias = true;
            canvasPaint.TextAlign = Paint.Align.Center;
            canvasPath.Reset ();

            if (loadAnimate || dataObject.Count == 0) {
                canvasPath.AddCircle (centerX, centerY, radius - slicePadding, Path.Direction.Cw);
                canvasPath.AddCircle (centerX, centerY, innerRadius - slicePadding, Path.Direction.Ccw);
                canvasPaint.Color = baseCircleColor;
                canvas.DrawPath (canvasPath, canvasPaint);
            }

            if (dataObject.Count == 0) {
                canvasPaint.Color = emptyStateColor;
                canvasPaint.TextSize = centerHeaderTextSize;
                canvas.DrawText (Resources.GetText (Resource.String.ReportsPieChartEmptyHeader), centerX, centerY, canvasPaint);

                textPaint.TextAlign = Paint.Align.Center;
                textPaint.AntiAlias = true;
                textPaint.Color = emptyStateColor;
                textPaint.TextSize = centerTextSize;

                StaticLayout emptyStateText = new StaticLayout (
                    Resources.GetText (Resource.String.ReportsPieChartEmptyText),
                    textPaint,
                    300,
                    StaticLayout.Alignment.AlignNormal,
                    1,
                    0,
                    false
                );
                canvas.Translate (centerX, centerY + 10);
                emptyStateText.Draw (canvas);
                return;
            }

            int count = 0;
            float currentSweep;
            foreach (PieSlice slice in dataObject) {
                currentSweep = ((float)slice.Value / (float)totalValue) * 360F;

                slice.Path = slice.Path ?? new Path ();
                slice.Path.Reset ();
                if ((int)currentSweep == 360 && animationProgress == 360) { // only one project
                    slice.Path.AddCircle (centerX, centerY, radius - slicePadding, Path.Direction.Cw);
                    slice.Path.AddCircle (centerX, centerY, innerRadius - slicePadding, Path.Direction.Ccw);
                } else {
                    slice.Path.ArcTo (
                        new RectF (
                            centerX - radius + slicePadding,
                            centerY - radius + slicePadding,
                            centerX + radius - slicePadding,
                            centerY + radius - slicePadding
                        ),
                        loadAnimation * currentAngle + angleCorrection,
                        loadAnimation * currentSweep
                    );
                    slice.Path.ArcTo (
                        new RectF (
                            centerX - innerRadius + slicePadding,
                            centerY - innerRadius + slicePadding,
                            centerX + innerRadius - slicePadding,
                            centerY + innerRadius - slicePadding
                        ),
                        loadAnimation * (currentAngle + currentSweep) + angleCorrection,
                        loadAnimation * -currentSweep
                    );
                }

                if (indexSelected != count && indexSelected != -1) {
                    canvasPaint.Alpha = (int) (255 - sliceSlideOutAnimation * 127F); // fade out other slices if one is selected
                }
                if ((indexSelected == count || deselectedIndex == count) && (int)currentSweep != 360) {
                    var sliceSector = currentAngle + (currentSweep / 2);
                    var angleToRadian = sliceSector / (180 / Math.PI);
                    var dx = (float)Math.Sin (angleToRadian) * slicePadding;
                    var dy = (float)Math.Cos (angleToRadian) * slicePadding * -1;
                    slice.Path.Offset (dx * sliceSlideOutAnimation, dy * sliceSlideOutAnimation);
                    selectedSliceValue = slice.Value;
                }

                slice.Path.Close ();
                slice.Region = new Region (
                    (int) (centerX - radius),
                    (int) (centerY - radius),
                    (int) (centerX + radius),
                    (int) (centerY + radius)
                );

                canvasPaint.Color = slice.Color;
                canvas.DrawPath (slice.Path, canvasPaint);
                currentAngle += currentSweep;
                count++;
            }

            canvasPaint.Color = centerTextColor;
            canvasPaint.TextAlign = Paint.Align.Center;
            canvasPaint.TextSize = centerHeaderTextSize;
            textBoundsRect = new Rect ();
            string duration = FormatMilliseconds (selectedSliceValue > 0 ? selectedSliceValue : totalValue);
            canvasPaint.GetTextBounds (duration, 0, duration.Length, textBoundsRect);
            canvas.DrawText (duration, centerX, centerY + textBoundsRect.Height() / 2, canvasPaint);
        }

        public override bool OnTouchEvent (MotionEvent e)
        {
            Point point = new Point ();
            point.X = (int)e.GetX ();
            point.Y = (int)e.GetY ();

            int clickedSlice = -1;
            int count = 0;

            if (e.Action == MotionEventActions.Down) {

                // get selected
                foreach (PieSlice slice in dataObject) {
                    var r = new Region ();
                    r.SetPath (slice.Path, slice.Region);
                    if (r.Contains (point.X, point.Y)) {
                        clickedSlice = count;
                    }
                    count++;
                }

                // set selected and deselected index
                if (clickedSlice != -1) {
                    deselectedIndex = (clickedSlice == indexSelected) ? clickedSlice : -1;
                    indexSelected = (clickedSlice == indexSelected) ? -1 : clickedSlice;
                    return true;
                }

                // or deselect all
                if (indexSelected != -1) {
                    deselectedIndex = indexSelected;
                    indexSelected = -1;
                    return true;
                }

            }

            if (e.Action == MotionEventActions.Move) {
                return false;
            }

            if (e.Action == MotionEventActions.Up) {
                if (indexSelected != -1) {
                    StartSliceSlideAnimation ();
                } else {
                    StartSlideBackAnimation ();
                }

                OnSliceSelected ();
                return false;

            }
            return false;
        }

        public void StartDrawAnimation ()
        {
            loadAnimate = true;
            var animator = ValueAnimator.OfInt (1, 360);
            animator.SetDuration (750);
            animator.Update += (sender, e) => AnimationProgress = (int)e.Animation.AnimatedValue;
            animator.Start ();
        }

        public void StartSliceSlideAnimation ()
        {
            var animator = ValueAnimator.OfInt (1, slicePadding);
            animator.SetDuration (300);
            animator.Update += (sender, e) => SlideAnimationProgress = (float)e.Animation.AnimatedValue;
            animator.Start ();
        }

        public void StartSlideBackAnimation ()
        {
            var animator = ValueAnimator.OfInt (slicePadding, 1);
            animator.SetDuration (300);
            animator.Update += (sender, e) => SlideAnimationProgress = (float)e.Animation.AnimatedValue;
            animator.Start ();
        }

        public float SlideAnimationProgress
        {
            get {
                return slideAnimationProgress;
            } set {
                slideAnimationProgress = value;
                if (deselectedIndex != -1 && slideAnimationProgress == 1) {
                    deselectedIndex = -1;
                }
                PostInvalidate ();
            }
        }

        public int AnimationProgress
        {
            get {
                return animationProgress;
            } set {
                if (value == 360) {
                    loadAnimate = false;
                }
                animationProgress = value;

                PostInvalidate ();
            }
        }

        public int CurrentSlice
        {
            get {
                return indexSelected;
            } set {
                indexSelected = value;
                PostInvalidate ();
            }
        }

    }

    public class PieSlice
    {
        public string Title { get; set; }

        public Color Color { get; set; }

        public long Value { get; set; }

        public Path Path { get; set; }

        public Region Region { get; set; }
    }
}


