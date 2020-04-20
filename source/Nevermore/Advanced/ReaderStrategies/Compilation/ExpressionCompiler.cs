using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Nevermore.Advanced.ReaderStrategies.Compilation
{
    internal static class ExpressionCompiler
    {
        public static CompiledExpression<TExpression> Compile<TExpression>(Expression<TExpression> expression, bool includeCompiledReadersInErrors)
        {
            var text = includeCompiledReadersInErrors 
                ? new Lazy<string>(() => ExpressionToString(expression)) 
                : new Lazy<string>(() => null);
            
            try
            {
                var compiled = expression.Compile();
                return new CompiledExpression<TExpression>(compiled, text);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error compiling reader expression: " + ex.Message + "\nThe expression being compiled was:\n" + text, ex);
            }
        }

        static string ExpressionToString<TExpression>(Expression<TExpression> expression)
        {
            var output = new StringBuilder();

            output.Append("(").AppendJoin(", ", expression.Parameters.Select(param => FormatTypeName(param.Type) + " " + param.Name)).AppendLine(") => ");
            
            output.AppendLine("{");
            
            PrintExpression(expression.Body, output, "    ");
            
            output.AppendLine("}");
            
            return output.ToString();
        }

        static string FormatTypeName(Type type)
        {
            return type.Name;
        }

        static void PrintExpression(Expression expression, StringBuilder output, string indent)
        {
            if (expression is ConditionalExpression ce)
            {
                output.Append(indent + "if (");
                PrintExpression(ce.Test, output, "");
                output.Append(")");

                output.AppendLine();
                output.AppendLine(indent + "{");
                PrintExpression(ce.IfTrue, output, indent + "    ");
                output.AppendLine(indent + "}");

                if (!(ce.IfFalse is DefaultExpression))
                {
                    output.AppendLine(indent + "else");
                    output.AppendLine(indent + "{");
                    PrintExpression(ce.IfFalse, output, indent + "    ");
                    output.AppendLine(indent + "}");
                }
            }
            else if (expression is BinaryExpression bin && bin.NodeType == ExpressionType.Assign && bin.Left is ParameterExpression pe)
            {
                output.Append(indent + FormatTypeName(pe.Type) + " " + pe.Name + " = ");
                output.Append(bin.Right);
            }
            else if (expression is BlockExpression be)
            {
                foreach (var exp in be.Expressions)
                {
                    PrintExpression(exp, output, indent);
                    output.AppendLine();
                }
            }
            else
            {
                var expressionText = expression.ToString();
                if (expressionText.StartsWith('(') && expressionText.EndsWith(')'))
                {
                    expressionText = expressionText.Substring(1, expressionText.Length - 2);
                }
                output.Append(indent + expressionText);
            }            
        }
    }
}