using Slipways.Infrastructure.Websites;

namespace Slipways.Api.Tests;

public class WebsiteParserTests
{
    [Fact]
    public void Reads_contact_details_and_opening_hours_from_json_ld()
    {
        const string html = """
            <html><head>
            <script type="application/ld+json">
            {
              "@context": "https://schema.org",
              "@graph": [
                { "@type": "WebSite", "name": "Hafen", "publisher": { "@id": "#org" } },
                { "@type": ["LocalBusiness", "Marina"], "@id": "#org", "telephone": "+49 39931 1234",
                  "email": "mailto:info@hafen.example",
                  "openingHoursSpecification": [
                    { "@type": "OpeningHoursSpecification", "dayOfWeek": ["https://schema.org/Monday", "Tuesday", "Wednesday", "Thursday", "Friday"],
                      "opens": "08:00:00", "closes": "18:00:00" },
                    { "dayOfWeek": "Saturday", "opens": "09:00", "closes": "12:00" },
                    { "dayOfWeek": "Sunday", "opens": "09:00", "closes": "12:00" }
                  ],
                  "employee": { "@type": "Person", "telephone": "0171 9999999" } }
              ]
            }
            </script></head>
            <body><a href="tel:0171 8888888">Vorstand</a></body></html>
            """;

        var details = WebsiteParser.Parse(html);

        Assert.Equal(new WebsiteDetails("+49 39931 1234", "info@hafen.example", "Mo-Fr 08:00-18:00; Sa,Su 09:00-12:00"), details);
    }

    [Fact]
    public void Takes_opening_hours_as_text()
    {
        const string html = """
            <script type='application/ld+json'>{ "@type": "Marina", "openingHours": ["Mo-Fr 08:00-18:00", "Sa 09:00-12:00"] }</script>
            """;

        Assert.Equal("Mo-Fr 08:00-18:00; Sa 09:00-12:00", WebsiteParser.Parse(html).OpeningHours);
    }

    [Fact]
    public void Falls_back_to_links_and_treats_national_and_international_numbers_as_one()
    {
        const string html = """
            <header><a href="tel:+4930123456">030 123456</a></header>
            <footer><a href="tel:030%20123456">Anrufen</a> <a href="MAILTO:hafen@example.de?subject=Liegeplatz">Mail</a></footer>
            """;

        Assert.Equal(new WebsiteDetails("+4930123456", "hafen@example.de", null), WebsiteParser.Parse(html));
    }

    [Fact]
    public void Takes_nothing_when_a_page_names_several_contacts()
    {
        const string html = """
            <a href="tel:0171 1111111">1. Vorsitzender</a>
            <a href="tel:0172 2222222">Hafenmeister</a>
            <a href="mailto:vorstand@club.example">Vorstand</a>
            <a href="mailto:hafen@club.example">Hafen</a>
            """;

        Assert.True(WebsiteParser.Parse(html).IsEmpty);
    }

    [Fact]
    public void Ignores_persons_broken_json_and_invalid_values()
    {
        const string html = """
            <script type="application/ld+json">{ "@type": "Person", "telephone": "0171 1234567", "email": "max@example.de" }</script>
            <script type="application/ld+json">{ "@type": "Marina", "telephone": </script>
            <a href="tel:12">kurz</a>
            <a href="mailto:keine-adresse">kaputt</a>
            """;

        Assert.True(WebsiteParser.Parse(html).IsEmpty);
    }

    [Fact]
    public void Skips_seasonal_opening_hours()
    {
        const string html = """
            <script type="application/ld+json">
            { "@type": "Marina", "openingHoursSpecification": [
              { "dayOfWeek": "Monday", "opens": "08:00", "closes": "20:00", "validFrom": "2026-05-01", "validThrough": "2026-09-30" } ] }
            </script>
            """;

        Assert.Null(WebsiteParser.Parse(html).OpeningHours);
    }
}
