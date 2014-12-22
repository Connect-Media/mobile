using Cirrious.FluentLayouts.Touch;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using System.Drawing;

namespace Toggl.Ross.Views
{
    public class LabelSwitchView : UIView
    {
        private readonly UILabel label;
        private readonly UISwitch toggle;

        public LabelSwitchView ()
        {
            Add (label = new UILabel () {
                TranslatesAutoresizingMaskIntoConstraints = false,
            });
            Add (toggle = new UISwitch () {
                TranslatesAutoresizingMaskIntoConstraints = false,
            });
        }

        public override void UpdateConstraints ()
        {
            if (Constraints.Length == 0) {
                this.AddConstraints (
                    toggle.AtRightOf (this, 15f),
                    toggle.WithSameCenterY (this),

                    label.AtLeftOf (this, 15f),
                    label.WithSameCenterY (this),
                    label.ToLeftOf (toggle, 5f),

                    null
                );
            }

            base.UpdateConstraints ();
        }

        public string Text
        {
            get { return label.Text; }
            set { label.Text = value; }
        }

        public UILabel Label
        {
            get { return label; }
        }

        public UISwitch Switch
        {
            get { return toggle; }
        }

        [Export ("requiresConstraintBasedLayout")]
        public static new bool RequiresConstraintBasedLayout ()
        {
            return true;
        }

        public override SizeF IntrinsicContentSize
        {
            get {
                return new SizeF ( Bounds.Width, 60f);
            }
        }

    }
}
