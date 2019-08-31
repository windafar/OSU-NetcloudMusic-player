using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PlayProjectGame.UserDefinedControl
{
    class ListViewExtend:ListView
    {
        protected override IEnumerator LogicalChildren => base.LogicalChildren;

        protected override int VisualChildrenCount => base.VisualChildrenCount;

        protected override Visual GetVisualChild(int index)
        {
            return base.GetVisualChild(index);
        }

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            return base.ArrangeOverride(arrangeBounds);
        }

        protected override Size MeasureOverride(Size constraint)
        {
            return base.MeasureOverride(constraint);
        }
    }
}
