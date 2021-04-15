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
        /// <summary>
        /// Serializes all nodes in the list, including topology of the Random links, into stream
        /// </summary>
        public Task Serialize(ListNode head, Stream s)
        {
            return Task.Factory.StartNew(() =>
                {
                    var dic = new Dictionary<string, List<(int LinkId, ListNode Node)>>();
                    var globalLinkId = 0;

                    //package [linkBytes 4byte][length 4byte][data][randomLink 4 byte]
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

                        byte[] bytes = Encoding.UTF8.GetBytes(current.Data);
                        var lengthBytes = BitConverter.GetBytes(bytes.Length);
                        s.Write(lengthBytes);
                        s.Write(bytes);
                        if (current.Random == null)
                        {
                            s.Write(BitConverter.GetBytes(0));
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
            in Dictionary<string, List<(int LinkId, ListNode Node)>> dictionary,
            in ListNode node,
            ref int linkCounter)
        {
            if (dictionary.TryGetValue(node.Data, out var listLink))
            {
                foreach (var item in listLink)
                {
                    if (object.ReferenceEquals(item.Node, node))
                    {
                        return item.LinkId;
                    }
                }

                linkCounter++;
                var linkId = linkCounter;
                dictionary.Add(node.Data, new List<(int, ListNode)>()
                {
                    (linkId, node)
                });
                return linkId;
            }
            else
            {
                linkCounter++;
                var linkId = linkCounter;
                dictionary.Add(node.Data, new List<(int, ListNode)>()
                {
                    (linkId, node)
                });

                return linkId;
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
                    var bufferData = new byte[length];
                    if (s.Read(bufferData, 0, bufferData.Length) <= 0)
                    {
                        throw new ArgumentException();
                    }

                    var data =Encoding.UTF8.GetString(bufferData);
                    current.Data = data;

                    var bufferRandomLink = new byte[length];
                    if (s.Read(bufferRandomLink, 0, bufferRandomLink.Length) <= 0)
                    {
                        throw new ArgumentException();
                    }

                    var randomLink = BitConverter.ToInt32(bufferRandomLink);

                    if (randomLink != 0)//means not null
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
            return Task<ListNode>.Factory.StartNew((() =>
            {
                using (var stream = new MemoryStream())
                {
                    var taskSerialize = Serialize(head, stream);
                    taskSerialize.Wait();
                    var taskDeserialize = Deserialize(stream);
                    taskDeserialize.Wait();
                    return taskDeserialize.Result;
                }
            }));
        }
    }
}