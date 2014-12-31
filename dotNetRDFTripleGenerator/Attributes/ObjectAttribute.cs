using System;

namespace dotNetRDFTripleGenerator.Attributes
{
    public class ObjectAttribute : Attribute
    {
        public string Predicate { get; set; }

        public string Prefix { get; set; }
    }
}