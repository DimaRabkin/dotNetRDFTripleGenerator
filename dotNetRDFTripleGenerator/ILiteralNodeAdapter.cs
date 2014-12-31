using VDS.RDF;

namespace dotNetRDFTripleGenerator
{
    public interface ILiteralNodeAdapter
    {
        INode CreateLiteralNode(object obj);

        INode CreateUriNode(string prefix, object obj);
        INode CreateUriNode(string uri);
    }
}