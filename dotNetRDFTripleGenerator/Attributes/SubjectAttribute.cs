using System;

namespace dotNetRDFTripleGenerator.Attributes
{
    public class SubjectAttribute : Attribute
    {
        public string Prefix { get; set; }
    }
}