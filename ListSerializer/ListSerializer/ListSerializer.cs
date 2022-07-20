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
    public class ListSerializer : IListSerializer
    {
        /// <summary>
        /// Serializes all nodes in the list, including topology of the Random links, into stream
        /// </summary>
        public Task Serialize(ListNode head, Stream s)
        {
            return Task.Factory.StartNew(() =>
            {
                var dic = new Dictionary<ListNode, int>();
                var globalLinkId = 0;
                ArrayPool<byte> arrayPool = ArrayPool<byte>.Shared;
                Span<byte> intBytes = stackalloc byte[sizeof(int)];

                //package [linkBytes 4byte][length 4byte][data][randomLink 4 byte or 0 if null]
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

                    var currentLinkId = GetLinkId(in dic, in current, ref globalLinkId);
                    Unsafe.As<byte, int>(ref intBytes[0]) = currentLinkId;
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

                    if (current.Random == null)
                    {
                        WriteNullRefferenceValue(in s);
                    }
                    else
                    {
                        var linkRandom = GetLinkId(in dic, in current.Random, ref globalLinkId);
                        Unsafe.As<byte, int>(ref intBytes[0]) = linkRandom;
                        s.Write(intBytes);
                    }
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
        private int GetLinkId(
            in Dictionary<ListNode, int> dictionary,
            in ListNode node,
            ref int linkCounter)
        {
            if (dictionary.TryGetValue(node, out var linkId))
            {
                return linkId;
            }
            else
            {
                linkCounter++;
                dictionary.Add(node, linkCounter);

                return linkCounter;
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

        private ListNode DeserializeInternal(object obj)
        {
            var s = (Stream)obj;
            s.Position = 0;
            ListNode head = null;
            Span<byte> bufferForInt32 = stackalloc byte[4];
            var linkDictionary = new Dictionary<int, ListNode>();
            var listNeedSetRandom = new List<(int linkId, int randomLinkId)>();

            ListNode current = null;
            ListNode previous = null;
            ArrayPool<byte> arrayPool = ArrayPool<byte>.Shared;

            while (s.Read(bufferForInt32) == bufferForInt32.Length)
            {
                var linkId = BitConverter.ToInt32(bufferForInt32);

                current = new ListNode();
                if (previous != null)
                {
                    previous.Next = current;
                    current.Previous = previous;
                }
                else
                {
                    head = current;
                }

                if (!linkDictionary.ContainsKey(linkId))
                {
                    linkDictionary.Add(linkId, current);
                }

                if (s.Read(bufferForInt32) < bufferForInt32.Length)
                {
                    throw new ArgumentException("not find length data in stream");
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

                if (s.Read(bufferForInt32) < bufferForInt32.Length)
                {
                    throw new ArgumentException();
                }

                var randomLink = BitConverter.ToInt32(bufferForInt32);
                if (randomLink != -1)//means not null
                {
                    if (linkDictionary.TryGetValue(randomLink, out var findNode))
                    {
                        current.Random = findNode;
                    }
                    else
                    {
                        listNeedSetRandom.Add((linkId, randomLink));
                    }
                }

                previous = current;
            }

            foreach (var item in listNeedSetRandom)
            {
                var node = linkDictionary[item.linkId];
                if (linkDictionary.TryGetValue(item.randomLinkId, out var findNodeRandom))
                {
                    node.Random = findNodeRandom;
                }
                else
                {
                    throw new ArgumentException();
                }
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