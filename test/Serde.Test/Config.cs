
using System;
using System.Collections.Immutable;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using NuGet.Frameworks;

namespace Serde.Test
{
    internal static class Config
    {
        public static ReferenceAssemblies LatestTfRefs =>
			new ReferenceAssemblies (
				"net7.0",
				new PackageIdentity ("Microsoft.NETCore.App.Ref", "7.0.0-preview.4.22229.4"),
				Path.Combine ("ref", "net7.0"))
			.WithNuGetConfigFilePath (Path.Combine (
                GetDirectoryPath(),
                "..",
                "NuGet.config"));

        private static string GetDirectoryPath([CallerFilePath]string path = "") => Path.GetDirectoryName(path)!;
    }
}