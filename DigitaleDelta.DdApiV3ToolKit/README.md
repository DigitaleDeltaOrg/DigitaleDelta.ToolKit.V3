# Digitale Delta DD API V3 Toolkit

Deze Toolkit levert een software-implementatie voor het ontsluiten van data volgens de **Digitale Delta API V3**. De Toolkit is een cross-platform .NET 10-project en is bedoeld om zonder eigen DD API V3-implementatie een werkende adapter te realiseren.

De inrichting gebeurt primair via configuratiebestanden, SQL-query’s of databaseviews. Organisatiespecifieke softwareontwikkeling is normaal gesproken beperkt tot kleine plugins voor bijvoorbeeld autorisatie en request logging.

## Documentatie

Lees bij voorkeur eerst de documenten in de `Docs`-folder:

| Document | Doel |
|----------|------|
| `Docs/Stappenplan.md` | Besluitvorming over inzet en inrichting, fasering, randvoorwaarden en go/no-go momenten |
| `Docs/IMPLEMENTATIE-GIDS.md` | Technische configuratie, SQL, mappings, plugins, deployment, logging en troubleshooting |
| `Docs/MANAGEMENT-OVERZICHT.md` | Business case, planning, risico’s, rollen en besluitvorming |
| `architecture.md` | Architectuurkeuzes en technische ontwerpprincipes |

## Gebruiksscenario's

### Scenario 1: Binary deployment

Aanbevolen voor organisaties die de Toolkit willen gebruiken zonder de source code aan te passen.

Globale werkwijze:

1. Download of bouw de publish-output van de Toolkit.
2. Kopieer de benodigde templates naar `ConfigurationFiles`.
3. Configureer databaseverbinding, query’s, mappings, OpenAPI/OAS en CSDL.
4. Maak indien nodig een plugin voor autorisatie of request logging.
5. Plaats plugin-DLL’s in de `Plugins`-folder.
6. Start de applicatie.

Voor details: zie `Docs/IMPLEMENTATIE-GIDS.md`.

### Scenario 2: Source deployment

Geschikt voor ontwikkelaars die de Toolkit lokaal willen draaien, debuggen of aan de Toolkit zelf willen bijdragen.

Ook bij source deployment geldt voor implementaties:

- gebruik configuratiebestanden voor implementatiespecifieke instellingen;
- gebruik plugins voor autorisatie en request logging;
- pas de Toolkit-source alleen aan wanneer je bewust aan de Toolkit zelf ontwikkelt.

Voor details: zie `Docs/IMPLEMENTATIE-GIDS.md`.

## Configuratie op hoofdlijnen

De belangrijkste implementatiespecifieke bestanden staan in `ConfigurationFiles`, onder andere:

- `appsettings.json`
- `observation-configuration.json`
- `reference-configuration.json`
- `observation-data.sql`
- `observation-count.sql`
- `reference-data.sql`
- `reference-count.sql`
- `csdl.xml`
- `openapi.yaml`

Pas de originele templates niet direct aan. Kopieer ze eerst naar `ConfigurationFiles`.

## Database-ondersteuning

De Toolkit is database-agnostisch en ondersteunt onder andere:

- PostgreSQL, eventueel met PostGIS
- SQL Server
- MySQL
- Oracle
- SQLite

Gebruik bij voorkeur databaseviews om de interne databasestructuur te ontkoppelen van de DD API V3-mapping.

Sinds versie 1.1.0 is de toolkit in staat op met Coverages om te gaan.
Daarbij geldt: 
- de coverage wordt niet door de toolkit samengesteld. Dat is aan de database laag
- de toolkit verwacht een JSON-blob die de coverage bevat. Die is volledig en geldig CoverageJSON.

Advies: maak de CoverageJSON niet on-the-fly aan. Dat zorgt mogelijk voor performance bottlenecks.


## Plugin-systeem

Voor organisatiespecifieke logica kunnen plugins worden gebruikt, bijvoorbeeld voor:

- autorisatie;
- request logging.

Zo blijft de Toolkit zelf generiek en blijven organisatie- of domeinspecifieke keuzes buiten de kerncode.

Zo blijft de Toolkit zelf generiek en blijven organisatie- of domeinspecifieke keuzes buiten de kerncode.

Zie `Docs/IMPLEMENTATIE-GIDS.md` voor pluginvoorbeelden en configuratie.

## Deployment

De Toolkit kan onder andere draaien als:

- binary deployment;
- source deployment;
- container;
- service achter een reverse proxy.

Voor productiegebruik moeten beveiliging, logging, monitoring, performance, beheer en acceptatie expliciet worden ingericht.

## Proof-of-concept en productie

Een beperkte proof-of-concept kan snel worden gerealiseerd wanneer brondata goed beschikbaar is en via een databaseview kan worden aangeboden. Een productiewaardige inrichting vraagt aanvullende aandacht voor productiecapaciteit, schaalbaarheid, security, monitoring, logging, performance, documentatie, beheer en formele acceptatie.

Zie `Docs/Stappenplan.md` voor de aanbevolen fasering.
