using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Herodotus;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;

namespace HerodotusTests
{
    [TestClass]
    public class ChangesetMergeTest
    {
        #region Delegates

        private delegate void ChangeAction();

        #endregion

        #region Methods

        [TestMethod]
        public void SimpleTest()
        {
            var manager = new LinearChangesetManager {IsTrackingEnabled = true};

            var list = new ObservableCollection<int> {1, 3, 7};
            BindCollectionToChangeManager<int>(manager, list);

            MakeChange(manager, () =>
            {
                list.Insert(1, 2);      // 1 2 3 7
                list.RemoveAt(2);       // 1 2 7
                list.Insert(2, 5);      // 1 2 5 7
            }, true);

            AssertListEqual(list, new List<int> {1, 2, 5, 7});

            manager.Undo();

            AssertListEqual(list, new List<int> {1, 3, 7});

            manager.Redo();

            AssertListEqual(list, new List<int> { 1, 2, 5, 7 });
        }
        
        [TestMethod]
        public void RandomListTest()
        {
            const int numTests = 1000;
            var random = new Random();
            var manager = new LinearChangesetManager {IsTrackingEnabled = true};
            for (var t = 0; t < numTests; t++)
            {
                var numOperations = random.Next(5, 100);
                SingleListRandomTest(manager, random, numOperations);
            }
        }

        [TestMethod]
        public void RandomSetTest()
        {
            const int numTests = 1000;
            var random = new Random(256);
            var manager = new LinearChangesetManager { IsTrackingEnabled = true };
            for (var t = 0; t < numTests; t++)
            {
                var numOperations = random.Next(5, 100);
                SingleSetRandomTest(manager, random, numOperations);
            }
        }

        private void SingleSetRandomTest(LinearChangesetManager manager, Random random, int numOperations,
            bool allowDuplicate = true, double addRate = 0.5)
        {
            var len = random.Next(3, 30);
            var set = new MockSet<int>();
            if (allowDuplicate)
            {
                TestHelper.GenerateRandomSequenceMaybeDuplicate(random, len, set);
            }
            else
            {
                TestHelper.GenerateRandomSequenceNonduplicate(random, len, set);
            }
            var refOrigSet = new HashSet<int>();
            set.CopyTo(refOrigSet);

            BindCollectionToChangeManager<int>(manager, set);

            MakeChange(manager, () =>
            {
                for (var i = 0; i < numOperations; i++)
                {
                    var op = random.NextDouble();
                    if (op < addRate)
                    {
                        // add
                        var numAdd = Math.Min(random.Next(1, (set.Count + 1) / 2 + 1), 500);
                        RandomAdd(random, set, numAdd, allowDuplicate);
                    }
                    else
                    {
                        // remove
                        var numRemove = Math.Min(random.Next(1, (set.Count + 1) / 2 + 1), set.Count);
                        RandomRemove(random, set, numRemove);
                    }
                }
            }, true);
        }

        private void SingleListRandomTest(LinearChangesetManager manager, Random random, int numOperations, 
            bool allowDuplicate=true, double addRate = 0.5)
        {
            var len = random.Next(3, 30);
            var list = new ObservableCollection<int>();
            if (allowDuplicate)
            {
                TestHelper.GenerateRandomSequenceMaybeDuplicate(random, len, list);
            }
            else
            {
                TestHelper.GenerateRandomSequenceNonduplicate(random, len, list);
            }
            var refOriglist = list.ToList();
            
            BindCollectionToChangeManager<int>(manager, list);

            MakeChange(manager, () =>
            {
                for (var i = 0; i < numOperations; i++)
                {
                    var op = random.NextDouble();
                    if (op < addRate)
                    {
                        // add
                        var numAdd = Math.Min(random.Next(1, (list.Count + 1)/2 + 1), 500);
                        RandomAdd(random, list, numAdd, allowDuplicate);
                    }
                    else
                    {
                        // remove
                        var numRemove = Math.Min(random.Next(1, (list.Count + 1) / 2 + 1), list.Count);
                        RandomRemove(random, list, numRemove);
                    }
                }
            }, true);

            var reList = list.ToList(); 

            manager.Undo();

            AssertListEqual(list, refOriglist);

            manager.Redo();

            AssertListEqual(list, reList);
        }

        private void RandomAdd(Random random, IList<int> list,
            int numAdd, bool allowDuplicate)
        {
            var add = new List<int>();
            if (allowDuplicate)
            {
                TestHelper.GenerateRandomSequenceMaybeDuplicate(random, numAdd, add);
            }
            else
            {
                var used = new HashSet<int>();
                foreach (var i in list)
                {
                    used.Add(i);
                }
                TestHelper.GenerateRandomSequenceNonduplicate(random, numAdd, add, used);
            }

            var at = random.Next(0, list.Count);
            foreach (var item in add)
            {
                list.Insert(at++, item);
            }
        }

        private void RandomRemove(Random random, IList<int> list, int numRemove)
        {
			var range = list.Count-numRemove;
			var at = random.Next(0, range);
			for (var i = 0; i < numRemove; i++)
			{
				list.RemoveAt(at); // in place removal
			}
        }

        private void RandomAdd(Random random, ISet<int> set, int numAdd, bool allowDuplicate)
        {
            var add = new List<int>();
            if (allowDuplicate)
            {
                TestHelper.GenerateRandomSequenceMaybeDuplicate(random, numAdd, add);
            }
            else
            {
                var used = new HashSet<int>();
                foreach (var i in set)
                {
                    used.Add(i);
                }
                TestHelper.GenerateRandomSequenceNonduplicate(random, numAdd, add, used);
            }

            foreach (var item in add)
            {
                set.Add(item);
            }
        }

        private void RandomRemove(Random random, ISet<int> set, int numRemove)
        {
            // selector
            var list = set.ToList();
            var seq = new List<int>();
            TestHelper.GenerateRandomSequenceNonduplicate(random, numRemove, seq, 0, numRemove);

            for (var i = 0; i < numRemove; i++)
            {
                var item = list[seq[i]];
                set.Remove(item);
            }
        }

        private static void AssertListEqual<T>(IList<T> list1, IList<T> list2)
        {
            Assert.IsTrue(list1.Count == list2.Count);
            for (var i = 0; i < list1.Count; i++)
            {
                if (!list1[i].Equals(list2[i]))
                {
                    Assert.Fail();
                }
            }
        }

        private static void BindCollectionToChangeManager<T>(TrackingManager manager, 
            INotifyCollectionChanged collection)
        {
            collection.CollectionChanged += (sender, args) =>
            {
                if (args.Action == NotifyCollectionChangedAction.Reset)
                {
                    manager.OnCollectionClearing((ICollection<T>)collection);
                }
                else
                {
                    manager.OnCollectionChanged<T>(sender, args);
                }
            };
        }

        private static void MakeChange(ITrackingManager manager, ChangeAction action, 
            bool merge=false, string name="Some changes")
        {
            if (manager == null) throw new ArgumentNullException("manager");
            manager.StartChangeset(name);

            action();

            manager.Commit(merge);
        }

        #endregion
    }
}
