using System;
using System.Collections.Generic;
using System.Windows.Forms;
using HGLib;
using ListViewSort;

namespace VisualHG
{
    internal class PendingItemsListView : ListView
    {
        //array to cache items for the virtual list
        private ListViewItem[] _cache;

        //stores the index of the first item in the cache
        private int _firstItem;

        // pending files list
        public List<HGFileStatusInfo> _list = new List<HGFileStatusInfo>();

        // status images
        public ImageMapper _ImageMapper = new ImageMapper();

        // latest sorted column index
        private int _previouslySortedColumn = -1;

        // remember selected files to restore selection
        private SortOrder _SortOrder = SortOrder.Ascending;

        // ------------------------------------------------------------------------
        // construction - setup virtual list handler
        // ------------------------------------------------------------------------
        public PendingItemsListView()
        {
            SmallImageList = _ImageMapper.StatusImageList;

            //Hook up handlers for VirtualMode events.
            RetrieveVirtualItem += this_RetrieveVirtualItem;
            CacheVirtualItems += this_CacheVirtualItems;
            SearchForVirtualItem += this_SearchForVirtualItem;
            ColumnClick += this_ColumnClick;
        }

        // ------------------------------------------------------------------------
        // status item sorter callback routine
        // ------------------------------------------------------------------------
        public int compareInfoItem(HGFileStatusInfo a, HGFileStatusInfo b)
        {
            if (_SortOrder == SortOrder.Ascending)
            {
                if (_previouslySortedColumn <= 0)
                    return a.fileName.CompareTo(b.fileName);
                if (_previouslySortedColumn == 1)
                    return a.fullPath.CompareTo(b.fullPath);
            }
            else
            {
                if (_previouslySortedColumn <= 0)
                    return b.fileName.CompareTo(b.fileName);
                if (_previouslySortedColumn == 1)
                    return b.fullPath.CompareTo(b.fullPath);
            }
            return 0;
        }

        // ------------------------------------------------------------------------
        // sort content by column
        // ------------------------------------------------------------------------
        private void SortByColumn(int mewColumn)
        {
            // store current selected items
            Dictionary<string, int> selection;
            StoreSelection(out selection);

            // toggle sort order and set column icon
            if (_previouslySortedColumn == mewColumn)
                _SortOrder = _SortOrder == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;
            else
                _SortOrder = SortOrder.Ascending;

            LVSort.SetSortIcons(this, ref _previouslySortedColumn, mewColumn);
            _previouslySortedColumn = mewColumn;

            // sort items and clear the cache
            _list.Sort(compareInfoItem);
            _cache = null;
            RestoreSelection(selection);
            Invalidate(false);
        }

        // ------------------------------------------------------------------------
        // update pending items list by status tracker object
        // ------------------------------------------------------------------------
        public void UpdatePendingList(HgStatusTracker tracker)
        {
            Dictionary<string, int> selection;
            StoreSelection(out selection);

            if (_previouslySortedColumn == -1)
                SortByColumn(0);

            // create new pending list ..
            List<HGFileStatusInfo> newList;
            tracker.CreatePendingFilesList(out newList);
            newList.Sort(compareInfoItem);

            // .. and compare it to the current one
            var somethingChanged = false;
            if (_list == null || newList.Count != _list.Count)
                somethingChanged = true;

            for (var pos = 0; !somethingChanged && pos < _list.Count; ++pos)
            {
                if (_list[pos].status != newList[pos].status)
                    somethingChanged = true;
                if (_list[pos].fullPath.CompareTo(newList[pos].fullPath) != 0)
                    somethingChanged = true;
            }

            // if we found changes between the lists, we now update the view
            if (somethingChanged)
            {
                // set new list into listview
                _list = newList;
                _cache = null;
                VirtualListSize = _list.Count;

                RestoreSelection(selection);
                Invalidate(false);
            }
        }

        // ------------------------------------------------------------------------
        // store current selected items to a map
        // ------------------------------------------------------------------------
        private void StoreSelection(out Dictionary<string, int> selection)
        {
            selection = new Dictionary<string, int>();
            foreach (int index in SelectedIndices)
            {
                var info = _list[index];
                selection.Add(info.fullPath, 0);
            }
        }

        // ------------------------------------------------------------------------
        // restore given selection
        // ------------------------------------------------------------------------
        private void RestoreSelection(Dictionary<string, int> selection)
        {
            SelectedIndices.Clear();
            for (var pos = 0; pos < _list.Count; ++pos)
                if (selection.ContainsKey(_list[pos].fullPath))
                    SelectedIndices.Add(pos);
        }

        // ------------------------------------------------------------------------
        // get item index by status
        // ------------------------------------------------------------------------
        private int GetStateIcon(HGFileStatus status)
        {
            switch (status)
            {
                case HGFileStatus.scsMissing: return 5; // missing
                case HGFileStatus.scsModified: return 1; // modified
                case HGFileStatus.scsAdded: return 2; // added
                case HGFileStatus.scsRemoved: return 4; // removed
                case HGFileStatus.scsRenamed: return 3; // renamed
                case HGFileStatus.scsCopied: return 6; // copied
                case HGFileStatus.scsUncontrolled: return 5; // unknown
            }
            return 0;
        }

        // ------------------------------------------------------------------------
        // Dynamically returns a ListViewItem with the required properties;
        // ------------------------------------------------------------------------
        private void this_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
        {
            //check to see if the requested item is currently in the cache
            if (_cache != null && e.ItemIndex >= _firstItem && e.ItemIndex < _firstItem + _cache.Length)
            {
                //A cache hit, so get the ListViewItem from the cache instead of making a new one.
                e.Item = _cache[e.ItemIndex - _firstItem];
            }
            else
            {
                //A cache miss, so create a new ListViewItem and pass it back.
                if (e.ItemIndex < _list.Count)
                {
                    var info = _list[e.ItemIndex];
                    e.Item = new ListViewItem(info.fileName);
                    e.Item.ImageIndex = GetStateIcon(info.status);
                    e.Item.SubItems.Add(info.fullPath);
                }
            }
        }

        // ------------------------------------------------------------------------
        // Manages the cache. ListView calls this when it might need a 
        // cache refresh.
        // ------------------------------------------------------------------------
        private void this_CacheVirtualItems(object sender, CacheVirtualItemsEventArgs e)
        {
            //We've gotten a request to refresh the cache.
            //First check if it's really neccesary.
            if (_cache != null && e.StartIndex >= _firstItem && e.EndIndex <= _firstItem + _cache.Length)
                return;

            // now we need to rebuild the cache.
            _firstItem = e.StartIndex;
            var length = e.EndIndex - e.StartIndex + 1; //indexes are inclusive
            _cache = new ListViewItem[length];

            for (var i = 0; i < length; i++)
            {
                var index = i + _firstItem;
                if (index < _list.Count)
                {
                    var info = _list[index];
                    var item = new ListViewItem(info.fileName);
                    item.ImageIndex = GetStateIcon(info.status);
                    item.SubItems.Add(info.fullPath);
                    _cache[i] = item;
                }
            }
        }

        // ------------------------------------------------------------------------
        // This event handler enables search functionality, and is called
        // for every search request when in Virtual mode.
        // ------------------------------------------------------------------------
        private void this_SearchForVirtualItem(object sender, SearchForVirtualItemEventArgs e)
        {
            for (var pos = 0; pos < _list.Count; ++pos)
                if (_list[pos].fileName.StartsWith(e.Text, StringComparison.OrdinalIgnoreCase))
                {
                    e.Index = pos;
                    break;
                }
        }

        // ------------------------------------------------------------------------
        // column click handler - sort and upadte items
        // ------------------------------------------------------------------------
        private void this_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            SortByColumn(e.Column);
        }
    }
}