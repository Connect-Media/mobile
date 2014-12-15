using System;
using Android.Animation;
using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Util;
using Android.Views;

namespace Toggl.Joey.UI.Views
{
    public class SnappyLayout : ViewGroup
    {
        private int activeChild;
        private float translateY;
        private float maxTranslateY;
        private float scrollThreshold;
        private float touchY;
        private Animator scrollAnim;
        private Rect scrollableArea;

        public SnappyLayout (Context ctx) : base (ctx)
        {
            Initialize ();
        }

        public SnappyLayout (IntPtr javaRef, JniHandleOwnership xfer) : base (javaRef, xfer)
        {
            Initialize ();
        }

        public SnappyLayout (Context ctx, IAttributeSet attrs) : base (ctx, attrs)
        {
            Initialize ();
        }

        public SnappyLayout (Context ctx, IAttributeSet attrs, int defStyle) : base (ctx, attrs, defStyle)
        {
            Initialize ();
        }

        private void Initialize()
        {
            var dm = Resources.DisplayMetrics;

            scrollThreshold = TypedValue.ApplyDimension (ComplexUnitType.Dip, 75, dm);
            scrollableArea = new Rect ();
        }

        private void ForEachChild (Action<View> act)
        {
            var count = ChildCount;
            for (var i = 0; i < count; i++) {
                act (GetChildAt (i));
            }
        }

        private void ForEachChild (Action<int, View> act)
        {
            var count = ChildCount;
            for (var i = 0; i < count; i++) {
                act (i, GetChildAt (i));
            }
        }

        private void UpdateChildrenTranslationY ()
        {
            translateY = Math.Min (0, Math.Max (maxTranslateY, translateY));

            ForEachChild (child => {
                child.TranslationY = translateY;
            });
        }

        public Rect ScrollableArea
        {
            get { return scrollableArea; }
            set {
                if (value == scrollableArea) {
                    return;
                }
                scrollableArea = value;
            }
        }

        public int ActiveChild
        {
            get { return activeChild; }
            set {
                if (value == activeChild) {
                    return;
                }

                SetActiveChild (value);

                CancelAnimations ();
                translateY = -GetChildAt (activeChild).Top;
                UpdateChildrenTranslationY ();
            }
        }

        private void SetActiveChild (int value)
        {
            if (value >= ChildCount) {
                throw new ArgumentOutOfRangeException ("value", "Invalid child index.");
            }

            activeChild = value;

            if (ActiveChildChanged != null) {
                ActiveChildChanged (this, EventArgs.Empty);
            }
        }

        public event EventHandler ActiveChildChanged;

        private void CancelAnimations()
        {
            // Stop any current animations
            if (scrollAnim != null) {
                scrollAnim.Cancel ();
                scrollAnim = null;
            }
        }

        public override bool OnTouchEvent (MotionEvent e)
        {
            switch (e.Action) {
            case MotionEventActions.Down:
                touchY = e.RawY;
                CancelAnimations ();
                return true;
            case MotionEventActions.Move:
                var dy = e.RawY - touchY;
                touchY = e.RawY;

                translateY += dy;
                UpdateChildrenTranslationY ();

                return true;
            case MotionEventActions.Up:
            case MotionEventActions.Cancel:
                if (ChildCount == 0) {
                    translateY = 0;
                } else {
                    // Determine new ActiveChild
                    var active = GetChildAt (ActiveChild);
                    var offset = -translateY - active.Top;

                    if (offset < 0 && ActiveChild > 0) {
                        // Scroll to one of the previous children
                        var previous = GetChildAt (ActiveChild - 1);
                        // Adjust the scroll threshold to account for children smaller than that
                        var threshold = Math.Min (previous.Height / 2f, scrollThreshold);

                        if (-offset > threshold) {
                            // Determine which child is the new active one
                            ForEachChild ((i, child) => {
                                var y = -translateY;
                                if (child.Top <= y && y <= child.Bottom) {
                                    SetActiveChild (i);
                                    active = child;
                                }
                            });
                        }

                    } else if (offset > 0 && ActiveChild + 1 < ChildCount) {
                        // Scroll to one of the following children
                        // Adjust the scroll threshold to account for children smaller than that
                        var threshold = Math.Min (active.Height / 2f, scrollThreshold);

                        if (offset > threshold) {
                            if (-translateY <= active.Bottom) {
                                // Just the next child
                                SetActiveChild (ActiveChild + 1);
                                active = GetChildAt (ActiveChild);
                            } else {
                                // Scroll many children at once
                                ForEachChild ((i, child) => {
                                    var y = -translateY;
                                    if (child.Top <= y && y <= child.Bottom) {
                                        SetActiveChild (i);
                                        active = child;
                                    }
                                });
                            }
                        }
                    }

                    // Scroll to active
                    var anim = ValueAnimator.OfFloat (translateY, -active.Top);
                    anim.Update += OnScrollAnimationUpdate;
                    anim.SetDuration (250);
                    anim.Start ();
                    scrollAnim = anim;

                    UpdateChildrenTranslationY ();
                }
                return true;
            }

            return base.OnTouchEvent (e);
        }

        public override bool OnInterceptTouchEvent (MotionEvent ev)
        {
            if (ev.Action == MotionEventActions.Move ) {
                if ( ev.GetY() > scrollableArea.Top && ev.GetY() < scrollableArea.Bottom) {
                    return true;
                }
            }
            return base.OnInterceptTouchEvent (ev);
        }

        private void OnScrollAnimationUpdate (object sender, ValueAnimator.AnimatorUpdateEventArgs e)
        {
            translateY = (float)e.Animation.AnimatedValue;
            UpdateChildrenTranslationY ();
        }

        protected override void OnMeasure (int widthMeasureSpec, int heightMeasureSpec)
        {
            var widthSize = MeasureSpec.GetSize (widthMeasureSpec);
            var widthMode = MeasureSpec.GetMode (widthMeasureSpec);
            var heightSize = MeasureSpec.GetSize (heightMeasureSpec);
            var heightMode = MeasureSpec.GetMode (heightMeasureSpec);

            ForEachChild (child => {
                var childWidth = widthMeasureSpec;
                if (widthMode != MeasureSpecMode.Unspecified) {
                    childWidth = MeasureSpec.MakeMeasureSpec (widthSize, MeasureSpecMode.AtMost);
                }
                var childHeight = heightMeasureSpec;
                if (heightMode != MeasureSpecMode.Unspecified) {
                    childHeight = MeasureSpec.MakeMeasureSpec (heightSize, MeasureSpecMode.AtMost);
                }

                child.Measure (childWidth, childHeight);
            });

            SetMeasuredDimension (widthSize, heightSize);
        }

        protected override void OnLayout (bool changed, int l, int t, int r, int b)
        {
            var width = r - l;
            var currentTop = 0;

            ForEachChild (child => {
                var childHeight = child.MeasuredHeight;

                child.Layout (0, currentTop, width, currentTop + childHeight);
                maxTranslateY = -currentTop;
                currentTop += childHeight;
            });
        }
    }
}
