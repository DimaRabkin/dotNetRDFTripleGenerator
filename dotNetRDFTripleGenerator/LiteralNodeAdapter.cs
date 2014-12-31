using System;
using System.Collections.Generic;
using VDS.RDF;
using VDS.RDF.Parsing;

namespace dotNetRDFTripleGenerator
{
    public class LiteralNodeAdapter : ILiteralNodeAdapter
    {
        private readonly INodeFactory _nodeFactory;
        private readonly Dictionary<Type, Func<object, INodeFactory, INode>> _mapper;

        public LiteralNodeAdapter(INodeFactory nodeFactory)
        {
            _nodeFactory = nodeFactory;
            _mapper = new Dictionary<Type, Func<object, INodeFactory, INode>>
            {
                {typeof(int), (o, factory) => factory.CreateLiteralNode(o.ToString(), new Uri(XmlSpecsHelper.XmlSchemaDataTypeInteger)) },
                {typeof(string), (o, factory) => factory.CreateLiteralNode(o.ToString(), new Uri(XmlSpecsHelper.XmlSchemaDataTypeString))},
                {typeof(ulong),(o, factory) => factory.CreateLiteralNode(o.ToString(), new Uri(XmlSpecsHelper.XmlSchemaDataTypeUnsignedLong)) },
                {typeof(bool), (o, factory) => factory.CreateLiteralNode(o.ToString(), new Uri(XmlSpecsHelper.XmlSchemaDataTypeBoolean))}
            };
        }

        public INode CreateLiteralNode(object obj)
        {
            var type = obj.GetType();
            Func<object, INodeFactory, INode> handler;
            if (!_mapper.TryGetValue(type, out handler))
            {
                throw new NotSupportedException(string.Format("No adapter to type: {0}", type));
            }

            return handler(obj, _nodeFactory);
        }

        public INode CreateUriNode(string uri)
        {
            return _nodeFactory.CreateUriNode(UriFactory.Create(uri));
        }
    }
}