﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Toggl.Phoebe;
using Toggl.Phoebe.Net;
using XPlatUtils;

namespace Toggl.Joey.UI.Components
{
    class ProfileImageView : ImageView
    {
        private static readonly string LogTag = "ProfileImageView";
        private static readonly int RectSize = 30; //In DP!

        public ProfileImageView(Context context) : base(context) {}
        public ProfileImageView(Context context, IAttributeSet attrs) : base(context, attrs) {}
        public ProfileImageView(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle) {}
        private String imageUrl;
        private Bitmap bitmap;


        public String ImageUrl {
            get{ return imageUrl; }
            set{
                SetImage (value);
            }
        }

        private async Task GetImage (String url)
        {
            try {
                var request = HttpWebRequest.Create (url);
                var resp = await request.GetResponseAsync ()
                    .ConfigureAwait (continueOnCapturedContext: false);
                var stream = resp.GetResponseStream ();
                bitmap = BitmapFactory.DecodeStream (stream);
                scaleImage ();
                cropImage ();
                makeImageRound ();
            } catch (Exception ex) {
                var log = ServiceContainer.Resolve<Logger> ();
                log.Debug (LogTag, ex, "Failed to get user profile image.");
            }
        }

        //Scaling image so that it has at least one of the sides be RectSize
        private void scaleImage ()
        {
            int rectSizePx = (int)TypedValue.ApplyDimension (ComplexUnitType.Dip, RectSize, Resources.DisplayMetrics);
            float minSize = (int)Math.Min (bitmap.Width, bitmap.Height);
            var scaleFactor = rectSizePx / minSize;
            int scaledWidth = (int)Math.Floor (scaleFactor * bitmap.Width);
            int scaledHeight = (int)Math.Floor (scaleFactor * bitmap.Height);
            bitmap = Bitmap.CreateScaledBitmap (bitmap, scaledWidth, scaledHeight, false);
        }

        //Make image rectangular
        private void cropImage ()
        {
            if (bitmap.Width >= bitmap.Height) {
                bitmap = Bitmap.CreateBitmap (bitmap, bitmap.Width / 2 - bitmap.Height / 2, 0, bitmap.Height, bitmap.Height);
            } else {
                bitmap = Bitmap.CreateBitmap (bitmap, 0, bitmap.Height / 2 - bitmap.Width / 2, bitmap.Width, bitmap.Width);
            }
        }

        private void makeImageRound ()
        {
            float roundPx = RectSize;

            Bitmap output = Bitmap.CreateBitmap (bitmap.Width, bitmap.Height, Bitmap.Config.Argb8888);
            Canvas canvas = new Canvas (output);

            Rect rect = new Rect (0, 0, bitmap.Width, bitmap.Height);
            RectF rectF = new RectF (rect);

            Paint paint = new Paint ();
            paint.AntiAlias = true;
            paint.Color = Color.Black;

            canvas.DrawARGB (0, 0, 0, 0);
            canvas.DrawRoundRect (rectF, roundPx, roundPx, paint);

            paint.SetXfermode (new PorterDuffXfermode (PorterDuff.Mode.SrcIn));

            canvas.DrawBitmap (bitmap, rect, rect, paint);

            bitmap = output;
        }

        private async void SetImage(String newImageUrl) {
            if (imageUrl == null || bitmap == null || imageUrl != newImageUrl) {
                await GetImage (newImageUrl);
                imageUrl = newImageUrl;
                SetImageBitmap (bitmap);
            }
        }
    }
}

