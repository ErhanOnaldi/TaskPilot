using TaskPilot.Domain.Knowledge;

namespace TaskPilot.Application.Tests;

public sealed class KnowledgePolicyTests
{
    [Fact]
    public void Normalize_converts_human_text_to_a_stable_slug()
    {
        Assert.Equal("release-notes-q3", KnowledgeSlugPolicy.Normalize(" Release Notes: Q3 "));
    }

    [Fact]
    public void Parse_extracts_slug_and_optional_alias_from_wiki_links()
    {
        var links = WikiLinkParser.Parse("Read [[release notes|the plan]] and [[architecture]].");

        Assert.Equal(2, links.Count);
        Assert.Equal(new WikiLinkReference("release-notes", "the plan"), links[0]);
        Assert.Equal(new WikiLinkReference("architecture", null), links[1]);
    }
}
