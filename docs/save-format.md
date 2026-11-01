# FM24-sparfilens format (det vi vet)

Det här är framtaget genom att jämföra en riktig FM24-sparfil med FMRTE. Allt nedan är verifierat
mot sparfilen "Mattias Nordström - Paris SG.fm" (speldatum 2023-07-10) om inget annat står.

## Ytterlager

- Filen börjar med `02 01 'fmf' ...` och består sedan av ~1 100 zstd-ramar (magic `28 b5 2f fd`).
- Packar man upp ramarna i ordning får man ~143 MB data. Det är den all läsning nedan gäller.
- Strängar lagras som `[u32 längd][UTF-8]`.
- Datum lagras som `[u16 dag på året (låga 9 bitar)][u16 år]`. `01 00 6c 07` (1 jan 1900) betyder "inget datum".

## Namnlistor

Tre listor med `[u32 id][u32 längd][UTF-8]`, id från 0: förnamn (~246 000), efternamn (~542 000)
och smeknamn/"common names" (~86 000). Persondatan börjar direkt efter smeknamnslistan.

## Person

Namnblock: `[u32 förnamn][00][u32 efternamn][00][u32 smeknamn eller ffffffff][00][u32 längd][fullständigt namn][födelsedatum]`.

## Spelare

Spelardatan ligger 100–1 600 byte före spelarens namnblock. Räknat från CA-fältet `c`:

| Offset | Typ | Innehåll |
|---|---|---|
| c-31 | datum | dagens speldatum |
| c-6, c-4, c-2 | i16 | rykte hemma / nuvarande / världen (0–10 000) |
| c | i16 | CA |
| c+2 | i16 | PA (negativt = intervall, t.ex. -95) |
| c+15 | u32 | klubb-id där spelaren spelar just nu (-1 = klubblös) |
| c+23 | 15 × u8 | positionsbetyg 1–20: GK, SW, D L, D C, D R, DM, M L, M C, M R, AM L, AM C, AM R, ST, WB L, WB R |
| c+38 | 54 × u8 | attribut 1–100 (visas som /5), ordning i `Attributes.SaveFileOrder` |
| c+92 | u32 | ett pengavärde (Mbappé 145 080 000). Troligen värde eller senaste avgift, inte säkert |
| c+97 | datum | när spelaren kom till klubben |

## Klubbar

Klubbpost: `[u32 index][u32 uid][u32 uid][00][u32 nation][ffffffff][nation][nation] ... [namn][kortnamn]`.
Id:t som spelarna pekar på står inne i posten efter byten `ff 00 0N`, och upprepas i slutet av posten.
PSG har id 658, Marseille 656.

## Inte hittat än

Löner, kontraktsslut, klubbudget, skador, avstängningar, moral, nationalitet, lånestatus
(utlånade spelare står på klubben de spelar i just nu). Skrivning tillbaka till filen är inte gjord.
