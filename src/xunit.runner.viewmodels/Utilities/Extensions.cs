using System;
using System.Collections.Generic;
using System.Text;

namespace Xunit.Runners
{
    static class Extensions
    {

        public static void ForEach<T>(this IEnumerable<T> This, Action<T> action)
        {
            foreach (var item in This)
                action(item);
        }

    }
}
