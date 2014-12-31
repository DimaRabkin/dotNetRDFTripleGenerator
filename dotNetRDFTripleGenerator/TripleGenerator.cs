using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using VDS.RDF;

namespace dotNetRDFTripleGenerator
{
    public class TripleGenerator
    {
        private readonly ConcurrentDictionary<Type, IFactory> _factories =
            new ConcurrentDictionary<Type, IFactory>();

        private readonly FactoryGenerator _factoryGenerator;

        public TripleGenerator(ILiteralNodeAdapter adapter)
        {
            _factoryGenerator = new FactoryGenerator(adapter);
        }

        public IEnumerable<Triple> GenerateTriples(object obj)
        {
            Type type = obj.GetType();

            IFactory factory = _factories.GetOrAdd(type, CreateFactory);
            IEnumerable<Triple> result = factory.CreateTriples(obj);
            return result;
        }

        private IFactory CreateFactory(Type type)
        {
            IFactory generateFactory = _factoryGenerator.GenerateFactory(type);
            return generateFactory;
        }
    }
}