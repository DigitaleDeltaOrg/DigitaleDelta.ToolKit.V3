# Digitale Delta - ToolKit V3

ToolKit V3 is de basis van deze solution.
De solution levert een complete, modulaire en configureerbare implementatie voor DD API V3 in C#/.NET.

## Wat deze solution doet

Deze repository bundelt bouwstenen om DD API V3-services op te zetten die:

- requests veilig kunnen afhandelen (authenticatie, rate limiting, error handling)
- OData-verzoeken kunnen vertalen en responses kunnen opbouwen
- query's en skip tokens kunnen verwerken
- workflow- en service-logica consistent kunnen orkestreren

Het doel is niet een monoliet, maar een toolkit: je combineert alleen de onderdelen die je nodig hebt.

## ToolKit V3 als basis

`DigitaleDelta.DdApiV3ToolKit` vormt de kernlaag waarop de overige componenten aansluiten.
Daaromheen staan losse libraries voor specifieke verantwoordelijkheden, zoals:

- `DigitaleDelta.Authentication`
- `DigitaleDelta.ODataTranslator`
- `DigitaleDelta.ODataWriter`
- `DigitaleDelta.QueryService`
- `DigitaleDelta.RateLimiting`
- `DigitaleDelta.SkipToken`
- `DigitaleDelta.ErrorHandling`
- `DigitaleDelta.RequestHelpers`

Elke module heeft een duidelijke taak en kan onafhankelijk worden getest en toegepast.

## Voor wie dit is

Deze solution is bedoeld voor teams die:

- DD API V3 op .NET willen implementeren
- controle willen houden over performance, security en uitbreidbaarheid
- liever met herbruikbare componenten werken dan met een vaste end-to-end template

## Structuur van de repository

- `DigitaleDelta.*`: herbruikbare libraries
- `*.Tests`: testprojecten per library
- `DigitaleDelta.DdApiV3ToolKit.ExamplePlugin`: voorbeeld-extensie voor integratie

## Ontwikkeling

Gebruik de solution `DigitaleDelta.Toolkit.V3.sln` om alle projecten lokaal te openen, bouwen en testen.
Raadpleeg per component de lokale `README.md` voor module-specifieke configuratie en gebruik.
