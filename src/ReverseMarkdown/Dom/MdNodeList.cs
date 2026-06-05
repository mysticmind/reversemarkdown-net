using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ReverseMarkdown.Dom
{
    /// <summary>
    /// A child collection that maintains the <see cref="MdNode.Parent"/> back-pointer as
    /// items are added, removed or replaced. Container nodes expose one of these so the
    /// mutable-DOM invariant (every node has at most one parent) holds automatically.
    /// </summary>
    public sealed class MdNodeList<T> : Collection<T> where T : MdNode
    {
        private readonly MdNode _owner;

        internal MdNodeList(MdNode owner)
        {
            _owner = owner;
        }

        public void AddRange(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                Add(item);
            }
        }

        protected override void InsertItem(int index, T item)
        {
            item.Parent = _owner;
            base.InsertItem(index, item);
        }

        protected override void SetItem(int index, T item)
        {
            this[index].Parent = null;
            item.Parent = _owner;
            base.SetItem(index, item);
        }

        protected override void RemoveItem(int index)
        {
            this[index].Parent = null;
            base.RemoveItem(index);
        }

        protected override void ClearItems()
        {
            foreach (var item in this)
            {
                item.Parent = null;
            }

            base.ClearItems();
        }
    }
}
