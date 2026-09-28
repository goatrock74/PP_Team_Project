# Save storage regression checks

Run `dotnet run --project Tests/SaveSlots/SaveSlotChecks.csproj` with .NET 10.
Uses a unique temporary directory and a fields-only JsonUtility shim; does not touch game saves.
Includes slot isolation, round trips, backup recovery, backup-only listing, recovery followed by
another damaged primary, malformed entries, path validation and deletion.

For actual Unity serialization and scene lifecycle coverage, use
`Tools > Save System > Run Save Reload Smoke Test` in the Unity editor while not playing.
This creates its own slot, checks wallet/date/position/inventory/crop clock through scene reload,
detects runtime errors, and removes only that test slot on exit. Result: `Temp/SaveSmokeResult.txt`.
