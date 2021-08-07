using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SKX
{

    /// <summary>
    /// Represents an ordered set of items that can be modified while being
    /// enumerated without impact (unlike every other useful collection
    /// that comes with .NET).  This is achieved by storing the items
    /// both in a typical List, but also in a private linked list that can be
    /// safely modified during enumeration because it's walked in a different
    /// direction from the direction in which the list expands (the list expands
    /// on the end (new items are added to the end), but is enumerated 
    /// from the last element added back to the first).
    /// </summary>
    public class SafeList<TItem> : IEnumerable<TItem>
    {

        /// <summary>
        /// Represents an IEnumerator that works on FragList's.
        /// </summary>
        /// <typeparam name="TObject">The type of object to enumerate</typeparam>
        public class NillEnumerator<TObject> : IEnumerator<TObject>
        {
            int i;
            SafeList<TObject> source;

            public NillEnumerator(SafeList<TObject> src)
            {
                source = src;
                i = src.Count;
            }

            public TObject Current => source[i];

            object IEnumerator.Current => source[i];

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                return (i-- > 0);
            }

            public void Reset()
            {
                i = source.Count;
            }
        }

        /// <summary>The internal linked list used for enumeration</summary>
        internal protected LinkedList<TItem> InternalLinkedList { get; private set; } = new LinkedList<TItem>();
        /// <summary>The internal linked list used for indexing</summary>
        internal protected List<TItem> InternalList { get; private set; } = new List<TItem>();
        /// <summary>Whether or not the collection is R/O</summary>
        internal protected bool ReadOnly = false;
        /// <summary>Creates a read-only version of the list for use by others</summary>
        public ReadOnlyList<TItem> AsReadOnly() => new ReadOnlyList<TItem>(this);

        /// <summary>
        /// Creates a new FragList, optionally from a source
        /// </summary>
        /// <param name="source"></param>
        /// <param name="makeRef"></param>
        /// <param name="makeReadOnly"></param>
        public SafeList(IEnumerable<TItem> source = null, bool makeRef = true, bool makeReadOnly = false)
        {
            ReadOnly = makeReadOnly;
            if (source != null)
            {
                if (makeRef && source is LinkedList<TItem>)
                {
                    Try.DuringEnumeration(() =>
                    {
                        InternalLinkedList = (LinkedList<TItem>)source;
                        InternalList = new List<TItem>(source);
                    });
                }
                else
                {
                    Try.DuringEnumeration(() =>
                    {
                        InternalLinkedList = new LinkedList<TItem>(source);
                        InternalList = new List<TItem>(source);
                    });

                }

            }
        }

        internal protected virtual void Add(TItem item)
        {
            InternalLinkedList.AddLast(item);
            InternalList.Add(item);
        }
        internal protected virtual bool Remove(TItem item)
        {
            var b = InternalLinkedList.Remove(item);
            b |= InternalList.Remove(item);
            return b;
        }
        internal protected virtual void Clear()
        {
            InternalLinkedList.Clear();
            InternalList.Clear();
        }
        internal protected virtual void AddRange(IEnumerable<TItem> collection)
        {
            if (collection == null) return;
            foreach (TItem x in collection)
            {
                Add(x);
            }
        }
        internal protected virtual void Reverse()
        {
            InternalList.Reverse();
            InternalLinkedList.Reverse();
        }

        public virtual bool Contains(TItem item)
        {
            return InternalList.Contains(item);
        }
        public virtual int Count => Math.Max(InternalLinkedList.Count, InternalList.Count);

        public bool IsConsistent => (InternalLinkedList.Count == InternalList.Count);

        public virtual TItem[] ToArray() => InternalList.ToArray();

        public IEnumerator<TItem> GetEnumerator() => new NillEnumerator<TItem>(InternalList);
        IEnumerator IEnumerable.GetEnumerator() => new NillEnumerator<TItem>(InternalList);

        public static implicit operator LinkedList<TItem>(SafeList<TItem> a) => a.InternalLinkedList;
        public static implicit operator SafeList<TItem>(List<TItem> a) => FromList(a);
        public static implicit operator SafeList<TItem>(LinkedList<TItem> a) => FromList(a);

        public TItem this[int index]
        {
            get => InternalList[index];
        }

        public static SafeList<TItem> FromList(List<TItem> list)
        {
            return new SafeList<TItem>(list);
        }
        public static SafeList<TItem> FromList(LinkedList<TItem> list)
        {
            return new SafeList<TItem>(list);
        }

        public SafeList<TItem> Clone()
        {
            return new SafeList<TItem>(InternalLinkedList, false, ReadOnly);
        }


    }

    public class ReadOnlyList<t> : IEnumerable<t>
    {
        private IEnumerable<t> parent;

        internal ReadOnlyList(IEnumerable<t> list)
        {
            parent = list;
        }

        public IEnumerator<t> GetEnumerator()
        {
            return parent.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return parent.GetEnumerator();
        }

    }

    /// <summary>
    /// Helper functions to help you do something after a few short retries.
    /// </summary>
    public static class Try
    {
        /// <summary>
        /// Executes a task and if a collection enumeration/modification exception is
        /// thrown, the operation is retried as specified.
        /// </summary>
        /// <param name="action">The action to carry out</param>
        /// <param name="retries">Max number of retries</param>
        /// <param name="delay">Number of milliseconds to delay after each failure</param>
        /// <returns>Returns the number of tries required, or -1 if retries exceeded</returns>
        public static int DuringEnumeration(System.Action action, int retries = 3, int delay = 250)
        {
            return Retry(action, retries, delay,
                (f) => f is InvalidOperationException ios &&
                (f.Message?.Contains("modified") ?? false));
        }

        /// <summary>
        /// Executes a task and if a collection enumeration/modification exception is
        /// thrown, the operation is retried as specified.
        /// </summary>
        /// <param name="action">The action to carry out</param>
        /// <param name="retries">Max number of retries</param>
        /// <param name="delay">Number of milliseconds to delay after each failure</param>
        /// <returns>Returns the number of tries required, or -1 if retries exceeded</returns>
        public async static Task<int> DuringEnumerationAsync(System.Action action, int retries = 3, int delay = 250)
        {
            return await RetryAsync(action, retries, delay,
                (f) => f is InvalidOperationException ios &&
                (f.Message?.Contains("modified") ?? false));
        }

        /// <summary>
        /// Executes a task and retries upon exception as specified
        /// </summary>
        /// <param name="action">The action to carry out</param>
        /// <param name="retries">Max number of retries</param>
        /// <param name="delay">Number of milliseconds to delay after each failure</param>
        /// <param name="filter">If provided, a delegate called to determine if an exception
        /// should be retried (true) or immediately thrown (false).</param>
        /// <returns>Returns the number of tries required, or -1 if retries exceeded</returns>
        public static int Retry(System.Action action, int retries = 3, int delay = 250,
                                Func<Exception, bool> filter = null)
        {
            var t = 0;
            if (delay < 1) delay = 1;

            while (t++ < retries)
            {
                try
                {
                    action?.Invoke();
                    return t;
                }
                catch (Exception ex)
                {
                    if (filter != null && !filter.Invoke(ex))
                        throw;
                }
                Task.Delay(delay);
            }

            return -1;
        }

        /// <summary>
        /// Executes a task and retries upon exception as specified
        /// </summary>
        /// <param name="action">The action to carry out</param>
        /// <param name="retries">Max number of retries</param>
        /// <param name="delay">Number of milliseconds to delay after each failure</param>
        /// <param name="filter">If provided, a delegate called to determine if an exception
        /// should be retried (true) or immediately thrown (false).</param>
        /// <returns>Returns the number of tries required, or -1 if retries exceeded</returns>
        public static async Task<int> RetryAsync(System.Action action, int retries = 3, int delay = 250,
                                Func<Exception, bool> filter = null)
        {
            var t = 0;
            if (delay < 1) delay = 1;

            while (t++ < retries)
            {
                try
                {
                    action?.Invoke();
                    return t;
                }
                catch (Exception ex)
                {
                    if (filter != null && !filter.Invoke(ex))
                        throw;
                }
                await Task.Delay(delay);
            }

            return -1;
        }
    }
}
