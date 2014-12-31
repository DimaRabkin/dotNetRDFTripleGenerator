using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using dotNetRDFTripleGenerator.Attributes;
using Microsoft.CSharp;
using VDS.RDF;

namespace dotNetRDFTripleGenerator
{
    public class FactoryGenerator
    {
        private readonly ILiteralNodeAdapter _adapter;

        public FactoryGenerator(ILiteralNodeAdapter adapter)
        {
            _adapter = adapter;
        }

        public IFactory GenerateFactory(Type type)
        {
            string factoryTypeName;
            var compilationUnit = CreateCompilationUnit(type, out factoryTypeName);

            IFactory generatedFactory = GenerateFactory(compilationUnit, factoryTypeName, type);
            return generatedFactory;
        }

        public IFactory GenerateFactory(CodeCompileUnit compileunit, string factoryTypeName, Type generatedType)
        {
            // Generate the code with the C# code provider.
            CSharpCodeProvider provider = new CSharpCodeProvider();

            var parameters = new CompilerParameters { GenerateInMemory = true };
            parameters.ReferencedAssemblies.Add("System.dll");
            parameters.ReferencedAssemblies.Add("System.Xml.dll");
            parameters.ReferencedAssemblies.Add(typeof(Triple).Assembly.Location);
            parameters.ReferencedAssemblies.Add(GetType().Assembly.Location);
            parameters.ReferencedAssemblies.Add(generatedType.Assembly.Location);
            var result = provider.CompileAssemblyFromDom(parameters, compileunit);
            if (result.Errors.Count > 0)
            {
                using (var writer = new StreamWriter("error.txt"))
                {
                    provider.GenerateCodeFromCompileUnit(compileunit, writer, new CodeGeneratorOptions { BracingStyle = "C"});
                }
                
                throw new Exception(string.Join(",", result.Errors.Cast<CompilerError>().Select(error => error.ErrorText)));
            }
            IFactory generatedFactory = (IFactory)Activator.CreateInstance(result.CompiledAssembly.GetType(factoryTypeName), _adapter);
            return generatedFactory;
        }


        private static CodeCompileUnit CreateCompilationUnit(Type type, out string factoryTypeName)
        {
            CodeCompileUnit compileUnit = new CodeCompileUnit();

            const string namespaceName = "TripleFactory";
            CodeNamespace samples = new CodeNamespace(namespaceName);
            compileUnit.Namespaces.Add(samples);
            samples.Imports.Add(new CodeNamespaceImport("System"));

            var factoryType = string.Format("{0}Factory", type.Name);
            factoryTypeName = string.Format("{0}.{1}", namespaceName, factoryType);
            CodeTypeDeclaration factoryClass =
                new CodeTypeDeclaration(factoryType);

            factoryClass.BaseTypes.Add(new CodeTypeReference(typeof(IFactory)));

            CodeMemberField field = CreateAdapterField();
            factoryClass.Members.Add(field);

            CodeConstructor constructor = CreateConstructor();
            factoryClass.Members.Add(constructor);

            var generateTriplesMethod = CreateCreateTriplesMethod(type);

            factoryClass.Members.Add(generateTriplesMethod);

            samples.Types.Add(factoryClass);
            return compileUnit;
        }

        private static CodeConstructor CreateConstructor()
        {
            var constructor = new CodeConstructor();

            constructor.Attributes = MemberAttributes.Public | MemberAttributes.Final;
            const string adapterName = "adapter";
            var parameter = new CodeParameterDeclarationExpression(typeof(LiteralNodeAdapter), adapterName);
            constructor.Parameters.Add(parameter);
            var adapterAssignment =
                new CodeAssignStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "_adapter"),
                    new CodeVariableReferenceExpression(adapterName));
            constructor.Statements.Add(adapterAssignment);
            return constructor;
        }

        private static CodeMemberField CreateAdapterField()
        {
            var field = new CodeMemberField(typeof(LiteralNodeAdapter), "_adapter");
            return field;
        }

        private static CodeMemberMethod CreateCreateTriplesMethod(Type type)
        {
            var generateTriplesMethod = new CodeMemberMethod
            {
                ReturnType = new CodeTypeReference(typeof(IEnumerable<Triple>)),
                Name = "CreateTriples",
                Attributes = MemberAttributes.Public
            };
            const string parameterName = "obj";
            generateTriplesMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(object), parameterName));
            generateTriplesMethod.Statements.AddRange(CreateStatements(type, parameterName));
            return generateTriplesMethod;
        }

        public static CodeStatementCollection CreateStatements(Type type, string parameterName)
        {
            var result = new CodeStatementCollection();
            var parameterReference = new CodeVariableReferenceExpression(parameterName);
            var casted = new CodeCastExpression(type, parameterReference);
            const string castedVariableName = "casted";

            var resultDeclaration = new CodeVariableDeclarationStatement(typeof(List<Triple>), "result");
            var resultReference = new CodeVariableReferenceExpression("result");
            var resultAssignment = new CodeAssignStatement(resultReference, new CodeObjectCreateExpression(typeof(List<Triple>)));
            result.AddRange(new CodeStatement[] { resultDeclaration, resultAssignment });

            var declaration = new CodeVariableDeclarationStatement(type, castedVariableName);
            result.Add(declaration);

            var castedVariable = new CodeVariableReferenceExpression(castedVariableName);
            var statement = new CodeAssignStatement(castedVariable, casted);

            result.Add(statement);

            var subjectProperty = type.GetProperties().Single(prop => prop.GetCustomAttribute<SubjectAttribute>() != null);
            var subjectPrefix = subjectProperty.GetCustomAttribute<SubjectAttribute>().Prefix;

            var adapterReference = new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "_adapter");

            // subject node
            var subjectPropertyReference = new CodePropertyReferenceExpression(castedVariable, subjectProperty.Name);
            const string subjectNodeName = "subjectNode";

            var subjectStatements = CreateNodeStatements(subjectNodeName,
                CreateUriExpression(adapterReference, new CodeBinaryOperatorExpression(new CodePrimitiveExpression(subjectPrefix), CodeBinaryOperatorType.Add, subjectPropertyReference)));
            
            var subjectNodeReference = new CodeVariableReferenceExpression(subjectNodeName);

            result.AddRange(subjectStatements);

            // object nodes
            var objectProperties = type.GetProperties()
                .Select(prop => new { PropertyName = prop.Name, ObjectAttribute = prop.GetCustomAttribute<ObjectAttribute>() })
                .Where(tuple => tuple.ObjectAttribute != null)
                .Select(tuple => new { tuple.PropertyName, tuple.ObjectAttribute.Prefix, tuple.ObjectAttribute.Predicate });

            foreach (var objectProperty in objectProperties)
            {
                var propertyName = objectProperty.PropertyName;

                var emptyLine = new CodeSnippetStatement();
                var comment = new CodeCommentStatement(propertyName);
                result.AddRange(new CodeStatement[] { emptyLine, comment });

                var objectNodeName = string.Format("{0}ObjectNode", propertyName.ToLower());
                var objectNodeStatements = CreateNodeStatements(objectNodeName,
                    CreateLiteralExpression(adapterReference, new CodePropertyReferenceExpression(castedVariable, propertyName)));

                var objectNodeReference = new CodeVariableReferenceExpression(objectNodeName);

                string predicateNodeName = string.Format("{0}PredicateNode", propertyName.ToLower());
                var predicateNodeStatements = CreateNodeStatements(predicateNodeName,
                    CreateUriExpression(adapterReference, new CodePrimitiveExpression(objectProperty.Predicate)));

                var predicateNodeReference = new CodeVariableReferenceExpression(predicateNodeName);

                var tripleCtor = new CodeObjectCreateExpression(typeof(Triple), subjectNodeReference, predicateNodeReference,
                    objectNodeReference);

                var tripleAddStatement = new CodeExpressionStatement(new CodeMethodInvokeExpression(resultReference, "Add", tripleCtor));

                result.AddRange(objectNodeStatements);
                result.AddRange(predicateNodeStatements);
                result.Add(tripleAddStatement);
            }

            var returnStatement = new CodeMethodReturnStatement(resultReference);
            result.Add(returnStatement);
            return result;
        }

        public static CodeStatement[] CreateNodeStatements(string nodeName, CodeExpression assingmentExpression)
        {
            var subjectNodeDeclaration = new CodeVariableDeclarationStatement(typeof(INode), nodeName);
            var subjectNodeReference = new CodeVariableReferenceExpression(nodeName);
            var subjectNodeAssignment = new CodeAssignStatement(subjectNodeReference, assingmentExpression);

            return new CodeStatement[] { subjectNodeDeclaration, subjectNodeAssignment};
        }

        public static CodeExpression CreateUriExpression(CodeExpression adapterReference, CodeExpression parameter)
        {
            return new CodeMethodInvokeExpression(adapterReference, "CreateUriNode", parameter);
        }

        public static CodeExpression CreateLiteralExpression(CodeExpression adapterReference, CodeExpression parameter)
        {
            return new CodeMethodInvokeExpression(adapterReference, "CreateLiteralNode", parameter);
        }
    }
}