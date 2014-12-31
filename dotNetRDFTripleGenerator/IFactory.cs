using System.Collections.Generic;
using VDS.RDF;

namespace dotNetRDFTripleGenerator
{
    public interface IFactory
    {
        IEnumerable<Triple> CreateTriples(object obj);
    }
}