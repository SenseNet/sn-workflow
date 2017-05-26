using System;
using SenseNet.ContentRepository.Storage;
using Repo = SenseNet.ContentRepository;

namespace SenseNet.Workflow
{
    public class WfReference
    {
        private string _path;

        public WfReference(string path)
        {
            _path = path;
        }

        private Node ContentNode
        {
            get { return Node.LoadNode(_path); }
        }

        public WfContent this[string fieldName]
        {
            get
            {
                var content = Repo.Content.Load(_path);
                if (content == null)
                    throw new ContentNotFoundException(_path);

                Repo.Field field;
                if (content.Fields.TryGetValue(fieldName, out field))
                {
                    var value = content[fieldName];
                    var nodeValue = value as Node;
                    if (nodeValue != null)
                        return new WfContent(nodeValue);

                    var enumerableValue = value as System.Collections.IEnumerable;
                    if (enumerableValue != null)
                    {
                        var iter = enumerableValue.GetEnumerator();
                        if (iter.MoveNext())
                        {
                            nodeValue = (Node)iter.Current;
                            return new WfContent(nodeValue);
                        }
                    }
                    return null;
                }

                throw new ApplicationException($"Field '{fieldName}' not found in a {content.ContentType.Name} content: {content.Path} ");
            }
            set
            {
                var nodes = new NodeList<Node>();
                var node = Node.LoadNode(value.Path);
                nodes.Add(node);
                var cNode = ContentNode;
                cNode[fieldName] = nodes;
                cNode.Save();
                //TODO: WF: Write back the timestamp (if the content is the relatedContent)
            }
        }
    }
}
