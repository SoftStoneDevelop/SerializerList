using System;
using System.Collections.Generic;

namespace Common
{
    public static class ListNodeInstanceHelper
    {
        /// <summary>
        /// Create random ListNode
        /// </summary>
        /// <param name="depth">depth created ListNode</param>
        /// <remarks>execution speed is not important: method for tests</remarks>
        public static ListNode CreateRandomListNode(int depth, bool withDuplicateDatas = true)
        {
            var head = new ListNode
            {
                Data = "Head"
            };
            var previus = head;

            var listAllNodes = new List<ListNode>(depth) {head};

            bool sameAsPrev = false;
            int nullCount = 0;
            for (int i = 0; i < depth; i++)
            {
                var next = new ListNode();
                if (nullCount++ != 4)
                {
                    if (withDuplicateDatas && sameAsPrev)
                    {
                        next.Data = $"Node № {i}" + new string('Q', i);
                        sameAsPrev = false;
                    }
                    else
                    {
                        next.Data = $"Node № {i + 1}" + new string('Q', i + 1);
                        sameAsPrev = true;
                    }
                }
                else
                {
                    nullCount = 0;
                }
                previus.Next = next;
                next.Previous = previus;
                previus = next;

                if (i + 1 == depth)
                {
                    next.Data = "Tail";
                }

                listAllNodes.Add(next);
            }

            var simpleRandomaiser = new Random();
            foreach (var node in listAllNodes)
            {
                var randomIndex = simpleRandomaiser.Next(0, depth + 1);
                node.Random = listAllNodes[randomIndex];
            }

            return head;
        }

        /// <summary>
        /// Left list is correct
        /// </summary>
        public static bool DeepEqual(ListNode left, ListNode right)
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

                currentId++;
                if(currentId > listLeft.Count)
                    return false;

                var leftNode = listLeft[currentId];
                if(!object.ReferenceEquals(leftNode.Random, current.Random))
                    return false;

                if (!leftNode.Data.Equals(current.Data))
                    return false;
            }
            while (current.Next != null);

            return true;
        }
    }
}