using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Toggl.Core.UI.Helper;
using Toggl.Core.UI.Views;
using Toggl.iOS.Extensions;
using Toggl.iOS.ViewSources.Common;
using UIKit;

namespace Toggl.iOS.ViewControllers.Common
{
    public sealed class SelectorViewController<T> : UIViewController
    {
        private readonly ImmutableList<SelectOption<T>> options;
        private readonly string title;
        private readonly Action<T> onChosen;
        private int selectedIndex;

        private UITableView tableView;
        private SelectorTableViewSource<T> source;

        public SelectorViewController(
            string title,
            IEnumerable<SelectOption<T>> options,
            int initialIndex,
            Action<T> onChosen)
        {
            selectedIndex = initialIndex;
            this.title = title;
            this.options = options.ToImmutableList();
            this.onChosen = onChosen;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            Title = title;
            
            tableView = new UITableView(View.Bounds);
            tableView.BackgroundColor = Colors.Settings.Background.ToNativeColor();
            tableView.TableFooterView = new UIView();
            View.AddSubview(tableView);

            source = new SelectorTableViewSource<T>(tableView, options, selectedIndex, onItemSelected);

            tableView.Source = source;
        }

        public override void ViewWillDisappear(bool animated)
        {
            base.ViewWillDisappear(animated);

            var selected = options[selectedIndex];
            onChosen(selected.Item);
        }

        private void onItemSelected(int selectedIndex)
        {
            this.selectedIndex = selectedIndex;

            if (NavigationController != null)
            {
                NavigationController.PopViewController(true);
            }
            else
            {
                this.Dismiss();
            }
        }
    }
}
