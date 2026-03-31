# Streetlights API Example

This example mirrors the AsyncAPI Streetlights tutorial and uses Saunter with ByteBard AsyncAPI v3.

## Running

The example project references the Saunter project directly, so install the UI assets first if you want to run it locally without publishing the npm bundle.

```bash
cd ~/saunter/src/Saunter.UI
npm install

cd ~/saunter/examples/StreetlightsAPI
dotnet run
```

Open:

- `http://localhost:5000/api/streetlights`
- `http://localhost:5000/asyncapi/asyncapi.json`
- `http://localhost:5000/asyncapi/ui/`

## Example Request

```bash
Invoke-WebRequest -Method POST -Uri 'http://localhost:5000/publish/light/measured' -Body '{"id":1, "lumens":400}' -ContentType 'application/json'
```

## Generated Document

The generated document is AsyncAPI v3 and uses root `operations`.

```json
{
  "asyncapi": "3.0.0",
  "info": {
    "title": "Streetlights API",
    "version": "1.0.0",
    "description": "The Smartylighting Streetlights API allows you to remotely manage the city lights."
  },
  "servers": {
    "mosquitto": {
      "host": "test.mosquitto.org",
      "protocol": "mqtt"
    },
    "webapi": {
      "host": "localhost:5000",
      "protocol": "http"
    }
  },
  "channels": {
    "streetlights.measurement": {
      "address": "publish/light/measured"
    }
  },
  "operations": {
    "StreetlightsController.MeasureLight.send": {
      "action": "send",
      "channel": {
        "$ref": "#/channels/streetlights.measurement"
      }
    },
    "StreetlightMessageBus.PublishLightMeasurement.receive": {
      "action": "receive",
      "channel": {
        "$ref": "#/channels/streetlights.measurement"
      }
    }
  }
}
```
