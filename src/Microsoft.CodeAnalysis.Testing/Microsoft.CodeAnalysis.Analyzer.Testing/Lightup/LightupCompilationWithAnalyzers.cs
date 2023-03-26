// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Testing.Lightup
{
    internal static class LightupCompilationWithAnalyzers
    {
        private static Func<Compilation, ImmutableArray<DiagnosticAnalyzer>, AnalyzerOptions, CancellationToken, bool?, CompilationWithAnalyzers> BuildCreatorFunc()
        {
            var compilationType = typeof(CompilationWithAnalyzers);
            var assembly = compilationType.GetTypeInfo().Assembly;

            var optionsType =
                assembly.DefinedTypes.FirstOrDefault(type => type.Name == "CompilationWithAnalyzersOptions")?.AsType();
            var suppressorType =
                assembly.DefinedTypes.FirstOrDefault(type => type.Name == "DiagnosticSuppressor")?.AsType();

            if (optionsType != null && suppressorType != null)
            {
                return (compilation, analyzers, options, _, reportSuppressedDiagnostics) =>
                {
                    reportSuppressedDiagnostics ??=
                        analyzers.Any(analyzer => suppressorType.IsInstanceOfType(analyzer));

                    var compilationWithAnalyzersOptions
                        = Activator.CreateInstance(optionsType, options, null, true, false, reportSuppressedDiagnostics.GetValueOrDefault());

                    return (CompilationWithAnalyzers)Activator.CreateInstance(compilationType, compilation, analyzers, compilationWithAnalyzersOptions);
                };
            }

            return (compilation, analyzers, options, cancellationToken, _) => compilation.WithAnalyzers(analyzers, options, cancellationToken);
        }

        public static readonly Func<Compilation, ImmutableArray<DiagnosticAnalyzer>, AnalyzerOptions, CancellationToken, bool?, CompilationWithAnalyzers> Create = BuildCreatorFunc();
    }
}
