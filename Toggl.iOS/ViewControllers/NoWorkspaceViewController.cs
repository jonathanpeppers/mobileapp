﻿using CoreGraphics;
using System;
using System.Reactive.Linq;
using Toggl.Core.UI.Helper;
using Toggl.Core.UI.ViewModels;
using Toggl.iOS.Extensions;
using Toggl.iOS.Extensions.Reactive;
using Toggl.Shared;
using Toggl.Shared.Extensions;
using UIKit;

namespace Toggl.iOS.ViewControllers
{
    public sealed partial class NoWorkspaceViewController
        : ReactiveViewController<NoWorkspaceViewModel>
    {
        private const float cardHeight = 368;

        public NoWorkspaceViewController(NoWorkspaceViewModel viewModel) : base(viewModel, nameof(NoWorkspaceViewController))
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            CreateWorkspaceButton.SetTitle(Resources.CreateNewWorkspace, UIControlState.Normal);
            HeadingLabel.Text = Resources.UhOh;
            TextLabel.Text = Resources.NoWorkspaceErrorMessage;

            var screenWidth = UIScreen.MainScreen.Bounds.Width;
            PreferredContentSize = new CGSize
            {
                // ScreenWidth - 32 for 16pt margins on both sides
                Width = screenWidth > 320 ? screenWidth - 32 : 312,
                Height = cardHeight
            };

            prepareViews();

            CreateWorkspaceButton.Rx()
                .BindAction(ViewModel.CreateWorkspaceWithDefaultName)
                .DisposedBy(DisposeBag);

            TryAgainButton.Rx()
                .BindAction(ViewModel.TryAgain)
                .DisposedBy(DisposeBag);

            ViewModel.IsLoading
                .Invert()
                .Subscribe(CreateWorkspaceButton.Rx().Enabled())
                .DisposedBy(DisposeBag);

            ViewModel.IsLoading
                .Invert()
                .Subscribe(TryAgainButton.Rx().IsVisibleWithFade())
                .DisposedBy(DisposeBag);

            ViewModel.IsLoading.StartWith(false)
                .Subscribe(ActivityIndicatorView.Rx().IsVisibleWithFade())
                .DisposedBy(DisposeBag);
        }

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);
            ActivityIndicatorView.StartSpinning();
        }

        private void prepareViews()
        {
            ActivityIndicatorView.IndicatorColor = Colors.NoWorkspace.ActivityIndicator.ToNativeColor();
            CreateWorkspaceButton.SetTitleColor(Colors.NoWorkspace.DisabledCreateWorkspaceButton.ToNativeColor(), UIControlState.Disabled);
        }
    }
}
