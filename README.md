# SCHQ_Web

Web-Interface für den Star Citizen Handle Query Server (SCHQ Server)

![Relations Data](/Screenshots/SCHQ_Web_Relations_Data.png?raw=true "Relations Data")

## Verwandte Projekte

### SCHQ_Server

Der gRPC-Server wird für die Verwendung des Web-Interfaces vorausgesetzt.

Repository-URL: https://github.com/Kuehlwagen/SCHQ_Server

### Star Citizen Handle Query

Das Tool `Star Citizen Handle Query` verwendet den `SCHQ_Server`, um Beziehungen Benutzer übergreifend zu synchronisieren.

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

### Kanäle

![Channels](/Screenshots/SCHQ_Web_Channels.png?raw=true "Channels")

Hier werden die konfigurierten Kanäle aufgelistet.

- __Filter:__
  - __Texteingabe:__ Während der Eingabe wird die Kanalliste gefiltert. Es werden Kanäle angezeigt, welche den eingegebenen Text im Namen enthalten.
  - __Aktualisieren:__ Liest die Kanalliste neu aus
- __Kanalliste:__
  - __Name:__ Name des Kanals
    - Beim Klick werden die Relationen des Kanals geöffnet
  - __Geschützt:__ Angabe, ob der Kanal durch ein Passwort geschützt ist
  - __Berechtigung:__ Angabe, welche Rechte ein Benutzer ohne Angabe des Kanalpassworts hat (siehe auch `Kanal erstellen`)
  - __Aktion:__ Beim Klick auf die `Verwalten`-Schaltfläche wird die Verwaltung des Kanals geöffnet

### Kanal hinzufügen

![Add Channel](/Screenshots/SCHQ_Web_Add_Channel.png?raw=true "Add Channel")

Hier kann ein neuer Kanal erstellt werden.

- __Kanalname:__ Name des Kanals
- __Kanalpasswort:__ Passwort des Kanals
- __Kanalpasswort wiederholen:__ Wiederholung des Passworts, damit eine Fehleingabe des Passworts verhindert wird
- __Kanalberechtigung:__ Angabe, welche Rechte ein Benutzer ohne Angabe des Kanalpassworts hat
  - `None`: Ohne Angabe des Kanalpassworts darf ein Benutzer weder lesen noch schreiben
  - `Read`: Ohne Angabe des Kanalpassworts darf ein Benutzer lesen, jedoch nicht schreiben
  - `Write`: Ohne Angabe des Kanalpassworts darf ein Benutzer lesen und schreiben
- __Kanal hinzufügen:__ Beim Klick der Schaltfläche wird der Kanal versucht anzulegen

### Kanal verwalten

![Manage Channel](/Screenshots/SCHQ_Web_Manage_Channel.png?raw=true "Manage Channel")

Hier kann ein Kanal verwaltet werden.

- __Kanalpassword:__ Angabe des Kanalpassworts
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
  - __Aktion:__ Beim Klick auf die `Entfernen`-Schaltfläche wird der Beziehungswert `Not Assigned` zugewiesen, sodass die Beziehung bei der Verwendung des Standardfilters ausgeblendet wird.

## Installation

SCHQ_Web kann auf einem IIS als .NET-Anwendung installiert werden.

### Voraussetzungen
Für die Ausführung wird ein laufender SCHQ_Server vorausgesetzt, auf den zugegriffen werden kann.

Siehe: https://github.com/Kuehlwagen/SCHQ_Server

### Konfigurationswerte

In der Datei `appsettings.json` müssen folgende Werte angepasst werden:
- __`gRPC_Url`:__ URL des zu verwendenden gRPC-Servers (SCHQ_Server)
``` JSON
{
  "gRPC_Url": "https://schq.sctools.de"
}
```
