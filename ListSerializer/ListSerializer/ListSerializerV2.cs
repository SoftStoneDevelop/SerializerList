using Common;
using System;
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
                var globalLinkId = 0;
                Span<byte> buffer = stackalloc byte[500];

                //package [linkBytes 4byte][length 4byte][data]
                //All stream packages...link datas 'idLinkNodeHead'...'idLinkNodeTail'
                s.Position = 0;
                s.Write(buffer.Slice(0, sizeof(int)));//reserved for count all unique nodes

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
                }
                while (current.Next != null);

                //count all unique nodes
                long tempPosition = s.Position;
                s.Position = 0;
                Unsafe.As<byte, int>(ref buffer[0]) = globalLinkId;
                s.Write(buffer.Slice(0, sizeof(int)));
                s.Position = tempPosition;

                //write comma between packages and links
                WriteNullRefferenceValue(in s);

                globalLinkId = -1;
                //write links
                current = null;
                do
                {
                    globalLinkId++;
                    if (current == null)
                    {
                        current = head;
                    }
                    else
                    {
                        current = current.Next;
                    }

                    if (current.Random == null)
                    {
                        WriteNullRefferenceValue(in s);
                        continue;
                    }

                    var randomLinkId = FindRealIdNode(in current, in globalLinkId);
                    Unsafe.As<byte, int>(ref buffer[0]) = randomLinkId;
                    s.Write(buffer.Slice(0, sizeof(int)));
                }
                while (current.Next != null);
            });
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
        private int FindRealIdNode(in ListNode node, in int currentId )
        {
            var currentLoopId = currentId;
            ListNode current = null;
            do
            {
                if (current == null)
                {
                    current = node;
                }
                else
                {
                    current = current.Previous;
                    currentLoopId--;
                }

                if (!ReferenceEquals(node.Random, current))
                    continue;

                return currentLoopId;
            }
            while (current.Previous != null);

            currentLoopId = currentId;
            current = null;
            do
            {
                if (current == null)
                {
                    current = node;
                }
                else
                {
                    current = current.Next;
                    currentLoopId++;
                }

                if (!ReferenceEquals(node.Random, current))
                    continue;

                return currentLoopId;
            }
            while (current.Next != null);

            return -1;
        }

        /// <summary>
        /// Deserializes the list from the stream, returns the head node of the list
        /// </summary>
        /// <exception cref="System.ArgumentException">Thrown when a stream has invalid data</exception>
        public Task<ListNode> Deserialize(Stream s)
        {
            return Task<ListNode>.Factory.StartNew(DeserializeInternal, s);
        }

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