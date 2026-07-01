---
name: new-device
description: >-
  Add a new smart-home device type (sensor or actuator) to the simulation,
  wiring all required layers: ItemModel, SimulationObjects fake device, and tests.
  Use when the user says "yeni cihaz ekle", "termostat/kamera/sensör ekle" or similar.
---

# New Device Type

Adding a device (e.g. thermostat, camera, humidity sensor) touches several layers. `Isik` (light, actuator) and `PIR` (motion sensor) are the canonical references — read their implementations first and mirror them exactly.

## Layers to wire

1. **`ItemModel/`** — the domain-side device class (like `Isik`, `PIR`). Constructor overloads taking an optional `SharedObject.AdleAreaBase` area plus name; `IpV4`/`IpV6` properties; state accessors named after the domain action (`OpenLight`/`CloseLight`/`GetLigthState` style — Turkish/English mix is the existing convention, follow it).

2. **`FakeDevices/` (project `SimulationObjects`)** — the fake hardware counterpart under `SimulationObjects.Device`. Register instances via the static `Device.Devices` registry (`Register`, `find(ip)`, `Reset`). The ItemModel class talks to its fake device through this registry by IP.

3. **Persistence (only if the device must be stored):** decide the owning EF6 stack — device catalog entities (`Item`) live in `DatabaseMigration`; simulation runtime `Devices` live in `SimulationDB_Migrations`. Use the `add-migration` skill if a schema change is needed.

4. **UI (only if asked):** device visuals/controls in `GUI` / `GUI_Simulation`.

## Tests (mandatory)

Add a test class in `FakeDevices.Test/` (project `SimulationObjects.Test`), modeled on `IsikTest.cs`:

- Start registry-touching tests with `Device.Devices.Reset()` — the registry is static and leaks between tests otherwise.
- Cover: initial state, each state transition, and lookup via `Device.Devices.find(ip)` with an area attached.

Run: `dotnet test FakeDevices.Test/SimulationObjects.Test.csproj`, then full `dotnet test` to catch state leaks.

## Report

List each layer touched with file paths, the device's state model, and test results.
