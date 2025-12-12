using DevToys.Api;
using System.ComponentModel.Composition;
using System.Reflection;

namespace CodeFormatter;

[Export(typeof(IResourceAssemblyIdentifier))]
[Name(nameof(CodeFormatterResourceIdentifier))]
internal sealed class CodeFormatterResourceIdentifier : IResourceAssemblyIdentifier
{
    public ValueTask<FontDefinition[]> GetFontDefinitionsAsync()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var fontStream = assembly.GetManifestResourceStream("CodeFormatter.Assets.CodeFormatterIcons.ttf");

        if (fontStream is null)
            return new ValueTask<FontDefinition[]>([]);

        return new ValueTask<FontDefinition[]>(
        [
            new FontDefinition("CodeFormatterIcons", fontStream)
        ]);
    }
}
