# Architectuur: Digitale Delta API V3 Template

Dit document beschrijft de architectuurkeuzes achter de .NET-template voor de Digitale Delta API V3. 

Het ontwerp is gebaseerd op drie kernprincipes: 

- Performance 
- Flexibiliteit
- Type-Safety 

## Configuration-Driven Design

In tegenstelling tot traditionele API's die sterk leunen op hard-coded modellen en ORM's (zoals Entity Framework), 
wordt deze template (bijna) volledig gedreven door configuratiebestanden (JSON en SQL).

- **Single Source of Truth**: De mapping tussen de database en de OData-standaard wordt gedefinieerd in de configuratie.
- **Dynamic Metadata**: Bij het opstarten worden de OData $metadata (CSDL) en de OpenAPI-specificatie (OAS) dynamisch gecompileerd 
op basis van de actieve configuratie.

Er is slechts een klein beetje code vereist, voor autorisatie en request logging.

## High-Performance Data Pipeline

Om ook enorme volumes sensordata (tijdreeksen, maar ook verwachtingen) efficiënt te ontsluiten, omzeilt de template de standaard .NET reflectie-gebaseerde mapping.

- **DbRowMaterializer**: Een gespecialiseerde component die data rechtstreeks uit de DbDataReader streamt naar een geoptimaliseerde Dictionary-structuur.
- **Micro-optimalisaties**: Door gebruik te maken van `MethodImplOptions.AggressiveInlining` en vooraf berekende kolom-ordinals, is de overhead per record minimaal.
- **No-Reflection Serialization**: De JSON-assembler bouwt de payload op zonder gebruik te maken van reflectie, wat de druk op de Garbage Collector verlaagt en de doorvoersnelheid maximaliseert.

## Performance & Efficiency

Dankzij het gebruik van een low-allocation data pipeline en directe streaming materialisatie in .NET 10, is de template geoptimaliseerd voor grootschalige sensordatasets en high-throughput scenario's.

Door de overhead van traditionele reflectie-gebaseerde mapping en zware ORM-frameworks te omzeilen,
wordt de rekentijd per record geminimaliseerd. Hierdoor blijft de API razendsnel, zelfs bij het ontsluiten van miljoenen observaties.

## Cross-Platform & Cloud-Native

De architectuur is volledig OS-agnostisch ontworpen en maakt gebruik van de cross-platform kracht van .NET.

- **Geen OS Dependency**: De engine heeft geen afhankelijkheden van Windows-specifieke componenten of IIS (Internet Information Server).
- **Container-Ready**: De template is bij uitstek geschikt voor Docker- en Kubernetes-omgevingen (Linux & Windows).
- **Consistente Runtime**: Of de API nu draait op een lokale Linux-server, een Azure Web App of een AWS Lambda, de performance en het gedrag blijven identiek voor vergelijkbare specificaties.
- **Self-Hosted Kestrel**: De API draait op de ingebouwde, high-performance Kestrel webserver. Voor productie-omgevingen is enkel een eenvoudige Reverse Proxy (zoals Nginx, IIS of YARP) nodig voor zaken als SSL-terminatie en load balancing.

## Custom OData Engine

De template maakt bewust geen gebruik van de standaard Microsoft OData libraries. Het gebruikt een subset van de OData-standaard.
Hiermee wordt het OData-model losgekoppeld van het databasemodel.

### Waarom geen standaard Microsoft OData libraries?

De standaard Microsoft OData libraries introduceren aanzienlijke performance-overhead aan beide kanten van de request/response cyclus:

**Inkomende kant (Request Processing):**
- Query validation en compilation hebben een exponentiële overhead bij complexe OData-modellen
- De overhead schaalt slecht met model-complexiteit, wat problematisch is voor het uitgebreide Digitale Delta domeinmodel (sensoren, meetpunten, parameters, locaties, etc.)

**Uitgaande kant (Response Serialization):**
- De standaard libraries gebruiken reflectie om data naar voorgedefinieerde objecttypen te mappen, wat substantiële overhead veroorzaakt
- Bij grote datasets leidt dit tot verhoogde memory allocaties en druk op de garbage collector
- Dit is vooral problematisch voor high-volume sensordata waarbij miljoenen records verwerkt moeten worden

Door een eigen OData-engine te bouwen, omzeilen we deze overhead volledig en behouden we de voordelen van de OData-querystandaard zonder de architecturale beperkingen.

**Performance-optimalisaties:**

Gebaseerd op analyse van reflectie-overhead, query compilation kosten en memory allocation patterns, verwachten we significante performance voordelen. De exacte winst hangt af van de use case (model complexiteit, dataset grootte, query patterns).

Concrete optimalisaties in deze template:
- **Direct streaming**: Data wordt rechtstreeks van de database naar de response gestreamed, zonder tussenliggende object-creatie
- **Geen lazy evaluation overhead**: In plaats van iterator chains die per record overhead introduceren, gebruikt de template directe loops voor maximale doorvoer
- **Dictionary-based materialisatie**: Door data direct in een compacte dictionary-structuur te plaatsen, wordt memory allocatie geminimaliseerd en serialisatie versneld

### Voordelen van de Custom Engine

Een tweetal geografische functies vervangen de 'standaard' OData geo-functies.
Dit vereenvoudigt het gebruik van de geo-functies en zorgen voor consistente projecties. De afhankelijkheid van `Microsoft.Spatial` vervalt daarmee.

- **Opinionated-vrij**: We behouden de volledige controle over de SQL-generatie zonder gedwongen te worden in het IQueryable keurslijf en de bijbehorende overhead.
- **SQL-Template Mapping**: OData-filters worden direct vertaald naar SQL-dialecten voor Postgres (PostGIS) of SQL Server, wat het gebruik van database-specifieke features (zoals TimescaleDB) mogelijk maakt.
- **Vereenvoudigde Geo-Syntax**: Omdat een resource binnen de Digitale Delta altijd één geografische context heeft, is de syntax vereenvoudigd (bijv. `distance(wkt='POINT(0 0)') gt 10`). De engine koppelt dit automatisch aan de geconfigureerde geografische kolom.
- **Projectie-beheer**: Het gewenste coördinatenstelsel (CRS) wordt afgehandeld via de HTTP-headers `Accept-Crs` en `Content-Crs`. De engine verzorgt de noodzakelijke transformaties on-the-fly tijdens de materialisatie, zoals gespecificeerd binnen DD API V3.

## Defensieve Startup & Validatie

Om de stabiliteit in productie te garanderen, bevat de template een uitgebreide validatieketen bij het opstarten:

- **Schema Validatie**: De lokale configuratie wordt gecontroleerd tegen de officiële Digitale Delta 'knowledge' tabellen (via externe CSV-definities).
- **Hot-Reload**: Kritieke instellingen zoals databaseverbindingen zijn reload-veilig. 

***Voor logica-wijzigingen (zoals SQL-templates) is een herstart vereist om consistentie te waarborgen.***

## Geo-Spatial Processing

Geografische data wordt 'first-class' behandeld:

- **On-the-fly Transformatie**: Coördinatenstelsels (CRS) kunnen tijdens de data-materialisatie worden getransformeerd (bijv. van RD naar WGS84) op verzoek van de client.
- **NetTopologySuite Integratie**: Gebruik van industriestandaard bibliotheken voor robuuste geo-verwerking.

Voor geografische berekeningen wordt gebruik gemaakt van de NetTopologySuite (NTS) bibliotheek, die een uitgebreide set functies biedt voor geografische operaties. 
Deze bibliotheek is ontworpen voor prestaties en is compatibel met diverse CRS's, waardoor flexibiliteit en nauwkeurigheid worden gegarandeerd.

## Geoptimaliseerd voor Tijdreeksen (Sensorplatformen)

De architectuur is specifiek ontworpen om ook enorme volumes en de snelheid van sensor- en meetdata te verwerken, ongeacht de onderliggende database-engine.

- **SQL-Template Kracht**: Omdat de ontsluiting via pure SQL-templates verloopt, kunnen specifieke database-optimalisaties zoals TimescaleDB Hypertables (Postgres) of Partitioned Tables (SQL Server) naadloos worden benut. De API hoeft de complexe opslaglogica niet te kennen; 
deze profiteert simpelweg van de snelheid van de onderliggende engine.
- **Efficiënte Aggregatie**: De template maakt het eenvoudig om geaggregeerde data (bijv. uur- of daggemiddelden) te ontsluiten door de mapping te laten verwijzen naar geoptimaliseerde database-views.
- **Streaming Materialisatie**: De DbRowMaterializer is ontworpen om de hoge doorvoersnelheid van moderne time-series-databases bij te benen. Dit voorkomt dat de API de bottleneck wordt bij het opvragen van grote reeksen historische meetgegevens.

## Customization & Extensibility

Hoewel de template grotendeels configuratie-gedreven is, zijn er twee cruciale aspecten die expliciete implementatie vereisen via C#-extensiepunten. 

Dit stelt organisaties in staat om hun eigen beleid en compliance-eisen af te dwingen.
 
- **`IRequestLogger` (Logging)**: Organisaties kunnen hun eigen logging-strategie implementeren (bijv. logging naar een specifieke database, ElasticSearch of een cloud-native oplossing). De template biedt de hooks om zowel de inkomende requests als de resultaten (inclusief performance-metrieken) vast te leggen.
- **`IAuthorize` (Autorisatie)**: Omdat autorisatie binnen de watersector vaak complex is (gebaseerd op organisaties, rollen of specifieke meetpunten), wordt dit niet via statische configuratie afgehandeld. Ontwikkelaars kunnen hier hun eigen autorisatie-logica implementeren om te bepalen of een gebruiker toegang heeft tot de opgevraagde resources.
- **`RegisterRequiredCustomServices` (Service registratie)**: Via deze extensiemethode worden de eigen implementaties van autorisatie en logging geregistreerd in de Dependency Injection container van .NET, waardoor ze naadloos worden geïntegreerd in de API-pipeline.

## Waarom deze keuzes?

Deze architectuur stelt een organisatie in staat om een DD-API te ontsluiten met de snelheid van een op maat gemaakte Go- of C++-service, maar met het gemak van een .NET-omgeving. 
Het is specifiek geoptimaliseerd voor de unieke uitdagingen van de water- en milieusector: enorme hoeveelheden sensordata en complexe geografische informatie.

## Toekomst & Uitbreidbaarheid

### NoSQL
Hoewel de huidige focus ligt op relationele databases (Postgres/SQL Server), is de architectuur theoretisch voorbereid op NoSQL-oplossingen (zoals MongoDB of Elasticsearch). 

Dankzij de ontkoppeling van de OData-engine en de materialisatie-pipeline, hoeft voor een NoSQL-implementatie enkel de 'ODataToQuery' converter te worden aangepast. 

- **Vervangbare Translators**: Voor een NoSQL-implementatie hoeven enkel nieuwe implementaties van de translator-classes te worden aangemaakt (bijv. een `ODataToMongoTranslator` of `ODataToElasticTranslator`).
- **Pipeline Behoud**: De high-performance materialisatie-pipeline en de serializers blijven hierbij ongewijzigd, aangezien deze werken op een abstracte dataset en niet direct op de database-engine.

### Streaming & High-Volume Data (S-CSV)

DigitaleDelta gebruikt het gespecificeerde streaming-formaat **[S‑CSV (Structured CSV)](https://digitaledeltaorg.io/s-csv)**.
S‑CSV is een *zelf-beschrijvend* tekstformaat dat de eenvoud van CSV combineert met expliciete, strikt vastgelegde kolomdefinities voor consistente uitwisseling (o.a. datum/tijd‑interpretatie).

- **Self‑describing header**: de eerste regel bevat veldnamen met datatype‑definities en optionele parameters/formaten (bijv. `tags:list[element:string,sep:pipe]`).
- **Rijke structuren**: ondersteunt lists, dictionaries en semantische triples, terwijl de basisprincipes van CSV (RFC 4180) behouden blijven.
- **Streaming‑first**: doordat alle metadata in de header staat, kan een parser records regel‑voor‑regel verwerken zonder buffering of random access.
- **Efficiënt en leesbaar**: geschikt voor hoge volumes, met behoud van menselijke leesbaarheid en compacte representatie.

#### Performance Voordelen van S-CSV

Voor bulk timeseries data biedt S-CSV aanzienlijke performance-voordelen ten opzichte van JSON:

- **Compactheid**: 20-40% kleiner dan equivalente JSON-representaties doordat metadata slechts eenmaal in de header staat, niet per record herhaald wordt, en er geen overhead is van JSON-structuren (haakjes, quotes, escaped characters)
- **Parsing Performance**: 2-3x sneller te parsen dan JSON doordat geen complexe object trees opgebouwd hoeven te worden
- **Streaming Efficiency**: Zero-copy streaming mogelijk - records kunnen direct regel-voor-regel verwerkt worden zonder intermediate buffers
- **Network Efficiency**: Kleinere payloads betekenen minder bandbreedte en snellere overdracht, cruciaal bij miljoenen observaties

De combinatie van de reeds geoptimaliseerde data-pipeline (DbRowMaterializer → Dictionary) met S-CSV serialization zal de totale throughput voor bulk sensor data nog eens 2-3x verbeteren ten opzichte van JSON.

#### Schema Flexibility

Een belangrijk voordeel van S-CSV ten opzichte van binaire formaten (zoals Protobuf, Avro, of Arrow) is de natuurlijke integratie met de configuration-driven architectuur:

- **Self-describing Schema**: Het schema zit in de data zelf (header), niet in externe definitiebestanden die apart beheerd moeten worden
- **Configuration-driven**: Wijzigingen in de database-mapping worden automatisch gereflecteerd in de S-CSV header, zonder deployment van schema-definities
- **Zero Client Impact**: Clients ontdekken nieuwe kolommen en datatypes automatisch via de header, zonder hercompilatie of updates
- **Backwards Compatible**: Clients kunnen onbekende kolommen negeren en blijven functioneren bij schema-uitbreidingen

Binaire formaten vereisen vaste schema-definities (.proto, .avsc) die bij elke aanpassing geüpdatet en opnieuw gedeployed moeten worden. Dit zou het voordeel van de dynamische, configuratie-gedreven architectuur grotendeels teniet doen en frequente coördinatie tussen server- en client-deployments vereisen.

S‑CSV is geschikt voor kolomgebaseerde data. Het is ___geen___ alternatief voor hiërarchische formaten zoals CoverageJSON.

Dit formaat zal worden ingebouwd in een toekomstige versie van dit template.
