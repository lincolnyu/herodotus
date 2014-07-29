using System;
using System.Collections.Generic;

namespace HerodotusTests
{
    public static class TestHelper
    {
        #region Methods

        public static void GenerateRandomSequenceNonduplicate(Random random, int length, ICollection<int> list, ISet<int> used=null)
        {
            var adopted = new HashSet<int>();

            for (var i = 0; i < length; i++)
            {
                int v;
                do
                {
                    v = random.Next();
                } while (adopted.Contains(v) || used != null && used.Contains(v));
                adopted.Add(v);
            }

            foreach (var num in adopted)
            {
                list.Add(num);   
            }
        }

        public static void GenerateRandomSequenceNonduplicate(Random random, int length, ICollection<int> list, int min,
            int max)
        {
            var adopted = new HashSet<int>();

            for (var i = 0; i < length; i++)
            {
                int v;
                do
                {
                    v = random.Next(min, max);
                } while (adopted.Contains(v));
                adopted.Add(v);
            }

            foreach (var num in adopted)
            {
                list.Add(num);
            }
        }

        public static void GenerateRandomSequenceMaybeDuplicate(Random random, int length, ICollection<int> list)
        {
            for (var i = 0; i < length; i++)
            {
                var v = random.Next();
                list.Add(v);
            }
        }

        #endregion
    }
}
