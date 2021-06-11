using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReactiveUI;

namespace WolvenKit.Functionality.Services
{
    public class ModelBase : ReactiveObject
    {

    }

    public interface IRecentlyUsedItemsService
    {
        ObservableCollection<RecentlyUsedItemModel> Items { get; set; }
        IObservable<bool> Updated { get; set; }
        ObservableCollection<RecentlyUsedItemModel> PinnedItems { get; set; }
        void RemoveItem(RecentlyUsedItemModel projectToDel);
        void AddItem(RecentlyUsedItemModel recentlyUsedItem);
        void PinItem(string parameter);
        void UnpinItem(string parameter);
    }

    public class RecentlyUsedItemsService : IRecentlyUsedItemsService
    {
        public RecentlyUsedItemsService()
        {
            Items = new ObservableCollection<RecentlyUsedItemModel>();
        }

        public ObservableCollection<RecentlyUsedItemModel> Items { get; set; }
        public IObservable<bool> Updated { get; set; }
        public ObservableCollection<RecentlyUsedItemModel> PinnedItems { get; set; }
        public void RemoveItem(RecentlyUsedItemModel projectToDel) => Items.Remove(projectToDel);

        public void AddItem(RecentlyUsedItemModel recentlyUsedItem) => Items.Add(recentlyUsedItem);

        public void PinItem(string parameter) => throw new NotImplementedException();

        public void UnpinItem(string parameter) => throw new NotImplementedException();
    }

    public class RecentlyUsedItemModel : ModelBase
    {
        public RecentlyUsedItemModel(string parameter, DateTime dateTime)
        {
            Name = parameter;
            DateTime = dateTime;
        }

        public string Name { get; set; }
        public DateTime DateTime { get; set; }
    }
}
