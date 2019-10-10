using System;
using System.Collections.Generic;
using System.Reactive;
using Android.Content.Res;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Toggl.Core.UI.ViewModels.Calendar.ContextualMenu;
using Toggl.Droid.Extensions;

namespace Toggl.Droid.ViewHolders
{
    public class CalendarMenuActionCellViewHolder: BaseRecyclerViewHolder<CalendarMenuAction>
    {
        private FrameLayout iconCircleOverlay;
        private ImageView icon;
        private TextView title;
        
        public static CalendarMenuActionCellViewHolder Create(View itemView)
            => new CalendarMenuActionCellViewHolder(itemView);
        
        public CalendarMenuActionCellViewHolder(View itemView) : base(itemView)
        {
        }

        public CalendarMenuActionCellViewHolder(IntPtr handle, JniHandleOwnership ownership) : base(handle, ownership)
        {
        }

        protected override void InitializeViews()
        {
            icon = ItemView.FindViewById<ImageView>(Resource.Id.Icon);
            title = ItemView.FindViewById<TextView>(Resource.Id.Text);
            iconCircleOverlay = ItemView.FindViewById<FrameLayout>(Resource.Id.Circle);
        }

        protected override void UpdateView()
        {
            title.Text = Item.Title;
            icon.SetImageResource(icons[Item.ActionKind]);
            var overlayColor = ItemView.Context.SafeGetColor(colors[Item.ActionKind]);
            iconCircleOverlay.BackgroundTintList = ColorStateList.ValueOf(overlayColor);
        }

        protected override void OnItemViewClick(object sender, EventArgs args)
        {
            Item.MenuItemAction.Execute(Unit.Default);
        }
        
        private static readonly Dictionary<CalendarMenuActionKind, int> icons = new Dictionary<CalendarMenuActionKind, int>
        {
            { CalendarMenuActionKind.Discard, Resource.Drawable.close },
            { CalendarMenuActionKind.Edit, Resource.Drawable.unsynced }, //todo: get res from designer
            { CalendarMenuActionKind.Save, Resource.Drawable.check }, //todo: get res from designer
            { CalendarMenuActionKind.Delete, Resource.Drawable.delete }, //todo: get res from designer
            { CalendarMenuActionKind.Copy, Resource.Drawable.unsynced }, //todo: get res from designer
            { CalendarMenuActionKind.Start, Resource.Drawable.play }, //todo: get res from designer
            { CalendarMenuActionKind.Continue, Resource.Drawable.play }, //todo: get res from designer
            { CalendarMenuActionKind.Stop, Resource.Drawable.ic_stop } //todo: get res from designer
        };
        
        private static readonly Dictionary<CalendarMenuActionKind, int> colors = new Dictionary<CalendarMenuActionKind, int>
        {
            { CalendarMenuActionKind.Discard, Resource.Color.contextualMenuDiscardIconOverlay },
            { CalendarMenuActionKind.Edit, Resource.Color.contextualMenuEditIconOverlay },
            { CalendarMenuActionKind.Save, Resource.Color.contextualMenuSaveIconOverlay },
            { CalendarMenuActionKind.Delete, Resource.Color.contextualMenuDeleteIconOverlay },
            { CalendarMenuActionKind.Copy, Resource.Color.contextualMenuCopyIconOverlay },
            { CalendarMenuActionKind.Start, Resource.Color.contextualMenuStartIconOverlay },
            { CalendarMenuActionKind.Continue, Resource.Color.contextualMenuContinueIconOverlay },
            { CalendarMenuActionKind.Stop, Resource.Color.contextualMenuStopIconOverlay }
        };
    }
}