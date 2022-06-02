using Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ListSerializer
{
    public class ListSerializer : IListSerializer
    {
        private const int NullReference = -1;

        /// <summary>
        /// Serializes all nodes in the list, including topology of the Random links, into stream
        /// </summary>
        public Task Serialize(ListNode head, Stream s)
        {
            return Task.Factory.StartNew(() =>
                {
                    var dic = new Dictionary<ListNode, int>();
                    var globalLinkId = 0;

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
                        s.Write(BitConverter.GetBytes(currentLinkId));

                        if (current.Data == null)
                        {
                            var lengthBytes = BitConverter.GetBytes(NullReference);
                            s.Write(lengthBytes);
                        }
                        else
                        {
                            byte[] bytes = Encoding.Unicode.GetBytes(current.Data);
                            var lengthBytes = BitConverter.GetBytes(bytes.Length);
                            s.Write(lengthBytes);
                            s.Write(bytes);
                        }
                        
                        if (current.Random == null)
                        {
                            s.Write(BitConverter.GetBytes(NullReference));
                        }
                        else
                        {
                            var linkRandom = GetLinkId(in dic, in current.Random, ref globalLinkId);
                            s.Write(BitConverter.GetBytes(linkRandom));
                        }
                    }
                    while (current.Next != null);
                })
                ;
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
            return Task<ListNode>.Factory.StartNew(() =>
            {
                s.Position = 0;
                ListNode head = null;
                var bufferLink = new byte[4];
                var linkDictionary = new Dictionary<int, ListNode>();
                var listNeedSetRandom = new List<(int linkId, int randomLinkId)>();

                ListNode current = null;
                ListNode previous = null;
                while (s.Read(bufferLink, 0, bufferLink.Length) > 0)
                {
                    var linkId = BitConverter.ToInt32(bufferLink, 0);

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

                    var bufferLength = new byte[4];
                    if (s.Read(bufferLength, 0, bufferLength.Length) <= 0)
                    {
                        throw new ArgumentException("not find length data in stream");
                    }

                    var length = BitConverter.ToInt32(bufferLength);

                    if (length != NullReference)
                    {
                        var bufferData = new byte[length];
                        if (s.Read(bufferData, 0, bufferData.Length) <= 0)
                        {
                            throw new ArgumentException();
                        }

                        var data = Encoding.Unicode.GetString(bufferData);
                        current.Data = data;
                    }

                    var bufferRandomLink = new byte[4];
                    if (s.Read(bufferRandomLink, 0, bufferRandomLink.Length) <= 0)
                    {
                        throw new ArgumentException();
                    }

                    var randomLink = BitConverter.ToInt32(bufferRandomLink);
                    if (randomLink != NullReference)//means not null
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
            });
        }

        /// <summary>
        /// Makes a deep copy of the list, returns the head node of the list 
        /// </summary>
        public Task<ListNode> DeepCopy(ListNode head)
        {
            return Task<ListNode>.Factory.StartNew(() =>
            {
                using (var stream = new MemoryStream())
                {
                    var taskSerialize = Serialize(head, stream);
                    taskSerialize.Wait();
                    var taskDeserialize = Deserialize(stream);
                    taskDeserialize.Wait();
                    return taskDeserialize.Result;
                }
            });
        }
    }
}