using System.Collections.Generic;
using System.Linq;
using WirePix.Core.Naming.Templates;
using WirePix.Core.Naming.Tokens;

namespace WirePix.Core.Validation;

public enum TemplateValidationCode
{
    None,
    UnknownTag,
    FirstSeparator,
    AdjacentSeparators,
    TagLimitExceeded,
    InvalidCustomText
}

public static class NameTemplateValidator
{
    public static TemplateValidationCode ValidateAppend(IReadOnlyList<string> tags, string candidate, int maxTags)
    {
        TagDefinition definition = TagCatalog.Get(candidate);
        if (definition == null) return TemplateValidationCode.UnknownTag;
        bool separator = definition.Group == TagCatalog.SeparatorGroup;
        if (separator && (tags == null || tags.Count == 0)) return TemplateValidationCode.FirstSeparator;
        if (separator && TagCatalog.Get(tags[^1])?.Group == TagCatalog.SeparatorGroup) return TemplateValidationCode.AdjacentSeparators;
        int nonSeparators = (tags ?? []).Count(tag => TagCatalog.Get(tag)?.Group != TagCatalog.SeparatorGroup);
        if (!separator && nonSeparators >= maxTags) return TemplateValidationCode.TagLimitExceeded;
        return TemplateValidationCode.None;
    }

    public static TemplateValidationCode ValidateCustomText(string text) =>
        NameTemplate.IsValidFileName(text) ? TemplateValidationCode.None : TemplateValidationCode.InvalidCustomText;
}
