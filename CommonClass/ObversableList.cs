using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;

namespace NCMMS.CommonClass
{
    public class ObversableList<T> : List<T> , INotifyCollectionChanged
    {

        public event NotifyCollectionChangedEventHandler CollectionChanged;
        protected void NotifyCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            if (CollectionChanged != null)
                CollectionChanged(this, args);
        }
        public virtual void Add(T item)
        {
            base.Add(item);
            NotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add));
        }
        public virtual bool Remove(T item)
        {
            bool b = base.Remove(item);
            NotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove));
            return b;
        }
    }
}
