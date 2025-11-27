using DevToys.Api;
using System.ComponentModel.Composition;

namespace CodeFormatter;

[Export(typeof(IResourceAssemblyIdentifier))]
[Name(nameof(CodeFormatterResourceIdentifier))]
internal sealed class CodeFormatterResourceIdentifier : IResourceAssemblyIdentifier
{
    public ValueTask<FontDefinition[]> GetFontDefinitionsAsync()
    {
        return new ValueTask<FontDefinition[]>([]);
    }
}
