using NUnit.Framework;
using System.Collections.Generic;
using Common;

namespace ListSerializerTest
{
    public static class ListEqualHelper
    {
        /// <summary>
        /// Left list is correct. Slow(O(n2 + 2n)) but work. Only for test, not for work under load.
        /// </summary>
        public static void DeepEqual(ListNode left, ListNode right)
        {
            var listLeft = new List<ListNode>();
            ListNode current = null;
            do
            {
                if (current == null)
                {
                    current = left;
                }
                else
                {
                    current = current.Next;
                }

                listLeft.Add(current);
            }
            while (current.Next != null);

            var listRight = new List<ListNode>(listLeft.Count);
            current = null;
            do
            {
                if (current == null)
                {
                    current = right;
                }
                else
                {
                    current = current.Next;
                }

                listRight.Add(current);
            }
            while (current.Next != null);

            var listLeftLink = new List<int>(listLeft.Count);
            current = null;
            do
            {
                if (current == null)
                {
                    current = left;
                }
                else
                {
                    current = current.Next;
                }

                if(current.Random == null)
                {
                    listLeftLink.Add(-1);
                }
                else
                {
                    var link = -1;
                    for (int i = 0; i < listLeft.Count; i++)
                    {
                        if (object.ReferenceEquals(listLeft[i], current.Random))
                        {
                            link = i;
                            break;
                        }
                    }

                    Assert.AreNotEqual(link, -1);
                    listLeftLink.Add(link);
                }
            }
            while (current.Next != null);

            current = null;
            var currentId = 0;

            do
            {
                if (current == null)
                {
                    current = right;
                }
                else
                {
                    current = current.Next;
                }

                Assert.Less(currentId, listLeft.Count);

                var leftNode = listLeft[currentId];

                var linkIdLeft = listLeftLink[currentId];
                if(linkIdLeft == -1)
                {
                    Assert.IsNull(current.Random);
                }
                else
                {
                    var rightLinkNode = listRight[linkIdLeft];
                    Assert.AreSame(rightLinkNode, current.Random);
                }

                Assert.AreEqual(leftNode.Data, current.Data);
                currentId++;
            }
            while (current.Next != null);
        }
    }
}