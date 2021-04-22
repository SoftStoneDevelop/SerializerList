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
        public static ListNode CreateRandomListNode(int depth)
        {
            var head = new ListNode
            {
                Data = "Head"
            };
            var previus = head;

            var listAllNodes = new List<ListNode>(depth) {head};

            for (int i = 0; i < depth; i++)
            {
                var next = new ListNode();
                if (i%2 >0)
                {
                    next.Data = $"Node № {i + 1}";
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
    }
}