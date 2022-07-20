using Common;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
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
                ArrayPool<byte> arrayPool = ArrayPool<byte>.Shared;
                Span<byte> intBytes = stackalloc byte[sizeof(int)];

                //package [linkBytes 4byte][length 4byte][data]
                //All stream packages...link datas 'idLinkNodeHead'...'idLinkNodeTail'
                s.Position = 0;
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
                    Unsafe.As<byte, int>(ref intBytes[0]) = globalLinkId++;
                    s.Write(intBytes);

                    if (current.Data == null)
                    {
                        WriteNullRefferenceValue(in s);
                    }
                    else
                    {
                        var byteCount = Encoding.Unicode.GetMaxByteCount(current.Data.Count());
                        var bytes = arrayPool.Rent(byteCount);
                        try
                        {
                            var size = Encoding.Unicode.GetBytes(current.Data, bytes);
                            Unsafe.As<byte, int>(ref intBytes[0]) = size;
                            s.Write(intBytes);
                            s.Write(bytes, 0, size);
                        }
                        finally
                        {
                            arrayPool.Return(bytes);
                        }
                    }
                }
                while (current.Next != null);

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

                    var randomLinkId = FindRealIdNode(current, globalLinkId);
                    Unsafe.As<byte, int>(ref intBytes[0]) = randomLinkId;
                    s.Write(intBytes);
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
        private int FindRealIdNode(ListNode node, int currentId )
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
            Span<byte> bufferForInt32 = stackalloc byte[sizeof(int)];
            var allUniqueNodes = new List<ListNode>();

            ListNode current = null;
            ListNode previous = null;
            ArrayPool<byte> arrayPool = ArrayPool<byte>.Shared;

            while (s.Read(bufferForInt32) == bufferForInt32.Length)
            {
                var linkId = BitConverter.ToInt32(bufferForInt32);
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

                if (s.Read(bufferForInt32) < bufferForInt32.Length)
                {
                    throw new ArgumentException("Unexpected end of stream, expect four bytes");
                }

                var length = BitConverter.ToInt32(bufferForInt32);
                if (length != -1)
                {
                    var bufferData = arrayPool.Rent(length);
                    try
                    {
                        if (s.Read(bufferData, 0, length) < length)
                        {
                            throw new ArgumentException("Unexpected end of stream, expect bytes represent string data");
                        }

                        current.Data = Encoding.Unicode.GetString(new Span<byte>(bufferData, 0, length));
                    }
                    finally
                    {
                        arrayPool.Return(bufferData);
                    }
                }

                previous = current;
            }

            int uniqueNodesIndex = 0;
            while (s.Read(bufferForInt32) == bufferForInt32.Length)
            {
                int linkIndex = BitConverter.ToInt32(bufferForInt32);
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