using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageTracerNet.Extensions
{
    //http://stackoverflow.com/a/27455822/294804
    public static class CollectionExtensions
    {
        public static void Add<T1, T2>(this IList<Tuple<T1, T2>> list, T1 item1, T2 item2)
        {
            list.Add(Tuple.Create(item1, item2));
        }

        public static void Add<T1, T2, T3>(this IList<Tuple<T1, T2, T3>> list, T1 item1, T2 item2, T3 item3)
        {
            list.Add(Tuple.Create(item1, item2, item3));
        }

        public static void Add<T1, T2, T3>(this IDictionary<Tuple<T1, T2>, T3> dictionary, T1 key1, T2 key2, T3 value)
        {
            dictionary.Add(Tuple.Create(key1, key2), value);
        }
    }
}
