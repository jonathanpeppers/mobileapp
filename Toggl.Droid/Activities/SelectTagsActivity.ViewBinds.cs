using Android.Support.V7.Widget;
using Android.Widget;
using Toggl.Droid.Adapters;

namespace Toggl.Droid.Activities
{
    public partial class SelectTagsActivity
    {
        private readonly SelectTagsRecyclerAdapter selectTagsRecyclerAdapter = new SelectTagsRecyclerAdapter();

        private EditText textField;
        private RecyclerView selectTagsRecyclerView;

        protected override void InitializeViews()
        {
            textField = FindViewById<EditText>(Resource.Id.TextField);
            selectTagsRecyclerView = FindViewById<RecyclerView>(Resource.Id.SelectTagsRecyclerView);

            textField.Hint = Shared.Resources.AddTags;
            var layoutManager = new LinearLayoutManager(this);
            layoutManager.ItemPrefetchEnabled = true;
            layoutManager.InitialPrefetchItemCount = 4;
            selectTagsRecyclerView.SetLayoutManager(layoutManager);
            selectTagsRecyclerView.SetAdapter(selectTagsRecyclerAdapter);
            
            SetupToolbar();
        }
    }
}
