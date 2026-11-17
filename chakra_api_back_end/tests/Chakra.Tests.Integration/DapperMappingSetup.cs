using System.Runtime.CompilerServices;
using Dapper;

namespace Chakra.Tests.Integration;

internal static class DapperMappingSetup
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }
}
