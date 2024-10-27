# SCHQ_Web

Web-Interface und gRPC-Server für die Applikation `Star Citizen Handle Query`

![Relations Data](/Screenshots/SCHQ_Web_Relations_Data.png?raw=true "Relations Data")

## Verwandte Projekte

### Star Citizen Handle Query

Das Tool `Star Citizen Handle Query` verwendet den gRPC-Server, um Beziehungen Benutzer übergreifend zu synchronisieren.

Repository-URL: https://github.com/Kuehlwagen/Star-Citizen-Handle-Query

## Benutzung

### Sprache

In der Auswahlliste kann die bevorzugte Sprache ausgewählt werden. Es stehen folgende Sprachen zur Verfügung:
- Deutsch
- English

### Thema

Hier kann das Thema ausgewählt werden. Es stehen folgende Themen zur Verfügung:
- Dunkel
- Hell
- Vorgabe System

### Lokaler Speicher

![Local Storage](/Screenshots/SCHQ_Web_LocalStorage.png?raw=true "Local Storage")

Hier können Beziehungen lokal im Browser (Local Storage) verwaltet werden.

- __Beziehungen herunterladen:__ Beim Klick werden die Beziehungen des Kanals als JSON-Datei heruntergeladen
- __Beziehungen hochladen:__ Beim Klick können Beziehungen als JSON-Datei hochgeladen werden
- __Beziehungen löschen:__ Beim Klick werden alle Beziehungen des lokalen Speichers des Browsers nach Bestätigung, dass die Beziehungen wirklich gelöscht werden sollen, gelöscht
- Für weitere Informationen, siehe Abschnitt `Beziehungen`

### Kanäle

![Channels](/Screenshots/SCHQ_Web_Channels.png?raw=true "Channels")

Hier werden die konfigurierten Kanäle aufgelistet.

- __Kanal hinzufügen:__ Durch einen Klick auf diese Schaltfläche kann ein Kanal hinzugefügt werden.
- __Filter:__
  - __Texteingabe:__ Während der Eingabe wird die Kanalliste gefiltert. Es werden Kanäle angezeigt, welche den eingegebenen Text im Namen enthalten.
  - __Aktualisieren:__ Liest die Kanalliste neu aus
- __Kanalliste:__
  - __Name:__ Name des Kanals
    - Beim Klick werden die Beziehungen des Kanals geöffnet
  - __Geschützt:__ Angabe, ob der Kanal durch ein Passwort geschützt ist
  - __Berechtigung:__ Angabe, welche Rechte ein Benutzer ohne Angabe des Kanalpassworts hat (siehe auch `Kanal erstellen`)
  - __Aktion:__ Beim Klick auf die `Verwalten`-Schaltfläche wird die Verwaltung des Kanals geöffnet

### Kanal hinzufügen

![Add Channel](/Screenshots/SCHQ_Web_Add_Channel.png?raw=true "Add Channel")

Hier kann ein neuer Kanal erstellt werden.

- __Kanalname:__ Name des Kanals
- __Kanalpasswort:__ Passwort des Kanals (Kanäle ohne Passwort werden beim Serverneustart gelöscht.)
- __Kanalpasswort wiederholen:__ Wiederholung des Passworts, damit eine Fehleingabe des Passworts verhindert wird
- __Kanalberechtigung:__ Angabe, welche Rechte ein Benutzer ohne Angabe des Kanalpassworts hat
  - `None`: Ohne Angabe des Kanalpassworts darf ein Benutzer weder lesen noch schreiben
  - `Read`: Ohne Angabe des Kanalpassworts darf ein Benutzer lesen, jedoch nicht schreiben
  - `Write`: Ohne Angabe des Kanalpassworts darf ein Benutzer lesen und schreiben
- __Kanal hinzufügen:__ Beim Klick der Schaltfläche wird der Kanal versucht anzulegen

### Beziehungen

![Relations](/Screenshots/SCHQ_Web_Relations.png?raw=true "Relations")

Hier können Beziehungen ausgelesen, geschrieben und synchronisiert werden.

- __Kanalpassword:__ Angabe des Kanalpassworts
- __Aktualisieren:__ Liest die Beziehungen neu aus
- __Sync:__ Startet oder beendet die Synchronisation der Beziehungen des Kanals, abhängig vom Status der Synchronisierung
  - Die Synchronisierung ist aktiv, wenn die Schaltfläche einen grünen Hintergrund hat
- __Handle / Organisation:__ Beim Klick kann zwischen der Handle- und Organisationssuche gewechselt werden
- __Texteingabe:__ Hier kann der Name des zu suchenden Spielers bzw. der zu suchenden Organisation eingegeben werden.
  - Wenn die Texteingabe geleert wird, wird der Typ auf `Handle` geändert und das Suchergebnis entfernt.
  - Die Suche wird ausgelöst, wenn eine der beiden Tasten `Enter` oder `NumpadEnter` gedrückt wird.
- __Suche:__ Startet die Suche
- __Entfernen:__ Leert die Texteingabe und somit auch das Suchergebnis
- __Suchergebnis:__ Das Suchergebnis enthält, abhängig vom Typ, Informationen zum Handle und dessen Organisationszugehörigkeiten oder zur Organisation.
  - Das Avatar eines Suchergebnisses kann geklickt werden, um die abhängige RSI-Seite in einem neuen Fenster zu öffnen.
  - Rechts neben dem Avatar wird, sofern eine Beziehung festgelegt wurde, die Beziehung farblich dargestellt
    - `Grün`: Friendly
    - `Grau`: Neutral
    - `Gelb`: Bogey
    - `Rot`: Bandit
  - Es ist möglich, via Klick auf die Schaltfläche mit dem Bearbeiten-Symbol einen Kommentar hinzuzufügen.
  - Durch einen Klick auf eine der Beziehung-Schaltflächen am rechten Rand des Suchergebnisses kann die Beziehung festgelegt werden.
  - Wenn im Community-Hub eines Spielers das Twitch-Konto verknüpft ist und der Spieler gerade aktiv streamt, wird ein Twitch-Symbol neben dem Handle dargestellt. Ein Klick auf das Symbol öffnet die Community Hub Seite des Handles.
- __Filter:__
  - __Texteingabe:__ Während der Eingabe wird die Beziehungsliste gefiltert. Es werden Beziehungen angezeigt, welche den eingegebenen Text im Namen enthalten.
  - __Grün:__ Wenn angehakt, werden freundliche Beziehungen berücksichtigt (Friendly)
  - __Grau:__ Wenn angehakt, werden neutrale Beziehungen berücksichtigt (Neutral)
  - __Gelb:__ Wenn angehakt, werden unbekannte Beziehungen berücksichtigt (Bogey)
  - __Rot:__ Wenn angehakt, werden feindliche Beziehungen berücksichtigt (Bandit)
  - __Hellblau:__ Wenn angehakt, werden Organisationen berücksichtigt
- __Beziehungsliste:__
  - __Typ:__ Beziehungstyp
    - Person = Handle
    - Weltkugel = Organisation
  - __Name:__ Name der Beziehung (bei Handle der Handle und bei Organisationen die SID der Organisation)
    - Durch einen Klick auf die Schaltfläche mit dem Bearbeiten-Symbol kann ein Kommentar hinzugefügt werden.
  - __Aktion:__ Beim Klick auf die `Entfernen`-Schaltfläche wird der Beziehungswert `Not Assigned` zugewiesen, sodass die Beziehung bei der Verwendung des Standardfilters ausgeblendet wird.

### Kanal verwalten

![Manage Channel](/Screenshots/SCHQ_Web_Manage_Channel.png?raw=true "Manage Channel")

Hier kann ein Kanal verwaltet werden.

- __Kanalpassword:__ Angabe des Kanalpassworts, um Berechtigungen für die folgenden Funktinalitäten zu bekommen
- __Beziehungen herunterladen:__ Beim Klick werden die Beziehungen des Kanals als JSON-Datei heruntergeladen
- __Beziehungen hochladen:__ Beim Klick können Beziehungen als JSON- oder CSV-Datei hochgeladen werden
- __Kanal löschen:__ Beim Klick wird der Kanal nach Bestätigung, dass der Kanal wirklich gelöscht werden soll, gelöscht

#### JSON
Es wird ein JSON-Array mit Beziehungsinformationen erwartet. Der Inhalt einer in der Kanalverwaltung heruntergeladenen Beziehungen-Datei entspricht dem erwarteten Format.
- __Type:__ Optionale Angabe, um was für einen Beziehungstyp es sich handelt
  - 0 = Handle __(Standard)__
  - 1 = Organization
- __Name:__ Name der Beziehung (bei Handle der Handle und bei Organisationen die SID der Organisation)
- __Relation:__ Optionale Angabe des Beziehungswerts
  - 0 = Not Assigned __(Standard)__
  - 1 = Friendly
  - 2 = Neutral
  - 3 = Bogey
  - 4 = Bandit
- __Comment:__ Optionale Angabe eines Kommentars
``` JSON
[
  {
    "Name": "Gentle81",
    "Relation": 3
  },
  {
    "Name": "Kuehlwagen",
    "Relation": 1,
    "Comment": "Cooler Typ"
  },
  {
    "Type": 1,
    "Name": "KRT",
    "Relation": 1,
    "Comment": "Beste Orga"
  }
]
```

#### CSV
Als Feldtrenner wird ein Semikolon `;` verwendet. Wenn das optionale Feld 3 angegeben wird, muss der Wert von Feld 2 ebenfalls angegeben werden.
- __Feld 1:__ Name der Beziehung (bei Handle der Handle und bei Organisationen die SID der Organisation)
- __Feld 2:__ Optionale Angabe des Beziehungswerts
  - 0 = Not Assigned __(Standard)__
  - 1 = Friendly
  - 2 = Neutral
  - 3 = Bogey
  - 4 = Bandit
- __Feld 3:__ Optionale Angabe, um was für einen Beziehungstyp es sich handelt
  - 0 = Handle __(Standard)__
  - 1 = Organization
- __Feld 4:__ Optionale Angabe eines Kommentars
``` CSV
Gentle81;3;0;
Kuehlwagen;1;0;Cooler Typ
KRT;1;1;Beste Orga
```

## Weitere Informationen

### gRPC-Server
- Für die Benutzer übergreifende Synchronisation von Beziehungen stellt `SCHQ_Web` einen gRPC-Server zur Verfügung. Hier können außerdem Kanäle verwaltet werden.
- Der gRPC-Server hat die selbe URL wie das Web-Interface.

### Memory Cache
- Das Web-Interface verwendet für die Verwaltung von bereits ermittelten Handle- und Organisations-Daten einen Memory Cache, damit die Daten, wenn sie in einem bestimmten Zeitraum erneut abgefragt werden, nicht erneut von der RSI-Webseite heruntergeladen werden müssen.
- Es werden maximal 1.024 Datensätze vorgehalten.
- Wird innerhalb von vier Stunden nach dem letzten Abruf eines Datensatzes dieser erneut abgefragt, hat dieser eine erneute Gültigkeit von vier Stunden.
- Spätestens 12 Stunden nach der initialen Erstellung eines Datensatzes wird dieser erneut von der RSI-Webseite heruntergeladen.

## Installation

SCHQ_Web kann auf einem IIS als .NET-Anwendung installiert werden.

### Konfigurationswerte

In der Datei `appsettings.json` können folgende Werte in der Gruppe `MemoryCache` angepasst werden:
- __`SizeLimit`:__ Angabe der maximal im Cache zu verwaltenden Handle- und Organisations-Daten
- __`SlidingExpirationInHours`:__ Angabe, in welchem Zeitraum nach dem Aufruf eines Datensatzes die Gültigkeit zurückgesetzt wird (Angabe in Stunden)
- __`AbsoluteExpirationInHours`:__ Angabe, wann ein Datensatz nach der initialen Erstellung spätestens gelöscht werden soll (Angabe in Stunden)
``` JSON
{
  "MemoryCache": {
    "SizeLimit": 1024,
    "SlidingExpirationInHours": 4,
    "AbsoluteExpirationInHours": 12
  }
}
```
