using Foundation;
using System;
using Toggl.Core.UI.ViewModels;
using Toggl.iOS.Cells;
using Toggl.Shared;
using UIKit;

namespace Toggl.iOS.Views.Tag
{
    public partial class CreateTagViewCell : BaseTableViewCell<SelectableTagBaseViewModel>
    {
        public static readonly string Identifier = nameof(CreateTagViewCell);
        public static readonly UINib Nib;

        static CreateTagViewCell()
        {
            Nib = UINib.FromName("CreateTagViewCell", NSBundle.MainBundle);
        }

        protected CreateTagViewCell(IntPtr handle) : base(handle)
        {
        }

        public override void AwakeFromNib()
        {
            base.AwakeFromNib();
            NameLabel.Text = string.Empty;
        }

        protected override void UpdateView()
        {
            NameLabel.Text = $"{Resources.CreateTag} \"{Item.Name.Trim()}\"";
        }
    }
}

