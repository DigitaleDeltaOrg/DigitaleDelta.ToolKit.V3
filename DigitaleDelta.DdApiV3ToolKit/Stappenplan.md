# Stappenplan voor besluitvorming over inzet en inrichting van de DigitaleDelta Toolkit

| Kenmerk            | Waarde                       |
|:-------------------|:-----------------------------|
| Documentversie     | 1.0.0                        |
| Status             | Concept                      |
| Datum              | 2026-09-18                   |
| Laatst gewijzigd   | 2026-09-18                   |
| Auteur(s)          | Digitale Delta, Geri Wolters |
| Eigenaar           | Digitale Delta               |
| Onder beheer bij   | Informatiehuis Water         |
| Van toepassing op  | DD API V3 Toolkit 1.0.x      |
| API-versie         | DD API V3                    |
| Licentie           | MIT                          |

Dit document is beschikbaar in zowel Microsoft Word- als Markdown-formaat.
De documenten waarnaar wordt verwezen, zijn beschikbaar in de `Docs`-map van `DigitaleDelta.DdApiV3ToolKit` op GitHub.
Voor die documenten is het Microsoft Word-formaat niet geschikt; ze bevatten technische documentatie, voorbeelden en Markdown-specifieke opmaak.

## 1. Executive summary

Dit document ondersteunt het beslisorgaan bij de vraag of en hoe de **DigitaleDelta DD API V3 Toolkit** wordt ingezet voor het ontsluiten van data via **DD API V3**.

Bij het ontwikkelen en in gebruik nemen van een digitale dienst vormen softwareontwikkeling en softwareonderhoud vaak een grote kostenpost. De Toolkit beperkt die kosten doordat deze de software-implementatie van DD API V3 levert. Organisaties hoeven in beginsel geen eigen DD API V3-software te ontwikkelen.

De Toolkit is daarbij niet alleen een voorbeeld of startpunt, maar de softwarebasis voor de DD API V3-adapter. Organisatiespecifieke code is normaal gesproken alleen nodig voor onderdelen die bewust buiten de generieke Toolkit vallen, zoals autorisatie en request logging via plugins.

De belangrijkste opgave voor een organisatie verschuift daardoor van softwareontwikkeling naar inrichting en ingebruikname:

- het kiezen van de juiste data
- het begrijpen van de betekenis en kwaliteit van die data
- het bepalen hoe de data geschikt wordt aangeboden
- het passend inrichten van de omgeving waarin de Toolkit draait
- het maken van keuzes over beveiliging en beheer
- het borgen van eigenaarschap, acceptatie en continuïteit.

Een proof-of-concept kan bij gunstige omstandigheden snel worden gerealiseerd. Dat zegt vooral iets over de snelheid waarmee een eerste werkende inrichting kan worden gemaakt.

Het centrale uitgangspunt voor besluitvorming is daarom:

> De Toolkit beperkt de benodigde softwareontwikkeling. Een proof-of-concept toont haalbaarheid aan. Productie vraagt daarnaast een passende productie-inrichting van de omgeving, inclusief schaalbaarheid, beveiliging, beheer en continuïteit.

## 2. Doel van dit document

Dit stappenplan beschrijft:

- welke besluiten het beslisorgaan moet nemen
- welke fasering wordt aanbevolen
- welke informatie nodig is om per fase te besluiten
- welke verantwoordelijkheden belegd moeten zijn
- wanneer opschaling naar productie verantwoord is.

Dit document is **geen technische handleiding** en bevat daarom geen configuratiestappen, testscenario’s, deploymentinstructies of systeembeheerchecklists.

Technische inrichting staat in `IMPLEMENTATIE-GIDS.md`.

## 3. Relatie met andere documenten

| Document                  | Doel                                                               |
|---------------------------|--------------------------------------------------------------------|
| `Stappenplan.md`          | Besluitvorming, fasering en go/no-go momenten                      |
| `MANAGEMENT-OVERZICHT.md` | Business case, risico’s, rollen en managementinformatie            |
| `IMPLEMENTATIE-GIDS.md`   | Technische inrichting, configuratie, deployment en troubleshooting |
| `architecture.md`         | Architectuurkeuzes en technische ontwerpprincipes                  |

## 4. Uitgangspunten voor besluitvorming

Voor besluitvorming gelden de volgende uitgangspunten:

- De Toolkit levert de software-implementatie van DD API V3.
- Organisaties hoeven in beginsel geen eigen DD API V3-software te ontwikkelen.
- Organisatiespecifieke softwareontwikkeling blijft normaal gesproken beperkt tot pluginimplementaties voor onderdelen die buiten de generieke Toolkit vallen, zoals autorisatie of request logging.
- De implementatie bestaat primair uit configuratie, datavoorbereiding en inrichting van de runtime- en productieomgeving.
- De Toolkit is een hulpmiddel voor ontsluiting, geen oplossing voor datakwaliteit.
- De organisatie blijft eigenaar van data, betekenis, beveiliging en beheer.
- Een beperkte proof-of-concept is niet hetzelfde als een opgeschaalde en beheerde productieomgeving.
- Productiegebruik vraagt expliciete acceptatie.
- Eigenaarschap moet vooraf worden belegd.
- Technische haalbaarheid is slechts één onderdeel van het besluit.
- Beheer, beveiliging en continuïteit zijn randvoorwaarden voor productie.

## 5. Besluit dat gevraagd wordt

Het beslisorgaan neemt per fase een expliciet besluit.

| Fase                   | Besluitvraag                                                   |
|------------------------|----------------------------------------------------------------|
| Verkenning             | Is er voldoende aanleiding om een proof-of-concept te starten? |
| Proof-of-concept       | Is de aanpak kansrijk genoeg voor vervolg?                     |
| Pilot                  | Is de oplossing bruikbaar genoeg voor productievoorbereiding?  |
| Productievoorbereiding | Zijn de randvoorwaarden voldoende ingevuld om live te gaan?    |
| Productie en beheer    | Is de voorziening stabiel en structureel belegd?               |

## 6. Implementatieniveaus

Maak vooraf onderscheid tussen drie niveaus.

| Niveau           | Betekenis                                                  | Bestuurlijke consequentie                                    |
|------------------|------------------------------------------------------------|--------------------------------------------------------------|
| Proof-of-concept | Aantonen dat ontsluiting kansrijk is                       | Beperkte scope en beperkte zekerheden zijn acceptabel        |
| Pilot            | Valideren met representatieve data en betrokken gebruikers | Bruikbaarheid, risico’s en beheerimpact worden beoordeeld    |
| Productie        | Beschikbaar stellen als beheerde voorziening               | Alleen verantwoord na formele acceptatie en beheerinrichting |

Een proof-of-concept mag beperkt zijn. Een productievoorziening niet.

## 7. Beslisinformatie vóór start

Voor start van een fase moet het beslisorgaan minimaal over de volgende informatie beschikken.

| Onderwerp     | Vraag                                                   |
|---------------|---------------------------------------------------------|
| Doel          | Waarom wordt deze data ontsloten?                       |
| Scope         | Welke datasets vallen binnen en buiten scope?           |
| Fase          | Gaat het om proof-of-concept, pilot of productie?       |
| Data          | Is de brondata voldoende bekend en beschikbaar?         |
| Aanpak        | Hoe wordt de data geschikt aangeboden voor ontsluiting? |
| Beveiliging   | Welke toegangsbeperkingen zijn nodig?                   |
| Eigenaarschap | Wie is functioneel en technisch eigenaar?               |
| Acceptatie    | Wanneer is de fase geslaagd?                            |
| Risico’s      | Welke risico’s of afhankelijkheden zijn bekend?         |

Zonder deze informatie kan technische uitvoering wel starten, maar is bestuurlijke acceptatie onvoldoende beheersbaar.

## 8. Rollen op hoofdlijn

De volgende verantwoordelijkheden moeten expliciet zijn belegd.

| Rol                           | Verantwoordelijkheid                                                                 |
|-------------------------------|--------------------------------------------------------------------------------------|
| Beslisorgaan / opdrachtgever  | Besluit over start, vervolg, productie-uitrol en randvoorwaarden                     |
| Product owner / projectleider | Bewaakt scope, voortgang, afhankelijkheden en besluitvorming                         |
| Functioneel eigenaar          | Is verantwoordelijk voor betekenis, kwaliteit en bruikbaarheid van de ontsloten data |
| Technisch eigenaar            | Is verantwoordelijk voor technische werking, beheerbaarheid en continuïteit          |
| Securityverantwoordelijke     | Beoordeelt toegangsbeleid, beveiliging en compliance                                 |
| Beheerorganisatie             | Neemt de voorziening na acceptatie in beheer                                         |

Eén persoon kan meerdere rollen vervullen, maar eigenaarschap mag niet impliciet blijven.

## 9. Aanbevolen fasering

### Fase 1: Verkenning

Doel: bepalen of een proof-of-concept zinvol is.

Benodigde beslisinformatie:

- doel van de ontsluiting
- beoogde dataset of datasets
- globale inschatting van datakwaliteit
- globale inschatting van afhankelijkheden
- voorgestelde scope van de proof-of-concept.

Besluit:

> Starten we een proof-of-concept?

### Fase 2: Proof-of-concept

Doel: aantonen dat ontsluiting via de Toolkit kansrijk is binnen beperkte scope.

Benodigde beslisinformatie:

- werkt de basisontsluiting binnen de gekozen scope
- is de gekozen aanpak begrijpelijk en uitvoerbaar
- welke beperkingen zijn zichtbaar geworden
- welke risico’s zijn nog open
- wat is het advies voor vervolg.

Besluit:

> Stoppen we, passen we de aanpak aan, starten we een pilot of starten we productievoorbereiding?

### Fase 3: Pilot

Doel: beoordelen of de oplossing bruikbaar is met representatieve data en betrokken gebruikers.

Benodigde beslisinformatie:

- is de ontsloten data inhoudelijk bruikbaar
- zijn de belangrijkste risico’s voldoende bekend
- is duidelijk wat nodig is voor beheer en beveiliging
- zijn er blokkerende restpunten
- wat is het advies voor productievoorbereiding.

Besluit:

> Is de oplossing voldoende onderbouwd om productievoorbereiding te starten?

### Fase 4: Productievoorbereiding

Doel: vaststellen of de voorziening verantwoord live kan gaan.

Benodigde beslisinformatie:

- functionele acceptatie is afgerond
- technische acceptatie is afgerond
- beveiliging is akkoord
- beheerafspraken zijn vastgelegd
- monitoring en incidentafhandeling zijn georganiseerd
- openstaande restpunten zijn beoordeeld.

Besluit:

> Gaan we live?

### Fase 5: Productie en beheer

Doel: de voorziening structureel beheren na livegang.

Benodigde beslisinformatie:

- draait de voorziening stabiel
- is eigenaarschap overgedragen
- zijn restpunten belegd
- is duidelijk hoe wijzigingen en incidenten worden afgehandeld
- is duidelijk wie releases en securitymeldingen volgt.

Besluit:

> Is de voorziening goed overgedragen aan beheer en structureel geborgd?

## 10. Go/no-go criteria

| Moment                 | Go als...                                                                | No-go als...                                                           |
|------------------------|--------------------------------------------------------------------------|------------------------------------------------------------------------|
| Start proof-of-concept | Doel, dataset en beperkte scope duidelijk zijn                           | Doel of dataset onvoldoende duidelijk is                               |
| Na proof-of-concept    | Haalbaarheid is aangetoond en risico’s beheersbaar lijken                | De aanpak niet werkt of risico’s onvoldoende duidelijk zijn            |
| Na pilot               | Bruikbaarheid, beheerimpact en securityrichting voldoende duidelijk zijn | Inhoudelijke kwaliteit, beheer of beveiliging onvoldoende duidelijk is |
| Voor productie         | Acceptatie, beveiliging, beheer en monitoring akkoord zijn               | Productievoorwaarden niet aantoonbaar zijn ingevuld                    |
| Na livegang            | Eigenaarschap en beheer zijn overgedragen                                | Beheer of verantwoordelijkheid onduidelijk blijft                      |

Go/no-go besluiten worden vastgelegd, inclusief restpunten en geaccepteerde risico’s.

## 11. Minimale voorwaarden voor productie

Een productie-uitrol is pas verantwoord wanneer minimaal het volgende bestuurlijk is vastgesteld.

| Onderwerp    | Voorwaarde                                                       |
|--------------|------------------------------------------------------------------|
| Inhoud       | De ontsloten data is inhoudelijk geaccepteerd            D        |
| Techniek     | De voorziening is voldoende betrouwbaar voor het beoogde gebruik |
| Beveiliging  | Toegang en beveiligingsmaatregelen zijn akkoord                  |
| Beheer       | Functioneel en technisch eigenaarschap zijn belegd               |
| Monitoring   | Beschikbaarheid en fouten worden bewaakt                         |
| Continuïteit | Incidentafhandeling, wijzigingsproces en rollback zijn geregeld  |
| Documentatie | Afnemers en beheerders beschikken over voldoende documentatie    |

Concrete controles, testgevallen en technische acceptatiecriteria horen in de implementatie-gids of in projectdocumentatie, niet in dit stappenplan.

## 12. Buiten scope van dit stappenplan

Dit document beschrijft niet:

- technische configuratie
- database-inrichting
- SQL-query’s
- mappings
- API-specificaties
- deployment details
- plugin implementatie
- functionele testscenario’s
- technische testscenario’s
- systeembeheer-checklists
- troubleshooting
- detailinrichting van
  - monitoring
  - logging
  - security.

Deze onderwerpen horen in `IMPLEMENTATIE-GIDS.md`, bestaande beheerprocedures of projectdocumentatie.

## 13. Resultaat

Na toepassing van dit stappenplan heeft het beslisorgaan per fase onderbouwd besloten of de inrichting wordt gestart, voortgezet, opgeschaald of in productie genomen.

Het resultaat is afhankelijk van het gekozen niveau:

- een proof-of-concept waarmee haalbaarheid is aangetoond
- een pilot waarmee bruikbaarheid is gevalideerd
- of een productiewaardige DD API V3-adapter waarvan acceptatie, beveiliging, beheer en continuïteit zijn geborgd.

## 14. Documentbeheer

| Versie | Datum      | Auteur         | Status  | Wijziging                                                                                                                |
|--------|------------|----------------|---------|--------------------------------------------------------------------------------------------------------------------------|
| 1.0.0  | 2026-09-18 | Digitale Delta | Concept | Eerste versie                                                                                                            |
