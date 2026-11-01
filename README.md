# MittFMRTE

En förenklad editor för Football Manager 2024, inspirerad av FMRTE. Programmet läser din sparfil (.fm)
och visar trupper och spelare med bättre presets och en sökfunktion för realistiska värvningar.

## Status

- Läser spelare, attribut, positioner, CA/PA, rykte och klubbar direkt från en FM24-sparfil.
- Truppvy, spelarvy och "Smarta värvningar".
- Ändringar görs bara i programmet. **Inget sparas tillbaka till sparfilen än.**

Se [docs/save-format.md](docs/save-format.md) för vad vi vet om sparfilens format.

## Ladda ner

Varje bygge på GitHub (fliken Actions, arbetsflödet "Build") skapar `MittFMRTE.exe` för Windows
under "Artifacts". Den behöver ingen installation.

## Bygga själv

Kräver .NET 8 SDK på Windows.

```
dotnet test tests/MittFmrte.Core.Tests
dotnet run --project src/MittFmrte.App
```

Testet mot en riktig sparfil körs om miljövariabeln `MITTFMRTE_SAVE` pekar på en .fm-fil.
