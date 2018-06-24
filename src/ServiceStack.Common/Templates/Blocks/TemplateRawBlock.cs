﻿using System;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;

namespace ServiceStack.Templates
{
    /// <summary>
    /// Special block which captures the raw body as a string fragment
    ///
    /// Usages: {{#raw}}emit {{ verbatim }} body{{/raw}}
    ///         {{#raw varname}}assigned to varname{{/raw}}
    ///         {{#raw appendTo varname}}appended to varname{{/raw}}
    /// </summary>
    public class TemplateRawBlock : TemplateBlock
    {
        public override string Name => "raw";
        
        public override async Task WriteAsync(TemplateScopeContext scope, PageBlockFragment fragment, CancellationToken cancel)
        {
            var strFragment = (PageStringFragment)fragment.Body[0];

            if (!string.IsNullOrWhiteSpace(fragment.Argument))
            {
                Capture(scope, fragment, strFragment);
            }
            else
            {
                await scope.OutputStream.WriteAsync(strFragment.Value, cancel);
            }
        }

        private static void Capture(TemplateScopeContext scope, PageBlockFragment fragment, PageStringFragment strFragment)
        {
            var literal = fragment.Argument.AsSpan().AdvancePastWhitespace();
            bool appendTo = false;
            if (literal.StartsWith("appendTo "))
            {
                appendTo = true;
                literal = literal.Advance("appendTo ".Length);
            }

            literal = literal.ParseVarName(out var name);
            var nameString = name.Value();
            if (appendTo && scope.PageResult.Args.TryGetValue(nameString, out var oVar)
                         && oVar is string existingString)
            {
                scope.PageResult.Args[nameString] = existingString + strFragment.Value.Value;
                return;
            }

            scope.PageResult.Args[nameString] = strFragment.Value.Value;
        }
    }
}