using System;
using System.Collections.Generic;
using System.Linq;

namespace truepele.Common
{
    public static class RandomExtensions
    {
        public static IEnumerable<int> NextIntEnumerable(this Random random, int count, int? maxValue = null)
        {
            if (random == null) throw new ArgumentNullException(nameof(random));

            if (count < 1) throw new ArgumentException($"{nameof(count)}", nameof(count));

            return Enumerable
                .Range(0, count)
                .Select(_ => maxValue.HasValue ? random.Next(maxValue.Value) : random.Next());
        }
    }
}