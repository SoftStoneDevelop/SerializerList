using Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace ListSerializer
{
    public class ListSerializerV2 : IListSerializer
    {
        /// <summary>
        /// Serializes all nodes in the list, including topology of the Random links, into stream
        /// </summary>
        public Task Serialize(ListNode head, Stream s)
        {
            return Task.Factory.StartNew(() =>
            {
                SerializeInternal(in head, in s);
            });
        }

        [SkipLocalsInit]
        private void SerializeInternal(in ListNode head, in Stream s)
        {
            var globalLinkId = 0;
            Span<byte> buffer = stackalloc byte[500];

            //package [linkBytes 4byte][length 4byte][data]
            //All stream packages...link datas 'idLinkNodeHead'...'idLinkNodeTail'
            s.Position = 0;
            s.Write(buffer.Slice(0, sizeof(int)));//reserved for count all unique nodes
            var uniqueNodes = new List<ListNode>();

            ListNode current = null;
            do
            {
                if (current == null)
                {
                    current = head;
                }
                else
                {
                    current = current.Next;
                }

                //write id unique Node
                Unsafe.As<byte, int>(ref buffer[0]) = globalLinkId++;
                s.Write(buffer.Slice(0, sizeof(int)));

                if (current.Data == null)
                {
                    WriteNullRefferenceValue(in s);
                }
                else
                {
                    var size = current.Data.Length * sizeof(char);
                    Unsafe.As<byte, int>(ref buffer[0]) = size;
                    s.Write(buffer.Slice(0, sizeof(int)));

                    int partDataSize = 0;
                    int offsetDestination = 0;
                    unsafe
                    {
                        fixed (byte* pDest = &buffer[0])
                        fixed (char* pSource = current.Data)
                        {
                            while (size > 0)
                            {
                                partDataSize = size > buffer.Length ? buffer.Length : size;
                                Buffer.MemoryCopy(pSource + offsetDestination, pDest, buffer.Length, partDataSize);
                                s.Write(buffer.Slice(0, partDataSize));

                                offsetDestination += partDataSize;
                                size -= partDataSize;
                            }
                        }
                    }
                }

                uniqueNodes.Add(current);
            }
            while (current.Next != null);

            NodesExtensions.QuickSort(uniqueNodes);

            //count all unique nodes
            long tempPosition = s.Position;
            s.Position = 0;
            Unsafe.As<byte, int>(ref buffer[0]) = uniqueNodes.Count;
            s.Write(buffer.Slice(0, sizeof(int)));
            s.Position = tempPosition;

            //write comma between packages and links
            WriteNullRefferenceValue(in s);

            //write links
            for (int i = 0; i < uniqueNodes.Count; i++)
            {
                ListNode item = uniqueNodes[i];
                if (item.Random == null)
                {
                    WriteNullRefferenceValue(in s);
                    continue;
                }

                var randomLinkId = FindRealIdNode(in uniqueNodes, in item);
                Unsafe.As<byte, int>(ref buffer[0]) = randomLinkId;
                s.Write(buffer.Slice(0, sizeof(int)));
            }
        }

        private void QuickSort(List<ListNode> nodes, int leftIndex, int rightIndex)
        {
            if(leftIndex < rightIndex)
            {
                var pivotIndex = Partition(nodes, leftIndex, rightIndex);
                QuickSort(nodes, leftIndex, pivotIndex - 1);
                QuickSort(nodes, pivotIndex + 1, rightIndex);
            }
        }

        private int Partition(List<ListNode> nodes, int leftIndex, int rightIndex)
        {
            var pivot = nodes[rightIndex];
            var i = leftIndex - 1;
            for (int j = leftIndex; j < rightIndex - 1; j++)
            {
                if (nodes[j].Data.CompareTo(pivot.Data) <= 0)
                {
                    i++;
                    nodes[i] = nodes[j];
                }
            }
            nodes[i + 1] = nodes[rightIndex];
            return i + 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteNullRefferenceValue(in Stream s)
        {
            s.WriteByte(byte.MaxValue);
            s.WriteByte(byte.MaxValue);
            s.WriteByte(byte.MaxValue);
            s.WriteByte(byte.MaxValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int FindRealIdNode(in List<ListNode> uniqueNodes, in ListNode node)//TODO find in sorted list
        {
            //uniqueNodes must be sorted
            if (node.Random.Data == null)
            {
                for (int i = 0; i < uniqueNodes.Count; i++)
                {
                    var currentNode = uniqueNodes[i];
                    if(currentNode.Data != null)
                    {
                        return -1;
                    }

                    if(ReferenceEquals(node.Random, currentNode))
                    {
                        return i;
                    }
                }

                return -1;
            }
            else
            {
                var finded = uniqueNodes.BinarySearch(node, new ListNodeComparer());
                if(finded < 0)
                {
                    return -1;
                }

                if (ReferenceEquals(uniqueNodes[finded], node.Random))
                {
                    return finded;
                }

                //search after
                for (int i = finded; i < uniqueNodes.Count; i++)
                {
                    var current = uniqueNodes[i];
                    if(current.Data == null || current.Data.CompareTo(node.Random.Data) > 0)
                    {
                        break;
                    }

                    if (ReferenceEquals(current, node.Random))
                    {
                        return i;
                    }

                    continue;
                }

                //search before
                for (int i = finded; i >= 0; i--)
                {
                    var current = uniqueNodes[i];
                    if (current.Data == null || current.Data.CompareTo(node.Random.Data) < 0)
                    {
                        break;
                    }

                    if (ReferenceEquals(current, node.Random))
                    {
                        return i;
                    }

                    continue;
                }

                return -1;
            }
        }

        public class ListNodeComparer : IComparer<ListNode>
        {
            int IComparer<ListNode>.Compare(ListNode x, ListNode y)
            {
                if(x.Data == null)
                {
                    return -1;
                }

                return x.Data.CompareTo(y.Random.Data);
            }
        }

        /// <summary>
        /// Deserializes the list from the stream, returns the head node of the list
        /// </summary>
        /// <exception cref="System.ArgumentException">Thrown when a stream has invalid data</exception>
        public Task<ListNode> Deserialize(Stream s)
        {
            return Task<ListNode>.Factory.StartNew(DeserializeInternal, s);
        }

        [SkipLocalsInit]
        private ListNode DeserializeInternal(object obj)
        {
            var s = (Stream)obj;
            s.Position = 0;
            ListNode head = null;
            Span<byte> buffer = stackalloc byte[500];
            if (s.Read(buffer.Slice(0, sizeof(int))) != sizeof(int))
            {
                throw new ArgumentException("Unexpected end of stream, expect four bytes");
            }

            var allUniqueNodes = new List<ListNode>(BitConverter.ToInt32(buffer.Slice(0, sizeof(int))));

            ListNode current = null;
            ListNode previous = null;

            while (s.Read(buffer.Slice(0, sizeof(int))) == sizeof(int))
            {
                var linkId = BitConverter.ToInt32(buffer.Slice(0, sizeof(int)));
                if (linkId == -1)//end unique nodes
                    break;

                current = new ListNode();
                allUniqueNodes.Add(current);
                if (previous != null)
                {
                    previous.Next = current;
                    current.Previous = previous;
                }
                else
                {
                    head = current;
                }

                if (s.Read(buffer.Slice(0, sizeof(int))) != sizeof(int))
                {
                    throw new ArgumentException("Unexpected end of stream, expect four bytes");
                }

                var length = BitConverter.ToInt32(buffer.Slice(0, sizeof(int)));
                if (length != -1)
                {
                    int partDataSize = 0;
                    int offsetDestination = 0;
                    current.Data = new string(' ', length / sizeof(char));

                    unsafe
                    {
                        fixed (byte* pSource = &buffer[0])
                        fixed (char* pDest = current.Data)
                        {
                            while (length > 0)
                            {
                                partDataSize = length > buffer.Length ? buffer.Length : length;
                                if (s.Read(buffer.Slice(0, partDataSize)) != partDataSize)
                                {
                                    throw new ArgumentException("Unexpected end of stream, expect bytes represent string data");
                                }

                                Buffer.MemoryCopy(pSource, pDest + offsetDestination, length, partDataSize);

                                offsetDestination += partDataSize;
                                length -= partDataSize;
                            }
                        }
                    }
                }

                previous = current;
            }

            NodesExtensions.QuickSort(allUniqueNodes);
            int uniqueNodesIndex = 0;
            while (s.Read(buffer.Slice(0, sizeof(int))) == sizeof(int))
            {
                int linkIndex = BitConverter.ToInt32(buffer.Slice(0, sizeof(int)));
                if (linkIndex != -1)
                    allUniqueNodes[uniqueNodesIndex].Random = allUniqueNodes[linkIndex];

                uniqueNodesIndex++;
            }

            return head;
        }

        /// <summary>
        /// Makes a deep copy of the list, returns the head node of the list 
        /// </summary>
        public Task<ListNode> DeepCopy(ListNode head)
        {
            return Task<ListNode>.Factory.StartNew(DeepCopyInternal, head);
        }

        private ListNode DeepCopyInternal(object obj)
        {
            var head = (ListNode)obj;
            using (var stream = new MemoryStream())
            {
                var taskSerialize = Serialize(head, stream);
                taskSerialize.Wait();
                var taskDeserialize = Deserialize(stream);
                taskDeserialize.Wait();
                return taskDeserialize.Result;
            }
        }
    }
}