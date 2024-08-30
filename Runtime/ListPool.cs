using System.Collections;
using System.Collections.Generic;
using uPools;

namespace Freya
{
    public static class ListPool<T>
    {
        private static readonly ObjectPool<List<T>> pool = new ObjectPool<List<T>>(() => new List<T>());
        private static readonly List<List<T>> used = new List<List<T>>();

        public static List<T> Create()
        {
            var item = pool.Rent();
            item.Clear();
            used.Add(item);
            return item;
        }

        public static void RecyclePool()
        {
            foreach (var item in used)
            {
                pool.Return(item);
            }
            used.Clear();
        }
    }
}